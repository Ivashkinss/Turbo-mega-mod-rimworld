`Defs/ApparelLayerDefs/Layers_Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- Drawn over Shell (200) and under Belt (300) -->
  <ApparelLayerDef>
    <defName>TMM_Backpack</defName>
    <label>backpack</label>
    <drawOrder>210</drawOrder>
  </ApparelLayerDef>

  <!-- Full-body powered exosuit worn over regular clothing, Shell, and Backpack -->
  <ApparelLayerDef>
    <defName>TMM_PowerArmor</defName>
    <label>power armor</label>
    <drawOrder>250</drawOrder>
  </ApparelLayerDef>

</Defs>
```

`Defs/ThingDefs/Equipment_Backpacks.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- 1. CANVAS BACKPACK (TIER 1)                                           -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Backpack_Canvas</defName>
    <label>canvas backpack</label>
    <description>A stitched canvas pack with leather straps worn on the back. Modestly increases carrying capacity at a slight cost to movement speed.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costStuffCount>45</costStuffCount>
    <stuffCategories>
      <li>Fabric</li>
      <li>Leathery</li>
    </stuffCategories>
    <costList>
      <Steel>15</Steel>
    </costList>
    <statBases>
      <MaxHitPoints>140</MaxHitPoints>
      <WorkToMake>3600</WorkToMake>
      <Mass>1.6</Mass>
      <EquipDelay>1.5</EquipDelay>
      <ArmorRating_Sharp>0.05</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.05</ArmorRating_Blunt>
    </statBases>
    <equippedStatOffsets>
      <CarryingCapacity>35</CarryingCapacity>
      <MoveSpeed>-0.05</MoveSpeed>
    </equippedStatOffsets>
    <recipeMaker>
      <skillRequirements>
        <Crafting>3</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
        <li>HandTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories>
      <li>ApparelUtility</li>
    </thingCategories>
    <apparel>
      <bodyPartGroups>
        <li>Torso</li>
        <li>Shoulders</li>
      </bodyPartGroups>
      <layers>
        <li>TMM_Backpack</li>
      </layers>
      <defaultOutfitTags>
        <li>Worker</li>
        <li>Soldier</li>
      </defaultOutfitTags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.BackpackExtension">
        <bonusCaravanKg>25</bonusCaravanKg>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 2. MILITARY BACKPACK (TIER 2)                                         -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Backpack_Military</defName>
    <label>military field backpack</label>
    <description>A reinforced tactical rucksack with an internal aluminum frame and modular webbing. Greatly expands carrying capacity while slightly slowing movement.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costStuffCount>70</costStuffCount>
    <stuffCategories>
      <li>Fabric</li>
      <li>Leathery</li>
    </stuffCategories>
    <costList>
      <Steel>35</Steel>
      <ComponentIndustrial>1</ComponentIndustrial>
    </costList>
    <statBases>
      <MaxHitPoints>220</MaxHitPoints>
      <WorkToMake>7200</WorkToMake>
      <Mass>2.8</Mass>
      <EquipDelay>2.0</EquipDelay>
      <ArmorRating_Sharp>0.12</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.08</ArmorRating_Blunt>
    </statBases>
    <equippedStatOffsets>
      <CarryingCapacity>70</CarryingCapacity>
      <MoveSpeed>-0.10</MoveSpeed>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>ComplexClothing</researchPrerequisite>
      <skillRequirements>
        <Crafting>5</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
        <li>HandTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories>
      <li>ApparelUtility</li>
    </thingCategories>
    <apparel>
      <bodyPartGroups>
        <li>Torso</li>
        <li>Shoulders</li>
      </bodyPartGroups>
      <layers>
        <li>TMM_Backpack</li>
      </layers>
      <defaultOutfitTags>
        <li>Worker</li>
        <li>Soldier</li>
      </defaultOutfitTags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.BackpackExtension">
        <bonusCaravanKg>50</bonusCaravanKg>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 3. POWERED EXO-BACKPACK (TIER 3 — RESEARCH LOCKED)                    -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Backpack_Power</defName>
    <label>powered exo-backpack</label>
    <description>A pre-war servo-assisted load-bearing frame built from plasteel and micro-hydraulics. Grants massive carrying capacity with minimal mobility penalty.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Plasteel>45</Plasteel>
      <Steel>50</Steel>
      <ComponentIndustrial>4</ComponentIndustrial>
      <ComponentSpacer>1</ComponentSpacer>
    </costList>
    <statBases>
      <MaxHitPoints>340</MaxHitPoints>
      <WorkToMake>16000</WorkToMake>
      <Mass>4.5</Mass>
      <EquipDelay>3.0</EquipDelay>
      <ArmorRating_Sharp>0.22</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.18</ArmorRating_Blunt>
    </statBases>
    <equippedStatOffsets>
      <CarryingCapacity>120</CarryingCapacity>
      <MoveSpeed>-0.05</MoveSpeed>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_PowerBackpackResearch</researchPrerequisite>
      <skillRequirements>
        <Crafting>8</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories>
      <li>ApparelUtility</li>
    </thingCategories>
    <apparel>
      <bodyPartGroups>
        <li>Torso</li>
        <li>Shoulders</li>
      </bodyPartGroups>
      <layers>
        <li>TMM_Backpack</li>
      </layers>
      <defaultOutfitTags>
        <li>Worker</li>
        <li>Soldier</li>
      </defaultOutfitTags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.BackpackExtension">
        <bonusCaravanKg>85</bonusCaravanKg>
      </li>
    </modExtensions>
  </ThingDef>

</Defs>
```

`Defs/ThingDefs/Equipment_WeaponMods.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- BASE FOR EQUIPMENT MODULES                                            -->
  <!-- ===================================================================== -->

  <ThingDef Name="TMM_EquipmentModBase" ParentName="ResourceBase" Abstract="True">
    <stackLimit>1</stackLimit>
    <useHitPoints>true</useHitPoints>
    <statBases>
      <MaxHitPoints>100</MaxHitPoints>
      <Flammability>0.2</Flammability>
      <DeteriorationRate>0.5</DeteriorationRate>
      <Mass>0.8</Mass>
    </statBases>
    <thingCategories>
      <li>Manufactured</li>
    </thingCategories>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 1. HOLY WATER TANK (WEAPON MOD, 30 CHARGES, TMM_HolyWater)            -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="TMM_EquipmentModBase">
    <defName>TMM_Mod_HolyWaterTank</defName>
    <label>holy water tank</label>
    <description>A pressurized weapon attachment filled with consecrated water. Coats projectiles or blades for 30 attacks, adding extra holy water burn damage that devastates demons.</description>
    <graphicData>
      <texPath>Things/Item/Resource/Chemfuel</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>260</MarketValue>
      <WorkToMake>2800</WorkToMake>
    </statBases>
    <costList>
      <Steel>25</Steel>
      <Silver>35</Silver>
      <ComponentIndustrial>1</ComponentIndustrial>
    </costList>
    <recipeMaker>
      <skillRequirements>
        <Crafting>4</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
        <li>DrugLab</li>
      </recipeUsers>
    </recipeMaker>
    <comps>
      <li Class="TurboMegaMod.CompProperties_EquipmentMod">
        <modKind>Weapon</modKind>
        <extraDamageDef>TMM_HolyWater</extraDamageDef>
        <extraDamageAmount>6</extraDamageAmount>
        <extraArmorPenetration>0.50</extraArmorPenetration>
        <maxCharges>30</maxCharges>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 2. BORAX TANK (WEAPON MOD, 30 CHARGES, TMM_BoraxBurn)                 -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="TMM_EquipmentModBase">
    <defName>TMM_Mod_BoraxTank</defName>
    <label>borax canister mod</label>
    <description>A compact under-barrel sodium borate reservoir. Coats shots or strikes for 30 attacks with caustic borax, triggering Leviathan weakness and stripping Dick Roman's invulnerability.</description>
    <graphicData>
      <texPath>Things/Item/Resource/Chemfuel</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>320</MarketValue>
      <WorkToMake>3600</WorkToMake>
    </statBases>
    <costList>
      <Steel>25</Steel>
      <Chemfuel>20</Chemfuel>
      <ComponentIndustrial>1</ComponentIndustrial>
    </costList>
    <recipeMaker>
      <researchPrerequisite>TMM_BoraxResearch</researchPrerequisite>
      <skillRequirements>
        <Crafting>5</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
        <li>DrugLab</li>
      </recipeUsers>
    </recipeMaker>
    <comps>
      <li Class="TurboMegaMod.CompProperties_EquipmentMod">
        <modKind>Weapon</modKind>
        <extraDamageDef>TMM_BoraxBurn</extraDamageDef>
        <extraDamageAmount>6</extraDamageAmount>
        <extraArmorPenetration>0.85</extraArmorPenetration>
        <maxCharges>30</maxCharges>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 3. ROCK SALT CHARGE (WEAPON MOD, 30 CHARGES, TMM_RockSalt)            -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="TMM_EquipmentModBase">
    <defName>TMM_Mod_RockSaltPack</defName>
    <label>rock salt charge pack</label>
    <description>A hunter's auxiliary magazine loaded with coarse rock salt crystals. Adds rock salt damage to 30 attacks, severely punishing demons and spirits.</description>
    <graphicData>
      <texPath>Things/Item/Resource/Medicine/MedicineHerbal</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>190</MarketValue>
      <WorkToMake>2200</WorkToMake>
    </statBases>
    <costList>
      <Steel>20</Steel>
      <Silver>15</Silver>
    </costList>
    <recipeMaker>
      <skillRequirements>
        <Crafting>3</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
        <li>ElectricSmithy</li>
        <li>FueledSmithy</li>
      </recipeUsers>
    </recipeMaker>
    <comps>
      <li Class="TurboMegaMod.CompProperties_EquipmentMod">
        <modKind>Weapon</modKind>
        <extraDamageDef>TMM_RockSalt</extraDamageDef>
        <extraDamageAmount>5</extraDamageAmount>
        <extraArmorPenetration>0.35</extraArmorPenetration>
        <maxCharges>30</maxCharges>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 4. SILVERED ENOCHIAN ENGRAVING (PERMANENT WEAPON MOD, TMM_Enochian)   -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="TMM_EquipmentModBase">
    <defName>TMM_Mod_SilverEngraving</defName>
    <label>silvered Enochian engraving kit</label>
    <description>Precision-inlaid silver runes and celestial bronze sigils designed to be etched onto any firearm or blade. Permanently adds Enochian strike damage with no charge limit, exploiting angelic vulnerability.</description>
    <graphicData>
      <texPath>Things/Item/Resource/ComponentIndustrial</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>680</MarketValue>
      <WorkToMake>6500</WorkToMake>
      <Mass>0.4</Mass>
    </statBases>
    <costList>
      <Silver>90</Silver>
      <Gold>8</Gold>
      <ComponentIndustrial>2</ComponentIndustrial>
    </costList>
    <recipeMaker>
      <skillRequirements>
        <Crafting>7</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
        <li>ElectricSmithy</li>
      </recipeUsers>
    </recipeMaker>
    <comps>
      <li Class="TurboMegaMod.CompProperties_EquipmentMod">
        <modKind>Weapon</modKind>
        <extraDamageDef>TMM_Enochian</extraDamageDef>
        <extraDamageAmount>5</extraDamageAmount>
        <extraArmorPenetration>0.65</extraArmorPenetration>
        <maxCharges>0</maxCharges>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 5. RAILROAD BALLISTIC WEAVE (APPAREL MOD, +0.25 SHARP ARMOR)          -->
  <!-- Unlocked by story flag: unlock.railroad_ballistic                     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="TMM_EquipmentModBase">
    <defName>TMM_Mod_RailroadBallisticWeave</defName>
    <label>Railroad ballistic weave</label>
    <description>Tinker Tom's ultra-dense synthetic weave lining, stitched into the inner shell of a coat, jacket, or duster. Adds +25% sharp armor protection without adding weight penalty. Crafting unlocks via the Railroad questline.</description>
    <graphicData>
      <texPath>Things/Item/Resource/Synthread</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>520</MarketValue>
      <WorkToMake>5400</WorkToMake>
      <Mass>0.5</Mass>
    </statBases>
    <comps>
      <li Class="TurboMegaMod.CompProperties_EquipmentMod">
        <modKind>Apparel</modKind>
        <armorSharpOffset>0.25</armorSharpOffset>
        <maxCharges>0</maxCharges>
        <requiredStoryFlag>unlock.railroad_ballistic</requiredStoryFlag>
      </li>
    </comps>
  </ThingDef>

  <RecipeDef>
    <defName>TMM_Make_RailroadBallisticWeave</defName>
    <label>craft Railroad ballistic weave</label>
    <description>Weave ultra-dense ballistic lining capable of reinforcing coats, jackets, and dusters with +25% sharp armor.</description>
    <jobString>Weaving ballistic fiber.</jobString>
    <workAmount>5400</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements>
      <Crafting>6</Crafting>
    </skillRequirements>
    <recipeUsers>
      <li>ElectricTailoringBench</li>
      <li>HandTailoringBench</li>
      <li>TableMachining</li>
    </recipeUsers>
    <ingredients>
      <li>
        <filter>
          <thingDefs>
            <li>Cloth</li>
            <li>DevilstrandCloth</li>
            <li>Synthread</li>
            <li>Hyperweave</li>
          </thingDefs>
        </filter>
        <count>45</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>Plasteel</li>
          </thingDefs>
        </filter>
        <count>15</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>ComponentIndustrial</li>
          </thingDefs>
        </filter>
        <count>1</count>
      </li>
    </ingredients>
    <products>
      <TMM_Mod_RailroadBallisticWeave>1</TMM_Mod_RailroadBallisticWeave>
    </products>
  </RecipeDef>

  <!-- ===================================================================== -->
  <!-- 6. ENCHANTED COLT SILVER BULLET (1 BULLET: 25 GOLD, 40 SILVER, 1 ADV) -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ResourceBase">
    <defName>TMM_Ammo_ColtSilverBullet</defName>
    <label>Colt silver bullet</label>
    <description>A hand-cast, rune-inscribed silver cartridge engineered specifically for Samuel Colt's 1851 revolver. Extremely costly to produce; used to reload The Colt up to 13 rounds.</description>
    <graphicData>
      <texPath>Things/Item/Resource/Silver</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <stackLimit>25</stackLimit>
    <statBases>
      <MarketValue>650</MarketValue>
      <Mass>0.08</Mass>
      <Flammability>0</Flammability>
    </statBases>
    <thingCategories>
      <li>Manufactured</li>
    </thingCategories>
  </ThingDef>

  <RecipeDef>
    <defName>TMM_Make_ColtSilverBullet</defName>
    <label>forge Colt silver bullet</label>
    <description>Cast and engrave a single enchanted silver bullet for The Colt.</description>
    <jobString>Forging Colt silver bullet.</jobString>
    <workAmount>8000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <researchPrerequisite>TMM_ColtBullets</researchPrerequisite>
    <skillRequirements>
      <Crafting>9</Crafting>
    </skillRequirements>
    <recipeUsers>
      <li>TableMachining</li>
      <li>ElectricSmithy</li>
    </recipeUsers>
    <ingredients>
      <li>
        <filter>
          <thingDefs>
            <li>Gold</li>
          </thingDefs>
        </filter>
        <count>25</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>Silver</li>
          </thingDefs>
        </filter>
        <count>40</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>ComponentSpacer</li>
          </thingDefs>
        </filter>
        <count>1</count>
      </li>
    </ingredients>
    <products>
      <TMM_Ammo_ColtSilverBullet>1</TMM_Ammo_ColtSilverBullet>
    </products>
  </RecipeDef>

  <!-- ===================================================================== -->
  <!-- 7. REFILL RECIPES FOR DEPLETED WEAPON TANKS / PACKS                   -->
  <!-- ===================================================================== -->

  <RecipeDef>
    <defName>TMM_Refill_HolyWaterTank</defName>
    <label>refill holy water tank</label>
    <description>Replenish a depleted holy water tank back to 30 full charges.</description>
    <jobString>Refilling holy water tank.</jobString>
    <workAmount>1200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <recipeUsers>
      <li>DrugLab</li>
      <li>TableMachining</li>
    </recipeUsers>
    <ingredients>
      <li>
        <filter>
          <thingDefs>
            <li>TMM_Mod_HolyWaterTank</li>
          </thingDefs>
        </filter>
        <count>1</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>Silver</li>
          </thingDefs>
        </filter>
        <count>15</count>
      </li>
    </ingredients>
    <products>
      <TMM_Mod_HolyWaterTank>1</TMM_Mod_HolyWaterTank>
    </products>
  </RecipeDef>

  <RecipeDef>
    <defName>TMM_Refill_BoraxTank</defName>
    <label>refill borax canister mod</label>
    <description>Repressurize a depleted borax canister back to 30 full charges.</description>
    <jobString>Refilling borax canister.</jobString>
    <workAmount>1400</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <researchPrerequisite>TMM_BoraxResearch</researchPrerequisite>
    <recipeUsers>
      <li>DrugLab</li>
      <li>TableMachining</li>
    </recipeUsers>
    <ingredients>
      <li>
        <filter>
          <thingDefs>
            <li>TMM_Mod_BoraxTank</li>
          </thingDefs>
        </filter>
        <count>1</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>Chemfuel</li>
          </thingDefs>
        </filter>
        <count>10</count>
      </li>
    </ingredients>
    <products>
      <TMM_Mod_BoraxTank>1</TMM_Mod_BoraxTank>
    </products>
  </RecipeDef>

  <RecipeDef>
    <defName>TMM_Refill_RockSaltPack</defName>
    <label>refill rock salt charge pack</label>
    <description>Repack a depleted rock salt magazine back to 30 full charges.</description>
    <jobString>Refilling rock salt pack.</jobString>
    <workAmount>1000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <recipeUsers>
      <li>TableMachining</li>
      <li>ElectricSmithy</li>
      <li>FueledSmithy</li>
    </recipeUsers>
    <ingredients>
      <li>
        <filter>
          <thingDefs>
            <li>TMM_Mod_RockSaltPack</li>
          </thingDefs>
        </filter>
        <count>1</count>
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>Silver</li>
          </thingDefs>
        </filter>
        <count>5</count>
      </li>
    </ingredients>
    <products>
      <TMM_Mod_RockSaltPack>1</TMM_Mod_RockSaltPack>
    </products>
  </RecipeDef>

</Defs>
```

`Defs/ResearchProjectDefs/Research_Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <ResearchProjectDef>
    <defName>TMM_PowerBackpackResearch</defName>
    <label>powered exo-backpacks</label>
    <description>Engineer servo-assisted plasteel backpacks capable of carrying massive loads with minimal impact on colonist movement speed.</description>
    <baseCost>1000</baseCost>
    <techLevel>Spacer</techLevel>
    <prerequisites>
      <li>Machining</li>
    </prerequisites>
    <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
    <researchViewX>9.0</researchViewX>
    <researchViewY>5.6</researchViewY>
  </ResearchProjectDef>

  <!-- Story-gated by flag unlock.colt_bullets via Harmony patch on CanStartNow -->
  <ResearchProjectDef>
    <defName>TMM_ColtBullets</defName>
    <label>Colt silver bullets</label>
    <description>Master Samuel Colt's arcane metallurgical ritual to forge replacement enchanted silver bullets for The Colt. Requires unlocking the hunter archives in the main story.</description>
    <baseCost>1500</baseCost>
    <techLevel>Spacer</techLevel>
    <prerequisites>
      <li>Machining</li>
      <li>MicroelectronicsBasics</li>
    </prerequisites>
    <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
    <researchViewX>10.0</researchViewX>
    <researchViewY>5.6</researchViewY>
  </ResearchProjectDef>

</Defs>
```

`Defs/JobDefs/Jobs_Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <JobDef>
    <defName>TMM_InstallEquipmentMod</defName>
    <driverClass>TurboMegaMod.JobDriver_InstallEquipmentMod</driverClass>
    <reportString>installing module on TargetA.</reportString>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>
  </JobDef>

  <JobDef>
    <defName>TMM_RemoveEquipmentMod</defName>
    <driverClass>TurboMegaMod.JobDriver_RemoveEquipmentMod</driverClass>
    <reportString>removing module from TargetA.</reportString>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>
  </JobDef>

  <JobDef>
    <defName>TMM_ReloadColt</defName>
    <driverClass>TurboMegaMod.JobDriver_ReloadColt</driverClass>
    <reportString>reloading The Colt with silver bullets.</reportString>
    <allowOpportunisticPrefix>true</allowOpportunisticPrefix>
  </JobDef>

</Defs>
```

`Patches/Equipment_WeaponModSlots.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>

  <!--
    Universal CompWeaponMods (all ranged & melee weapons) and CompApparelMods (all Shell coats/jackets/dusters)
    plus TMM_PowerArmor layer migration are injected cleanly in C# at [StaticConstructorOnStartup]
    (EquipmentDefsInjector) so every vanilla and modded ThingDef is covered regardless of ParentName.
    Below are declarative XML patches for Human ITab, Protector Power Armor layer, and The Colt ammo comp.
  -->

  <!-- 1. Add additional Warcraft-style Equipment ITab to Human pawns (keeping vanilla ITab_Pawn_Gear intact) -->
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/ThingDef[defName="Human"]/inspectorTabs</xpath>
    <value>
      <li>TurboMegaMod.ITab_TMM_Equipment</li>
    </value>
  </Operation>

  <!-- 2. Move Protector Power Armor (if loaded) onto the dedicated TMM_PowerArmor layer -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/ThingDef[defName="TMM_Legendary_ProtectorArmor"]/apparel/layers</xpath>
    <match Class="PatchOperationReplace">
      <xpath>Defs/ThingDef[defName="TMM_Legendary_ProtectorArmor"]/apparel/layers</xpath>
      <value>
        <layers>
          <li>TMM_PowerArmor</li>
        </layers>
      </value>
    </match>
  </Operation>

  <!-- 3. Attach CompProperties_ColtAmmo (13 rounds, reloadable with silver bullets) onto The Colt if loaded -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/ThingDef[defName="TMM_Legendary_TheColt"]/comps</xpath>
    <match Class="PatchOperationAdd">
      <xpath>Defs/ThingDef[defName="TMM_Legendary_TheColt"]/comps</xpath>
      <value>
        <li Class="TurboMegaMod.CompProperties_ColtAmmo">
          <maxCharges>13</maxCharges>
          <ammoDefName>TMM_Ammo_ColtSilverBullet</ammoDefName>
        </li>
      </value>
    </match>
  </Operation>

</Patch>
```

`Source/EquipmentSlots.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF EXTENSIONS & ENUMS
    // =========================================================================

    public enum EquipmentModKind
    {
        Weapon,
        Apparel
    }

    public sealed class BackpackExtension : DefModExtension
    {
        public float bonusCaravanKg = 25f;
    }

    // =========================================================================
    // UTILITIES: STORY FLAGS, WORKBENCHES, SIMPLE SIDEARMS REFLECTION
    // =========================================================================

    public static class EquipmentUtil
    {
        private static bool s_sidearmsChecked;
        private static Type s_sidearmsCompType;
        private static MethodInfo s_sidearmsGetMemoryMethod;
        private static PropertyInfo s_sidearmsRememberedProp;
        private static FieldInfo s_sidearmsRememberedField;

        private static bool s_storyChecked;
        private static Type s_sparkStoryType;
        private static PropertyInfo s_storyInstanceProp;
        private static MethodInfo s_storyGetFlagMethod;
        private static FieldInfo s_storyFlagsField;

        public static bool HasStoryFlag(string flagName)
        {
            if (flagName.NullOrEmpty()) return true;

            if (!s_storyChecked)
            {
                s_storyChecked = true;
                s_sparkStoryType = AccessTools.TypeByName("TurboMegaMod.SparkStory");
                if (s_sparkStoryType != null)
                {
                    s_storyInstanceProp = AccessTools.Property(s_sparkStoryType, "Instance");
                    s_storyGetFlagMethod = AccessTools.Method(s_sparkStoryType, "GetFlag", new Type[] { typeof(string) }); // CHECK API
                    s_storyFlagsField = AccessTools.Field(s_sparkStoryType, "flags"); // CHECK API
                }
            }

            if (s_sparkStoryType == null || s_storyInstanceProp == null) return true;

            try
            {
                object inst = s_storyInstanceProp.GetValue(null, null);
                if (inst == null) return false;

                if (s_storyGetFlagMethod != null)
                {
                    object val = s_storyGetFlagMethod.Invoke(inst, new object[] { flagName });
                    string str = val != null ? val.ToString() : null;
                    return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) || str == "1";
                }

                if (s_storyFlagsField != null)
                {
                    IDictionary dict = s_storyFlagsField.GetValue(inst) as IDictionary;
                    if (dict != null && dict.Contains(flagName))
                    {
                        object val = dict[flagName];
                        string str = val != null ? val.ToString() : null;
                        return string.Equals(str, "true", StringComparison.OrdinalIgnoreCase) || str == "1";
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static bool IsWeaponModEligible(ThingDef def)
        {
            if (def == null) return false;
            if (def.equipmentType != EquipmentType.Primary) return false;
            return def.IsRangedWeapon || def.IsMeleeWeapon;
        }

        public static int DefaultWeaponSlotCount(ThingDef def)
        {
            if (def == null) return 0;
            if (def.defName == "TMM_Legendary_FirstBlade" ||
                def.defName == "TMM_Legendary_ArchangelBlade" ||
                def.defName == "TMM_Legendary_TheColt")
            {
                return 0;
            }
            if (def.defName != null && def.defName.StartsWith("TMM_Legendary_", StringComparison.Ordinal))
            {
                return 1;
            }
            return 2;
        }

        public static bool IsApparelModEligible(ThingDef def)
        {
            if (def == null || !def.IsApparel || def.apparel == null || def.apparel.layers == null) return false;
            if (IsPowerArmorDef(def)) return false;

            ApparelLayerDef shell = ApparelLayerDefOf.Shell;
            if (shell == null || !def.apparel.layers.Contains(shell)) return false;

            if (def.apparel.bodyPartGroups != null && def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
            {
                return true;
            }
            return false;
        }

        public static bool IsPowerArmorDef(ThingDef def)
        {
            if (def == null || !def.IsApparel || def.apparel == null) return false;
            if (def.apparel.layers != null)
            {
                for (int i = 0; i < def.apparel.layers.Count; i++)
                {
                    if (def.apparel.layers[i] != null && def.apparel.layers[i].defName == "TMM_PowerArmor") return true;
                }
            }

            string n = def.defName;
            if (n.NullOrEmpty()) return false;
            if (n == "TMM_Legendary_ProtectorArmor") return true;
            if (n.StartsWith("TMM_", StringComparison.Ordinal) &&
                (n.IndexOf("PowerArmor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("T60", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("T45", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 n.IndexOf("X01", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
            return false;
        }

        public static Building FindNearestWorkbench(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Map.listerBuildings == null) return null;

            List<Building> buildings = pawn.Map.listerBuildings.allBuildingsColonist;
            Building bestPreferred = null;
            float bestPreferredDist = float.MaxValue;
            Building bestAny = null;
            float bestAnyDist = float.MaxValue;

            for (int i = 0; i < buildings.Count; i++)
            {
                Building b = buildings[i];
                if (b == null || b.Destroyed || !(b is Building_WorkTable)) continue;
                if (!pawn.CanReach(b, PathEndMode.InteractionCell, Danger.Deadly)) continue;

                float d = pawn.Position.DistanceToSquared(b.Position);
                string dn = b.def != null ? b.def.defName : string.Empty;
                bool preferred = dn == "TableMachining" || dn == "ElectricSmithy" || dn == "FueledSmithy" ||
                                 dn == "ElectricTailoringBench" || dn == "HandTailoringBench";
                if (preferred && d < bestPreferredDist)
                {
                    bestPreferredDist = d;
                    bestPreferred = b;
                }
                else if (d < bestAnyDist)
                {
                    bestAnyDist = d;
                    bestAny = b;
                }
            }

            return bestPreferred ?? bestAny;
        }

        /// <summary>
        /// Reads backup/sidearm weapons via Simple Sidearms reflection if embedded in the mod,
        /// falling back cleanly to weapon Things inside pawn.inventory.innerContainer without errors.
        /// </summary>
        public static List<ThingWithComps> GetSidearms(Pawn pawn)
        {
            List<ThingWithComps> result = new List<ThingWithComps>();
            if (pawn == null) return result;

            HashSet<ThingWithComps> seen = new HashSet<ThingWithComps>();
            ThingWithComps primary = pawn.equipment != null ? pawn.equipment.Primary : null;

            // 1. Always collect actual weapon items carried in pawn.inventory
            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                ThingOwner<Thing> inv = pawn.inventory.innerContainer;
                for (int i = 0; i < inv.Count; i++)
                {
                    ThingWithComps twc = inv[i] as ThingWithComps;
                    if (twc != null && twc != primary && twc.def != null && (twc.def.IsRangedWeapon || twc.def.IsMeleeWeapon))
                    {
                        if (seen.Add(twc)) result.Add(twc);
                    }
                }
            }

            // 2. Reflection hook into embedded Simple Sidearms (if present) to verify remembered sidearms
            if (!s_sidearmsChecked)
            {
                s_sidearmsChecked = true;
                s_sidearmsCompType = AccessTools.TypeByName("SimpleSidearms.rimworld.CompSidearmMemory") ??
                                     AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Memory.CompSidearmMemory"); // CHECK API
                if (s_sidearmsCompType != null)
                {
                    s_sidearmsGetMemoryMethod = AccessTools.Method(s_sidearmsCompType, "GetMemoryCompForPawn", new Type[] { typeof(Pawn), typeof(bool) }); // CHECK API
                    s_sidearmsRememberedProp = AccessTools.Property(s_sidearmsCompType, "RememberedWeapons"); // CHECK API
                    s_sidearmsRememberedField = AccessTools.Field(s_sidearmsCompType, "rememberedWeapons"); // CHECK API
                }
            }

            if (s_sidearmsCompType != null)
            {
                try
                {
                    object memComp = null;
                    if (s_sidearmsGetMemoryMethod != null)
                    {
                        memComp = s_sidearmsGetMemoryMethod.Invoke(null, new object[] { pawn, false });
                    }
                    else if (pawn.AllComps != null)
                    {
                        for (int i = 0; i < pawn.AllComps.Count; i++)
                        {
                            if (s_sidearmsCompType.IsInstanceOfType(pawn.AllComps[i]))
                            {
                                memComp = pawn.AllComps[i];
                                break;
                            }
                        }
                    }

                    if (memComp != null)
                    {
                        IEnumerable remembered = null;
                        if (s_sidearmsRememberedProp != null) remembered = s_sidearmsRememberedProp.GetValue(memComp, null) as IEnumerable;
                        else if (s_sidearmsRememberedField != null) remembered = s_sidearmsRememberedField.GetValue(memComp) as IEnumerable;

                        // Simple Sidearms remembers ThingDefStuffDefPair; weapons themselves live in inventory
                        // which we already added above, so no duplicate entries are created.
                        if (remembered != null)
                        {
                            // Touch enumerator safely to verify reflection bridge without throwing
                            IEnumerator en = remembered.GetEnumerator();
                            if (en != null) en.MoveNext();
                        }
                    }
                }
                catch
                {
                    // Silent fallback to inventory-based sidearms list
                }
            }

            return result;
        }

        public static List<Thing> FindAvailableModItems(Pawn pawn, EquipmentModKind kind)
        {
            List<Thing> results = new List<Thing>();
            if (pawn == null) return results;

            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                ThingOwner<Thing> inv = pawn.inventory.innerContainer;
                for (int i = 0; i < inv.Count; i++)
                {
                    Thing t = inv[i];
                    if (IsMatchingModItem(t, kind)) results.Add(t);
                }
            }

            if (pawn.Map != null && pawn.Map.listerThings != null)
            {
                List<Thing> all = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
                for (int i = 0; i < all.Count; i++)
                {
                    Thing t = all[i];
                    if (t == null || t.Destroyed || t.IsForbidden(Faction.OfPlayer)) continue;
                    if (!IsMatchingModItem(t, kind)) continue;
                    if (pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        results.Add(t);
                    }
                }
            }

            return results;
        }

        private static bool IsMatchingModItem(Thing t, EquipmentModKind kind)
        {
            if (t == null || t.Destroyed) return false;
            CompEquipmentMod mod = t.TryGetComp<CompEquipmentMod>();
            if (mod == null || mod.Props.modKind != kind) return false;
            if (mod.Props.maxCharges > 0 && mod.Charges <= 0) return false;
            return true;
        }

        public static Thing FindColtBulletThing(Pawn pawn, string ammoDefName)
        {
            if (pawn == null || ammoDefName.NullOrEmpty()) return null;
            ThingDef ammoDef = DefDatabase<ThingDef>.GetNamedSilentFail(ammoDefName);
            if (ammoDef == null) return null;

            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing t = pawn.inventory.innerContainer[i];
                    if (t != null && !t.Destroyed && t.def == ammoDef && t.stackCount > 0) return t;
                }
            }

            if (pawn.Map != null && pawn.Map.listerThings != null)
            {
                List<Thing> list = pawn.Map.listerThings.ThingsOfDef(ammoDef);
                Thing best = null;
                float bestDist = float.MaxValue;
                for (int i = 0; i < list.Count; i++)
                {
                    Thing t = list[i];
                    if (t == null || t.Destroyed || t.stackCount <= 0 || t.IsForbidden(Faction.OfPlayer)) continue;
                    if (!pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly)) continue;
                    float d = pawn.Position.DistanceToSquared(t.Position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = t;
                    }
                }
                return best;
            }

            return null;
        }
    }

    // =========================================================================
    // MODULE ITEM COMP (STORES CHARGES ON THE MODULE ITEM ITSELF)
    // =========================================================================

    public sealed class CompProperties_EquipmentMod : CompProperties
    {
        public EquipmentModKind modKind = EquipmentModKind.Weapon;
        public string extraDamageDef;
        public float extraDamageAmount = 5f;
        public float extraArmorPenetration = 0.40f;
        public int maxCharges = 0; // 0 = permanent / unlimited
        public float armorSharpOffset = 0f;
        public float armorBluntOffset = 0f;
        public float armorHeatOffset = 0f;
        public string requiredStoryFlag;

        public CompProperties_EquipmentMod()
        {
            compClass = typeof(CompEquipmentMod);
        }
    }

    public sealed class CompEquipmentMod : ThingComp
    {
        private int charges = -1;

        public CompProperties_EquipmentMod Props
        {
            get { return (CompProperties_EquipmentMod)props; }
        }

        public int Charges
        {
            get
            {
                if (Props.maxCharges <= 0) return 0;
                if (charges < 0) charges = Props.maxCharges;
                return charges;
            }
            set
            {
                if (Props.maxCharges <= 0) charges = 0;
                else charges = Mathf.Clamp(value, 0, Props.maxCharges);
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = Props.maxCharges;
        }

        public override string TransformLabel(string label)
        {
            if (Props.maxCharges > 0)
            {
                return string.Format("{0} [{1}/{2}]", label, Charges, Props.maxCharges);
            }
            return label;
        }

        public override string CompInspectStringExtra()
        {
            if (Props.modKind == EquipmentModKind.Weapon)
            {
                string chargePart = Props.maxCharges > 0
                    ? string.Format(" ({0}/{1})", Charges, Props.maxCharges)
                    : " (permanent)";
                return string.Format("Weapon module: +{0:0.#} {1}{2}",
                    Props.extraDamageAmount, Props.extraDamageDef ?? "damage", chargePart);
            }
            return string.Format("Apparel module: +{0:0}% sharp armor", Props.armorSharpOffset * 100f);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", -1);
            if (Props.maxCharges > 0 && charges < 0) charges = Props.maxCharges;
        }
    }

    // =========================================================================
    // SERIALIZABLE INSTALLED MOD RECORD
    // =========================================================================

    public sealed class InstalledModEntry : IExposable
    {
        public string modDefName;
        public int charges;
        public int maxCharges;
        public int hitPoints = 100;

        public ThingDef ModDef
        {
            get { return !modDefName.NullOrEmpty() ? DefDatabase<ThingDef>.GetNamedSilentFail(modDefName) : null; }
        }

        public CompProperties_EquipmentMod ModProps
        {
            get
            {
                ThingDef d = ModDef;
                return d != null ? d.GetCompProperties<CompProperties_EquipmentMod>() : null;
            }
        }

        public bool IsDepleted
        {
            get { return maxCharges > 0 && charges <= 0; }
        }

        public bool CanTrigger
        {
            get { return maxCharges <= 0 || charges > 0; }
        }

        public void ConsumeCharge()
        {
            if (maxCharges > 0 && charges > 0)
            {
                charges--;
            }
        }

        public string DisplayLabel()
        {
            ThingDef d = ModDef;
            string name = d != null ? d.LabelCap.ToString() : (modDefName ?? "Mod");
            if (maxCharges > 0)
            {
                return string.Format("{0} [{1}/{2}]", name, charges, maxCharges);
            }
            return name;
        }

        public Thing CreateReturnedThing()
        {
            ThingDef d = ModDef;
            if (d == null) return null;
            Thing t = ThingMaker.MakeThing(d);
            if (t == null) return null;
            if (d.useHitPoints && hitPoints > 0)
            {
                t.HitPoints = Mathf.Clamp(hitPoints, 1, t.MaxHitPoints);
            }
            CompEquipmentMod comp = t.TryGetComp<CompEquipmentMod>();
            if (comp != null && maxCharges > 0)
            {
                comp.Charges = charges;
            }
            return t;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref modDefName, "modDefName", null);
            Scribe_Values.Look(ref charges, "charges", 0);
            Scribe_Values.Look(ref maxCharges, "maxCharges", 0);
            Scribe_Values.Look(ref hitPoints, "hitPoints", 100);
        }
    }

    // =========================================================================
    // WEAPON MOD SLOTS COMP (2 SLOTS ON STANDARD WEAPONS, 0-1 ON LEGENDARIES)
    // =========================================================================

    public sealed class CompProperties_WeaponMods : CompProperties
    {
        public int maxSlots = 2;

        public CompProperties_WeaponMods()
        {
            compClass = typeof(CompWeaponMods);
        }
    }

    public sealed class CompWeaponMods : ThingComp
    {
        private List<InstalledModEntry> installedMods = new List<InstalledModEntry>();

        public CompProperties_WeaponMods Props
        {
            get { return (CompProperties_WeaponMods)props; }
        }

        public List<InstalledModEntry> InstalledMods
        {
            get
            {
                if (installedMods == null) installedMods = new List<InstalledModEntry>();
                return installedMods;
            }
        }

        public int MaxSlots
        {
            get { return Props != null ? Props.maxSlots : 0; }
        }

        public bool HasFreeSlot
        {
            get { return InstalledMods.Count < MaxSlots; }
        }

        public bool CanInstall(Thing modThing, out string failReason)
        {
            failReason = null;
            if (MaxSlots <= 0)
            {
                failReason = "No module slots available";
                return false;
            }
            if (!HasFreeSlot)
            {
                failReason = "All module slots are occupied";
                return false;
            }
            if (modThing == null || modThing.Destroyed)
            {
                failReason = "Invalid module";
                return false;
            }
            CompEquipmentMod mod = modThing.TryGetComp<CompEquipmentMod>();
            if (mod == null || mod.Props.modKind != EquipmentModKind.Weapon)
            {
                failReason = "Not a weapon module";
                return false;
            }
            if (mod.Props.maxCharges > 0 && mod.Charges <= 0)
            {
                failReason = "Module tank is empty";
                return false;
            }
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                if (InstalledMods[i].modDefName == modThing.def.defName)
                {
                    failReason = "Duplicate module already installed";
                    return false;
                }
            }
            return true;
        }

        public bool InstallMod(Thing modThing)
        {
            string reason;
            if (!CanInstall(modThing, out reason)) return false;

            CompEquipmentMod modComp = modThing.TryGetComp<CompEquipmentMod>();
            InstalledModEntry entry = new InstalledModEntry();
            entry.modDefName = modThing.def.defName;
            entry.maxCharges = modComp.Props.maxCharges;
            entry.charges = modComp.Charges;
            entry.hitPoints = modThing.HitPoints;
            InstalledMods.Add(entry);

            if (modThing.stackCount > 1)
            {
                Thing split = modThing.SplitOff(1);
                if (split != null && !split.Destroyed) split.Destroy(DestroyMode.Vanish);
            }
            else if (!modThing.Destroyed)
            {
                modThing.Destroy(DestroyMode.Vanish);
            }

            return true;
        }

        public Thing RemoveMod(int index, Map map, IntVec3 dropPos)
        {
            if (index < 0 || index >= InstalledMods.Count) return null;
            InstalledModEntry entry = InstalledMods[index];
            InstalledMods.RemoveAt(index);

            Thing returned = entry.CreateReturnedThing();
            if (returned != null && map != null && dropPos.InBounds(map))
            {
                GenPlace.TryPlaceThing(returned, dropPos, map, ThingPlaceMode.Near);
            }
            return returned;
        }

        public override string CompInspectStringExtra()
        {
            if (MaxSlots <= 0) return null;
            if (InstalledMods.Count == 0)
            {
                return string.Format("TMM_WeaponModsEmpty".Translate(), MaxSlots);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("TMM_WeaponModsPrefix".Translate());
            sb.Append(" ");
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(InstalledMods[i].DisplayLabel());
            }
            return sb.ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref installedMods, "installedMods", LookMode.Deep);
            if (installedMods == null) installedMods = new List<InstalledModEntry>();
        }
    }

    // =========================================================================
    // APPAREL MOD SLOTS COMP (1 SLOT ON COATS / JACKETS / DUSTERS)
    // =========================================================================

    public sealed class CompProperties_ApparelMods : CompProperties
    {
        public int maxSlots = 1;

        public CompProperties_ApparelMods()
        {
            compClass = typeof(CompApparelMods);
        }
    }

    public sealed class CompApparelMods : ThingComp
    {
        private List<InstalledModEntry> installedMods = new List<InstalledModEntry>();

        public CompProperties_ApparelMods Props
        {
            get { return (CompProperties_ApparelMods)props; }
        }

        public List<InstalledModEntry> InstalledMods
        {
            get
            {
                if (installedMods == null) installedMods = new List<InstalledModEntry>();
                return installedMods;
            }
        }

        public int MaxSlots
        {
            get { return Props != null ? Props.maxSlots : 0; }
        }

        public bool HasFreeSlot
        {
            get { return InstalledMods.Count < MaxSlots; }
        }

        public bool CanInstall(Thing modThing, out string failReason)
        {
            failReason = null;
            if (MaxSlots <= 0 || !HasFreeSlot)
            {
                failReason = "No apparel lining slots free";
                return false;
            }
            if (modThing == null || modThing.Destroyed)
            {
                failReason = "Invalid module";
                return false;
            }
            CompEquipmentMod mod = modThing.TryGetComp<CompEquipmentMod>();
            if (mod == null || mod.Props.modKind != EquipmentModKind.Apparel)
            {
                failReason = "Not an apparel module";
                return false;
            }
            return true;
        }

        public bool InstallMod(Thing modThing)
        {
            string reason;
            if (!CanInstall(modThing, out reason)) return false;

            CompEquipmentMod modComp = modThing.TryGetComp<CompEquipmentMod>();
            InstalledModEntry entry = new InstalledModEntry();
            entry.modDefName = modThing.def.defName;
            entry.maxCharges = modComp.Props.maxCharges;
            entry.charges = modComp.Charges;
            entry.hitPoints = modThing.HitPoints;
            InstalledMods.Add(entry);

            if (modThing.stackCount > 1)
            {
                Thing split = modThing.SplitOff(1);
                if (split != null && !split.Destroyed) split.Destroy(DestroyMode.Vanish);
            }
            else if (!modThing.Destroyed)
            {
                modThing.Destroy(DestroyMode.Vanish);
            }

            return true;
        }

        public Thing RemoveMod(int index, Map map, IntVec3 dropPos)
        {
            if (index < 0 || index >= InstalledMods.Count) return null;
            InstalledModEntry entry = InstalledMods[index];
            InstalledMods.RemoveAt(index);

            Thing returned = entry.CreateReturnedThing();
            if (returned != null && map != null && dropPos.InBounds(map))
            {
                GenPlace.TryPlaceThing(returned, dropPos, map, ThingPlaceMode.Near);
            }
            return returned;
        }

        public float GetArmorOffset(StatDef stat)
        {
            if (stat == null || InstalledMods.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                CompProperties_EquipmentMod mp = InstalledMods[i].ModProps;
                if (mp == null) continue;
                if (stat == StatDefOf.ArmorRating_Sharp) sum += mp.armorSharpOffset;
                else if (stat == StatDefOf.ArmorRating_Blunt) sum += mp.armorBluntOffset;
                else if (stat == StatDefOf.ArmorRating_Heat) sum += mp.armorHeatOffset;
            }
            return sum;
        }

        public override string CompInspectStringExtra()
        {
            if (InstalledMods.Count == 0) return null;
            StringBuilder sb = new StringBuilder();
            sb.Append("TMM_ApparelModsPrefix".Translate());
            sb.Append(" ");
            for (int i = 0; i < InstalledMods.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(InstalledMods[i].DisplayLabel());
            }
            return sb.ToString();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref installedMods, "installedMods", LookMode.Deep);
            if (installedMods == null) installedMods = new List<InstalledModEntry>();
        }
    }

    // =========================================================================
    // STAT PART FOR APPAREL MODS (REFLECTS +0.25 SHARP ARMOR EVERYWHERE)
    // =========================================================================

    public sealed class StatPart_ApparelMods : StatPart
    {
        private readonly StatDef targetStat;

        public StatPart_ApparelMods(StatDef stat)
        {
            targetStat = stat;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || req.Thing == null) return;
            CompApparelMods comp = req.Thing.TryGetComp<CompApparelMods>();
            if (comp == null) return;
            float bonus = comp.GetArmorOffset(targetStat);
            if (bonus > 0f)
            {
                val += bonus;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing || req.Thing == null) return null;
            CompApparelMods comp = req.Thing.TryGetComp<CompApparelMods>();
            if (comp == null) return null;
            float bonus = comp.GetArmorOffset(targetStat);
            if (bonus <= 0f) return null;
            return string.Format("TMM_ApparelModStatLine".Translate(), bonus.ToStringPercent());
        }
    }

    // =========================================================================
    // COLT AMMO COMP (13 ROUNDS, RELOADABLE WITH TMM_Ammo_ColtSilverBullet)
    // Attached to TMM_Legendary_TheColt via XML patch & DefsLoaded injector
    // =========================================================================

    public sealed class CompProperties_ColtAmmo : CompProperties
    {
        public int maxCharges = 13;
        public string ammoDefName = "TMM_Ammo_ColtSilverBullet";

        public CompProperties_ColtAmmo()
        {
            compClass = typeof(CompColtAmmo);
        }
    }

    public sealed class CompColtAmmo : ThingComp
    {
        private int charges = -1;

        public CompProperties_ColtAmmo Props
        {
            get { return (CompProperties_ColtAmmo)props; }
        }

        public int MaxCharges
        {
            get { return Props != null ? Props.maxCharges : 13; }
        }

        public int Charges
        {
            get
            {
                int synced;
                if (TryReadCompLimitedCharges(out synced))
                {
                    charges = synced;
                    return synced;
                }
                if (charges < 0) charges = MaxCharges;
                return charges;
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = MaxCharges;
        }

        public bool ConsumeRound()
        {
            if (Charges <= 0) return false;
            charges = Charges - 1;
            TryWriteCompLimitedCharges(charges);
            return true;
        }

        public int ReloadRounds(int amount)
        {
            if (amount <= 0) return 0;
            int cur = Charges;
            int missing = MaxCharges - cur;
            int add = Mathf.Min(missing, amount);
            if (add <= 0) return 0;
            charges = cur + add;
            TryWriteCompLimitedCharges(charges);
            return add;
        }

        public override string CompInspectStringExtra()
        {
            // Avoid duplicating inspect text if CompLimitedCharges is already mounted
            if (HasCompLimitedCharges()) return null;
            return string.Format("TMM_ColtAmmoInspect".Translate(), Charges, MaxCharges);
        }

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra() // CHECK API
        {
            Pawn holder = parent.ParentHolder is Pawn_EquipmentTracker
                ? ((Pawn_EquipmentTracker)parent.ParentHolder).pawn : null;
            if (holder == null || !holder.IsColonistPlayerControlled) yield break;

            if (Charges < MaxCharges)
            {
                Command_Action cmd = new Command_Action();
                cmd.defaultLabel = string.Format("TMM_ReloadColtGizmo".Translate(), Charges, MaxCharges);
                cmd.defaultDesc = "TMM_ReloadColtDesc".Translate();
                cmd.icon = TexCommand.RearmTrap; // CHECK API
                cmd.action = delegate
                {
                    Thing bullets = EquipmentUtil.FindColtBulletThing(holder, Props.ammoDefName);
                    if (bullets == null)
                    {
                        Messages.Message("TMM_NoColtBullets".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_ReloadColt");
                    if (jd != null)
                    {
                        Job job = JobMaker.MakeJob(jd, parent, bullets);
                        job.count = MaxCharges - Charges;
                        holder.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };
                yield return cmd;
            }
        }

        public bool HasCompLimitedCharges()
        {
            return FindCompLimitedCharges() != null;
        }

        private ThingComp FindCompLimitedCharges()
        {
            if (parent == null || parent.AllComps == null) return null;
            for (int i = 0; i < parent.AllComps.Count; i++)
            {
                ThingComp c = parent.AllComps[i];
                if (c != null && c.GetType().Name == "CompLimitedCharges") return c;
            }
            return null;
        }

        private bool TryReadCompLimitedCharges(out int value)
        {
            value = 0;
            ThingComp lc = FindCompLimitedCharges();
            if (lc == null) return false;
            PropertyInfo prop = AccessTools.Property(lc.GetType(), "Charges");
            if (prop != null)
            {
                object v = prop.GetValue(lc, null);
                if (v is int)
                {
                    value = (int)v;
                    return true;
                }
            }
            return false;
        }

        private void TryWriteCompLimitedCharges(int newCharges)
        {
            ThingComp lc = FindCompLimitedCharges();
            if (lc == null) return;
            FieldInfo field = AccessTools.Field(lc.GetType(), "charges");
            if (field != null)
            {
                field.SetValue(lc, newCharges);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", -1);
            if (charges < 0) charges = MaxCharges;
        }
    }

    // =========================================================================
    // JOB DRIVERS (INSTALL MOD, REMOVE MOD, RELOAD COLT)
    // =========================================================================

    public sealed class JobDriver_InstallEquipmentMod : JobDriver
    {
        public const int DefaultWorkTicks = 2500; // 1 in-game hour

        private Thing TargetEquipment { get { return job.targetA.Thing; } }
        private Thing ModThing { get { return job.targetB.Thing; } }
        private Building Workbench { get { return job.targetC.Thing as Building; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (ModThing != null && ModThing.Spawned)
            {
                if (!pawn.Reserve(job.targetB, job, 1, 1, null, errorOnFailed)) return false;
            }
            if (Workbench != null && Workbench.Spawned)
            {
                if (!pawn.Reserve(job.targetC, job, 1, -1, null, errorOnFailed)) return false;
            }
            if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return TargetEquipment == null || TargetEquipment.Destroyed || ModThing == null || ModThing.Destroyed;
            });

            // 1. Fetch module if lying on map
            if (ModThing != null && ModThing.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            }

            // 2. Walk to workbench (or to equipment if on ground and no workbench)
            if (Workbench != null && Workbench.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            }
            else if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }

            // 3. Work at bench for 1 in-game hour
            Toil work = Toils_General.Wait(DefaultWorkTicks);
            work.WithProgressBarToilDelay(Workbench != null ? TargetIndex.C : TargetIndex.A);
            work.WithEffect(EffecterDefOf.ConstructMetal, Workbench != null ? TargetIndex.C : TargetIndex.A); // CHECK API
            yield return work;

            // 4. Perform installation
            Toil install = new Toil();
            install.initAction = delegate
            {
                Thing eq = TargetEquipment;
                Thing mod = ModThing;
                if (eq == null || mod == null || eq.Destroyed || mod.Destroyed) return;

                CompWeaponMods wm = eq.TryGetComp<CompWeaponMods>();
                if (wm != null)
                {
                    wm.InstallMod(mod);
                    return;
                }
                CompApparelMods am = eq.TryGetComp<CompApparelMods>();
                if (am != null)
                {
                    am.InstallMod(mod);
                }
            };
            install.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return install;
        }
    }

    public sealed class JobDriver_RemoveEquipmentMod : JobDriver
    {
        public const int DefaultWorkTicks = 2500; // 1 in-game hour

        private Thing TargetEquipment { get { return job.targetA.Thing; } }
        private Building Workbench { get { return job.targetC.Thing as Building; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Workbench != null && Workbench.Spawned)
            {
                if (!pawn.Reserve(job.targetC, job, 1, -1, null, errorOnFailed)) return false;
            }
            if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return TargetEquipment == null || TargetEquipment.Destroyed;
            });

            if (Workbench != null && Workbench.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            }
            else if (TargetEquipment != null && TargetEquipment.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }

            Toil work = Toils_General.Wait(DefaultWorkTicks);
            work.WithProgressBarToilDelay(Workbench != null ? TargetIndex.C : TargetIndex.A);
            yield return work;

            Toil remove = new Toil();
            remove.initAction = delegate
            {
                Thing eq = TargetEquipment;
                if (eq == null || eq.Destroyed) return;

                int idx = Mathf.Max(0, job.count);
                CompWeaponMods wm = eq.TryGetComp<CompWeaponMods>();
                if (wm != null)
                {
                    wm.RemoveMod(idx, pawn.Map, pawn.Position);
                    return;
                }
                CompApparelMods am = eq.TryGetComp<CompApparelMods>();
                if (am != null)
                {
                    am.RemoveMod(idx, pawn.Map, pawn.Position);
                }
            };
            remove.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return remove;
        }
    }

    public sealed class JobDriver_ReloadColt : JobDriver
    {
        private Thing Colt { get { return job.targetA.Thing; } }
        private Thing Ammo { get { return job.targetB.Thing; } }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Ammo != null && Ammo.Spawned)
            {
                if (!pawn.Reserve(job.targetB, job, 1, Mathf.Max(1, job.count), null, errorOnFailed)) return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                return Colt == null || Colt.Destroyed || Ammo == null || Ammo.Destroyed || Ammo.stackCount <= 0;
            });

            if (Ammo != null && Ammo.Spawned)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            }

            Toil wait = Toils_General.Wait(120);
            wait.WithProgressBarToilDelay(TargetIndex.A);
            yield return wait;

            Toil reload = new Toil();
            reload.initAction = delegate
            {
                CompColtAmmo ammoComp = Colt != null ? Colt.TryGetComp<CompColtAmmo>() : null;
                if (ammoComp == null || Ammo == null || Ammo.Destroyed) return;

                int need = ammoComp.MaxCharges - ammoComp.Charges;
                int take = Mathf.Min(need, Ammo.stackCount);
                if (take <= 0) return;

                int loaded = ammoComp.ReloadRounds(take);
                if (loaded > 0)
                {
                    if (Ammo.stackCount > loaded)
                    {
                        Thing split = Ammo.SplitOff(loaded);
                        if (split != null && !split.Destroyed) split.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Ammo.Destroy(DestroyMode.Vanish);
                    }
                }
            };
            reload.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return reload;
        }
    }

    // =========================================================================
    // WARCRAFT-STYLE EQUIPMENT ITAB (ADDITIVE — DOES NOT REPLACE VANILLA GEAR)
    // =========================================================================

    public sealed class ITab_TMM_Equipment : ITab
    {
        private static readonly Color EmptySlotBg = new Color(0.18f, 0.19f, 0.21f, 0.90f);
        private static readonly Color FilledSlotBg = new Color(0.24f, 0.26f, 0.30f, 0.95f);
        private static readonly Color SlotBorderColor = new Color(0.45f, 0.48f, 0.52f, 1.0f);
        private static readonly Color MiniModBg = new Color(0.15f, 0.16f, 0.18f, 0.95f);

        public ITab_TMM_Equipment()
        {
            size = new Vector2(540f, 560f);
            labelKey = "TMM_TabEquipment";
            tutorTag = "TMM_Equipment";
        }

        public override bool IsVisible
        {
            get
            {
                Pawn p = SelPawn;
                return p != null && p.RaceProps != null && p.RaceProps.Humanlike;
            }
        }

        protected override void FillTab()
        {
            Pawn p = SelPawn;
            if (p == null) return;
            DrawEquipmentTab(new Rect(0f, 0f, size.x, size.y).ContractedBy(10f), p);
        }

        /// <summary>
        /// Direct entry point used by InGameEquipmentProbe during a real EventType.Repaint pass.
        /// </summary>
        internal void DebugDrawInOnGUI(Rect rect, Pawn pawn)
        {
            if (pawn == null) return;
            GUI.BeginGroup(rect);
            try
            {
                DrawEquipmentTab(new Rect(0f, 0f, rect.width, rect.height).ContractedBy(10f), pawn);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void DrawEquipmentTab(Rect rect, Pawn pawn)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, 30f);
            Widgets.Label(titleRect, string.Format("{0} — {1}", pawn.LabelShortCap, "TMM_TabEquipment".Translate()));
            Text.Font = GameFont.Small;

            float topY = rect.y + 34f;
            float slotSize = 62f;
            float rowStep = 70f;

            // Classify worn apparel into paperdoll slots
            Apparel head = FindSlotApparel(pawn, "head");
            Apparel eyes = FindSlotApparel(pawn, "eyes");
            Apparel skinTorso = FindSlotApparel(pawn, "skin");
            Apparel midTorso = FindSlotApparel(pawn, "middle");
            Apparel shellTorso = FindSlotApparel(pawn, "shell");
            Apparel legs = FindSlotApparel(pawn, "legs");
            Apparel belt = FindSlotApparel(pawn, "belt");
            Apparel backpack = FindSlotApparel(pawn, "backpack");
            Apparel powerArmor = FindSlotApparel(pawn, "powerarmor");

            // LEFT COLUMN: Head, Eyes, Torso (Skin, Middle, Shell + Mod)
            float leftX = rect.x + 8f;
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 0f, slotSize, slotSize), head, "TMM_Slot_Head".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 1f, slotSize, slotSize), eyes, "TMM_Slot_Eyes".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 2f, slotSize, slotSize), skinTorso, "TMM_Slot_Skin".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 3f, slotSize, slotSize), midTorso, "TMM_Slot_Middle".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(leftX, topY + rowStep * 4f, slotSize, slotSize), shellTorso, "TMM_Slot_Shell".Translate(), pawn, true);

            // Apparel Mod mini-slot next to Shell
            DrawApparelModSlot(new Rect(leftX + slotSize + 8f, topY + rowStep * 4f + 15f, 32f, 32f), shellTorso, pawn);

            // CENTER PAPERDOLL PORTRAIT & STATS
            float centerW = 210f;
            float centerX = rect.x + (rect.width - centerW) * 0.5f;
            Rect portraitBox = new Rect(centerX + 35f, topY + 12f, 140f, 175f);
            Widgets.DrawBoxSolid(portraitBox, new Color(0.12f, 0.13f, 0.15f, 0.9f));
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(portraitBox, 1);
            GUI.color = Color.white;

            RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(portraitBox.width, portraitBox.height), Rot4.South, default(Vector3), 1.15f); // CHECK API
            if (portrait != null)
            {
                GUI.DrawTexture(portraitBox.ContractedBy(4f), portrait, ScaleMode.ScaleToFit);
            }

            // Stats summary box under portrait
            Rect statsBox = new Rect(centerX, topY + 200f, centerW, 130f);
            Widgets.DrawBoxSolid(statsBox, new Color(0.14f, 0.15f, 0.17f, 0.85f));
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(statsBox, 1);
            GUI.color = Color.white;

            Rect st = statsBox.ContractedBy(8f);
            Text.Font = GameFont.Tiny;
            float carryStat = pawn.GetStatValue(StatDefOf.CarryingCapacity);
            float caravanCap = MassUtility.Capacity(pawn); // CHECK API
            float moveSpd = pawn.GetStatValue(StatDefOf.MoveSpeed);
            float bestSharp = ComputeBestArmor(pawn, StatDefOf.ArmorRating_Sharp);
            float bestBlunt = ComputeBestArmor(pawn, StatDefOf.ArmorRating_Blunt);

            Widgets.Label(new Rect(st.x, st.y, st.width, 20f), string.Format("{0}: {1:0} / {2:0.#} kg", "TMM_Stat_Carry".Translate(), carryStat, caravanCap));
            Widgets.Label(new Rect(st.x, st.y + 20f, st.width, 20f), string.Format("{0}: {1:0.00} c/s", "TMM_Stat_Move".Translate(), moveSpd));
            Widgets.Label(new Rect(st.x, st.y + 42f, st.width, 20f), string.Format("{0}: {1}", "TMM_Stat_Sharp".Translate(), bestSharp.ToStringPercent()));
            Widgets.Label(new Rect(st.x, st.y + 62f, st.width, 20f), string.Format("{0}: {1}", "TMM_Stat_Blunt".Translate(), bestBlunt.ToStringPercent()));
            Text.Font = GameFont.Small;

            // RIGHT COLUMN: Legs, Belt, Backpack, Power Armor
            float rightX = rect.xMax - slotSize - 8f;
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 0f, slotSize, slotSize), legs, "TMM_Slot_Legs".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 1f, slotSize, slotSize), belt, "TMM_Slot_Belt".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 2f, slotSize, slotSize), backpack, "TMM_Slot_Backpack".Translate(), pawn, true);
            DrawEquipmentSlot(new Rect(rightX, topY + rowStep * 3f, slotSize, slotSize), powerArmor, "TMM_Slot_PowerArmor".Translate(), pawn, true);

            // BOTTOM WEAPONS & MOD SLOTS PANEL
            float weaponsY = topY + rowStep * 5f + 6f;
            Rect weaponsBox = new Rect(rect.x, weaponsY, rect.width, rect.yMax - weaponsY);
            Widgets.DrawBoxSolid(weaponsBox, new Color(0.13f, 0.14f, 0.16f, 0.90f));
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(weaponsBox, 1);
            GUI.color = Color.white;

            Rect wp = weaponsBox.ContractedBy(8f);
            ThingWithComps primary = pawn.equipment != null ? pawn.equipment.Primary : null;

            // Primary weapon + up to 2 mod slots
            DrawEquipmentSlot(new Rect(wp.x, wp.y, slotSize, slotSize), primary, "TMM_Slot_Primary".Translate(), pawn, false);
            DrawWeaponModSlotsRow(new Rect(wp.x, wp.y + slotSize + 6f, 110f, 30f), primary, pawn);

            // Sidearms (Simple Sidearms reflection + inventory weapons)
            float sideX = wp.x + 130f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(sideX, wp.y, wp.width - 130f, 18f), "TMM_Slot_Sidearms".Translate());
            Text.Font = GameFont.Small;

            List<ThingWithComps> sidearms = EquipmentUtil.GetSidearms(pawn);
            int maxShow = 4;
            float sideSlot = 48f;
            for (int i = 0; i < maxShow; i++)
            {
                ThingWithComps sw = i < sidearms.Count ? sidearms[i] : null;
                Rect sRect = new Rect(sideX + i * (sideSlot + 34f), wp.y + 20f, sideSlot, sideSlot);
                DrawEquipmentSlot(sRect, sw, "TMM_Slot_Backup".Translate(), pawn, false);
                if (sw != null)
                {
                    DrawWeaponModSlotsRow(new Rect(sRect.x, sRect.yMax + 4f, sideSlot + 24f, 24f), sw, pawn);
                }
            }
        }

        private void DrawEquipmentSlot(Rect rect, Thing item, string emptyLabel, Pawn pawn, bool isApparel)
        {
            Widgets.DrawBoxSolid(rect, item != null ? FilledSlotBg : EmptySlotBg);
            GUI.color = SlotBorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;

            if (item == null)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.68f, 0.70f, 0.72f, 0.9f);
                Widgets.Label(rect.ContractedBy(3f), emptyLabel);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                return;
            }

            Rect iconRect = rect.ContractedBy(6f);
            Widgets.ThingIcon(iconRect, item);

            // Quality badge top-left
            QualityCategory qc;
            if (item.TryGetQuality(out qc))
            {
                Text.Font = GameFont.Tiny;
                Rect qRect = new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, 16f);
                string qShort = qc.GetLabelShort();
                if (qShort.Length > 3) qShort = qShort.Substring(0, 3);
                Widgets.Label(qRect, qShort);
                Text.Font = GameFont.Small;
            }

            // Colt charge badge top-right
            CompColtAmmo coltAmmo = item.TryGetComp<CompColtAmmo>();
            if (coltAmmo != null)
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, 16f), coltAmmo.Charges.ToString());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            // Durability bar along bottom
            if (item.def.useHitPoints && item.MaxHitPoints > 0)
            {
                float pct = Mathf.Clamp01((float)item.HitPoints / (float)item.MaxHitPoints);
                Rect barBg = new Rect(rect.x + 3f, rect.yMax - 6f, rect.width - 6f, 3f);
                Widgets.DrawBoxSolid(barBg, new Color(0.08f, 0.08f, 0.08f, 0.9f));
                Color barCol = pct > 0.5f ? new Color(0.3f, 0.85f, 0.35f) : (pct > 0.25f ? Color.yellow : Color.red);
                Widgets.DrawBoxSolid(new Rect(barBg.x, barBg.y, barBg.width * pct, barBg.height), barCol);
            }

            // Tooltip with stats & mods
            TooltipHandler.TipRegion(rect, BuildItemTooltip(item));

            // Right-click menu: Info / Unequip / Install Mod / Reload Colt
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(rect))
            {
                OpenSlotContextMenu(item, pawn, isApparel);
                Event.current.Use();
            }
        }

        private void DrawWeaponModSlotsRow(Rect area, ThingWithComps weapon, Pawn pawn)
        {
            if (weapon == null) return;
            CompWeaponMods comp = weapon.GetComp<CompWeaponMods>();
            if (comp == null || comp.MaxSlots <= 0) return;

            float miniSize = Mathf.Min(28f, area.height);
            for (int i = 0; i < comp.MaxSlots; i++)
            {
                Rect mRect = new Rect(area.x + i * (miniSize + 4f), area.y, miniSize, miniSize);
                InstalledModEntry entry = i < comp.InstalledMods.Count ? comp.InstalledMods[i] : null;
                DrawModMiniSlot(mRect, weapon, entry, i, EquipmentModKind.Weapon, pawn);
            }
        }

        private void DrawApparelModSlot(Rect mRect, Apparel apparel, Pawn pawn)
        {
            if (apparel == null) return;
            CompApparelMods comp = apparel.GetComp<CompApparelMods>();
            if (comp == null || comp.MaxSlots <= 0) return;

            InstalledModEntry entry = comp.InstalledMods.Count > 0 ? comp.InstalledMods[0] : null;
            DrawModMiniSlot(mRect, apparel, entry, 0, EquipmentModKind.Apparel, pawn);
        }

        private void DrawModMiniSlot(Rect rect, Thing hostItem, InstalledModEntry entry, int slotIndex, EquipmentModKind kind, Pawn pawn)
        {
            Widgets.DrawBoxSolid(rect, MiniModBg);
            GUI.color = entry != null ? new Color(0.35f, 0.75f, 0.95f) : SlotBorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;

            if (entry != null && entry.ModDef != null)
            {
                Widgets.DefIcon(rect.ContractedBy(3f), entry.ModDef);
                if (entry.maxCharges > 0)
                {
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.LowerRight;
                    Widgets.Label(rect.ContractedBy(1f), entry.charges.ToString());
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }
                TooltipHandler.TipRegion(rect, entry.DisplayLabel());
            }
            else
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.6f, 0.62f, 0.65f);
                Widgets.Label(rect, "+");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                TooltipHandler.TipRegion(rect, "TMM_EmptyModSlotTip".Translate());
            }

            if (Event.current.type == EventType.MouseDown && (Event.current.button == 0 || Event.current.button == 1) && Mouse.IsOver(rect))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (entry != null)
                {
                    if (entry.ModDef != null)
                    {
                        opts.Add(new FloatMenuOption("TMM_Menu_Info".Translate(), delegate
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(entry.ModDef));
                        }));
                    }
                    if (pawn != null && pawn.IsColonistPlayerControlled)
                    {
                        int capturedIndex = slotIndex;
                        opts.Add(new FloatMenuOption("TMM_Menu_RemoveMod".Translate(), delegate
                        {
                            QueueRemoveModJob(pawn, hostItem, capturedIndex);
                        }));
                    }
                }
                else if (pawn != null && pawn.IsColonistPlayerControlled)
                {
                    AddInstallModOptions(opts, pawn, hostItem, kind);
                }

                if (opts.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(opts));
                }
                Event.current.Use();
            }
        }

        private void OpenSlotContextMenu(Thing item, Pawn pawn, bool isApparel)
        {
            List<FloatMenuOption> opts = new List<FloatMenuOption>();

            opts.Add(new FloatMenuOption("TMM_Menu_Info".Translate(), delegate
            {
                Find.WindowStack.Add(new Dialog_InfoCard(item));
            }));

            if (pawn != null && pawn.IsColonistPlayerControlled)
            {
                opts.Add(new FloatMenuOption("TMM_Menu_Unequip".Translate(), delegate
                {
                    if (isApparel && item is Apparel && pawn.apparel != null)
                    {
                        Apparel dropped;
                        pawn.apparel.TryDrop((Apparel)item, out dropped, pawn.Position, true);
                    }
                    else if (!isApparel && item is ThingWithComps)
                    {
                        ThingWithComps twc = (ThingWithComps)item;
                        if (pawn.equipment != null && pawn.equipment.Primary == twc)
                        {
                            ThingWithComps dropped;
                            pawn.equipment.TryDropEquipment(twc, out dropped, pawn.Position, true);
                        }
                        else if (pawn.inventory != null && pawn.inventory.innerContainer.Contains(twc))
                        {
                            Thing dropped;
                            pawn.inventory.innerContainer.TryDrop(twc, pawn.Position, pawn.Map, ThingPlaceMode.Near, out dropped);
                        }
                    }
                }));

                CompWeaponMods wm = item.TryGetComp<CompWeaponMods>();
                if (wm != null && wm.HasFreeSlot)
                {
                    AddInstallModOptions(opts, pawn, item, EquipmentModKind.Weapon);
                }

                CompApparelMods am = item.TryGetComp<CompApparelMods>();
                if (am != null && am.HasFreeSlot)
                {
                    AddInstallModOptions(opts, pawn, item, EquipmentModKind.Apparel);
                }

                CompColtAmmo coltAmmo = item.TryGetComp<CompColtAmmo>();
                if (coltAmmo != null && coltAmmo.Charges < coltAmmo.MaxCharges)
                {
                    opts.Add(new FloatMenuOption(string.Format("TMM_ReloadColtGizmo".Translate(), coltAmmo.Charges, coltAmmo.MaxCharges), delegate
                    {
                        Thing bullets = EquipmentUtil.FindColtBulletThing(pawn, coltAmmo.Props.ammoDefName);
                        if (bullets == null)
                        {
                            Messages.Message("TMM_NoColtBullets".Translate(), MessageTypeDefOf.RejectInput, false);
                            return;
                        }
                        JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_ReloadColt");
                        if (jd != null)
                        {
                            Job j = JobMaker.MakeJob(jd, item, bullets);
                            j.count = coltAmmo.MaxCharges - coltAmmo.Charges;
                            pawn.jobs.TryTakeOrderedJob(j, JobTag.Misc);
                        }
                    }));
                }
            }

            if (opts.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static void AddInstallModOptions(List<FloatMenuOption> opts, Pawn pawn, Thing hostItem, EquipmentModKind kind)
        {
            List<Thing> mods = EquipmentUtil.FindAvailableModItems(pawn, kind);
            if (mods.Count == 0)
            {
                opts.Add(new FloatMenuOption("TMM_NoModsAvailable".Translate(), null));
                return;
            }

            HashSet<string> shownDefs = new HashSet<string>();
            for (int i = 0; i < mods.Count; i++)
            {
                Thing modThing = mods[i];
                if (!shownDefs.Add(modThing.def.defName)) continue;

                string fail;
                bool can = false;
                CompWeaponMods wm = hostItem.TryGetComp<CompWeaponMods>();
                CompApparelMods am = hostItem.TryGetComp<CompApparelMods>();
                if (wm != null) can = wm.CanInstall(modThing, out fail);
                else if (am != null) can = am.CanInstall(modThing, out fail);
                else fail = "Incompatible";

                if (!can) continue;

                Thing capturedMod = modThing;
                opts.Add(new FloatMenuOption(string.Format("TMM_Menu_InstallMod".Translate(), capturedMod.LabelShortCap), delegate
                {
                    QueueInstallModJob(pawn, hostItem, capturedMod);
                }));
            }
        }

        public static void QueueInstallModJob(Pawn pawn, Thing hostItem, Thing modThing)
        {
            JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_InstallEquipmentMod");
            if (jd == null || pawn == null || hostItem == null || modThing == null) return;
            Building bench = EquipmentUtil.FindNearestWorkbench(pawn);
            Job job = bench != null
                ? JobMaker.MakeJob(jd, hostItem, modThing, bench)
                : JobMaker.MakeJob(jd, hostItem, modThing);
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public static void QueueRemoveModJob(Pawn pawn, Thing hostItem, int slotIndex)
        {
            JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_RemoveEquipmentMod");
            if (jd == null || pawn == null || hostItem == null) return;
            Building bench = EquipmentUtil.FindNearestWorkbench(pawn);
            Job job = bench != null
                ? JobMaker.MakeJob(jd, hostItem, LocalTargetInfo.Invalid, bench)
                : JobMaker.MakeJob(jd, hostItem);
            job.count = slotIndex;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static Apparel FindSlotApparel(Pawn pawn, string slotKey)
        {
            if (pawn == null || pawn.apparel == null) return null;
            List<Apparel> worn = pawn.apparel.WornApparel;

            for (int i = 0; i < worn.Count; i++)
            {
                Apparel ap = worn[i];
                if (ap == null || ap.def == null || ap.def.apparel == null || ap.def.apparel.layers == null) continue;
                List<ApparelLayerDef> layers = ap.def.apparel.layers;
                List<BodyPartGroupDef> parts = ap.def.apparel.bodyPartGroups;

                if (slotKey == "powerarmor")
                {
                    if (HasLayer(layers, "TMM_PowerArmor")) return ap;
                }
                else if (slotKey == "backpack")
                {
                    if (HasLayer(layers, "TMM_Backpack")) return ap;
                }
                else if (slotKey == "belt")
                {
                    if (layers.Contains(ApparelLayerDefOf.Belt)) return ap;
                }
                else if (slotKey == "eyes")
                {
                    if (layers.Contains(ApparelLayerDefOf.EyeCover)) return ap;
                }
                else if (slotKey == "head")
                {
                    if (layers.Contains(ApparelLayerDefOf.Overhead)) return ap;
                }
                else if (slotKey == "shell")
                {
                    if (layers.Contains(ApparelLayerDefOf.Shell) && !HasLayer(layers, "TMM_PowerArmor")) return ap;
                }
                else if (slotKey == "middle")
                {
                    if (layers.Contains(ApparelLayerDefOf.Middle) && parts != null && parts.Contains(BodyPartGroupDefOf.Torso)) return ap;
                }
                else if (slotKey == "skin")
                {
                    if (layers.Contains(ApparelLayerDefOf.OnSkin) && parts != null && parts.Contains(BodyPartGroupDefOf.Torso)) return ap;
                }
                else if (slotKey == "legs")
                {
                    bool hasLegs = parts != null && parts.Contains(BodyPartGroupDefOf.Legs);
                    bool hasTorso = parts != null && parts.Contains(BodyPartGroupDefOf.Torso);
                    if (hasLegs && !hasTorso && !HasLayer(layers, "TMM_PowerArmor")) return ap;
                }
            }
            return null;
        }

        private static bool HasLayer(List<ApparelLayerDef> layers, string defName)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].defName == defName) return true;
            }
            return false;
        }

        private static float ComputeBestArmor(Pawn pawn, StatDef stat)
        {
            float best = 0f;
            if (pawn == null || pawn.apparel == null) return best;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] == null) continue;
                float val = worn[i].GetStatValue(stat);
                if (val > best) best = val;
            }
            return best;
        }

        private static string BuildItemTooltip(Thing item)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(item.LabelCap);
            if (item.def.useHitPoints)
            {
                sb.AppendLine(string.Format("HP: {0} / {1}", item.HitPoints, item.MaxHitPoints));
            }
            if (item is Apparel)
            {
                sb.AppendLine(string.Format("Sharp: {0}  Blunt: {1}  Heat: {2}",
                    item.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent(),
                    item.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent(),
                    item.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent()));
            }
            CompWeaponMods wm = item.TryGetComp<CompWeaponMods>();
            if (wm != null && wm.InstalledMods.Count > 0)
            {
                sb.AppendLine("--- Modules ---");
                for (int i = 0; i < wm.InstalledMods.Count; i++)
                {
                    sb.AppendLine("• " + wm.InstalledMods[i].DisplayLabel());
                }
            }
            CompApparelMods am = item.TryGetComp<CompApparelMods>();
            if (am != null && am.InstalledMods.Count > 0)
            {
                sb.AppendLine("--- Lining ---");
                for (int i = 0; i < am.InstalledMods.Count; i++)
                {
                    sb.AppendLine("• " + am.InstalledMods[i].DisplayLabel());
                }
            }
            return sb.ToString().TrimEnd();
        }
    }

    // =========================================================================
    // DEF INJECTION & HARMONY PATCHES
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class EquipmentSlotsBootstrap
    {
        private static bool s_applyingModDamage;

        static EquipmentSlotsBootstrap()
        {
            InjectDefs();
            ApplyHarmonyPatches();
        }

        private static void InjectDefs()
        {
            ApparelLayerDef powerArmorLayer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("TMM_PowerArmor");

            // 1. Register StatParts for apparel mods on Sharp, Blunt, Heat armor
            RegisterArmorStatPart(StatDefOf.ArmorRating_Sharp);
            RegisterArmorStatPart(StatDefOf.ArmorRating_Blunt);
            RegisterArmorStatPart(StatDefOf.ArmorRating_Heat);

            // 2. Inject CompWeaponMods, CompApparelMods, CompColtAmmo, PowerArmor layer, and ITab_TMM_Equipment
            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (def == null) continue;

                // Power armor layer migration
                if (powerArmorLayer != null && EquipmentUtil.IsPowerArmorDef(def))
                {
                    if (def.apparel.layers == null) def.apparel.layers = new List<ApparelLayerDef>();
                    if (!def.apparel.layers.Contains(powerArmorLayer))
                    {
                        def.apparel.layers.Remove(ApparelLayerDefOf.Shell);
                        def.apparel.layers.Add(powerArmorLayer);
                    }
                }

                // Universal weapon mod slots
                if (EquipmentUtil.IsWeaponModEligible(def))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (def.GetCompProperties<CompProperties_WeaponMods>() == null)
                    {
                        CompProperties_WeaponMods cp = new CompProperties_WeaponMods();
                        cp.maxSlots = EquipmentUtil.DefaultWeaponSlotCount(def);
                        def.comps.Add(cp);
                    }
                }

                // Universal coat / jacket / duster apparel mod slot
                if (EquipmentUtil.IsApparelModEligible(def))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (def.GetCompProperties<CompProperties_ApparelMods>() == null)
                    {
                        CompProperties_ApparelMods ap = new CompProperties_ApparelMods();
                        ap.maxSlots = 1;
                        def.comps.Add(ap);
                    }
                }

                // Guarantee Colt ammo comp on The Colt
                if (def.defName == "TMM_Legendary_TheColt")
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (def.GetCompProperties<CompProperties_ColtAmmo>() == null)
                    {
                        def.comps.Add(new CompProperties_ColtAmmo());
                    }
                }

                // Guarantee additive Equipment ITab on all humanlike races (without touching ITab_Pawn_Gear)
                if (def.race != null && def.race.Humanlike)
                {
                    if (def.inspectorTabs == null) def.inspectorTabs = new List<Type>();
                    if (!def.inspectorTabs.Contains(typeof(ITab_TMM_Equipment)))
                    {
                        def.inspectorTabs.Add(typeof(ITab_TMM_Equipment));
                    }
                    if (def.inspectorTabsResolved != null)
                    {
                        bool hasResolved = false;
                        for (int t = 0; t < def.inspectorTabsResolved.Count; t++)
                        {
                            if (def.inspectorTabsResolved[t] is ITab_TMM_Equipment)
                            {
                                hasResolved = true;
                                break;
                            }
                        }
                        if (!hasResolved)
                        {
                            def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_TMM_Equipment))); // CHECK API
                        }
                    }
                }
            }
        }

        private static void RegisterArmorStatPart(StatDef stat)
        {
            if (stat == null) return;
            if (stat.parts == null) stat.parts = new List<StatPart>();
            for (int i = 0; i < stat.parts.Count; i++)
            {
                if (stat.parts[i] is StatPart_ApparelMods) return;
            }
            stat.parts.Add(new StatPart_ApparelMods(stat));
        }

        private static void ApplyHarmonyPatches()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.equipment");

            // 1. Extra weapon module damage & charge consumption on hit
            MethodInfo postApply = AccessTools.Method(typeof(Pawn), "PostApplyDamage", new Type[] { typeof(DamageInfo), typeof(float) }); // CHECK API
            if (postApply != null)
            {
                harmony.Patch(postApply, postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_Pawn_PostApplyDamage"));
            }

            // 2. Caravan carrying capacity boost from TMM_Backpack apparel
            MethodInfo massCap = AccessTools.Method(typeof(MassUtility), "Capacity", new Type[] { typeof(Pawn), typeof(StringBuilder) }); // CHECK API
            if (massCap != null)
            {
                harmony.Patch(massCap, postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_MassUtility_Capacity"));
            }

            // 3. Story gate for TMM_ColtBullets research (unlock.colt_bullets)
            PropertyInfo canStartProp = AccessTools.Property(typeof(ResearchProjectDef), "CanStartNow"); // CHECK API
            if (canStartProp != null && canStartProp.GetGetMethod() != null)
            {
                harmony.Patch(canStartProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_Research_CanStartNow"));
            }

            // 4. Story gate for Railroad Ballistic Weave recipe (unlock.railroad_ballistic)
            PropertyInfo availProp = AccessTools.Property(typeof(RecipeDef), "AvailableNow"); // CHECK API
            if (availProp != null && availProp.GetGetMethod() != null)
            {
                harmony.Patch(availProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_Recipe_AvailableNow"));
            }

            // 5. Right-click FloatMenu options for module items & Colt silver bullets on map
            MethodInfo floatMenu = AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"); // CHECK API
            if (floatMenu != null)
            {
                harmony.Patch(floatMenu, postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_AddHumanlikeOrders"));
            }

            // 6. Fallback Colt ammo consumption if CompColtAmmo runs standalone without CompLimitedCharges
            MethodInfo tryCastShot = AccessTools.Method(typeof(Verb_Shoot), "TryCastShot");
            if (tryCastShot != null)
            {
                harmony.Patch(tryCastShot,
                    prefix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Prefix_VerbShoot_ColtAmmo"),
                    postfix: new HarmonyMethod(typeof(EquipmentSlotsBootstrap), "Postfix_VerbShoot_ColtAmmo"));
            }
        }

        public static void Postfix_Pawn_PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (s_applyingModDamage) return;
            if (__instance == null || __instance.Dead) return;
            if (dinfo.Def == null || !dinfo.Def.externalViolence) return;

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return;

            ThingWithComps weapon = FindAttackerWeapon(attacker, dinfo.Weapon);
            if (weapon == null) return;

            CompWeaponMods mods = weapon.GetComp<CompWeaponMods>();
            if (mods == null || mods.InstalledMods.Count == 0) return;

            s_applyingModDamage = true;
            try
            {
                for (int i = 0; i < mods.InstalledMods.Count; i++)
                {
                    InstalledModEntry entry = mods.InstalledMods[i];
                    if (entry == null || !entry.CanTrigger) continue;

                    CompProperties_EquipmentMod mp = entry.ModProps;
                    if (mp == null || mp.extraDamageDef.NullOrEmpty() || mp.extraDamageAmount <= 0f) continue;

                    DamageDef extraDef = DefDatabase<DamageDef>.GetNamedSilentFail(mp.extraDamageDef) ?? DamageDefOf.Burn;
                    entry.ConsumeCharge();

                    if (!__instance.Dead)
                    {
                        DamageInfo extraInfo = new DamageInfo(extraDef, mp.extraDamageAmount, mp.extraArmorPenetration,
                            dinfo.Angle, attacker, dinfo.HitPart, dinfo.Weapon);
                        __instance.TakeDamage(extraInfo);
                    }
                }
            }
            finally
            {
                s_applyingModDamage = false;
            }
        }

        private static ThingWithComps FindAttackerWeapon(Pawn attacker, ThingDef weaponDef)
        {
            if (attacker == null) return null;
            if (attacker.equipment != null && attacker.equipment.Primary != null)
            {
                if (weaponDef == null || attacker.equipment.Primary.def == weaponDef)
                {
                    return attacker.equipment.Primary;
                }
            }
            if (weaponDef != null && attacker.inventory != null && attacker.inventory.innerContainer != null)
            {
                for (int i = 0; i < attacker.inventory.innerContainer.Count; i++)
                {
                    ThingWithComps twc = attacker.inventory.innerContainer[i] as ThingWithComps;
                    if (twc != null && twc.def == weaponDef) return twc;
                }
            }
            return null;
        }

        public static void Postfix_MassUtility_Capacity(Pawn p, StringBuilder explanation, ref float __result)
        {
            if (p == null || p.apparel == null) return;
            List<Apparel> worn = p.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel ap = worn[i];
                if (ap == null || ap.def == null) continue;
                BackpackExtension ext = ap.def.GetModExtension<BackpackExtension>();
                if (ext != null && ext.bonusCaravanKg > 0f)
                {
                    __result += ext.bonusCaravanKg;
                    if (explanation != null)
                    {
                        explanation.AppendLine(string.Format("  - {0}: +{1:0.#} kg", ap.LabelCap, ext.bonusCaravanKg));
                    }
                }
            }
        }

        public static void Postfix_Research_CanStartNow(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            if (__instance.defName == "TMM_ColtBullets")
            {
                if (!EquipmentUtil.HasStoryFlag("unlock.colt_bullets"))
                {
                    __result = false;
                }
            }
        }

        public static void Postfix_Recipe_AvailableNow(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            if (__instance.defName == "TMM_Make_RailroadBallisticWeave")
            {
                if (!EquipmentUtil.HasStoryFlag("unlock.railroad_ballistic"))
                {
                    __result = false;
                }
            }
        }

        public static void Postfix_AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn == null || opts == null || pawn.Map == null || !pawn.IsColonistPlayerControlled) return;
            IntVec3 cell = IntVec3.FromVector3(clickPos);
            if (!cell.InBounds(pawn.Map)) return;

            List<Thing> things = cell.GetThingList(pawn.Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (t == null || t.Destroyed) continue;

                // Option 1: Install clicked module onto equipped primary weapon or Shell apparel
                CompEquipmentMod modComp = t.TryGetComp<CompEquipmentMod>();
                if (modComp != null)
                {
                    if (modComp.Props.modKind == EquipmentModKind.Weapon && pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps w = pawn.equipment.Primary;
                        CompWeaponMods wm = w.GetComp<CompWeaponMods>();
                        string fail;
                        if (wm != null && wm.CanInstall(t, out fail))
                        {
                            Thing capturedMod = t;
                            opts.Add(new FloatMenuOption(string.Format("TMM_Float_InstallOn".Translate(), capturedMod.LabelShortCap, w.LabelShortCap), delegate
                            {
                                ITab_TMM_Equipment.QueueInstallModJob(pawn, w, capturedMod);
                            }));
                        }
                    }
                    else if (modComp.Props.modKind == EquipmentModKind.Apparel && pawn.apparel != null)
                    {
                        List<Apparel> worn = pawn.apparel.WornApparel;
                        for (int a = 0; a < worn.Count; a++)
                        {
                            Apparel ap = worn[a];
                            CompApparelMods am = ap != null ? ap.GetComp<CompApparelMods>() : null;
                            string fail;
                            if (am != null && am.CanInstall(t, out fail))
                            {
                                Thing capturedMod = t;
                                Apparel capturedAp = ap;
                                opts.Add(new FloatMenuOption(string.Format("TMM_Float_InstallOn".Translate(), capturedMod.LabelShortCap, capturedAp.LabelShortCap), delegate
                                {
                                    ITab_TMM_Equipment.QueueInstallModJob(pawn, capturedAp, capturedMod);
                                }));
                            }
                        }
                    }
                }

                // Option 2: Reload Colt from clicked Silver Colt Bullets
                if (t.def != null && t.def.defName == "TMM_Ammo_ColtSilverBullet" && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    ThingWithComps colt = pawn.equipment.Primary;
                    CompColtAmmo coltAmmo = colt.GetComp<CompColtAmmo>();
                    if (coltAmmo != null && coltAmmo.Charges < coltAmmo.MaxCharges)
                    {
                        Thing capturedAmmo = t;
                        opts.Add(new FloatMenuOption(string.Format("TMM_ReloadColtGizmo".Translate(), coltAmmo.Charges, coltAmmo.MaxCharges), delegate
                        {
                            JobDef jd = DefDatabase<JobDef>.GetNamedSilentFail("TMM_ReloadColt");
                            if (jd != null)
                            {
                                Job j = JobMaker.MakeJob(jd, colt, capturedAmmo);
                                j.count = coltAmmo.MaxCharges - coltAmmo.Charges;
                                pawn.jobs.TryTakeOrderedJob(j, JobTag.Misc);
                            }
                        }));
                    }
                }
            }
        }

        public static bool Prefix_VerbShoot_ColtAmmo(Verb_Shoot __instance, ref bool __result)
        {
            ThingWithComps eq = __instance.EquipmentSource;
            if (eq == null) return true;
            CompColtAmmo ammo = eq.GetComp<CompColtAmmo>();
            if (ammo == null || ammo.HasCompLimitedCharges()) return true;
            if (ammo.Charges <= 0)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static void Postfix_VerbShoot_ColtAmmo(Verb_Shoot __instance, bool __result)
        {
            if (!__result) return;
            ThingWithComps eq = __instance.EquipmentSource;
            if (eq == null) return;
            CompColtAmmo ammo = eq.GetComp<CompColtAmmo>();
            if (ammo == null || ammo.HasCompLimitedCharges()) return;
            ammo.ConsumeRound();
        }
    }
}
```

`Languages/English/Keyed/Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_TabEquipment>Equipment</TMM_TabEquipment>

  <TMM_Slot_Head>Head</TMM_Slot_Head>
  <TMM_Slot_Eyes>Eyes</TMM_Slot_Eyes>
  <TMM_Slot_Skin>Skin</TMM_Slot_Skin>
  <TMM_Slot_Middle>Middle</TMM_Slot_Middle>
  <TMM_Slot_Shell>Shell</TMM_Slot_Shell>
  <TMM_Slot_Legs>Legs</TMM_Slot_Legs>
  <TMM_Slot_Belt>Belt</TMM_Slot_Belt>
  <TMM_Slot_Backpack>Backpack</TMM_Slot_Backpack>
  <TMM_Slot_PowerArmor>Power Armor</TMM_Slot_PowerArmor>
  <TMM_Slot_Primary>Primary</TMM_Slot_Primary>
  <TMM_Slot_Sidearms>Sidearms</TMM_Slot_Sidearms>
  <TMM_Slot_Backup>Backup</TMM_Slot_Backup>

  <TMM_Stat_Carry>Carry</TMM_Stat_Carry>
  <TMM_Stat_Move>Speed</TMM_Stat_Move>
  <TMM_Stat_Sharp>Sharp Armor</TMM_Stat_Sharp>
  <TMM_Stat_Blunt>Blunt Armor</TMM_Stat_Blunt>

  <TMM_WeaponModsEmpty>Module slots: 0/{0}</TMM_WeaponModsEmpty>
  <TMM_WeaponModsPrefix>Modules:</TMM_WeaponModsPrefix>
  <TMM_ApparelModsPrefix>Lining:</TMM_ApparelModsPrefix>
  <TMM_ApparelModStatLine>Installed lining: +{0}</TMM_ApparelModStatLine>
  <TMM_EmptyModSlotTip>Empty module slot (click to install)</TMM_EmptyModSlotTip>

  <TMM_Menu_Info>Info</TMM_Menu_Info>
  <TMM_Menu_Unequip>Unequip</TMM_Menu_Unequip>
  <TMM_Menu_InstallMod>Install {0}</TMM_Menu_InstallMod>
  <TMM_Menu_RemoveMod>Remove module</TMM_Menu_RemoveMod>
  <TMM_NoModsAvailable>No compatible modules available</TMM_NoModsAvailable>
  <TMM_Float_InstallOn>Install {0} onto {1}</TMM_Float_InstallOn>

  <TMM_ColtAmmoInspect>Silver bullets: {0} / {1}</TMM_ColtAmmoInspect>
  <TMM_ReloadColtGizmo>Reload Colt ({0}/{1})</TMM_ReloadColtGizmo>
  <TMM_ReloadColtDesc>Reload The Colt up to 13 rounds using enchanted Colt silver bullets from inventory or stockpiles.</TMM_ReloadColtDesc>
  <TMM_NoColtBullets>No Colt silver bullets available in inventory or stockpiles.</TMM_NoColtBullets>

</LanguageData>
```

`Languages/Russian/Keyed/Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_TabEquipment>Снаряжение</TMM_TabEquipment>

  <TMM_Slot_Head>Голова</TMM_Slot_Head>
  <TMM_Slot_Eyes>Глаза</TMM_Slot_Eyes>
  <TMM_Slot_Skin>Кожа</TMM_Slot_Skin>
  <TMM_Slot_Middle>Средний</TMM_Slot_Middle>
  <TMM_Slot_Shell>Верх</TMM_Slot_Shell>
  <TMM_Slot_Legs>Ноги</TMM_Slot_Legs>
  <TMM_Slot_Belt>Пояс</TMM_Slot_Belt>
  <TMM_Slot_Backpack>Рюкзак</TMM_Slot_Backpack>
  <TMM_Slot_PowerArmor>Сил. броня</TMM_Slot_PowerArmor>
  <TMM_Slot_Primary>Основное</TMM_Slot_Primary>
  <TMM_Slot_Sidearms>Запасное оружие</TMM_Slot_Sidearms>
  <TMM_Slot_Backup>Запас</TMM_Slot_Backup>

  <TMM_Stat_Carry>Груз</TMM_Stat_Carry>
  <TMM_Stat_Move>Скорость</TMM_Stat_Move>
  <TMM_Stat_Sharp>Защита (остр.)</TMM_Stat_Sharp>
  <TMM_Stat_Blunt>Защита (туп.)</TMM_Stat_Blunt>

  <TMM_WeaponModsEmpty>Слоты модулей: 0/{0}</TMM_WeaponModsEmpty>
  <TMM_WeaponModsPrefix>Модули:</TMM_WeaponModsPrefix>
  <TMM_ApparelModsPrefix>Улучшение:</TMM_ApparelModsPrefix>
  <TMM_ApparelModStatLine>Установленный модуль: +{0}</TMM_ApparelModStatLine>
  <TMM_EmptyModSlotTip>Пустой слот модуля (нажмите для установки)</TMM_EmptyModSlotTip>

  <TMM_Menu_Info>Информация</TMM_Menu_Info>
  <TMM_Menu_Unequip>Снять</TMM_Menu_Unequip>
  <TMM_Menu_InstallMod>Установить {0}</TMM_Menu_InstallMod>
  <TMM_Menu_RemoveMod>Снять модуль</TMM_Menu_RemoveMod>
  <TMM_NoModsAvailable>Нет подходящих модулей</TMM_NoModsAvailable>
  <TMM_Float_InstallOn>Установить {0} на {1}</TMM_Float_InstallOn>

  <TMM_ColtAmmoInspect>Серебряные пули: {0} / {1}</TMM_ColtAmmoInspect>
  <TMM_ReloadColtGizmo>Зарядить Кольт ({0}/{1})</TMM_ReloadColtGizmo>
  <TMM_ReloadColtDesc>Дозарядить «Кольт» серебряными пулями из инвентаря или со складов до 13 патронов.</TMM_ReloadColtDesc>
  <TMM_NoColtBullets>Нет серебряных пуль Кольта в инвентаре или на складах.</TMM_NoColtBullets>

</LanguageData>
```

`Languages/Russian/DefInjected/ApparelLayerDef/Layers_Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Backpack.label>рюкзак</TMM_Backpack.label>
  <TMM_PowerArmor.label>силовая броня</TMM_PowerArmor.label>

</LanguageData>
```

`Languages/Russian/DefInjected/ThingDef/Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- Рюкзаки -->
  <TMM_Backpack_Canvas.label>холщовый рюкзак</TMM_Backpack_Canvas.label>
  <TMM_Backpack_Canvas.description>Прочный сшитый из холстины рюкзак с кожаными лямками. Умеренно увеличивает переносимый вес ценой небольшого замедления.</TMM_Backpack_Canvas.description>

  <TMM_Backpack_Military.label>армейский рюкзак</TMM_Backpack_Military.label>
  <TMM_Backpack_Military.description>Усиленный тактический рюкзак с внутренним каркасом и креплениями. Значительно повышает грузоподъёмность ценой небольшого снижения скорости.</TMM_Backpack_Military.description>

  <TMM_Backpack_Power.label>силовой рюкзак</TMM_Backpack_Power.label>
  <TMM_Backpack_Power.description>Довоенный разгрузочный ранец из пластали с микросервоприводами. Огромная прибавка к переносимому весу при минимальном штрафе к скорости.</TMM_Backpack_Power.description>

  <!-- Модули оружия -->
  <TMM_Mod_HolyWaterTank.label>баллон святой воды</TMM_Mod_HolyWaterTank.label>
  <TMM_Mod_HolyWaterTank.description>Подствольный баллон под давлением с освящённой водой. Наносит дополнительный урон святой водой в течение 30 атак, сжигая демонов.</TMM_Mod_HolyWaterTank.description>

  <TMM_Mod_BoraxTank.label>баллон буры</TMM_Mod_BoraxTank.label>
  <TMM_Mod_BoraxTank.description>Компактный резервуар с раствором тетрабората натрия. На 30 атак добавляет урон бурой, пробивая уязвимость левиафанов и снимая неуязвимость Дика Романа.</TMM_Mod_BoraxTank.description>

  <TMM_Mod_RockSaltPack.label>соляной заряд</TMM_Mod_RockSaltPack.label>
  <TMM_Mod_RockSaltPack.description>Дополнительный магазин охотника с кристаллами каменной соли. Добавляет урон каменной солью на 30 атак против демонов и духов.</TMM_Mod_RockSaltPack.description>

  <TMM_Mod_SilverEngraving.label>серебрёная гравировка</TMM_Mod_SilverEngraving.label>
  <TMM_Mod_SilverEngraving.description>Тонкая вязка енохианских рун из серебра и небесной бронзы для нанесения на любое оружие. Навсегда добавляет енохианский урон без расхода зарядов, поражая уязвимость ангелов.</TMM_Mod_SilverEngraving.description>

  <!-- Модуль одежды и патроны Кольта -->
  <TMM_Mod_RailroadBallisticWeave.label>баллистическое волокно Подземки</TMM_Mod_RailroadBallisticWeave.label>
  <TMM_Mod_RailroadBallisticWeave.description>Сверхплотная синтетическая подкладка Техника Тома, вшиваемая в плащ, куртку или дастер. Даёт +25% к защите от режущего урона без штрафа к скорости. Открывается по сюжету Подземки.</TMM_Mod_RailroadBallisticWeave.description>

  <TMM_Ammo_ColtSilverBullet.label>серебряная пуля Кольта</TMM_Ammo_ColtSilverBullet.label>
  <TMM_Ammo_ColtSilverBullet.description>Отлитый вручную и испещрённый рунами серебряный патрон для легендарного револьвера Сэмюэля Кольта. Крайне дорог в изготовлении; используется для дозарядки «Кольта» до 13 выстрелов.</TMM_Ammo_ColtSilverBullet.description>

</LanguageData>
```

`Languages/Russian/DefInjected/RecipeDef/Equipment_Recipes.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Make_RailroadBallisticWeave.label>изготовить баллистическое волокно Подземки</TMM_Make_RailroadBallisticWeave.label>
  <TMM_Make_RailroadBallisticWeave.description>Соткать сверхплотную баллистическую подкладку (+25% к защите от острого урона) для плащей, курток и дастеров.</TMM_Make_RailroadBallisticWeave.description>
  <TMM_Make_RailroadBallisticWeave.jobString>Ткёт баллистическое волокно.</TMM_Make_RailroadBallisticWeave.jobString>

  <TMM_Make_ColtSilverBullet.label>отлить серебряную пулю Кольта</TMM_Make_ColtSilverBullet.label>
  <TMM_Make_ColtSilverBullet.description>Отлить и зачаровать одну серебряную пулю для легендарного «Кольта».</TMM_Make_ColtSilverBullet.description>
  <TMM_Make_ColtSilverBullet.jobString>Отливает серебряную пулю Кольта.</TMM_Make_ColtSilverBullet.jobString>

  <TMM_Refill_HolyWaterTank.label>перезаправить баллон святой воды</TMM_Refill_HolyWaterTank.label>
  <TMM_Refill_HolyWaterTank.description>Восполнить опустевший баллон святой воды до 30 зарядов.</TMM_Refill_HolyWaterTank.description>
  <TMM_Refill_HolyWaterTank.jobString>Перезаправляет баллон святой воды.</TMM_Refill_HolyWaterTank.jobString>

  <TMM_Refill_BoraxTank.label>перезаправить баллон буры</TMM_Refill_BoraxTank.label>
  <TMM_Refill_BoraxTank.description>Восполнить опустевший баллон буры до 30 зарядов.</TMM_Refill_BoraxTank.description>
  <TMM_Refill_BoraxTank.jobString>Перезаправляет баллон буры.</TMM_Refill_BoraxTank.jobString>

  <TMM_Refill_RockSaltPack.label>перезарядить соляной заряд</TMM_Refill_RockSaltPack.label>
  <TMM_Refill_RockSaltPack.description>Восполнить опустевший соляной заряд до 30 зарядов.</TMM_Refill_RockSaltPack.description>
  <TMM_Refill_RockSaltPack.jobString>Перезаряжает соляной заряд.</TMM_Refill_RockSaltPack.jobString>

</LanguageData>
```

`Languages/Russian/DefInjected/ResearchProjectDef/Research_Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_PowerBackpackResearch.label>силовые рюкзаки</TMM_PowerBackpackResearch.label>
  <TMM_PowerBackpackResearch.description>Разработка разгрузочных ранцев из пластали с микросервоприводами, резко увеличивающих переносимый вес с минимальной потерей скорости.</TMM_PowerBackpackResearch.description>

  <TMM_ColtBullets.label>серебряные пули Кольта</TMM_ColtBullets.label>
  <TMM_ColtBullets.description>Освоение древнего оккультно-металлургического ритуала Сэмюэля Кольта для отливки сменных серебряных патронов к «Кольту». Открывается по сюжету охотников.</TMM_ColtBullets.description>

</LanguageData>
```

`Languages/Russian/DefInjected/JobDef/Jobs_Equipment.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_InstallEquipmentMod.reportString>устанавливает модуль на TargetA.</TMM_InstallEquipmentMod.reportString>
  <TMM_RemoveEquipmentMod.reportString>снимает модуль с TargetA.</TMM_RemoveEquipmentMod.reportString>
  <TMM_ReloadColt.reportString>перезаряжает «Кольт» серебряными пулями.</TMM_ReloadColt.reportString>

</LanguageData>
```

`Tests/InGameEquipmentProbe.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    [StaticConstructorOnStartup]
    public static class EquipmentProbeBootstrap
    {
        public const string SaveName = "TMM-Equipment";

        public static bool FreshMode
        {
            get { return HasArg("-tmm-equipment-probe"); }
        }

        public static bool ReloadMode
        {
            get { return HasArg("-tmm-equipment-reload"); }
        }

        static EquipmentProbeBootstrap()
        {
            if (!FreshMode && !ReloadMode) return;

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (ReloadMode && Current.ProgramState == ProgramState.Entry)
                {
                    GameDataSaveLoader.LoadGame(SaveName); // CHECK API
                }
            });
        }

        private static bool HasArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// In-game verification probe for EquipmentSlots:
    /// - Backpack increases CarryingCapacity &amp; MassUtility.Capacity;
    /// - Real JobDriver_InstallEquipmentMod installs Holy Water Tank onto rifle;
    /// - Shooting TMM_Demon deals more damage than shooting human &amp; consumes tank charges;
    /// - Real JobDriver_RemoveEquipmentMod returns the module item with preserved charges;
    /// - Railroad Ballistic Weave on duster boosts ArmorRating_Sharp by +0.25;
    /// - ITab_TMM_Equipment draws cleanly inside a real EventType.Repaint pass;
    /// - Save (TMM-Equipment) and reload (-tmm-equipment-reload) preserve installed mods &amp; charges.
    /// </summary>
    public sealed class InGameEquipmentProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private const string ProbeColonistNick = "EqProbeHero";

        private static bool s_logicDone;
        private static bool s_finishedAll;
        private int m_ticksWaited;
        private bool m_waitingForRepaint;
        private Pawn m_guiTestPawn;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        private static readonly MethodInfo TryCastShotMethod = AccessTools.Method(typeof(Verb), "TryCastShot"); // CHECK API
        private static readonly MethodInfo ProjectileImpactMethod = AccessTools.Method(typeof(Projectile), "Impact", new Type[] { typeof(Thing), typeof(bool) }); // CHECK API

        public InGameEquipmentProbe(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (s_finishedAll) return;
            if (!EquipmentProbeBootstrap.FreshMode && !EquipmentProbeBootstrap.ReloadMode) return;
            if (map != Find.AnyPlayerHomeMap) return;

            if (!s_logicDone)
            {
                m_ticksWaited++;
                if (m_ticksWaited < 30) return;

                s_logicDone = true;
                Application.logMessageReceived += OnUnityLog;

                try
                {
                    if (EquipmentProbeBootstrap.FreshMode)
                    {
                        RunFreshLogic();
                        m_waitingForRepaint = true;
                    }
                    else if (EquipmentProbeBootstrap.ReloadMode)
                    {
                        RunReloadLogic();
                        FinishAndShutdown("EquipmentReloadProbe.json", false);
                    }
                }
                catch (Exception ex)
                {
                    m_errors.Add("Unhandled exception: " + ex.ToString());
                    FinishAndShutdown(EquipmentProbeBootstrap.ReloadMode ? "EquipmentReloadProbe.json" : "EquipmentProbe.json", false);
                }
            }
        }

        public override void MapComponentOnGUI()
        {
            if (!m_waitingForRepaint || s_finishedAll) return;
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            m_waitingForRepaint = false;
            bool repaintOk = false;
            try
            {
                if (m_guiTestPawn != null)
                {
                    ITab_TMM_Equipment tab = new ITab_TMM_Equipment();
                    tab.DebugDrawInOnGUI(new Rect(20f, 20f, 540f, 560f), m_guiTestPawn);
                    repaintOk = true;
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("ITab Repaint exception: " + ex.ToString());
            }

            AddCheck("ITab_RepaintNoExceptions", repaintOk, "true", repaintOk.ToString());
            FinishAndShutdown("EquipmentProbe.json", true);
        }

        private void FinishAndShutdown(string reportFile, bool saveBeforeQuit)
        {
            if (s_finishedAll) return;
            s_finishedAll = true;

            try
            {
                if (saveBeforeQuit)
                {
                    GameDataSaveLoader.SaveGame(EquipmentProbeBootstrap.SaveName); // CHECK API
                }
            }
            catch (Exception ex)
            {
                m_errors.Add("SaveGame exception: " + ex.ToString());
            }
            finally
            {
                Application.logMessageReceived -= OnUnityLog;
                WriteReport(reportFile);
                Root.Shutdown();
            }
        }

        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                m_errors.Add(type.ToString() + ": " + condition);
            }
        }

        private void RunFreshLogic()
        {
            IntVec3 origin = FindClearCenter();

            // Create workbench adjacent to origin so real JobDrivers run end-to-end
            ThingDef benchDef = DefDatabase<ThingDef>.GetNamedSilentFail("TableMachining");
            Building bench = null;
            if (benchDef != null)
            {
                Thing bThing = ThingMaker.MakeThing(benchDef, benchDef.MadeFromStuff ? ThingDefOf.Steel : null);
                bThing.SetFaction(Faction.OfPlayer);
                bench = GenSpawn.Spawn(bThing, origin + new IntVec3(2, 0, 0), map, WipeMode.Vanish) as Building;
            }

            Pawn hero = SpawnPawn(null, Faction.OfPlayer, origin);
            hero.Name = new NameTriple("Equipment", ProbeColonistNick, "Tester");
            m_guiTestPawn = hero;

            // ---------------- 1. BACKPACK INCREASES CARRYING CAPACITY ----------------
            float carryBefore = hero.GetStatValue(StatDefOf.CarryingCapacity);
            float caravanBefore = MassUtility.Capacity(hero); // CHECK API

            Apparel backpack = WearApparel(hero, "TMM_Backpack_Military");
            float carryAfter = hero.GetStatValue(StatDefOf.CarryingCapacity);
            float caravanAfter = MassUtility.Capacity(hero); // CHECK API

            AddCheck("Backpack_IncreasesCarryingCapacity",
                backpack != null && carryAfter > carryBefore + 50f && caravanAfter > caravanBefore + 30f,
                string.Format("carry>{0:0}, kg>{1:0}", carryBefore + 50f, caravanBefore + 30f),
                string.Format("carry={0:0}->{1:0}, kg={2:0}->{3:0}", carryBefore, carryAfter, caravanBefore, caravanAfter));

            // ---------------- 2. REAL JOB: INSTALL HOLY WATER TANK ON RIFLE ----------------
            ThingWithComps rifle = EquipWeapon(hero, "Gun_AssaultRifle");
            ThingDef tankDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Mod_HolyWaterTank");
            Thing tank = tankDef != null ? GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(-1, 0, 0), map, WipeMode.Vanish) : null;
            if (tank != null) tank.SetForbidden(false, false);

            ITab_TMM_Equipment.QueueInstallModJob(hero, rifle, tank);
            bool installJobDone = RunPawnJobToCompletion(hero, 4000);

            CompWeaponMods rifleMods = rifle != null ? rifle.GetComp<CompWeaponMods>() : null;
            bool tankInstalled = rifleMods != null && rifleMods.InstalledMods.Count == 1 &&
                                 rifleMods.InstalledMods[0].modDefName == "TMM_Mod_HolyWaterTank" &&
                                 rifleMods.InstalledMods[0].charges == 30;
            AddCheck("WeaponMod_InstallJobSucceeded", installJobDone && tankInstalled,
                "jobDone=true, mod=HolyWaterTank[30]",
                string.Format("jobDone={0}, count={1}, charges={2}",
                    installJobDone,
                    rifleMods != null ? rifleMods.InstalledMods.Count : -1,
                    (rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1));

            // ---------------- 3. SHOOT DEMON VS HUMAN (HOLY WATER WEAKNESS + CHARGE LOSS) ----------------
            Pawn demon = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(6, 0, 0));
            Pawn human = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(6, 0, 2));

            FireWeaponOnceAt(hero, rifle, demon);
            FireWeaponOnceAt(hero, rifle, human);

            float demonDmg = SumInjuries(demon);
            float humanDmg = SumInjuries(human);
            int chargesAfter2Shots = (rifleMods != null && rifleMods.InstalledMods.Count > 0) ? rifleMods.InstalledMods[0].charges : -1;

            bool demonTookMore = demon != null && human != null && (demon.Dead || demonDmg > humanDmg + 4f);
            AddCheck("WeaponMod_HolyWaterHurtsDemonMoreThanHuman", demonTookMore,
                "demonDmg > humanDmg + 4",
                string.Format("demon={0:0.0}(dead={1}), human={2:0.0}", demonDmg, demon != null && demon.Dead, humanDmg));
            AddCheck("WeaponMod_ChargesDecreased", chargesAfter2Shots == 28, "28", chargesAfter2Shots.ToString());

            // ---------------- 4. REAL JOB: REMOVE MOD RETURNS ITEM WITH PRESERVED CHARGES ----------------
            ThingWithComps secondRifle = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDef.Named("Gun_AssaultRifle")), origin + new IntVec3(0, 0, 2), map, WipeMode.Vanish) as ThingWithComps;
            Thing secondTank = tankDef != null ? GenSpawn.Spawn(ThingMaker.MakeThing(tankDef), origin + new IntVec3(-1, 0, 2), map, WipeMode.Vanish) : null;
            if (secondTank != null)
            {
                secondTank.SetForbidden(false, false);
                CompEquipmentMod c = secondTank.TryGetComp<CompEquipmentMod>();
                if (c != null) c.Charges = 19;
            }

            CompWeaponMods secondMods = secondRifle != null ? secondRifle.GetComp<CompWeaponMods>() : null;
            if (secondMods != null && secondTank != null) secondMods.InstallMod(secondTank);

            int tanksBeforeRemove = map.listerThings.ThingsOfDef(tankDef).Count;
            ITab_TMM_Equipment.QueueRemoveModJob(hero, secondRifle, 0);
            bool removeJobDone = RunPawnJobToCompletion(hero, 4000);

            List<Thing> tanksAfter = map.listerThings.ThingsOfDef(tankDef);
            int returnedCharges = -1;
            if (tanksAfter.Count > tanksBeforeRemove)
            {
                CompEquipmentMod rc = tanksAfter[tanksAfter.Count - 1].TryGetComp<CompEquipmentMod>();
                if (rc != null) returnedCharges = rc.Charges;
            }
            AddCheck("WeaponMod_RemoveJobReturnsItem",
                removeJobDone && secondMods != null && secondMods.InstalledMods.Count == 0 && returnedCharges == 19,
                "removed & returned charges=19",
                string.Format("jobDone={0}, remaining={1}, returnedCharges={2}",
                    removeJobDone, secondMods != null ? secondMods.InstalledMods.Count : -1, returnedCharges));

            // ---------------- 5. RAILROAD BALLISTIC WEAVE ON DUSTER (+0.25 SHARP ARMOR) ----------------
            Apparel duster = WearApparel(hero, "Apparel_Duster");
            float sharpBefore = duster != null ? duster.GetStatValue(StatDefOf.ArmorRating_Sharp) : 0f;

            ThingDef weaveDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Mod_RailroadBallisticWeave");
            Thing weave = weaveDef != null ? GenSpawn.Spawn(ThingMaker.MakeThing(weaveDef), origin + new IntVec3(-1, 0, -1), map, WipeMode.Vanish) : null;
            if (weave != null) weave.SetForbidden(false, false);

            ITab_TMM_Equipment.QueueInstallModJob(hero, duster, weave);
            bool weaveJobDone = RunPawnJobToCompletion(hero, 4000);

            float sharpAfter = duster != null ? duster.GetStatValue(StatDefOf.ArmorRating_Sharp) : 0f;
            AddCheck("ApparelMod_BallisticWeaveBoostsArmor",
                weaveJobDone && duster != null && (sharpAfter - sharpBefore) >= 0.24f,
                string.Format("+{0:0.00} sharp", 0.25f),
                string.Format("jobDone={0}, sharp={1:0.00}->{2:0.00}", weaveJobDone, sharpBefore, sharpAfter));

            // ---------------- 6. EQUIP POWER ARMOR FOR ITAB REPAINT TEST (OVER BACKPACK & DUSTER) ----------------
            Apparel powerArmor = WearApparel(hero, "TMM_Legendary_ProtectorArmor");
            if (powerArmor == null) powerArmor = WearApparel(hero, "Apparel_PowerArmor");
            bool wearingAllThree = hero.apparel.WornApparel.Contains(backpack) &&
                                   hero.apparel.WornApparel.Contains(duster) &&
                                   (powerArmor != null && hero.apparel.WornApparel.Contains(powerArmor));
            AddCheck("Layers_BackpackAndPowerArmorCoexist", wearingAllThree, "all 3 worn simultaneously", wearingAllThree.ToString());
        }

        private void RunReloadLogic()
        {
            Pawn hero = null;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                if (colonists[i] != null && colonists[i].Name != null && colonists[i].Name.ToStringShort == ProbeColonistNick)
                {
                    hero = colonists[i];
                    break;
                }
            }

            AddCheck("Reload_HeroFound", hero != null, "non-null", hero != null ? hero.LabelShort : "null");
            if (hero == null) return;

            ThingWithComps rifle = hero.equipment != null ? hero.equipment.Primary : null;
            CompWeaponMods rifleMods = rifle != null ? rifle.GetComp<CompWeaponMods>() : null;
            bool weaponModPersisted = rifleMods != null && rifleMods.InstalledMods.Count == 1 &&
                                      rifleMods.InstalledMods[0].modDefName == "TMM_Mod_HolyWaterTank" &&
                                      rifleMods.InstalledMods[0].charges == 28;
            AddCheck("Reload_WeaponModAndChargesPersisted", weaponModPersisted,
                "HolyWaterTank[28]",
                (rifleMods != null && rifleMods.InstalledMods.Count > 0)
                    ? string.Format("{0}[{1}]", rifleMods.InstalledMods[0].modDefName, rifleMods.InstalledMods[0].charges)
                    : "none");

            Apparel duster = null;
            for (int i = 0; i < hero.apparel.WornApparel.Count; i++)
            {
                if (hero.apparel.WornApparel[i].def.defName == "Apparel_Duster")
                {
                    duster = hero.apparel.WornApparel[i];
                    break;
                }
            }
            CompApparelMods dusterMods = duster != null ? duster.GetComp<CompApparelMods>() : null;
            bool apparelModPersisted = dusterMods != null && dusterMods.InstalledMods.Count == 1 &&
                                       dusterMods.InstalledMods[0].modDefName == "TMM_Mod_RailroadBallisticWeave";
            AddCheck("Reload_ApparelModPersisted", apparelModPersisted, "RailroadBallisticWeave",
                (dusterMods != null && dusterMods.InstalledMods.Count > 0) ? dusterMods.InstalledMods[0].modDefName : "none");
        }

        private static bool RunPawnJobToCompletion(Pawn pawn, int maxTicks)
        {
            if (pawn == null || pawn.jobs == null || pawn.CurJob == null) return false;
            JobDef startedDef = pawn.CurJobDef;
            for (int t = 0; t < maxTicks; t++)
            {
                if (pawn.CurJobDef != startedDef) return true;
                pawn.Tick(); // CHECK API
            }
            return pawn.CurJobDef != startedDef;
        }

        private bool FireWeaponOnceAt(Pawn shooter, ThingWithComps gun, Pawn target)
        {
            if (shooter == null || gun == null || target == null || TryCastShotMethod == null) return false;
            CompEquippable eq = gun.GetComp<CompEquippable>();
            if (eq == null || eq.PrimaryVerb == null) return false;

            Verb verb = eq.PrimaryVerb;
            verb.caster = shooter;

            FieldInfo curTargetField = AccessTools.Field(typeof(Verb), "currentTarget"); // CHECK API
            if (curTargetField != null)
            {
                curTargetField.SetValue(verb, new LocalTargetInfo(target));
            }

            object res = TryCastShotMethod.Invoke(verb, null);
            bool fired = (res is bool) && (bool)res;
            if (!fired) return false;

            List<Thing> snapshot = new List<Thing>(map.listerThings.AllThings);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                Projectile proj = snapshot[i] as Projectile;
                if (proj == null || proj.Destroyed) continue;
                if (proj.Launcher == shooter)
                {
                    proj.Position = target.Position;
                    if (ProjectileImpactMethod != null)
                    {
                        ProjectileImpactMethod.Invoke(proj, new object[] { target, false });
                    }
                    break;
                }
            }
            return true;
        }

        private ThingWithComps EquipWeapon(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.equipment == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            ThingWithComps w = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null) as ThingWithComps;
            if (w == null) return null;
            pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(w);
            return w;
        }

        private Apparel WearApparel(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.apparel == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            ThingDef stuff = def.MadeFromStuff ? ThingDefOf.Cloth : null;
            if (def.MadeFromStuff && stuff != null && !def.stuffCategories.Contains(StuffCategoryDefOf.Fabric))
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            Apparel ap = ThingMaker.MakeThing(def, stuff) as Apparel;
            if (ap == null) return null;
            pawn.apparel.Wear(ap, false);
            return ap;
        }

        private Pawn SpawnPawn(string xenotypeDefName, Faction fac, IntVec3 nearCell)
        {
            XenotypeDef xeno = (ModsConfig.BiotechActive && !xenotypeDefName.NullOrEmpty())
                ? DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName)
                : (ModsConfig.BiotechActive ? XenotypeDefOf.Baseliner : null);

            PawnGenerationRequest req = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                fac ?? Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                -1,
                true, false, false, true, false, 1f, false, true, false, false, false,
                false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null,
                25f, 25f, null, null, null, null, null,
                false, false, false, false, null, null, xeno, null, null, 0f, DevelopmentalStage.Adult); // CHECK API

            Pawn p = PawnGenerator.GeneratePawn(req);
            if (p == null) return null;
            if (p.apparel != null) p.apparel.DestroyAll();

            IntVec3 cell = FindStandableNear(nearCell);
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
        }

        private static float SumInjuries(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return 0f;
            float sum = 0f;
            List<Hediff> list = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < list.Count; i++)
            {
                Hediff_Injury inj = list[i] as Hediff_Injury;
                if (inj != null) sum += inj.Severity;
            }
            return sum;
        }

        private static Faction FindHostileFaction()
        {
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && !all[i].IsPlayer && !all[i].defeated && all[i].HostileTo(Faction.OfPlayer))
                {
                    return all[i];
                }
            }
            return Faction.OfPirates;
        }

        private IntVec3 FindClearCenter()
        {
            return FindStandableNear(map.Center);
        }

        private IntVec3 FindStandableNear(IntVec3 near)
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(near, map, 6, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            return map.Center;
        }

        private void AddCheck(string name, bool passed, string expected, string actual)
        {
            CheckItem item = new CheckItem();
            item.name = name;
            item.passed = passed;
            item.expected = expected ?? string.Empty;
            item.actual = actual ?? string.Empty;
            m_checks.Add(item);
        }

        private void WriteReport(string fileName)
        {
            bool allPassed = m_errors.Count == 0 && m_checks.Count > 0;
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (!m_checks[i].passed)
                {
                    allPassed = false;
                    break;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"passed\":");
            sb.Append(allPassed ? "true" : "false");
            sb.Append(",\"checksCount\":");
            sb.Append(m_checks.Count);
            sb.Append(",\"checks\":[");
            for (int i = 0; i < m_checks.Count; i++)
            {
                if (i > 0) sb.Append(',');
                CheckItem c = m_checks[i];
                sb.Append("{\"name\":\"");
                sb.Append(EscapeJson(c.name));
                sb.Append("\",\"passed\":");
                sb.Append(c.passed ? "true" : "false");
                sb.Append(",\"expected\":\"");
                sb.Append(EscapeJson(c.expected));
                sb.Append("\",\"actual\":\"");
                sb.Append(EscapeJson(c.actual));
                sb.Append("\"}");
            }
            sb.Append("],\"errors\":[");
            for (int i = 0; i < m_errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(EscapeJson(m_errors[i]));
                sb.Append('"');
            }
            sb.Append("]}");

            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, fileName);
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }
    }
}
```