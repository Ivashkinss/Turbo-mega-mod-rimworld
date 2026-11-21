**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

## Порядок ответа (важно)
Отвечай частями, **один файл на ответ**, без вступлений, потом жди «дальше». Части называй `52_1`, `52_2`… Над каждым блоком — путь файла.

## Задача 52 — машины и винтокрылы фракций на Vehicle Framework
Мы встроили в мод **Vehicle Framework (SmashPhil, MIT, версия 1.6.2144)**: в RimWorld 1.7 он грузится без ошибок, 211 патчей, 93 его определения. Сам VF не содержит машин — только каркас. Нужны первые **пять наших машин**. Это не копии чужих модов: свой дизайн в духе Fallout × «Сверхъестественное».

1. `TMM_Vehicle_Impala` — «Детка»: чёрный седан 1967 года братьев Винчестеров. Быстрая машина на 4 места, небольшой багажник (обереги, оружие), без турели. Постройка — поздняя индустриальная эпоха, исследование `TMM_Vehicle_Engines` (новое).
2. `TMM_Vehicle_HunterTruck` — бронированный пикап минитменов и охотников. 3 места, хороший багажник, ручной пулемёт в кузове (турель VF). Исследование `TMM_Vehicle_Engines`.
3. `TMM_Vehicle_GunnerTechnical` — техничка стрелков: ржавый пикап с тяжёлым пулемётом. Только для фракции стрелков (рейды), игрок строить не может.
4. `TMM_Vehicle_CargoHauler` — грузовик-тягач для аванпостов: 2 места, большой груз, медленный. Исследование `TMM_Vehicle_Engines`.
5. `TMM_Vehicle_Vertibird` — винтокрыл Братства Стали: летающая машина VF (аэро, перелёт по карте мира, как шаттл), 6 мест, пулемёт у двери. Исследование `TMM_Vehicle_Vertibird` (новое, после `TMM_Vehicle_Engines`, космическая эпоха). Анклаву — та же модель в чёрной раскраске `TMM_Vehicle_VertibirdEnclave`, только для фракции.

### Требования к коду (XML)
- Строго по схеме **Vehicle Framework 1.6** (`Vehicles.VehicleDef`, `Vehicles.VehicleBuildDef`, `Vehicles.VehicleTurretDef`, компоненты VF). Наследуйся от `BaseVehiclePawn` (образец ниже). Где не уверен в имени поля VF, ставь `<!-- CHECK VF -->`: я сверю с DLL через Cecil.
- Графика: `graphicData` VF с `texPath` вида `Things/Vehicles/TMM/<defName>/<defName>` (файлы `_north`, `_south`, `_east`), маску не используй.
- Русский перевод: `Languages/Russian/DefInjected/...` для label, description, ролей и турелей.
- Исследования `TMM_Vehicle_Engines` и `TMM_Vehicle_Vertibird` — свои `ResearchProjectDef` в основной вкладке исследований (без своего `tab`), как остальные исследования мода. Пререквизиты: ванильные `Electricity` и `Machining` для двигателей, `MicroelectronicsBasics` для винтокрыла.
- Фракционные машины (техничка, винтокрыл Анклава) подключи к `pawnGroupMakers` нельзя без VF-API рейдов — просто пометь `<!-- TODO raids -->`, рейды сделаем отдельно.

## Проверенный образец (уже в моде, работает в игре)
Я собрал прототип `TMM_Vehicle_HunterTruck`. В RimWorld 1.7 он спавнится, водитель садится, машина проезжает 18 клеток, сохраняется и загружается без ошибок. **Делай остальные машины по этому образцу** и дополни его (турель в кузове, заменишь мой временный арт).

Что выяснилось при проверке:
- Шейдеры VF (`CutoutComplexRGB/Pattern/Skin`) лежат в AssetBundle, а 1.7 их не загружает. Ставь **`<shaderType>CutoutComplex</shaderType>`**, `graphicClass` `Vehicles.Graphic_Vehicle`, без маски и паттернов.
- `drawSize` **квадратный** (например, `(5,5)` для машины 2×4): текстуры квадратные, машина нарисована по центру, вид `east` — та же машина, повёрнутая вправо.
- Стата `AccelerationRate` в VF 1.6 **нет**. Есть `Mass`, `MoveSpeed`, `CargoCapacity`, `RepairRate`, `FlightSpeed`, `FlightControl`, `BodyIntegrity`, `WorkToSabotage`.
- `categories` у компонентов — эти же статы VF.
- Русские ключи: `<defName>.properties.roles.N.label`, `<defName>.components.N.label`. Папки `Vehicles.VehicleDef` и `Vehicles.VehicleBuildDef`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Vehicles of Turbo mega mod on the integrated Vehicle Framework (SmashPhil, MIT).
     Loaded only when the framework is present: the defs use its classes. Graphics use the vanilla CutoutComplex
     shader because the framework's own RGB shaders come in AssetBundles that RimWorld 1.7 does not load. -->
<Defs>

  <ResearchProjectDef>
    <defName>TMM_Vehicle_Engines</defName>
    <label>combustion vehicles</label>
    <description>Rebuild pre-war combustion engines, gearboxes and wheels into working ground vehicles: trucks and cars that carry colonists and cargo across the map and the wasteland.</description>
    <baseCost>1500</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>Machining</li>
      <li>Electricity</li>
    </prerequisites>
    <researchViewX>9.0</researchViewX>
    <researchViewY>5.6</researchViewY>
  </ResearchProjectDef>

  <Vehicles.VehicleDef ParentName="BaseVehiclePawn">
    <defName>TMM_Vehicle_HunterTruck</defName>
    <label>hunter pickup</label>
    <description>An armoured pre-war pickup kept running by the Minutemen and travelling monster hunters. Seats a driver and two passengers, carries a good load in the bed and burns chemfuel. Its doors are lined with salt and iron.</description>
    <graphicData>
      <texPath>Things/Vehicles/TMM/TMM_Vehicle_HunterTruck/TMM_Vehicle_HunterTruck</texPath>
      <graphicClass>Vehicles.Graphic_Vehicle</graphicClass>
      <shaderType>CutoutComplex</shaderType>
      <drawSize>(5,5)</drawSize>
      <color>(1,1,1,1)</color>
      <colorTwo>(1,1,1,1)</colorTwo>
    </graphicData>
    <fillPercent>1</fillPercent>
    <buildDef>TMM_Vehicle_HunterTruck_Blueprint</buildDef>
    <size>(2,4)</size>
    <vehicleCategory>Transport</vehicleCategory>
    <techLevel>Industrial</techLevel>
    <vehicleType>Land</vehicleType>
    <statBases>
      <ArmorRating_Blunt>0.3</ArmorRating_Blunt>
      <ArmorRating_Sharp>0.45</ArmorRating_Sharp>
      <ArmorRating_Heat>0.3</ArmorRating_Heat>
      <Flammability>0.4</Flammability>
      <MarketValue>3200</MarketValue>
    </statBases>
    <vehicleStats>
      <Mass>220</Mass>
      <MoveSpeed>9</MoveSpeed>
      <CargoCapacity>500</CargoCapacity>
      <RepairRate>0.5</RepairRate>
    </vehicleStats>
    <race>
      <baseBodySize>3</baseBodySize>
    </race>
    <properties>
      <worldSpeedMultiplier>1.2</worldSpeedMultiplier>
      <visibilityWeight>3</visibilityWeight>
      <roles>
        <li>
          <key>driver</key>
          <label>driver</label>
          <slots>1</slots>
          <slotsToOperate>1</slotsToOperate>
          <handlingTypes>Movement</handlingTypes>
        </li>
        <li>
          <key>passenger</key>
          <label>passenger</label>
          <slots>2</slots>
        </li>
      </roles>
    </properties>
    <components>
      <li>
        <key>Engine</key>
        <label>engine</label>
        <health>220</health>
        <depth>Internal</depth>
        <categories>
          <li>MoveSpeed</li>
        </categories>
      </li>
      <li>
        <key>Wheels</key>
        <label>wheels</label>
        <health>240</health>
        <depth>External</depth>
        <categories>
          <li>MoveSpeed</li>
        </categories>
      </li>
      <li>
        <key>Bed</key>
        <label>cargo bed</label>
        <health>300</health>
        <depth>External</depth>
        <categories>
          <li>CargoCapacity</li>
        </categories>
      </li>
      <li>
        <key>Body</key>
        <label>body</label>
        <health>600</health>
        <depth>External</depth>
      </li>
    </components>
    <comps>
      <li Class="Vehicles.CompProperties_FueledTravel">
        <fuelType>Chemfuel</fuelType>
        <leakDef>Filth_Fuel</leakDef>
        <fuelConsumptionRate>15</fuelConsumptionRate>
        <fuelConsumptionWorldMultiplier>0.5</fuelConsumptionWorldMultiplier>
        <fuelCapacity>80</fuelCapacity>
      </li>
    </comps>
  </Vehicles.VehicleDef>

  <Vehicles.VehicleBuildDef ParentName="VehicleBaseBuildable">
    <defName>TMM_Vehicle_HunterTruck_Blueprint</defName>
    <label>hunter pickup</label>
    <description>Assemble a hunter pickup from a rebuilt engine, steel and spare parts.</description>
    <thingToSpawn>TMM_Vehicle_HunterTruck</thingToSpawn>
    <size>(2,4)</size>
    <graphicData>
      <texPath>Things/Vehicles/TMM/TMM_Vehicle_HunterTruck/TMM_Vehicle_HunterTruck</texPath>
      <graphicClass>Graphic_Multi</graphicClass>
      <drawSize>(5,5)</drawSize>
    </graphicData>
    <statBases>
      <MaxHitPoints>400</MaxHitPoints>
      <WorkToBuild>6000</WorkToBuild>
      <Flammability>0.1</Flammability>
    </statBases>
    <costList>
      <Steel>260</Steel>
      <ComponentIndustrial>6</ComponentIndustrial>
      <WoodLog>40</WoodLog>
    </costList>
    <researchPrerequisites>
      <li>TMM_Vehicle_Engines</li>
    </researchPrerequisites>
    <constructionSkillPrerequisite>6</constructionSkillPrerequisite>
  </Vehicles.VehicleBuildDef>

</Defs>
```
```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>
  <TMM_Vehicle_HunterTruck.label>пикап охотников</TMM_Vehicle_HunterTruck.label>
  <TMM_Vehicle_HunterTruck.description>Бронированный довоенный пикап, который держат на ходу минитмены и бродячие охотники на чудовищ. Вмещает водителя и двух пассажиров, хорошо везёт груз в кузове и ездит на химтопливе. Двери изнутри обложены солью и железом.</TMM_Vehicle_HunterTruck.description>
  <TMM_Vehicle_HunterTruck.properties.roles.0.label>водитель</TMM_Vehicle_HunterTruck.properties.roles.0.label>
  <TMM_Vehicle_HunterTruck.properties.roles.1.label>пассажир</TMM_Vehicle_HunterTruck.properties.roles.1.label>
  <TMM_Vehicle_HunterTruck.components.0.label>двигатель</TMM_Vehicle_HunterTruck.components.0.label>
  <TMM_Vehicle_HunterTruck.components.1.label>колёса</TMM_Vehicle_HunterTruck.components.1.label>
  <TMM_Vehicle_HunterTruck.components.2.label>кузов</TMM_Vehicle_HunterTruck.components.2.label>
  <TMM_Vehicle_HunterTruck.components.3.label>корпус</TMM_Vehicle_HunterTruck.components.3.label>
</LanguageData>
```

### Порядок частей
1. `Defs/VehicleDefs/Vehicles_TMM.xml` — все пять машин (+ вариант Анклава).
2. `Defs/VehicleDefs/VehicleTurrets_TMM.xml` — турели.
3. `Defs/ResearchProjectDefs/Research_Vehicles.xml` и рецепты/постройка (`VehicleBuildDef`).
4. `Languages/Russian/DefInjected/...` — все русские строки.
5–10. **Арт (SVG)**: для каждой машины три вида `Art/Vehicles/<defName>_north.svg`, `_south`, `_east` (вид сверху под углом, как машины RimWorld; `viewBox="0 0 512 512"`, прозрачный фон, контур `#121519` толщиной 10–14, без текста и фильтров). Одна машина = один ответ (3 SVG).

## Образец VF: BaseVehiclePawn
``xml
<?xml version="1.0" encoding="utf-8"?>

<Defs>
	<Vehicles.VehicleDef Name="BaseVehiclePawn" Abstract="True">
		<thingClass>Vehicles.VehiclePawn</thingClass>
		<category>Pawn</category>
		<selectable>true</selectable>
		<tickerType>Normal</tickerType>
		<altitudeLayer>LayingPawn</altitudeLayer>
		<hasTooltip>true</hasTooltip>
		
		<passability>PassThroughOnly</passability>
		<pathCost>250</pathCost>
		
		<useHitPoints>false</useHitPoints>
		<repairEffect>Repair</repairEffect>
		
		<!-- Temporary until icons are fixed and vehicle prices are balanced -->
		<tradeability FeatureFlag="TradeableVehicles">All</tradeability>

		<statBases>
			<PsychicSensitivity>0</PsychicSensitivity>
			<ToxicResistance>1</ToxicResistance>
			<Flammability>0</Flammability>
			<ComfyTemperatureMin>-200</ComfyTemperatureMin>
			<ComfyTemperatureMax>200</ComfyTemperatureMax>
			<!-- Redundant value, vehicles don't have meat anyways. Serves as a final stop-gap so no vehicle meat is ever spawned -->
			<MeatAmount>0</MeatAmount> 
		</statBases>
		
		<statEvents>
			<li>
				<statDef>MoveSpeed</statDef>
				
				<eventDefs>
					<li>HealthChanged</li>
					<li>Spawned</li>
          <li>CargoAdded</li>
				</eventDefs>
			</li>
			<li>
				<statDef>BodyIntegrity</statDef>
				
				<eventDefs>
					<li>HealthChanged</li>
				</eventDefs>
			</li>
			<li>
				<statDef>Mass</statDef>
				
				<eventDefs>
					<li>CargoAdded</li>
					<li>CargoRemoved</li>
				</eventDefs>
			</li>
			<li>
				<statDef>FlightSpeed</statDef>
				
				<eventDefs>
					<li>HealthChanged</li>
					<li>Spawned</li>
				</eventDefs>
			</li>
		</statEvents>
		
		<soundOneShotsOnEvent>
			<li>
				<key>IgnitionOn</key>
				<value>DraftOn</value>
			</li>
			<li>
				<key>IgnitionOff</key>
				<value>DraftOff</value>
			</li>
		</soundOneShotsOnEvent>
		
		<inspectorTabs>
			<li>Vehicles.ITab_Vehicle_Health</li>
			<li>Vehicles.ITab_Vehicle_Passengers</li>
			<li>Vehicles.ITab_Vehicle_Cargo</li>
			<li>ITab_Pawn_FormingCaravan</li>
		</inspectorTabs>
		
		<properties>
			<diagonalRotation>true</diagonalRotation>
			<manhunterTargetsVehicle>false</manhunterTargetsVehicle>
			
			<customThingCosts>
				<li>
					<key>Column</key>
					<value>10000</value>
				</li>
			</customThingCosts>
		</properties>
		
		<race>
			<body>emptyBody</body>
			<trainability>None</trainability>
			<thinkTreeMain>Vehicle</thinkTreeMain>
			<thinkTreeConstant>Vehicle_Constant</thinkTreeConstant>
			<intelligence>ToolUser</intelligence>
			<fleshType>MetalVehicle</fleshType>
			<needsRest>false</needsRest>
			<hasGenders>false</hasGenders>
			<foodType>None</foodType>
			<alwaysAwake>true</alwaysAwake>
			<doesntMove>true</doesntMove> <!-- prevents vanilla from considering it capable of being downed -->
			<renderTree>PointlessVehicleRenderTree</renderTree>
			
			<baseBodySize>1.0</baseBodySize>
			<baseHealthScale>1.0</baseHealthScale>
			
			<!-- These sounds will never trigger, vehicles can't melee -->
			<soundMeleeHitPawn>Pawn_Melee_MechanoidSlash_HitPawn</soundMeleeHitPawn>
			<soundMeleeHitBuilding>Pawn_Melee_MechanoidSlash_HitPawn</soundMeleeHitBuilding>
			<soundMeleeMiss>Pawn_Melee_MechanoidSlash_HitPawn</soundMeleeMiss>
			
			<lifeStageAges>
				<li>
					<def>MechanoidFullyFormed</def>
					<minAge>0</minAge>
					<soundWounded>BulletImpact_Metal</soundWounded>
				</li>
			</lifeStageAges>
		</race>
		
		<comps>
			<li>
				<compClass>CompAttachBase</compClass>
			</li>
		</comps>
		
		<drawGUIOverlay>true</drawGUIOverlay>
	</Vehicles.VehicleDef>
</Defs>
``
## Образец VF: постройка
``xml
<?xml version="1.0" encoding="utf-8" ?>

<Defs>
	<!-- Vehicle base -->
	<Vehicles.VehicleBuildDef Name="VehicleBaseBuildable" Abstract="True">
		<thingClass>Vehicles.VehicleBuilding</thingClass>
		<selectable>true</selectable>
		<altitudeLayer>Building</altitudeLayer>
		<terrainAffordanceNeeded>Heavy</terrainAffordanceNeeded>
		<constructEffect>ConstructMetal</constructEffect>
		<leaveResourcesWhenKilled>true</leaveResourcesWhenKilled>
		<passability>Impassable</passability>
		<fillPercent>1</fillPercent>
		<neverMultiSelect>true</neverMultiSelect>
		<designationCategory>VF_Vehicles</designationCategory>
		<clearBuildingArea>true</clearBuildingArea>
		<category>Building</category>
		<rotatable>true</rotatable>
		<blockWind>true</blockWind>
		<useHitPoints>true</useHitPoints>
		
		<building>
			<canPlaceOverImpassablePlant>false</canPlaceOverImpassablePlant>
			<paintable>false</paintable>
		</building>
	</Vehicles.VehicleBuildDef>
	
	<!-- Vehicle base -->
	<Vehicles.VehicleBuildDef Name="VehicleBoatBaseBuildable" ParentName="VehicleBaseBuildable" Abstract="True">
		<terrainAffordanceNeeded>ShallowWater</terrainAffordanceNeeded>
	</Vehicles.VehicleBuildDef>
</Defs>
``
## Образец VF: турель
``xml
<?xml version="1.0" encoding="utf-8"?>

<Defs>
	<Vehicles.VehicleTurretDef Name="VehicleTurretBase" Abstract="True">
		<gizmoDescription>Select Target.\n\nFire for effect</gizmoDescription>
	</Vehicles.VehicleTurretDef>
</Defs>
``
