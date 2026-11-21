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
