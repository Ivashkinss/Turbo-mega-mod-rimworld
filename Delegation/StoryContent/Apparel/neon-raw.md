`Defs/ResearchProjectDefs/Research_FactionApparel.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <ResearchProjectDef>
    <defName>TMM_WastelandMilitaryGear</defName>
    <label>wasteland military tailoring</label>
    <description>Techniques for manufacturing standardized mercenary combat armor, tactical helmets, and authoritarian officer uniforms.</description>
    <baseCost>1600</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>FlakArmor</li>
    </prerequisites>
    <researchViewX>6.0</researchViewX>
    <researchViewY>5.6</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_VaultTecStandards</defName>
    <label>Vault-Tec protective gear</label>
    <description>Pre-war blueprints for durable synthetic vault jumpsuits and heavy riot-control security vests.</description>
    <baseCost>1400</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>ComplexClothing</li>
      <li>FlakArmor</li>
    </prerequisites>
    <researchViewX>6.0</researchViewX>
    <researchViewY>6.3</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_RadSurvivalGear</defName>
    <label>radiation survival garments</label>
    <description>Methods of lining heavy coats with rubberized canvas and lead strips to endure toxic fallout.</description>
    <baseCost>1200</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>ComplexClothing</li>
    </prerequisites>
    <researchViewX>5.0</researchViewX>
    <researchViewY>6.3</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_HuntersMilitiaCraft</defName>
    <label>hunter and militia outfitting</label>
    <description>Craft rugged leather dusters reinforced with silver stitching alongside iconic colonial militia headgear.</description>
    <baseCost>1500</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>ComplexClothing</li>
    </prerequisites>
    <researchViewX>5.0</researchViewX>
    <researchViewY>7.0</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_OccultTailoring</defName>
    <label>occult garment weaving</label>
    <description>Incorporate silver filaments, flame-resistant linings, and Enochian wards into tailored suits and coats.</description>
    <baseCost>2200</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>ComplexClothing</li>
    </prerequisites>
    <researchViewX>6.0</researchViewX>
    <researchViewY>7.0</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_SuperMutantArmor</defName>
    <label>titan scrap armoring</label>
    <description>Heavy welding techniques required to forge oversized plate harnesses and cage helmets for super mutants.</description>
    <baseCost>1800</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>PlateArmor</li>
      <li>Machining</li>
    </prerequisites>
    <researchViewX>7.0</researchViewX>
    <researchViewY>5.6</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_UndergroundElectronics</defName>
    <label>covert signal gear</label>
    <description>Assemble improvised frequency-jamming headgear and sensor goggles favored by underground operatives.</description>
    <baseCost>1800</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>MicroelectronicsBasics</li>
    </prerequisites>
    <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
    <researchViewX>7.0</researchViewX>
    <researchViewY>6.3</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_BrotherhoodGear</defName>
    <label>Brotherhood field equipment</label>
    <description>Produce reinforced scribe fatigues and ballistic power-armor undersuits used by Brotherhood chapters.</description>
    <baseCost>2600</baseCost>
    <techLevel>Spacer</techLevel>
    <prerequisites>
      <li>FlakArmor</li>
      <li>Fabrication</li>
    </prerequisites>
    <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
    <researchViewX>11.0</researchViewX>
    <researchViewY>5.6</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_InstituteTextiles</defName>
    <label>Institute polymer weaving</label>
    <description>Synthesize sterile lab coats and lightweight ceramic-polymer armor plates modeled on Institute designs.</description>
    <baseCost>3200</baseCost>
    <techLevel>Spacer</techLevel>
    <prerequisites>
      <li>ReconArmor</li> <!-- CHECK API: verify ReconArmor is Core in 1.7 (else PoweredArmor) -->
      <li>Fabrication</li>
    </prerequisites>
    <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
    <researchViewX>11.0</researchViewX>
    <researchViewY>6.3</researchViewY>
  </ResearchProjectDef>

  <ResearchProjectDef>
    <defName>TMM_ClanBioferriteCraft</defName>
    <label>nocturnal clan alchemy</label>
    <description>Treat heavy velvet and leather with alchemical preservatives to craft aristocratic nocturnal garments.</description>
    <baseCost>2400</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>ComplexClothing</li>
    </prerequisites>
    <researchViewX>7.0</researchViewX>
    <researchViewY>7.0</researchViewY>
  </ResearchProjectDef>

</Defs>
```

`Defs/ThingDefs/Apparel_Factions.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- 1. HELL / DEMONS                                                      -->
  <!-- ===================================================================== -->

  <!-- 1.1 Crossroads Tailored Suit (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_demon_crossroads_suit</defName>
    <label>crossroads tailored suit</label>
    <description>An immaculate black three-piece suit with subtle crimson silk lining and silver cufflinks. Worn by dealmakers who prefer sealing contracts over brawling, its fabric is unnaturally resistant to scorching heat.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>75</Synthread>
      <DevilstrandCloth>25</DevilstrandCloth>
    </costList>
    <statBases>
      <WorkToMake>4200</WorkToMake>
      <MaxHitPoints>200</MaxHitPoints>
      <Mass>1.4</Mass>
      <MarketValue>650</MarketValue>
      <ArmorRating_Sharp>0.18</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.10</ArmorRating_Blunt>
      <ArmorRating_Heat>0.85</ArmorRating_Heat>
      <Insulation_Cold>12</Insulation_Cold>
      <Insulation_Heat>24</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.20</SocialImpact>
      <TradePriceImprovement>0.10</TradePriceImprovement>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_OccultTailoring</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_HellApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 1.2 Infernal Operative Coat (quest) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_demon_infernal_trenchcoat</defName>
    <label>infernal operative coat</label>
    <description>A heavy charcoal-black leather trenchcoat stitched with dark crimson thread along the lapels. Designed for long nights on the road, it turns aside blades and shrugs off flames.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>260</MaxHitPoints>
      <Mass>2.6</Mass>
      <MarketValue>920</MarketValue>
      <ArmorRating_Sharp>0.42</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.22</ArmorRating_Blunt>
      <ArmorRating_Heat>0.90</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>20</Insulation_Heat>
      <EquipDelay>2.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <PainShockThreshold>0.08</PainShockThreshold>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_HellApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 2. HEAVEN / ANGELS (angel_only)                                       -->
  <!-- ===================================================================== -->

  <!-- 2.1 Seraphic Rumpled Trenchcoat (research, angel_only) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_angel_vessel_trenchcoat</defName>
    <label>seraphic rumpled trenchcoat</label>
    <description>A weathered beige cotton trenchcoat worn over a dark suit and loose tie, tailored with reinforced vertical slits across the upper back to accommodate manifestation of celestial wings. Can only be worn by Angels.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>85</Synthread>
      <Gold>5</Gold>
    </costList>
    <statBases>
      <WorkToMake>4800</WorkToMake>
      <MaxHitPoints>240</MaxHitPoints>
      <Mass>2.2</Mass>
      <MarketValue>780</MarketValue>
      <ArmorRating_Sharp>0.36</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.20</ArmorRating_Blunt>
      <ArmorRating_Heat>0.45</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <PsychicSensitivity>0.15</PsychicSensitivity>
      <SocialImpact>0.10</SocialImpact>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_OccultTailoring</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_AngelApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelExtension">
        <requiredXenotype>TMM_Angel</requiredXenotype>
        <wingFriendly>true</wingFriendly>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- 2.2 Celestial Host Vestment (quest, angel_only, -35% demon/anomaly dmg) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_angel_celestial_raiment</defName>
    <label>celestial host vestment</label>
    <description>Gleaming white-and-gold layered armor plates worn over a tailored tunic, open across the shoulder blades for angelic wings. Etched with Enochian script, it deflects demonic curses and cuts incoming demon and anomaly damage by 35%. Can only be worn by Angels.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>360</MaxHitPoints>
      <Mass>6.5</Mass>
      <MarketValue>2900</MarketValue>
      <ArmorRating_Sharp>1.08</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.54</ArmorRating_Blunt>
      <ArmorRating_Heat>0.85</ArmorRating_Heat>
      <Insulation_Cold>22</Insulation_Cold>
      <Insulation_Heat>22</Insulation_Heat>
      <EquipDelay>4.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MeleeDodgeChance>0.08</MeleeDodgeChance>
      <ToxicResistance>0.30</ToxicResistance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_AngelApparel</li></tags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelExtension">
        <requiredXenotype>TMM_Angel</requiredXenotype>
        <wingFriendly>true</wingFriendly>
        <supernaturalDamageReduction>0.35</supernaturalDamageReduction>
        <demonAnomalyOnlyReduction>true</demonAnomalyOnlyReduction>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 3. BROTHERHOOD OF STEEL                                               -->
  <!-- ===================================================================== -->

  <!-- 3.1 Brotherhood Scribe Fatigues (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_bos_scribe_fatigues</defName>
    <label>brotherhood scribe fatigues</label>
    <description>A practical crimson-and-tan canvas tunic fitted with leather tool pouches, brass buckles, and an embroidered gear-and-sword emblem on the shoulder.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>65</Synthread>
      <Steel>15</Steel>
    </costList>
    <statBases>
      <WorkToMake>3600</WorkToMake>
      <MaxHitPoints>210</MaxHitPoints>
      <Mass>1.8</Mass>
      <MarketValue>520</MarketValue>
      <ArmorRating_Sharp>0.24</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.14</ArmorRating_Blunt>
      <ArmorRating_Heat>0.25</ArmorRating_Heat>
      <Insulation_Cold>14</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.12</ResearchSpeed>
      <GeneralLaborSpeed>0.08</GeneralLaborSpeed>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_BrotherhoodGear</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_BrotherhoodApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 3.2 T-60 Brotherhood Power Armor (quest, layer TMM_PowerArmor) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_bos_t60_power_armor</defName>
    <label>T-60 Brotherhood power armor</label>
    <description>Heavy steel-grey hydraulic power armor stamped with the Brotherhood of Steel crest on the chestplate. Worn on the dedicated Power Armor layer over regular clothing.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>440</MaxHitPoints>
      <Mass>15.0</Mass>
      <MarketValue>3900</MarketValue>
      <Flammability>0.1</Flammability>
      <ArmorRating_Sharp>1.24</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.58</ArmorRating_Blunt>
      <ArmorRating_Heat>0.72</ArmorRating_Heat>
      <Insulation_Cold>36</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>10.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <CarryingCapacity>65</CarryingCapacity>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.50</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li><li>Legs</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>TMM_PowerArmor</li></layers>
      <tags><li>TMM_BrotherhoodPowerArmor</li></tags>
    </apparel>
  </ThingDef>

  <!-- 3.3 T-60 Power Armor Helmet (quest) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_bos_t60_helmet</defName>
    <label>T-60 power armor helmet</label>
    <description>An angular steel-grey armored helmet with a built-in tactical headlamp, rebreather filters, and armored glass visor.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>220</MaxHitPoints>
      <Mass>3.2</Mass>
      <MarketValue>1250</MarketValue>
      <Flammability>0.1</Flammability>
      <ArmorRating_Sharp>1.18</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.52</ArmorRating_Blunt>
      <ArmorRating_Heat>0.65</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>10</Insulation_Heat>
      <EquipDelay>2.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.06</ShootingAccuracyPawn>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.50</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories><li>ArmorHeadgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>FullHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_BrotherhoodPowerArmor</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 4. BROTHERHOOD OUTCASTS                                               -->
  <!-- ===================================================================== -->

  <!-- 4.1 Outcast Recon Bodysuit (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_outcast_recon_underarmor</defName>
    <label>Outcast recon bodysuit</label>
    <description>A form-fitting dark charcoal and crimson ballistic undersuit fitted with reinforced elbow pads and power-armor interface ports.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Pants/Pants</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>70</Synthread>
      <Plasteel>15</Plasteel>
    </costList>
    <statBases>
      <WorkToMake>4500</WorkToMake>
      <MaxHitPoints>240</MaxHitPoints>
      <Mass>2.0</Mass>
      <MarketValue>680</MarketValue>
      <ArmorRating_Sharp>0.34</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.18</ArmorRating_Blunt>
      <ArmorRating_Heat>0.30</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>2.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MoveSpeed>0.12</MoveSpeed>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_BrotherhoodGear</researchPrerequisite>
      <skillRequirements><Crafting>6</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Pants/Pants</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li></layers>
      <tags><li>TMM_OutcastsApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 4.2 Outcast T-45d Power Armor (quest, layer TMM_PowerArmor) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_outcast_power_armor</defName>
    <label>Outcast T-45d power armor</label>
    <description>Refurbished T-45d power armor painted matte black with stark blood-red trim and shoulder plates. Worn on the Power Armor layer over regular clothing.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>410</MaxHitPoints>
      <Mass>14.5</Mass>
      <MarketValue>3600</MarketValue>
      <Flammability>0.1</Flammability>
      <ArmorRating_Sharp>1.18</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.56</ArmorRating_Blunt>
      <ArmorRating_Heat>0.78</ArmorRating_Heat>
      <Insulation_Cold>34</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>9.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <CarryingCapacity>55</CarryingCapacity>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.50</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li><li>Legs</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>TMM_PowerArmor</li></layers>
      <tags><li>TMM_OutcastsPowerArmor</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 5. ENCLAVE                                                            -->
  <!-- ===================================================================== -->

  <!-- 5.1 Enclave Officer Uniform (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_enclave_officer_uniform</defName>
    <label>Enclave officer uniform</label>
    <description>A crisp black-and-slate double-breasted military uniform with silver shoulder boards and an eagle-and-stars insignia. Radiates cold authoritarian discipline.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>80</Synthread>
      <Plasteel>10</Plasteel>
    </costList>
    <statBases>
      <WorkToMake>4600</WorkToMake>
      <MaxHitPoints>220</MaxHitPoints>
      <Mass>1.9</Mass>
      <MarketValue>740</MarketValue>
      <ArmorRating_Sharp>0.28</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.16</ArmorRating_Blunt>
      <ArmorRating_Heat>0.25</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>12</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SuppressionPower MayRequire="Ludeon.RimWorld.Ideology">0.25</SuppressionPower>
      <SocialImpact>0.15</SocialImpact>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_WastelandMilitaryGear</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_EnclaveApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 5.2 Enclave X-01 Power Armor (quest, layer TMM_PowerArmor) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_enclave_x01_power_armor</defName>
    <label>Enclave X-01 power armor</label>
    <description>Sleek, sloped composite power armor in jet-black finish with a towering neck guard. Worn on the Power Armor layer over regular clothing, its curved plates deflect high-velocity rounds and plasma blasts.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>500</MaxHitPoints>
      <Mass>14.0</Mass>
      <MarketValue>4800</MarketValue>
      <Flammability>0.05</Flammability>
      <ArmorRating_Sharp>1.32</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.64</ArmorRating_Blunt>
      <ArmorRating_Heat>0.92</ArmorRating_Heat>
      <Insulation_Cold>40</Insulation_Cold>
      <Insulation_Heat>24</Insulation_Heat>
      <EquipDelay>10.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <CarryingCapacity>75</CarryingCapacity>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.60</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li><li>Legs</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>TMM_PowerArmor</li></layers>
      <tags><li>TMM_EnclavePowerArmor</li></tags>
    </apparel>
  </ThingDef>

  <!-- 5.3 X-01 Insectoid Helmet (quest) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_enclave_x01_helmet</defName>
    <label>X-01 insectoid helmet</label>
    <description>An intimidating black armored helmet shaped like a predatory insect head, featuring glowing amber optical lenses and dual filtration canisters.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>250</MaxHitPoints>
      <Mass>3.0</Mass>
      <MarketValue>1550</MarketValue>
      <Flammability>0.05</Flammability>
      <ArmorRating_Sharp>1.25</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.58</ArmorRating_Blunt>
      <ArmorRating_Heat>0.80</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>12</Insulation_Heat>
      <EquipDelay>2.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.08</ShootingAccuracyPawn>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.40</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories><li>ArmorHeadgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>FullHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_EnclavePowerArmor</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 6. INSTITUTE                                                          -->
  <!-- ===================================================================== -->

  <!-- 6.1 Institute Division Lab Coat (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_institute_lab_coat</defName>
    <label>Institute division lab coat</label>
    <description>A pristine, sterile white synthetic coat with clean geometric seams and colored division stripes along the sleeves. Keeps its wearer comfortable in sterile subterranean labs.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>75</Synthread>
      <Hyperweave>10</Hyperweave>
    </costList>
    <statBases>
      <WorkToMake>3800</WorkToMake>
      <MaxHitPoints>190</MaxHitPoints>
      <Mass>1.2</Mass>
      <MarketValue>580</MarketValue>
      <ArmorRating_Sharp>0.16</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.08</ArmorRating_Blunt>
      <ArmorRating_Heat>0.25</ArmorRating_Heat>
      <Insulation_Cold>10</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>1.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.16</ResearchSpeed>
      <MedicalTendQuality>0.08</MedicalTendQuality>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_InstituteTextiles</researchPrerequisite>
      <skillRequirements><Crafting>6</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_InstituteApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 6.2 Institute Ceramic Synth Armor (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_institute_synth_armor</defName>
    <label>Institute ceramic synth armor</label>
    <description>Lightweight white ceramic-polymer plates strapped over a dark grey bodysuit. Mass-produced for synth strike teams, it offers crisp protection against energy weapons.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Plasteel>55</Plasteel>
      <Synthread>45</Synthread>
      <ComponentIndustrial>4</ComponentIndustrial>
    </costList>
    <statBases>
      <WorkToMake>11500</WorkToMake>
      <MaxHitPoints>320</MaxHitPoints>
      <Mass>5.2</Mass>
      <MarketValue>1650</MarketValue>
      <ArmorRating_Sharp>0.88</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.42</ArmorRating_Blunt>
      <ArmorRating_Heat>0.95</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>22</Insulation_Heat>
      <EquipDelay>4.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <AimingDelayFactor>-0.06</AimingDelayFactor>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_InstituteTextiles</researchPrerequisite>
      <skillRequirements><Crafting>7</Crafting></skillRequirements>
      <recipeUsers>
        <li>FabricationBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_InstituteApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 6.3 Institute Courser Coat (quest) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_institute_courser_uniform</defName>
    <label>Institute Courser coat</label>
    <description>A sleek, matte-black armored leather trenchcoat reinforced with woven carbon fibers and internal ceramic trauma plates. Worn by the Institute's deadliest synth retrievers.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>350</MaxHitPoints>
      <Mass>4.4</Mass>
      <MarketValue>2400</MarketValue>
      <ArmorRating_Sharp>1.02</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.50</ArmorRating_Blunt>
      <ArmorRating_Heat>0.85</ArmorRating_Heat>
      <Insulation_Cold>24</Insulation_Cold>
      <Insulation_Heat>20</Insulation_Heat>
      <EquipDelay>3.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MoveSpeed>0.18</MoveSpeed>
      <MeleeDodgeChance>0.08</MeleeDodgeChance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_InstituteApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 7. RAILROAD                                                           -->
  <!-- ===================================================================== -->

  <!-- 7.1 Ballistic-Weave Heavy Coat (quest) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_railroad_armored_coat</defName>
    <label>ballistic-weave heavy coat</label>
    <description>A patched, unassuming brown wasteland coat secretly lined with ultra-dense ballistic weave fibers. Looks like ordinary civilian clothing while stopping rifle rounds.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>320</MaxHitPoints>
      <Mass>3.4</Mass>
      <MarketValue>1750</MarketValue>
      <ArmorRating_Sharp>0.94</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.48</ArmorRating_Blunt>
      <ArmorRating_Heat>0.55</ArmorRating_Heat>
      <Insulation_Cold>24</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>2.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MoveSpeed>0.10</MoveSpeed>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_RailroadApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- 7.2 Signal-Tracker Beanie & Goggles (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_railroad_tinker_headgear</defName>
    <label>signal-tracker beanie &amp; goggles</label>
    <description>A knitted wool beanie wired with copper antennae and paired with tinted brass welding goggles. Built to spot stealth fields and block stray psychic noise.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Cloth>45</Cloth>
      <Steel>20</Steel>
      <ComponentIndustrial>2</ComponentIndustrial>
    </costList>
    <statBases>
      <WorkToMake>3400</WorkToMake>
      <MaxHitPoints>150</MaxHitPoints>
      <Mass>0.6</Mass>
      <MarketValue>490</MarketValue>
      <ArmorRating_Sharp>0.22</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.14</ArmorRating_Blunt>
      <ArmorRating_Heat>0.20</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>4</Insulation_Heat>
      <EquipDelay>1.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.05</ShootingAccuracyPawn>
      <PsychicSensitivity>-0.25</PsychicSensitivity>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_UndergroundElectronics</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>Headgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>UpperHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_RailroadApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 8. GUNNERS                                                            -->
  <!-- ===================================================================== -->

  <!-- 8.1 Gunner Reinforced Combat Armor (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_gunner_combat_armor</defName>
    <label>Gunner reinforced combat armor</label>
    <description>Olive-drab modular military armor plates strapped over dark green fatigues, stenciled with a white skull emblem on the shoulder guard. Built for professional wasteland mercenaries.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Steel>85</Steel>
      <Plasteel>25</Plasteel>
      <ComponentIndustrial>3</ComponentIndustrial>
    </costList>
    <statBases>
      <WorkToMake>9200</WorkToMake>
      <MaxHitPoints>310</MaxHitPoints>
      <Mass>7.5</Mass>
      <MarketValue>1350</MarketValue>
      <ArmorRating_Sharp>0.92</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.45</ArmorRating_Blunt>
      <ArmorRating_Heat>0.42</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>12</Insulation_Heat>
      <EquipDelay>4.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <AimingDelayFactor>-0.05</AimingDelayFactor>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_WastelandMilitaryGear</researchPrerequisite>
      <skillRequirements><Crafting>6</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_GunnersApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 8.2 Gunner Helmet with Skull Bandana (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_gunner_skull_helmet</defName>
    <label>Gunner helmet with skull bandana</label>
    <description>An olive-green army combat helmet paired with a lower-face bandana printed with a skeletal jaw. Protects against shrapnel and dusty wasteland winds.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/AdvancedHelmet/AdvancedHelmet</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Steel>40</Steel>
      <Cloth>25</Cloth>
    </costList>
    <statBases>
      <WorkToMake>3800</WorkToMake>
      <MaxHitPoints>170</MaxHitPoints>
      <Mass>1.6</Mass>
      <MarketValue>540</MarketValue>
      <ArmorRating_Sharp>0.78</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.38</ArmorRating_Blunt>
      <ArmorRating_Heat>0.30</ArmorRating_Heat>
      <Insulation_Cold>10</Insulation_Cold>
      <Insulation_Heat>6</Insulation_Heat>
      <EquipDelay>1.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.20</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_WastelandMilitaryGear</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ArmorHeadgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>FullHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/AdvancedHelmet/AdvancedHelmet</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_GunnersApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 9. VAULT-TEC                                                          -->
  <!-- ===================================================================== -->

  <!-- 9.1 Vault-Tec Ribbed Jumpsuit (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_vault_tec_jumpsuit</defName>
    <label>Vault-Tec ribbed jumpsuit</label>
    <description>A form-fitting cobalt-blue synthetic jumpsuit accented with bright yellow trim, brass zippers, and bold vault numerals across the back. Comfortable, durable, and iconic.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>65</Synthread>
    </costList>
    <statBases>
      <WorkToMake>3200</WorkToMake>
      <MaxHitPoints>200</MaxHitPoints>
      <Mass>1.3</Mass>
      <MarketValue>460</MarketValue>
      <ArmorRating_Sharp>0.22</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.14</ArmorRating_Blunt>
      <ArmorRating_Heat>0.35</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>1.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <WorkSpeedGlobal>0.08</WorkSpeedGlobal>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.15</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_VaultTecStandards</researchPrerequisite>
      <skillRequirements><Crafting>4</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li></layers>
      <tags><li>TMM_VaultTecApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 9.2 Vault Security Vest (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_vault_security_armor</defName>
    <label>Vault security vest</label>
    <description>A dark navy padded ballistic vest stenciled with 'SECURITY' in crisp white block letters, built to quell underground riots and hold airlock chokepoints.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Steel>55</Steel>
      <Synthread>35</Synthread>
      <ComponentIndustrial>1</ComponentIndustrial>
    </costList>
    <statBases>
      <WorkToMake>6400</WorkToMake>
      <MaxHitPoints>260</MaxHitPoints>
      <Mass>4.8</Mass>
      <MarketValue>820</MarketValue>
      <ArmorRating_Sharp>0.86</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.52</ArmorRating_Blunt>
      <ArmorRating_Heat>0.32</ArmorRating_Heat>
      <Insulation_Cold>12</Insulation_Cold>
      <Insulation_Heat>8</Insulation_Heat>
      <EquipDelay>3.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SuppressionPower MayRequire="Ludeon.RimWorld.Ideology">0.15</SuppressionPower>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_VaultTecStandards</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/FlakVest/FlakVest</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li></layers>
      <tags><li>TMM_VaultTecApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 10. MINUTEMEN & HUNTERS                                               -->
  <!-- ===================================================================== -->

  <!-- 10.1 Weathered Monster-Hunter Duster (research; Leathery stuff per balance fix) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_hunter_leather_duster</defName>
    <label>weathered monster-hunter duster</label>
    <description>A heavy, road-worn brown leather duster layered over a red-and-black flannel shirt, with hidden inner holsters and silver-stitched cuffs to fend off claws and fangs.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costStuffCount>85</costStuffCount>
    <stuffCategories>
      <li>Leathery</li>
    </stuffCategories>
    <costList>
      <Silver>15</Silver>
    </costList>
    <statBases>
      <WorkToMake>5400</WorkToMake>
      <MaxHitPoints>250</MaxHitPoints>
      <Mass>2.8</Mass>
      <MarketValue>890</MarketValue>
      <ArmorRating_Sharp>0.54</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.30</ArmorRating_Blunt>
      <ArmorRating_Heat>0.38</ArmorRating_Heat>
      <Insulation_Cold>24</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MeleeDodgeChance>0.06</MeleeDodgeChance>
      <AimingDelayFactor>-0.05</AimingDelayFactor>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_HuntersMilitiaCraft</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_HuntersApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- 10.2 Minutemen Colonial Duster (quest; uses Leathery in RecipeDef) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_minutemen_general_uniform</defName>
    <label>Minutemen colonial duster</label>
    <description>A weathered navy-blue colonial frock coat reinforced with boiled leather shoulder plates, crossed bandoliers, and brass buttons. Inspires settlers to stand their ground.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>290</MaxHitPoints>
      <Mass>3.5</Mass>
      <MarketValue>1280</MarketValue>
      <ArmorRating_Sharp>0.68</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.34</ArmorRating_Blunt>
      <ArmorRating_Heat>0.35</ArmorRating_Heat>
      <Insulation_Cold>22</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.22</SocialImpact>
      <NegotiationAbility>0.15</NegotiationAbility>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_HuntersApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- 10.3 Minutemen Tricorn Hat (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_minutemen_tricorn_hat</defName>
    <label>Minutemen tricorn hat</label>
    <description>A sturdy dark felt tricorn hat pinned with a brass musket-and-lightning badge. Shields the wearer from harsh sun and rain while projecting frontier authority.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/CowboyHat/CowboyHat</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costStuffCount>35</costStuffCount>
    <stuffCategories>
      <li>Leathery</li>
      <li>Fabric</li>
    </stuffCategories>
    <costList>
      <Steel>5</Steel>
    </costList>
    <statBases>
      <WorkToMake>2200</WorkToMake>
      <MaxHitPoints>130</MaxHitPoints>
      <Mass>0.4</Mass>
      <MarketValue>340</MarketValue>
      <ArmorRating_Sharp>0.18</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.12</ArmorRating_Blunt>
      <ArmorRating_Heat>0.20</ArmorRating_Heat>
      <Insulation_Cold>10</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>0.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.12</SocialImpact>
      <ShootingAccuracyPawn>0.03</ShootingAccuracyPawn>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_HuntersMilitiaCraft</researchPrerequisite>
      <skillRequirements><Crafting>4</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>Headgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>UpperHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/CowboyHat/CowboyHat</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_HuntersApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 11. BRITISH MEN OF LETTERS                                            -->
  <!-- ===================================================================== -->

  <!-- 11.1 Men of Letters Warded Suit (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_mol_arcane_tweed_suit</defName>
    <label>Men of Letters warded suit</label>
    <description>A bespoke British tweed three-piece suit interwoven with fine silver-alloy filaments and concealed Enochian wards sewn into the waistcoat.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Synthread>70</Synthread>
      <Silver>35</Silver>
    </costList>
    <statBases>
      <WorkToMake>6800</WorkToMake>
      <MaxHitPoints>230</MaxHitPoints>
      <Mass>2.1</Mass>
      <MarketValue>1150</MarketValue>
      <ArmorRating_Sharp>0.46</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.24</ArmorRating_Blunt>
      <ArmorRating_Heat>0.45</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>12</Insulation_Heat>
      <EquipDelay>2.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.12</ResearchSpeed>
      <PsychicSensitivity>-0.35</PsychicSensitivity>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_OccultTailoring</researchPrerequisite>
      <skillRequirements><Crafting>6</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_MenOfLettersApparel</li></tags>
    </apparel>
  </ThingDef>

  <!-- 11.2 London Black-Ops Trenchcoat (quest, -25% supernatural damage) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_mol_covert_trenchcoat</defName>
    <label>London black-ops trenchcoat</label>
    <description>A tailored navy-black waterproof trenchcoat fitted with concealed kevlar panels and brass ward-plates etched with binding sigils that reduce incoming supernatural damage by 25%.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>310</MaxHitPoints>
      <Mass>3.6</Mass>
      <MarketValue>1950</MarketValue>
      <ArmorRating_Sharp>0.84</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.40</ArmorRating_Blunt>
      <ArmorRating_Heat>0.65</ArmorRating_Heat>
      <Insulation_Cold>24</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <PsychicSensitivity>-0.50</PsychicSensitivity>
      <ShootingAccuracyPawn>0.05</ShootingAccuracyPawn>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_MenOfLettersApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelExtension">
        <supernaturalDamageReduction>0.25</supernaturalDamageReduction>
        <demonAnomalyOnlyReduction>false</demonAnomalyOnlyReduction>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 12. SENTIENT GHOULS                                                   -->
  <!-- ===================================================================== -->

  <!-- 12.1 Crimson Revolutionary Coat (quest) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_ghoul_revolutionary_frockcoat</defName>
    <label>crimson revolutionary coat</label>
    <description>A faded, bullet-nicked crimson colonial frock coat worn over a battered waistcoat. Popularized by Goodneighbor's ghoul mayor, it commands respect among wasteland outcasts.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>240</MaxHitPoints>
      <Mass>2.4</Mass>
      <MarketValue>720</MarketValue>
      <ArmorRating_Sharp>0.44</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.24</ArmorRating_Blunt>
      <ArmorRating_Heat>0.30</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.20</SocialImpact>
      <TradePriceImprovement>0.08</TradePriceImprovement>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_GhoulsApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- 12.2 Patched Lead-Lined Poncho (research) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_ghoul_lead_lined_poncho</defName>
    <label>patched lead-lined poncho</label>
    <description>A heavy wasteland poncho stitched from rubberized canvas, scrap leather, and thin lead foil strips. Built to endure toxic storms and acid rain.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Parka/Parka</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costStuffCount>65</costStuffCount>
    <stuffCategories>
      <li>Leathery</li>
    </stuffCategories>
    <costList>
      <Steel>20</Steel>
    </costList>
    <statBases>
      <WorkToMake>3600</WorkToMake>
      <MaxHitPoints>220</MaxHitPoints>
      <Mass>3.2</Mass>
      <MarketValue>510</MarketValue>
      <ArmorRating_Sharp>0.38</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.22</ArmorRating_Blunt>
      <ArmorRating_Heat>0.45</ArmorRating_Heat>
      <Insulation_Cold>28</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.45</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_RadSurvivalGear</researchPrerequisite>
      <skillRequirements><Crafting>4</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Parka/Parka</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_GhoulsApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 13. SUPER MUTANTS (super_mutant_only)                                 -->
  <!-- ===================================================================== -->

  <!-- 13.1 Super Mutant Scrap Harness (research, super_mutant_only; Leathery per balance fix) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_mutant_scrap_harness</defName>
    <label>super mutant scrap harness</label>
    <description>Massive, crudely welded steel plates, rusted highway signs, and heavy iron chains bolted together to fit the hulking torso of a super mutant. Too huge for humans to wear.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PlateArmor/PlateArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costStuffCount>40</costStuffCount>
    <stuffCategories>
      <li>Leathery</li>
    </stuffCategories>
    <costList>
      <Steel>140</Steel>
    </costList>
    <statBases>
      <WorkToMake>8800</WorkToMake>
      <MaxHitPoints>420</MaxHitPoints>
      <Mass>16.0</Mass>
      <MarketValue>1100</MarketValue>
      <ArmorRating_Sharp>0.96</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.55</ArmorRating_Blunt>
      <ArmorRating_Heat>0.40</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>6.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MeleeDamageFactor>0.10</MeleeDamageFactor>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_SuperMutantArmor</researchPrerequisite>
      <skillRequirements><Crafting>5</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PlateArmor/PlateArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SuperMutantApparel</li></tags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelExtension">
        <requiredXenotype>TMM_SuperMutant</requiredXenotype>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- 13.2 Super Mutant Rebar Cage Helmet (research, super_mutant_only) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_mutant_rebar_cage_helmet</defName>
    <label>super mutant rebar cage helmet</label>
    <description>A brutal iron cage welded from thick rebar rods and bolted steel scrap, sized to protect a super mutant's massive skull in melee brawls. Can only be worn by Super Mutants.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/SimpleHelmet/SimpleHelmet</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <Steel>60</Steel>
    </costList>
    <statBases>
      <WorkToMake>3500</WorkToMake>
      <MaxHitPoints>220</MaxHitPoints>
      <Mass>4.2</Mass>
      <MarketValue>480</MarketValue>
      <ArmorRating_Sharp>0.76</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.48</ArmorRating_Blunt>
      <ArmorRating_Heat>0.25</ArmorRating_Heat>
      <Insulation_Cold>8</Insulation_Cold>
      <Insulation_Heat>8</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <PainShockThreshold>0.10</PainShockThreshold>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_SuperMutantArmor</researchPrerequisite>
      <skillRequirements><Crafting>4</Crafting></skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ArmorHeadgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>FullHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/SimpleHelmet/SimpleHelmet</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_SuperMutantApparel</li></tags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelExtension">
        <requiredXenotype>TMM_SuperMutant</requiredXenotype>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 14. MONSTER CLANS                                                     -->
  <!-- ===================================================================== -->

  <!-- 14.1 Nocturnal Clan Velvet Coat (research; MoveSpeed +0.08 per Claude's balance fix) -->
  <ThingDef ParentName="ApparelMakeableBase">
    <defName>TMM_Apparel_clan_nocturnal_aristocrat_coat</defName>
    <label>nocturnal clan velvet coat</label>
    <description>A long, high-collared midnight-black velvet and leather coat worn by ancient vampire elders. Tailored for silent movement and intimidating presence.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <costList>
      <DevilstrandCloth>75</DevilstrandCloth>
      <Silver>10</Silver>
    </costList>
    <statBases>
      <WorkToMake>5600</WorkToMake>
      <MaxHitPoints>250</MaxHitPoints>
      <Mass>2.3</Mass>
      <MarketValue>950</MarketValue>
      <ArmorRating_Sharp>0.48</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.24</ArmorRating_Blunt>
      <ArmorRating_Heat>0.20</ArmorRating_Heat>
      <Insulation_Cold>26</Insulation_Cold>
      <Insulation_Heat>10</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MoveSpeed>0.08</MoveSpeed>
      <SocialImpact>0.18</SocialImpact>
    </equippedStatOffsets>
    <recipeMaker>
      <researchPrerequisite>TMM_ClanBioferriteCraft</researchPrerequisite>
      <skillRequirements><Crafting>6</Crafting></skillRequirements>
      <recipeUsers>
        <li>ElectricTailoringBench</li>
      </recipeUsers>
    </recipeMaker>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_MonsterClansApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- 14.2 Living Bioferrite Carapace (quest, layer TMM_PowerArmor, self-repairing) -->
  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Apparel_clan_bioferrite_carapace</defName>
    <label>living bioferrite carapace</label>
    <description>Organic crimson-black chitinous plates grown from hardened bioferrite and monster blood. Worn on the Power Armor layer over regular clothing, the living carapace accelerates wound healing by 25% and slowly repairs its own durability.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>420</MaxHitPoints>
      <Mass>8.5</Mass>
      <MarketValue>3400</MarketValue>
      <Flammability>0.2</Flammability>
      <ArmorRating_Sharp>1.16</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.62</ArmorRating_Blunt>
      <ArmorRating_Heat>0.35</ArmorRating_Heat>
      <Insulation_Cold>28</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>6.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MeleeDamageFactor>0.15</MeleeDamageFactor>
      <InjuryHealingFactor>0.25</InjuryHealingFactor>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li><li>Legs</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>TMM_PowerArmor</li></layers>
      <tags><li>TMM_MonsterClansPowerArmor</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LivingCarapaceRepair">
        <repairIntervalTicks>600</repairIntervalTicks>
        <hitPointsPerRepair>1</hitPointsPerRepair>
      </li>
    </comps>
  </ThingDef>

</Defs>
```

`Defs/RecipeDefs/Recipes_FactionApparel.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!--
    Story-gated recipes for all 13 quest-unlocked faction apparel items.
    Availability is controlled by FactionApparelHarmony.Postfix_RecipeDef_AvailableNow,
    which checks SparkStory for requiredStoryFlag ("unlock.<item_id>=true") OR any alias
    in altStoryFlags (set by Story_<faction>.xml quests).
  -->

  <!-- 1. Infernal Operative Coat -->
  <RecipeDef>
    <defName>TMM_Make_demon_infernal_trenchcoat</defName>
    <label>craft infernal operative coat</label>
    <description>Tailor a flame-resistant charcoal leather trenchcoat stitched with crimson thread.</description>
    <jobString>Tailoring infernal operative coat.</jobString>
    <workAmount>5800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>65</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>15</count></li>
    </ingredients>
    <products><TMM_Apparel_demon_infernal_trenchcoat>1</TMM_Apparel_demon_infernal_trenchcoat></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.demon_infernal_trenchcoat</requiredStoryFlag>
        <altStoryFlags><li>unlock.hell_operative_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 2. Celestial Host Vestment (angel_only) -->
  <RecipeDef>
    <defName>TMM_Make_angel_celestial_raiment</defName>
    <label>forge celestial host vestment</label>
    <description>Fabricate white-and-gold Enochian plate vestments cut for angelic wings.</description>
    <jobString>Forging celestial host vestment.</jobString>
    <workAmount>14000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>75</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>20</count></li>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>50</count></li>
    </ingredients>
    <products><TMM_Apparel_angel_celestial_raiment>1</TMM_Apparel_angel_celestial_raiment></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.angel_celestial_raiment</requiredStoryFlag>
        <altStoryFlags><li>unlock.celestial_vestments</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 3. T-60 Brotherhood Power Armor -->
  <RecipeDef>
    <defName>TMM_Make_bos_t60_power_armor</defName>
    <label>fabricate T-60 Brotherhood power armor</label>
    <description>Assemble a heavy T-60 Brotherhood power armor suit powered by uranium fuel cells.</description>
    <jobString>Fabricating T-60 power armor.</jobString>
    <workAmount>24000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>9</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <!-- NOTE: Nuclear reactor researchPrerequisite can be added once that ResearchProjectDef exists. -->
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>110</count></li>
      <li><filter><thingDefs><li>Uranium</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>8</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Apparel_bos_t60_power_armor>1</TMM_Apparel_bos_t60_power_armor></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.bos_t60_power_armor</requiredStoryFlag>
        <altStoryFlags><li>unlock.bos_t60_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 4. T-60 Power Armor Helmet -->
  <RecipeDef>
    <defName>TMM_Make_bos_t60_helmet</defName>
    <label>fabricate T-60 power armor helmet</label>
    <description>Assemble an armored T-60 helmet with integrated rebreather and tactical visor.</description>
    <jobString>Fabricating T-60 helmet.</jobString>
    <workAmount>8500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>40</count></li>
      <li><filter><thingDefs><li>Uranium</li></thingDefs></filter><count>5</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>4</count></li>
    </ingredients>
    <products><TMM_Apparel_bos_t60_helmet>1</TMM_Apparel_bos_t60_helmet></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.bos_t60_helmet</requiredStoryFlag>
        <altStoryFlags><li>unlock.bos_t60_power_armor</li><li>unlock.bos_t60_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 5. Outcast T-45d Power Armor -->
  <RecipeDef>
    <defName>TMM_Make_outcast_power_armor</defName>
    <label>fabricate Outcast T-45d power armor</label>
    <description>Assemble refurbished matte-black and crimson Outcast T-45d power armor.</description>
    <jobString>Fabricating Outcast power armor.</jobString>
    <workAmount>21000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>95</count></li>
      <li><filter><thingDefs><li>Uranium</li></thingDefs></filter><count>20</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>8</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>1</count></li>
    </ingredients>
    <products><TMM_Apparel_outcast_power_armor>1</TMM_Apparel_outcast_power_armor></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.outcast_power_armor</requiredStoryFlag>
        <altStoryFlags><li>unlock.outcasts_t45d_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 6. Enclave X-01 Power Armor -->
  <RecipeDef>
    <defName>TMM_Make_enclave_x01_power_armor</defName>
    <label>fabricate Enclave X-01 power armor</label>
    <description>Assemble sloped composite Enclave X-01 power armor.</description>
    <jobString>Fabricating X-01 power armor.</jobString>
    <workAmount>28000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>10</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>135</count></li>
      <li><filter><thingDefs><li>Uranium</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>10</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>3</count></li>
    </ingredients>
    <products><TMM_Apparel_enclave_x01_power_armor>1</TMM_Apparel_enclave_x01_power_armor></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.enclave_x01_power_armor</requiredStoryFlag>
        <altStoryFlags><li>unlock.enclave_x01_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 7. X-01 Insectoid Helmet -->
  <RecipeDef>
    <defName>TMM_Make_enclave_x01_helmet</defName>
    <label>fabricate X-01 insectoid helmet</label>
    <description>Assemble an Enclave X-01 power armor helmet with amber optical lenses.</description>
    <jobString>Fabricating X-01 helmet.</jobString>
    <workAmount>9500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>9</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>45</count></li>
      <li><filter><thingDefs><li>Uranium</li></thingDefs></filter><count>8</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>1</count></li>
    </ingredients>
    <products><TMM_Apparel_enclave_x01_helmet>1</TMM_Apparel_enclave_x01_helmet></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.enclave_x01_helmet</requiredStoryFlag>
        <altStoryFlags><li>unlock.enclave_x01_power_armor</li><li>unlock.enclave_x01_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 8. Institute Courser Coat -->
  <RecipeDef>
    <defName>TMM_Make_institute_courser_uniform</defName>
    <label>fabricate Institute Courser coat</label>
    <description>Craft a matte-black hyperweave and ceramic armored Courser coat.</description>
    <jobString>Fabricating Courser coat.</jobString>
    <workAmount>15000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Hyperweave</li></thingDefs></filter><count>45</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>40</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>1</count></li>
    </ingredients>
    <products><TMM_Apparel_institute_courser_uniform>1</TMM_Apparel_institute_courser_uniform></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.institute_courser_uniform</requiredStoryFlag>
        <altStoryFlags><li>unlock.courser_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 9. Railroad Ballistic-Weave Heavy Coat -->
  <RecipeDef>
    <defName>TMM_Make_railroad_armored_coat</defName>
    <label>craft ballistic-weave heavy coat</label>
    <description>Line a heavy wasteland duster with Railroad ballistic weave fibers.</description>
    <jobString>Tailoring ballistic-weave coat.</jobString>
    <workAmount>9800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>85</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>3</count></li>
    </ingredients>
    <products><TMM_Apparel_railroad_armored_coat>1</TMM_Apparel_railroad_armored_coat></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.railroad_armored_coat</requiredStoryFlag>
        <altStoryFlags><li>unlock.railroad_ballistic</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 10. Minutemen Colonial Duster (Leathery category per balance fix) -->
  <RecipeDef>
    <defName>TMM_Make_minutemen_general_uniform</defName>
    <label>craft Minutemen colonial duster</label>
    <description>Tailor a reinforced colonial general's duster with boiled leather shoulder guards.</description>
    <jobString>Tailoring Minutemen duster.</jobString>
    <workAmount>7600</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>65</count></li>
      <li><filter><categories><li>Leathers</li></categories></filter><count>45</count></li>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>25</count></li>
    </ingredients>
    <products><TMM_Apparel_minutemen_general_uniform>1</TMM_Apparel_minutemen_general_uniform></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.minutemen_general_uniform</requiredStoryFlag>
        <altStoryFlags><li>unlock.minutemen_general_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 11. London Black-Ops Trenchcoat -->
  <RecipeDef>
    <defName>TMM_Make_mol_covert_trenchcoat</defName>
    <label>craft London black-ops trenchcoat</label>
    <description>Assemble a kevlar-lined, ward-etched waterproof trenchcoat of the British Men of Letters.</description>
    <jobString>Tailoring London black-ops trenchcoat.</jobString>
    <workAmount>10500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>75</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>5</count></li>
    </ingredients>
    <products><TMM_Apparel_mol_covert_trenchcoat>1</TMM_Apparel_mol_covert_trenchcoat></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.mol_covert_trenchcoat</requiredStoryFlag>
        <altStoryFlags><li>unlock.ketch_suit</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 12. Crimson Revolutionary Coat -->
  <RecipeDef>
    <defName>TMM_Make_ghoul_revolutionary_frockcoat</defName>
    <label>craft crimson revolutionary coat</label>
    <description>Tailor a weathered crimson frock coat in Goodneighbor's iconic style.</description>
    <jobString>Tailoring crimson revolutionary coat.</jobString>
    <workAmount>4400</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>5</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Cloth</li></thingDefs></filter><count>75</count></li>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>25</count></li>
    </ingredients>
    <products><TMM_Apparel_ghoul_revolutionary_frockcoat>1</TMM_Apparel_ghoul_revolutionary_frockcoat></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.ghoul_revolutionary_frockcoat</requiredStoryFlag>
        <altStoryFlags><li>unlock.hancock_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 13A. Living Bioferrite Carapace (Fallback without Anomaly DLC) -->
  <RecipeDef>
    <defName>TMM_Make_clan_bioferrite_carapace</defName>
    <label>grow living carapace armor</label>
    <description>Cultivate an organic self-repairing carapace from plasteel weave and devilstrand.</description>
    <jobString>Cultivating living carapace.</jobString>
    <workAmount>18500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>85</count></li>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>70</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Apparel_clan_bioferrite_carapace>1</TMM_Apparel_clan_bioferrite_carapace></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.clan_bioferrite_carapace</requiredStoryFlag>
        <altStoryFlags><li>unlock.living_bioferrite_armor</li></altStoryFlags>
        <hideWhenAnomalyActive>true</hideWhenAnomalyActive>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 13B. Living Bioferrite Carapace (Anomaly DLC version using BioferriteShaper + Bioferrite) -->
  <RecipeDef MayRequire="Ludeon.RimWorld.Anomaly">
    <defName>TMM_Make_clan_bioferrite_carapace_Anomaly</defName>
    <label>shape living bioferrite carapace</label>
    <description>Shape hardened bioferrite, plasteel, and devilstrand into a living self-repairing carapace.</description>
    <jobString>Shaping living bioferrite carapace.</jobString>
    <workAmount>18500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>BioferriteShaper</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Bioferrite</li></thingDefs></filter><count>110</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>35</count></li>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>40</count></li>
    </ingredients>
    <products><TMM_Apparel_clan_bioferrite_carapace>1</TMM_Apparel_clan_bioferrite_carapace></products>
    <modExtensions>
      <li Class="TurboMegaMod.FactionApparelRecipeExtension">
        <requiredStoryFlag>unlock.clan_bioferrite_carapace</requiredStoryFlag>
        <altStoryFlags><li>unlock.living_bioferrite_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

</Defs>
```

`Source/FactionApparel.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF MOD EXTENSIONS
    // =========================================================================

    /// <summary>
    /// Controls race restrictions (TMM_Angel, TMM_SuperMutant), wing friendliness,
    /// and supernatural damage reduction on faction apparel ThingDefs.
    /// </summary>
    public sealed class FactionApparelExtension : DefModExtension
    {
        public string requiredXenotype;
        public bool wingFriendly;
        public float supernaturalDamageReduction;
        public bool demonAnomalyOnlyReduction;
    }

    /// <summary>
    /// Controls story-flag unlocking on quest-gated faction apparel RecipeDefs.
    /// Story quests set either requiredStoryFlag ("unlock.<item_id>=true") or any alias in altStoryFlags.
    /// </summary>
    public sealed class FactionApparelRecipeExtension : DefModExtension
    {
        public string requiredStoryFlag;
        public List<string> altStoryFlags = new List<string>();
        public bool hideWhenAnomalyActive;
    }

    // =========================================================================
    // LIVING BIOFERRITE CARAPACE SELF-REPAIR COMP
    // =========================================================================

    public sealed class CompProperties_LivingCarapaceRepair : CompProperties
    {
        public int repairIntervalTicks = 600;
        public int hitPointsPerRepair = 1;

        public CompProperties_LivingCarapaceRepair()
        {
            compClass = typeof(CompLivingCarapaceRepair);
        }
    }

    public sealed class CompLivingCarapaceRepair : ThingComp
    {
        private int tickCounter;

        public CompProperties_LivingCarapaceRepair Props
        {
            get { return (CompProperties_LivingCarapaceRepair)props; }
        }

        public override void CompTick()
        {
            base.CompTick();
            StepRepair(1);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            StepRepair(250);
        }

        public bool StepRepair(int ticks)
        {
            if (parent == null || parent.Destroyed || !parent.def.useHitPoints) return false;
            if (parent.HitPoints >= parent.MaxHitPoints)
            {
                tickCounter = 0;
                return false;
            }

            tickCounter += ticks;
            int interval = Mathf.Max(60, Props.repairIntervalTicks);
            if (tickCounter < interval) return false;

            tickCounter -= interval;
            parent.HitPoints = Mathf.Min(parent.MaxHitPoints, parent.HitPoints + Mathf.Max(1, Props.hitPointsPerRepair));
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }

    // =========================================================================
    // RACE RESTRICTION & STORY FLAG UTILITIES
    // =========================================================================

    public static class FactionApparelUtil
    {
        private static bool s_storyChecked;
        private static Type s_storyType;
        private static PropertyInfo s_storyInstanceProp;
        private static MethodInfo s_storyGetFlagMethod;
        private static FieldInfo s_storyFlagsField;

        public static bool CanPawnWearApparel(Pawn pawn, ThingDef apparelDef, out string failReason)
        {
            failReason = null;
            if (pawn == null || apparelDef == null) return true;

            FactionApparelExtension ext = apparelDef.GetModExtension<FactionApparelExtension>();
            if (ext == null || ext.requiredXenotype.NullOrEmpty()) return true;

            if (MatchesRequiredRace(pawn, ext.requiredXenotype)) return true;

            if (ext.requiredXenotype == "TMM_Angel")
            {
                failReason = "Only Angels can wear winged celestial apparel.";
            }
            else if (ext.requiredXenotype == "TMM_SuperMutant")
            {
                failReason = "Only Super Mutants fit this oversized armor.";
            }
            else
            {
                failReason = "Incompatible race/xenotype (" + ext.requiredXenotype + ").";
            }
            return false;
        }

        public static bool MatchesRequiredRace(Pawn pawn, string requiredXenotype)
        {
            if (pawn == null || requiredXenotype.NullOrEmpty()) return true;

            // Check Biotech xenotype first
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == requiredXenotype)
                {
                    return true;
                }

                // Gene fallback for hybrid/custom pawns carrying core signature genes
                if (requiredXenotype == "TMM_Angel")
                {
                    GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_AngelCelestialGrace");
                    if (g != null && pawn.genes.HasActiveGene(g)) return true;
                }
                else if (requiredXenotype == "TMM_SuperMutant")
                {
                    GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_MutantFevMusculature");
                    if (g != null && pawn.genes.HasActiveGene(g)) return true;
                }
            }

            // BodyType fallback for Super Mutants (body TMM_SuperMutant)
            if (requiredXenotype == "TMM_SuperMutant" && pawn.story != null && pawn.story.bodyType != null)
            {
                if (pawn.story.bodyType.defName == "TMM_SuperMutant") return true;
            }

            return false;
        }

        public static bool IsQuestRecipeUnlocked(FactionApparelRecipeExtension ext)
        {
            if (ext == null) return true;
            if (ext.hideWhenAnomalyActive && ModsConfig.AnomalyActive) return false;

            if (!ext.requiredStoryFlag.NullOrEmpty() && IsStoryFlagTrue(ext.requiredStoryFlag))
            {
                return true;
            }

            if (ext.altStoryFlags != null)
            {
                for (int i = 0; i < ext.altStoryFlags.Count; i++)
                {
                    if (!ext.altStoryFlags[i].NullOrEmpty() && IsStoryFlagTrue(ext.altStoryFlags[i]))
                    {
                        return true;
                    }
                }
            }

            return ext.requiredStoryFlag.NullOrEmpty() && (ext.altStoryFlags == null || ext.altStoryFlags.Count == 0);
        }

        public static bool IsStoryFlagTrue(string flagName)
        {
            if (flagName.NullOrEmpty()) return false;

            if (!s_storyChecked)
            {
                s_storyChecked = true;
                s_storyType = AccessTools.TypeByName("TurboMegaMod.SparkStory");
                if (s_storyType != null)
                {
                    s_storyInstanceProp = AccessTools.Property(s_storyType, "Instance");
                    s_storyGetFlagMethod = AccessTools.Method(s_storyType, "GetFlag", new Type[] { typeof(string) }); // CHECK API
                    s_storyFlagsField = AccessTools.Field(s_storyType, "flags"); // CHECK API
                }
            }

            if (s_storyType == null || s_storyInstanceProp == null) return false;

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

        public static bool IsSupernaturalSource(DamageInfo dinfo, bool demonAndAnomalyOnly)
        {
            if (dinfo.Def != null)
            {
                string dn = dinfo.Def.defName;
                if (!demonAndAnomalyOnly &&
                    (dn == "TMM_HolyWater" || dn == "TMM_RockSalt" || dn == "TMM_Enochian" || dn == "TMM_BoraxBurn"))
                {
                    return true;
                }
            }

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null) return false;

            if (ModsConfig.AnomalyActive && attacker.RaceProps != null && attacker.RaceProps.IsAnomalyEntity) // CHECK API
            {
                return true;
            }

            if (ModsConfig.BiotechActive && attacker.genes != null && attacker.genes.Xenotype != null)
            {
                string x = attacker.genes.Xenotype.defName;
                if (x == "TMM_Demon") return true;
                if (!demonAndAnomalyOnly && (x == "TMM_Angel" || x == "TMM_Leviathan" || x == "TMM_Ghoul"))
                {
                    return true;
                }
            }

            if (attacker.Faction != null && attacker.Faction.def != null)
            {
                string fd = attacker.Faction.def.defName;
                if (fd == "TMM_HellFaction") return true;
                if (!demonAndAnomalyOnly && (fd == "TMM_HeavenFaction" || fd == "TMM_MonsterClansFaction"))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // =========================================================================
    // HARMONY PATCHES (EXPLICIT REGISTRATION)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class FactionApparelHarmony
    {
        static FactionApparelHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.factionapparel");

            // 1. ApparelUtility.HasPartsToWear: blocks NPC generation & AI outfits for wrong race
            MethodInfo hasParts = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear", new Type[] { typeof(Pawn), typeof(ThingDef) }); // CHECK API
            if (hasParts != null)
            {
                harmony.Patch(hasParts, postfix: new HarmonyMethod(typeof(FactionApparelHarmony), "Postfix_HasPartsToWear"));
            }

            // 2. EquipmentUtility.CanEquip: blocks right-click "Wear" UI with clear reason
            MethodInfo canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }); // CHECK API
            if (canEquip != null)
            {
                harmony.Patch(canEquip, postfix: new HarmonyMethod(typeof(FactionApparelHarmony), "Postfix_CanEquip"));
            }

            // 3. Pawn_ApparelTracker.Wear: hard runtime guard so direct Wear() calls also enforce race
            MethodInfo wear = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear",
                new Type[] { typeof(Apparel), typeof(bool), typeof(bool) }); // CHECK API
            if (wear != null)
            {
                harmony.Patch(wear, prefix: new HarmonyMethod(typeof(FactionApparelHarmony), "Prefix_ApparelTracker_Wear"));
            }

            // 4. RecipeDef.AvailableNow: enforces quest unlock.<id> story flags on faction apparel recipes
            PropertyInfo availProp = AccessTools.Property(typeof(RecipeDef), "AvailableNow"); // CHECK API
            if (availProp != null && availProp.GetGetMethod() != null)
            {
                harmony.Patch(availProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(FactionApparelHarmony), "Postfix_RecipeDef_AvailableNow"));
            }

            // 5. Thing.TakeDamage: supernatural damage reduction for Angel Vestment (-35%) & London Trenchcoat (-25%)
            MethodInfo takeDamage = AccessTools.Method(typeof(Thing), "TakeDamage", new Type[] { typeof(DamageInfo) });
            if (takeDamage != null)
            {
                harmony.Patch(takeDamage, prefix: new HarmonyMethod(typeof(FactionApparelHarmony), "Prefix_Thing_TakeDamage"));
            }
        }

        public static void Postfix_HasPartsToWear(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null || apparel == null) return;
            string reason;
            if (!FactionApparelUtil.CanPawnWearApparel(p, apparel, out reason))
            {
                __result = false;
            }
        }

        public static void Postfix_CanEquip(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result || thing == null || pawn == null) return;
            if (!(thing is Apparel)) return;
            string reason;
            if (!FactionApparelUtil.CanPawnWearApparel(pawn, thing.def, out reason))
            {
                cantReason = reason;
                __result = false;
            }
        }

        public static bool Prefix_ApparelTracker_Wear(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (__instance == null || __instance.pawn == null || newApparel == null) return true;
            string reason;
            if (!FactionApparelUtil.CanPawnWearApparel(__instance.pawn, newApparel.def, out reason))
            {
                return false;
            }
            return true;
        }

        public static void Postfix_RecipeDef_AvailableNow(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            FactionApparelRecipeExtension ext = __instance.GetModExtension<FactionApparelRecipeExtension>();
            if (ext == null) return;

            if (!FactionApparelUtil.IsQuestRecipeUnlocked(ext))
            {
                __result = false;
            }
        }

        public static void Prefix_Thing_TakeDamage(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn victim = __instance as Pawn;
            if (victim == null || victim.Dead || victim.apparel == null) return;

            float bestReduction = 0f;
            List<Apparel> worn = victim.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel ap = worn[i];
                if (ap == null || ap.def == null) continue;
                FactionApparelExtension ext = ap.def.GetModExtension<FactionApparelExtension>();
                if (ext == null || ext.supernaturalDamageReduction <= 0f) continue;

                if (FactionApparelUtil.IsSupernaturalSource(dinfo, ext.demonAnomalyOnlyReduction))
                {
                    if (ext.supernaturalDamageReduction > bestReduction)
                    {
                        bestReduction = ext.supernaturalDamageReduction;
                    }
                }
            }

            if (bestReduction > 0f)
            {
                dinfo.SetAmount(dinfo.Amount * Mathf.Clamp01(1f - bestReduction));
            }
        }
    }
}
```

`Languages/Russian/DefInjected/ResearchProjectDef/Research_FactionApparel.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_WastelandMilitaryGear.label>военная экипировка Пустоши</TMM_WastelandMilitaryGear.label>
  <TMM_WastelandMilitaryGear.description>Технологии пошива и штамповки армейской брони наёмников, тактических шлемов и офицерской формы.</TMM_WastelandMilitaryGear.description>

  <TMM_VaultTecStandards.label>стандарты снаряжения Волт-Тек</TMM_VaultTecStandards.label>
  <TMM_VaultTecStandards.description>Довоенные чертежи износостойких синтетических комбинезонов Убежищ и брони службы безопасности.</TMM_VaultTecStandards.description>

  <TMM_RadSurvivalGear.label>противорадиационная одежда Пустоши</TMM_RadSurvivalGear.label>
  <TMM_RadSurvivalGear.description>Методы пропитки и обшивки тяжёлых плащей прорезиненной тканью и свинцом для защиты от токсичных осадков.</TMM_RadSurvivalGear.description>

  <TMM_HuntersMilitiaCraft.label>снаряжение охотников и ополчения</TMM_HuntersMilitiaCraft.label>
  <TMM_HuntersMilitiaCraft.description>Пошив прочных кожаных плащей с серебряной прострочкой для охоты на нечисть и парадных головных уборов ополчения.</TMM_HuntersMilitiaCraft.description>

  <TMM_OccultTailoring.label>оккультное портняжное дело</TMM_OccultTailoring.label>
  <TMM_OccultTailoring.description>Вплетение серебряных нитей, огнеупорной подкладки и енохианских оберегов в костюмы и плащи.</TMM_OccultTailoring.description>

  <TMM_SuperMutantArmor.label>броня титанов Пустоши</TMM_SuperMutantArmor.label>
  <TMM_SuperMutantArmor.description>Методы тяжёлой сварки для создания негабаритных бронеплит и шлемов-клеток под исполинское тело супермутанта.</TMM_SuperMutantArmor.description>

  <TMM_UndergroundElectronics.label>подпольная электроника</TMM_UndergroundElectronics.label>
  <TMM_UndergroundElectronics.description>Сборка кустарных глушителей сигналов и сенсорных очков, используемых подпольными агентами.</TMM_UndergroundElectronics.description>

  <TMM_BrotherhoodGear.label>полевое снаряжение Братства Стали</TMM_BrotherhoodGear.label>
  <TMM_BrotherhoodGear.description>Производство усиленной формы писцов и баллистических поддоспешников для силовой брони.</TMM_BrotherhoodGear.description>

  <TMM_InstituteTextiles.label>синтетические ткани Института</TMM_InstituteTextiles.label>
  <TMM_InstituteTextiles.description>Синтез стерильных лабораторных халатов и лёгкой керамико-полимерной брони по образцам Института.</TMM_InstituteTextiles.description>

  <TMM_ClanBioferriteCraft.label>алхимия ночных кланов</TMM_ClanBioferriteCraft.label>
  <TMM_ClanBioferriteCraft.description>Обработка плотного бархата и кожи алхимическими составами для создания одежды ночных кланов.</TMM_ClanBioferriteCraft.description>

</LanguageData>
```

`Languages/Russian/DefInjected/ThingDef/Apparel_Factions.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- Ад -->
  <TMM_Apparel_demon_crossroads_suit.label>деловой костюм перекрёстка</TMM_Apparel_demon_crossroads_suit.label>
  <TMM_Apparel_demon_crossroads_suit.description>Безупречный чёрный костюм-тройка с тёмно-алой шёлковой подкладкой и серебряными запонками. Одежда демонов-дельцов, предпочитающих контракты дракам; ткань неестественно устойчива к жару и огню.</TMM_Apparel_demon_crossroads_suit.description>

  <TMM_Apparel_demon_infernal_trenchcoat.label>плащ адского оперативника</TMM_Apparel_demon_infernal_trenchcoat.label>
  <TMM_Apparel_demon_infernal_trenchcoat.description>Тяжёлый угольно-чёрный кожаный плащ, простроченный тёмно-алой нитью вдоль лацканов. Создан для долгих ночных вылазок, хорошо держит удары клинков и открытое пламя.</TMM_Apparel_demon_infernal_trenchcoat.description>

  <!-- Небеса -->
  <TMM_Apparel_angel_vessel_trenchcoat.label>бежевый плащ сосуда</TMM_Apparel_angel_vessel_trenchcoat.label>
  <TMM_Apparel_angel_vessel_trenchcoat.description>Потрёпанный бежевый хлопковый плащ поверх тёмного костюма и слегка ослабленного галстука, скроенный с усиленными прорезями на спине для раскрытия небесных крыльев. Подходит только ангелам.</TMM_Apparel_angel_vessel_trenchcoat.description>

  <TMM_Apparel_angel_celestial_raiment.label>облачение небесного воинства</TMM_Apparel_angel_celestial_raiment.label>
  <TMM_Apparel_angel_celestial_raiment.description>Сияющие бело-золотые пластинчатые латы поверх туники с открытыми прорезями на лопатках для крыльев. Покрыты енохианской вязью, снижающей урон от демонов и аномалий на 35%. Подходит только ангелам.</TMM_Apparel_angel_celestial_raiment.description>

  <!-- Братство Стали -->
  <TMM_Apparel_bos_scribe_fatigues.label>форма писца Братства</TMM_Apparel_bos_scribe_fatigues.label>
  <TMM_Apparel_bos_scribe_fatigues.description>Практичная красно-песочная туника из плотной парусины с кожаными подсумками для инструментов, латунными пряжками и нашивкой шестерни с мечом на плече.</TMM_Apparel_bos_scribe_fatigues.description>

  <TMM_Apparel_bos_t60_power_armor.label>силовая броня T-60 Братства</TMM_Apparel_bos_t60_power_armor.label>
  <TMM_Apparel_bos_t60_power_armor.description>Тяжёлая стально-серая гидравлическая силовая броня с гербом Братства Стали на нагруднике. Надевается в отдельный слой силовой брони поверх обычной одежды.</TMM_Apparel_bos_t60_power_armor.description>

  <TMM_Apparel_bos_t60_helmet.label>шлем силовой брони T-60</TMM_Apparel_bos_t60_helmet.label>
  <TMM_Apparel_bos_t60_helmet.description>Угловатый стально-серый бронешлем со встроенным налобным фонарём, фильтрами респиратора и визором из бронестекла.</TMM_Apparel_bos_t60_helmet.description>

  <!-- Изгои -->
  <TMM_Apparel_outcast_recon_underarmor.label>разведкостюм Изгоев</TMM_Apparel_outcast_recon_underarmor.label>
  <TMM_Apparel_outcast_recon_underarmor.description>Облегающий тёмно-угольный с алыми вставками баллистический поддоспешник с усиленными налокотниками и разъёмами для стыковки с силовой бронёй.</TMM_Apparel_outcast_recon_underarmor.description>

  <TMM_Apparel_outcast_power_armor.label>силовая броня Изгоев</TMM_Apparel_outcast_power_armor.label>
  <TMM_Apparel_outcast_power_armor.description>Модернизированная силовая броня T-45d, выкрашенная в матовый чёрный цвет с контрастными кроваво-красными наплечниками. Надевается в слой силовой брони поверх одежды.</TMM_Apparel_outcast_power_armor.description>

  <!-- Анклав -->
  <TMM_Apparel_enclave_officer_uniform.label>офицерский мундир Анклава</TMM_Apparel_enclave_officer_uniform.label>
  <TMM_Apparel_enclave_officer_uniform.description>Безупречный чёрно-грифельный двубортный военный мундир с серебряными погонами и эмблемой со звёздами. Внушает холодную авторитарную дисциплину.</TMM_Apparel_enclave_officer_uniform.description>

  <TMM_Apparel_enclave_x01_power_armor.label>силовая броня X-01 Анклава</TMM_Apparel_enclave_x01_power_armor.label>
  <TMM_Apparel_enclave_x01_power_armor.description>Обтекаемая композитная силовая броня угольно-чёрного цвета с высоким воротником-щитком. Надевается в слой силовой брони поверх одежды, отражая бронебойные пули и плазму.</TMM_Apparel_enclave_x01_power_armor.description>

  <TMM_Apparel_enclave_x01_helmet.label>шлем силовой брони X-01</TMM_Apparel_enclave_x01_helmet.label>
  <TMM_Apparel_enclave_x01_helmet.description>Устрашающий чёрный бронешлем, напоминающий голову хищного насекомого, со светящимися янтарными окулярами и двойными фильтрами очистки воздуха.</TMM_Apparel_enclave_x01_helmet.description>

  <!-- Институт -->
  <TMM_Apparel_institute_lab_coat.label>халат учёного Института</TMM_Apparel_institute_lab_coat.label>
  <TMM_Apparel_institute_lab_coat.description>Безупречно белый стерильный синтетический халат с геометрическими швами и цветными полосами отделов на рукавах.</TMM_Apparel_institute_lab_coat.description>

  <TMM_Apparel_institute_synth_armor.label>керамическая броня синтов</TMM_Apparel_institute_synth_armor.label>
  <TMM_Apparel_institute_synth_armor.description>Лёгкие белые керамико-полимерные пластины поверх тёмно-серого комбинезона. Серийная броня штурмовых отрядов синтов, отлично рассеивающая энергетический огонь.</TMM_Apparel_institute_synth_armor.description>

  <TMM_Apparel_institute_courser_uniform.label>плащ Охотника Института</TMM_Apparel_institute_courser_uniform.label>
  <TMM_Apparel_institute_courser_uniform.description>Гладкий матово-чёрный бронированный кожаный плащ, усиленный углеволокном и скрытыми керамическими пластинами. Униформа самых смертоносных ликвидаторов Института.</TMM_Apparel_institute_courser_uniform.description>

  <!-- Подземка -->
  <TMM_Apparel_railroad_armored_coat.label>бронеплащ Подземки</TMM_Apparel_railroad_armored_coat.label>
  <TMM_Apparel_railroad_armored_coat.description>Поношенный на вид коричневый плащ путника, изнутри прошитый сверхпрочным баллистическим волокном. Выглядит как обычная одежда, но держит винтовочные пули.</TMM_Apparel_railroad_armored_coat.description>

  <TMM_Apparel_railroad_tinker_headgear.label>шапка и очки конспиролога</TMM_Apparel_railroad_tinker_headgear.label>
  <TMM_Apparel_railroad_tinker_headgear.description>Вязаная шерстяная шапка с вплетёнными медными антеннами в комплекте с затемнёнными сварочными очками. Помогает выслеживать стелс-поля и глушит психический шум.</TMM_Apparel_railroad_tinker_headgear.description>

  <!-- Стрелки -->
  <TMM_Apparel_gunner_combat_armor.label>боевая броня Стрелков</TMM_Apparel_gunner_combat_armor.label>
  <TMM_Apparel_gunner_combat_armor.description>Оливковые модульные армейские бронепластины поверх тёмно-зелёной формы с белым трафаретным черепом на наплечнике. Надёжная экипировка профессиональных наёмников.</TMM_Apparel_gunner_combat_armor.description>

  <TMM_Apparel_gunner_skull_helmet.label>каска Стрелков с банданой</TMM_Apparel_gunner_skull_helmet.label>
  <TMM_Apparel_gunner_skull_helmet.description>Оливковая армейская каска в паре с банданой, на которой напечатана челюсть черепа. Защищает от осколков, пыли и токсичных ветров.</TMM_Apparel_gunner_skull_helmet.description>

  <!-- Волт-Тек -->
  <TMM_Apparel_vault_tec_jumpsuit.label>комбинезон Убежища</TMM_Apparel_vault_tec_jumpsuit.label>
  <TMM_Apparel_vault_tec_jumpsuit.description>Облегающий кобальтово-синий синтетический комбинезон с ярко-жёлтой отделкой, латунными молниями и крупным номером Убежища на спине.</TMM_Apparel_vault_tec_jumpsuit.description>

  <TMM_Apparel_vault_security_armor.label>броня службы безопасности Убежища</TMM_Apparel_vault_security_armor.label>
  <TMM_Apparel_vault_security_armor.description>Тёмно-синий стёганый баллистический бронежилет с чёткой белой надписью «SECURITY» на груди и спине, созданный для подавления бунтов и обороны шлюзов.</TMM_Apparel_vault_security_armor.description>

  <!-- Охотники и Минитмены -->
  <TMM_Apparel_hunter_leather_duster.label>потёртый плащ охотника на нечисть</TMM_Apparel_hunter_leather_duster.label>
  <TMM_Apparel_hunter_leather_duster.description>Тяжёлый потёртый в дороге коричневый кожаный плащ поверх красно-чёрной фланелевой рубахи, со скрытыми внутренними кобурами и простроченными серебряной нитью манжетами.</TMM_Apparel_hunter_leather_duster.description>

  <TMM_Apparel_minutemen_general_uniform.label>мундир генерала Минитменов</TMM_Apparel_minutemen_general_uniform.label>
  <TMM_Apparel_minutemen_general_uniform.description>Потрёпанный тёмно-синий колониальный сюртук с наплечниками из вываренной кожи, перекрещенными патронташами и латунными пуговицами. Вдохновляет поселенцев стоять до конца.</TMM_Apparel_minutemen_general_uniform.description>

  <TMM_Apparel_minutemen_tricorn_hat.label>треуголка Минитменов</TMM_Apparel_minutemen_tricorn_hat.label>
  <TMM_Apparel_minutemen_tricorn_hat.description>Прочная тёмная фетровая треуголка с латунной кокардой в виде скрещённых мушкетов и молнии. Защищает от солнца и дождя и подчёркивает авторитет командира.</TMM_Apparel_minutemen_tricorn_hat.description>

  <!-- Британские Просвещённые -->
  <TMM_Apparel_mol_arcane_tweed_suit.label>рунический костюм Просвещённых</TMM_Apparel_mol_arcane_tweed_suit.label>
  <TMM_Apparel_mol_arcane_tweed_suit.description>Сшитый на заказ британский твидовый костюм-тройка, пронизанный тончайшими серебряными нитями и скрытыми енохианскими оберегами в подкладке жилета.</TMM_Apparel_mol_arcane_tweed_suit.description>

  <TMM_Apparel_mol_covert_trenchcoat.label>тактический плащ лондонского ордена</TMM_Apparel_mol_covert_trenchcoat.label>
  <TMM_Apparel_mol_covert_trenchcoat.description>Строгий тёмно-синий непромокаемый плащ со скрытыми кевларовыми пластинами и латунными медальонами, снижающими урон от нечисти, ангелов и демонов на 25%.</TMM_Apparel_mol_covert_trenchcoat.description>

  <!-- Разумные гули -->
  <TMM_Apparel_ghoul_revolutionary_frockcoat.label>алый революционный сюртук</TMM_Apparel_ghoul_revolutionary_frockcoat.label>
  <TMM_Apparel_ghoul_revolutionary_frockcoat.description>Выцветший, пробитый пулями алый колониальный сюртук поверх потрёпанного жилета. Прославленный мэром Добрососедства, он внушает уважение среди изгоев Пустоши.</TMM_Apparel_ghoul_revolutionary_frockcoat.description>

  <TMM_Apparel_ghoul_lead_lined_poncho.label>прорезиненное пончо Пустоши</TMM_Apparel_ghoul_lead_lined_poncho.label>
  <TMM_Apparel_ghoul_lead_lined_poncho.description>Тяжёлое пончо скитальца Пустоши, сшитое из прорезиненного брезента, обрезков кожи и тонких свинцовых пластин. Надёжно укрывает от токсичных бурь и кислотных дождей.</TMM_Apparel_ghoul_lead_lined_poncho.description>

  <!-- Супермутанты -->
  <TMM_Apparel_mutant_scrap_harness.label>нагрудник супермутанта из лома</TMM_Apparel_mutant_scrap_harness.label>
  <TMM_Apparel_mutant_scrap_harness.description>Массивные грубо сваренные стальные листы, ржавые дорожные знаки и тяжёлые цепи, скреплённые болтами по меркам исполинского торса супермутанта. Люди не могут его надеть.</TMM_Apparel_mutant_scrap_harness.description>

  <TMM_Apparel_mutant_rebar_cage_helmet.label>шлем-клетка супермутанта</TMM_Apparel_mutant_rebar_cage_helmet.label>
  <TMM_Apparel_mutant_rebar_cage_helmet.description>Грубая железная клетка, сваренная из толстой арматуры и стальных обрезков по размеру массивного черепа супермутанта. Подходит только супермутантам.</TMM_Apparel_mutant_rebar_cage_helmet.description>

  <!-- Кланы нечисти -->
  <TMM_Apparel_clan_nocturnal_aristocrat_coat.label>бархатный плащ ночного клана</TMM_Apparel_clan_nocturnal_aristocrat_coat.label>
  <TMM_Apparel_clan_nocturnal_aristocrat_coat.description>Длинный полуночно-чёрный плащ из плотного бархата и кожи с высоким воротником, который носят старейшины вампиров. Скроен для бесшумного движения и внушения трепета.</TMM_Apparel_clan_nocturnal_aristocrat_coat.description>

  <TMM_Apparel_clan_bioferrite_carapace.label>живой биоферритовый панцирь</TMM_Apparel_clan_bioferrite_carapace.label>
  <TMM_Apparel_clan_bioferrite_carapace.description>Органические багрово-чёрные хитиновые пластины, выращенные из закалённого биоферрита и крови тварей. Надевается в слой силовой брони поверх одежды, ускоряет заживление ран на 25% и сам восстанавливает прочность.</TMM_Apparel_clan_bioferrite_carapace.description>

</LanguageData>
```

`Languages/Russian/DefInjected/RecipeDef/Recipes_FactionApparel.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Make_demon_infernal_trenchcoat.label>сшить плащ адского оперативника</TMM_Make_demon_infernal_trenchcoat.label>
  <TMM_Make_demon_infernal_trenchcoat.description>Сшить тяжёлый огнестойкий кожаный плащ адского оперативника.</TMM_Make_demon_infernal_trenchcoat.description>
  <TMM_Make_demon_infernal_trenchcoat.jobString>Шьёт плащ адского оперативника.</TMM_Make_demon_infernal_trenchcoat.jobString>

  <TMM_Make_angel_celestial_raiment.label>выковать облачение небесного воинства</TMM_Make_angel_celestial_raiment.label>
  <TMM_Make_angel_celestial_raiment.description>Изготовить бело-золотые енохианские латы с прорезями под крылья ангела.</TMM_Make_angel_celestial_raiment.description>
  <TMM_Make_angel_celestial_raiment.jobString>Куёт облачение небесного воинства.</TMM_Make_angel_celestial_raiment.jobString>

  <TMM_Make_bos_t60_power_armor.label>собрать силовую броню T-60 Братства</TMM_Make_bos_t60_power_armor.label>
  <TMM_Make_bos_t60_power_armor.description>Собрать тяжёлую силовую броню T-60 Братства Стали на урановых элементах.</TMM_Make_bos_t60_power_armor.description>
  <TMM_Make_bos_t60_power_armor.jobString>Собирает силовую броню T-60.</TMM_Make_bos_t60_power_armor.jobString>

  <TMM_Make_bos_t60_helmet.label>собрать шлем силовой брони T-60</TMM_Make_bos_t60_helmet.label>
  <TMM_Make_bos_t60_helmet.description>Собрать бронешлем силовой брони T-60 со встроенным респиратором.</TMM_Make_bos_t60_helmet.description>
  <TMM_Make_bos_t60_helmet.jobString>Собирает шлем T-60.</TMM_Make_bos_t60_helmet.jobString>

  <TMM_Make_outcast_power_armor.label>собрать силовую броню Изгоев</TMM_Make_outcast_power_armor.label>
  <TMM_Make_outcast_power_armor.description>Собрать чёрно-красную силовую броню T-45d Изгоев.</TMM_Make_outcast_power_armor.description>
  <TMM_Make_outcast_power_armor.jobString>Собирает силовую броню Изгоев.</TMM_Make_outcast_power_armor.jobString>

  <TMM_Make_enclave_x01_power_armor.label>собрать силовую броню X-01 Анклава</TMM_Make_enclave_x01_power_armor.label>
  <TMM_Make_enclave_x01_power_armor.description>Собрать композитную силовую броню X-01 Анклава.</TMM_Make_enclave_x01_power_armor.description>
  <TMM_Make_enclave_x01_power_armor.jobString>Собирает силовую броню X-01.</TMM_Make_enclave_x01_power_armor.jobString>

  <TMM_Make_enclave_x01_helmet.label>собрать шлем силовой брони X-01</TMM_Make_enclave_x01_helmet.label>
  <TMM_Make_enclave_x01_helmet.description>Собрать бронешлем X-01 с янтарными окулярами.</TMM_Make_enclave_x01_helmet.description>
  <TMM_Make_enclave_x01_helmet.jobString>Собирает шлем X-01.</TMM_Make_enclave_x01_helmet.jobString>

  <TMM_Make_institute_courser_uniform.label>изготовить плащ Охотника Института</TMM_Make_institute_courser_uniform.label>
  <TMM_Make_institute_courser_uniform.description>Изготовить бронированный плащ ликвидатора Института из гиперткани и керамики.</TMM_Make_institute_courser_uniform.description>
  <TMM_Make_institute_courser_uniform.jobString>Изготавливает плащ Охотника.</TMM_Make_institute_courser_uniform.jobString>

  <TMM_Make_railroad_armored_coat.label>сшить бронеплащ Подземки</TMM_Make_railroad_armored_coat.label>
  <TMM_Make_railroad_armored_coat.description>Сшить неприметный плащ путника с подкладкой из баллистического волокна Подземки.</TMM_Make_railroad_armored_coat.description>
  <TMM_Make_railroad_armored_coat.jobString>Шьёт бронеплащ Подземки.</TMM_Make_railroad_armored_coat.jobString>

  <TMM_Make_minutemen_general_uniform.label>сшить мундир генерала Минитменов</TMM_Make_minutemen_general_uniform.label>
  <TMM_Make_minutemen_general_uniform.description>Сшить усиленный колониальный сюртук генерала Минитменов.</TMM_Make_minutemen_general_uniform.description>
  <TMM_Make_minutemen_general_uniform.jobString>Шьёт мундир Минитменов.</TMM_Make_minutemen_general_uniform.jobString>

  <TMM_Make_mol_covert_trenchcoat.label>сшить тактический плащ лондонского ордена</TMM_Make_mol_covert_trenchcoat.label>
  <TMM_Make_mol_covert_trenchcoat.description>Изготовить непромокаемый плащ Просвещённых со скрытым кевларом и оберегами.</TMM_Make_mol_covert_trenchcoat.description>
  <TMM_Make_mol_covert_trenchcoat.jobString>Шьёт тактический плащ ордена.</TMM_Make_mol_covert_trenchcoat.jobString>

  <TMM_Make_ghoul_revolutionary_frockcoat.label>сшить алый революционный сюртук</TMM_Make_ghoul_revolutionary_frockcoat.label>
  <TMM_Make_ghoul_revolutionary_frockcoat.description>Сшить алый сюртук в стиле мэра Добрососедства.</TMM_Make_ghoul_revolutionary_frockcoat.description>
  <TMM_Make_ghoul_revolutionary_frockcoat.jobString>Шьёт алый сюртук.</TMM_Make_ghoul_revolutionary_frockcoat.jobString>

  <TMM_Make_clan_bioferrite_carapace.label>вырастить живой панцирь кланов</TMM_Make_clan_bioferrite_carapace.label>
  <TMM_Make_clan_bioferrite_carapace.description>Вырастить органический самовосстанавливающийся панцирь из пластали и дьяволоткани.</TMM_Make_clan_bioferrite_carapace.description>
  <TMM_Make_clan_bioferrite_carapace.jobString>Выращивает живой панцирь.</TMM_Make_clan_bioferrite_carapace.jobString>

  <TMM_Make_clan_bioferrite_carapace_Anomaly.label>вырастить живой биоферритовый панцирь</TMM_Make_clan_bioferrite_carapace_Anomaly.label>
  <TMM_Make_clan_bioferrite_carapace_Anomaly.description>Сформировать живой хитиновый панцирь из закалённого биоферрита, пластали и дьяволоткани.</TMM_Make_clan_bioferrite_carapace_Anomaly.description>
  <TMM_Make_clan_bioferrite_carapace_Anomaly.jobString>Формирует биоферритовый панцирь.</TMM_Make_clan_bioferrite_carapace_Anomaly.jobString>

</LanguageData>
```

`Tests/InGameFactionApparelProbe.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    /// <summary>
    /// In-game verification probe for all 32 faction apparel items &amp; 10 research projects:
    /// - Wears one item per faction and verifies armor + stat bonuses;
    /// - Verifies angel_only apparel blocks humans and fits TMM_Angel;
    /// - Verifies super_mutant_only apparel blocks humans and fits TMM_SuperMutant;
    /// - Verifies quest recipe gating (locked without flag, unlocked after SparkStory.SetFlag);
    /// - Verifies Power Armor uses layer TMM_PowerArmor and stacks over Shell + TMM_Backpack;
    /// - Verifies special effects (MoveSpeed +0.08 on nocturnal coat, -35% demon damage on angel raiment,
    ///   self-repair on living bioferrite carapace).
    /// Activated via -tmm-faction-apparel-probe.
    /// </summary>
    public sealed class InGameFactionApparelProbe : MapComponent
    {
        private sealed class CheckItem
        {
            public string name;
            public bool passed;
            public string expected;
            public string actual;
        }

        private static bool s_completed;
        private int m_ticksWaited;
        private List<CheckItem> m_checks = new List<CheckItem>();
        private List<string> m_errors = new List<string>();

        public InGameFactionApparelProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-faction-apparel-probe", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }

        public override void MapComponentTick()
        {
            if (s_completed || !ProbeEnabled) return;
            if (map != Find.AnyPlayerHomeMap) return;

            m_ticksWaited++;
            if (m_ticksWaited < 30) return;

            s_completed = true;
            Application.logMessageReceived += OnUnityLog;

            try
            {
                RunAllChecks();
            }
            catch (Exception ex)
            {
                m_errors.Add("Unhandled exception: " + ex.ToString());
            }
            finally
            {
                Application.logMessageReceived -= OnUnityLog;
                WriteReport("FactionApparelProbe.json");
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

        private void RunAllChecks()
        {
            IntVec3 origin = FindClearCenter();

            // ---------------- 1. ALL 10 RESEARCH PROJECTS LOADED ----------------
            string[] researchIds = new string[]
            {
                "TMM_WastelandMilitaryGear",
                "TMM_VaultTecStandards",
                "TMM_RadSurvivalGear",
                "TMM_HuntersMilitiaCraft",
                "TMM_OccultTailoring",
                "TMM_SuperMutantArmor",
                "TMM_UndergroundElectronics",
                "TMM_BrotherhoodGear",
                "TMM_InstituteTextiles",
                "TMM_ClanBioferriteCraft"
            };
            int researchLoaded = 0;
            for (int i = 0; i < researchIds.Length; i++)
            {
                if (DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchIds[i]) != null) researchLoaded++;
            }
            AddCheck("Research_All10Loaded", researchLoaded == 10, "10", researchLoaded.ToString());

            // ---------------- 2. ONE ITEM PER FACTION: WEAR + ARMOR & STAT CHECK ----------------
            VerifyHumanApparelStat("Faction_Hell_Suit", "TMM_Apparel_demon_crossroads_suit", StatDefOf.SocialImpact, 0.15f, 0.15f, origin);
            VerifyHumanApparelStat("Faction_BoS_Scribe", "TMM_Apparel_bos_scribe_fatigues", StatDefOf.ResearchSpeed, 0.10f, 0.20f, origin);
            VerifyHumanApparelStat("Faction_Outcasts_Recon", "TMM_Apparel_outcast_recon_underarmor", StatDefOf.MoveSpeed, 0.08f, 0.30f, origin);
            VerifyHumanApparelStat("Faction_Enclave_Officer", "TMM_Apparel_enclave_officer_uniform", StatDefOf.SocialImpact, 0.12f, 0.25f, origin);
            VerifyHumanApparelStat("Faction_Institute_LabCoat", "TMM_Apparel_institute_lab_coat", StatDefOf.ResearchSpeed, 0.14f, 0.14f, origin);
            VerifyHumanApparelStat("Faction_Railroad_Coat", "TMM_Apparel_railroad_armored_coat", StatDefOf.MoveSpeed, 0.06f, 0.90f, origin);
            VerifyHumanApparelStat("Faction_Gunners_Combat", "TMM_Apparel_gunner_combat_armor", StatDefOf.AimingDelayFactor, -0.02f, 0.88f, origin, true);
            VerifyHumanApparelStat("Faction_VaultTec_Jumpsuit", "TMM_Apparel_vault_tec_jumpsuit", StatDefOf.WorkSpeedGlobal, 0.06f, 0.20f, origin);
            VerifyHumanApparelStat("Faction_Hunters_Duster", "TMM_Apparel_hunter_leather_duster", StatDefOf.MeleeDodgeChance, 0.04f, 0.50f, origin);
            VerifyHumanApparelStat("Faction_MoL_TweedSuit", "TMM_Apparel_mol_arcane_tweed_suit", StatDefOf.ResearchSpeed, 0.10f, 0.42f, origin);
            VerifyHumanApparelStat("Faction_Ghouls_Frockcoat", "TMM_Apparel_ghoul_revolutionary_frockcoat", StatDefOf.SocialImpact, 0.15f, 0.40f, origin);

            // Nocturnal Clan Velvet Coat: specifically verify Claude's MoveSpeed +0.08 balance rule
            Pawn clanTester = SpawnPawn(null, Faction.OfPlayer, origin);
            float baseMove = clanTester.GetStatValue(StatDefOf.MoveSpeed);
            Apparel nocturnalCoat = MakeApparel("TMM_Apparel_clan_nocturnal_aristocrat_coat");
            if (nocturnalCoat != null) clanTester.apparel.Wear(nocturnalCoat, false);
            float moveDelta = clanTester.GetStatValue(StatDefOf.MoveSpeed) - baseMove;
            AddCheck("Faction_MonsterClans_NocturnalCoatSpeed008",
                nocturnalCoat != null && clanTester.apparel.WornApparel.Contains(nocturnalCoat) && moveDelta >= 0.06f && moveDelta <= 0.11f,
                "delta ~ +0.08", string.Format("{0:+0.000;-0.000}", moveDelta));

            // ---------------- 3. RACE RESTRICTION: ANGEL ONLY ----------------
            Pawn human = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
            Pawn angel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(3, 0, 0));
            ThingDef angelCoatDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Apparel_angel_vessel_trenchcoat");
            Apparel angelCoat = MakeApparel("TMM_Apparel_angel_vessel_trenchcoat");

            bool humanCanHasParts = angelCoatDef != null && ApparelUtility.HasPartsToWear(human, angelCoatDef);
            string humanCantReason;
            bool humanCanEquip = angelCoat != null && EquipmentUtility.CanEquip(angelCoat, human, out humanCantReason); // CHECK API
            if (angelCoat != null) human.apparel.Wear(angelCoat, false);
            bool humanWoreIt = angelCoat != null && human.apparel.WornApparel.Contains(angelCoat);

            bool angelCanHasParts = angelCoatDef != null && angel != null && ApparelUtility.HasPartsToWear(angel, angelCoatDef);
            if (angel != null && angelCoat != null) angel.apparel.Wear(angelCoat, false);
            bool angelWoreIt = angel != null && angelCoat != null && angel.apparel.WornApparel.Contains(angelCoat);

            AddCheck("Race_AngelCoatBlockedOnHuman", !humanCanHasParts && !humanCanEquip && !humanWoreIt,
                "all false", string.Format("hasParts={0}, canEquip={1}, worn={2}", humanCanHasParts, humanCanEquip, humanWoreIt));
            AddCheck("Race_AngelCoatWornByAngel", angelCanHasParts && angelWoreIt,
                "hasParts=true, worn=true", string.Format("hasParts={0}, worn={1}", angelCanHasParts, angelWoreIt));

            // ---------------- 4. RACE RESTRICTION: SUPER MUTANT ONLY ----------------
            Pawn mutant = SpawnPawn("TMM_SuperMutant", Faction.OfPlayer, origin + new IntVec3(-2, 0, 0));
            ThingDef harnessDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Apparel_mutant_scrap_harness");
            Apparel harness = MakeApparel("TMM_Apparel_mutant_scrap_harness");
            Apparel cageHelmet = MakeApparel("TMM_Apparel_mutant_rebar_cage_helmet");

            bool humanCanMutantArmor = harnessDef != null && ApparelUtility.HasPartsToWear(human, harnessDef);
            if (harness != null) human.apparel.Wear(harness, false);
            bool humanWoreMutantArmor = harness != null && human.apparel.WornApparel.Contains(harness);

            bool mutantCanArmor = harnessDef != null && mutant != null && ApparelUtility.HasPartsToWear(mutant, harnessDef);
            if (mutant != null && harness != null) mutant.apparel.Wear(harness, false);
            if (mutant != null && cageHelmet != null) mutant.apparel.Wear(cageHelmet, false);
            bool mutantWoreBoth = mutant != null && harness != null && cageHelmet != null &&
                                  mutant.apparel.WornApparel.Contains(harness) &&
                                  mutant.apparel.WornApparel.Contains(cageHelmet);

            AddCheck("Race_MutantArmorBlockedOnHuman", !humanCanMutantArmor && !humanWoreMutantArmor,
                "both false", string.Format("hasParts={0}, worn={1}", humanCanMutantArmor, humanWoreMutantArmor));
            AddCheck("Race_MutantArmorWornBySuperMutant", mutantCanArmor && mutantWoreBoth,
                "both worn", string.Format("hasParts={0}, bothWorn={1}", mutantCanArmor, mutantWoreBoth));

            // ---------------- 5. QUEST RECIPE FLAG GATING ----------------
            SparkStory story = SparkStory.Instance;
            RecipeDef t60Recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("TMM_Make_bos_t60_power_armor");
            RecipeDef coatRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("TMM_Make_demon_infernal_trenchcoat");
            if (story != null && t60Recipe != null && coatRecipe != null)
            {
                story.SetFlag("unlock.bos_t60_power_armor", "false"); // CHECK API
                story.SetFlag("unlock.bos_t60_armor", "false");
                bool lockedBefore = !t60Recipe.AvailableNow;

                story.SetFlag("unlock.bos_t60_power_armor", "true"); // CHECK API
                bool unlockedAfter = t60Recipe.AvailableNow;

                story.SetFlag("unlock.demon_infernal_trenchcoat", "false");
                story.SetFlag("unlock.hell_operative_coat", "false");
                bool coatLockedBefore = !coatRecipe.AvailableNow;
                story.SetFlag("unlock.demon_infernal_trenchcoat", "true");
                bool coatUnlockedAfter = coatRecipe.AvailableNow;

                AddCheck("QuestRecipe_LockedWithoutFlagAndUnlockedWithFlag",
                    lockedBefore && unlockedAfter && coatLockedBefore && coatUnlockedAfter,
                    "t60(lock->open) & coat(lock->open)",
                    string.Format("t60({0}->{1}), coat({2}->{3})", lockedBefore, unlockedAfter, coatLockedBefore, coatUnlockedAfter));
            }
            else
            {
                AddCheck("QuestRecipe_LockedWithoutFlagAndUnlockedWithFlag", false, "story & recipes exist",
                    string.Format("story={0}, t60={1}, coat={2}", story != null, t60Recipe != null, coatRecipe != null));
            }

            // ---------------- 6. POWER ARMOR LAYER TMM_PowerArmor & STACKING ----------------
            string[] paDefs = new string[]
            {
                "TMM_Apparel_bos_t60_power_armor",
                "TMM_Apparel_outcast_power_armor",
                "TMM_Apparel_enclave_x01_power_armor",
                "TMM_Apparel_clan_bioferrite_carapace"
            };
            bool allInPowerLayer = true;
            for (int i = 0; i < paDefs.Length; i++)
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(paDefs[i]);
                if (d == null || d.apparel == null || d.apparel.layers == null || !HasLayer(d.apparel.layers, "TMM_PowerArmor"))
                {
                    allInPowerLayer = false;
                }
            }

            Pawn paWearer = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 3));
            Apparel duster = MakeApparel("TMM_Apparel_railroad_armored_coat");
            Apparel backpack = MakeApparel("TMM_Backpack_Canvas");
            Apparel t60 = MakeApparel("TMM_Apparel_bos_t60_power_armor");
            if (duster != null) paWearer.apparel.Wear(duster, false);
            if (backpack != null) paWearer.apparel.Wear(backpack, false);
            if (t60 != null) paWearer.apparel.Wear(t60, false);

            bool stackedCleanly = duster != null && t60 != null &&
                                  paWearer.apparel.WornApparel.Contains(duster) &&
                                  paWearer.apparel.WornApparel.Contains(t60) &&
                                  (backpack == null || paWearer.apparel.WornApparel.Contains(backpack));
            AddCheck("PowerArmor_LayerTMM_PowerArmorAndStacks", allInPowerLayer && stackedCleanly,
                "all 4 in TMM_PowerArmor & stacks over coat+backpack",
                string.Format("allLayer={0}, stacked={1}", allInPowerLayer, stackedCleanly));

            // ---------------- 7. SPECIAL EFFECTS: SELF-REPAIR CARAPACE & ANGEL DAMAGE REDUCTION ----------------
            Apparel carapace = MakeApparel("TMM_Apparel_clan_bioferrite_carapace");
            bool repaired = false;
            if (carapace != null)
            {
                carapace.HitPoints = Mathf.Max(1, carapace.MaxHitPoints - 25);
                int hpBefore = carapace.HitPoints;
                CompLivingCarapaceRepair rep = carapace.GetComp<CompLivingCarapaceRepair>();
                if (rep != null) rep.StepRepair(600);
                repaired = carapace.HitPoints > hpBefore;
            }
            AddCheck("Special_BioferriteCarapaceSelfRepair", repaired, "HP increased", repaired.ToString());

            Pawn demonAttacker = SpawnPawn("TMM_Demon", FindHostileFaction(), origin + new IntVec3(-4, 0, 2));
            Pawn bareAngel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(-3, 0, 2));
            Pawn armoredAngel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(-3, 0, 3));
            Apparel raiment = MakeApparel("TMM_Apparel_angel_celestial_raiment");
            if (armoredAngel != null && raiment != null) armoredAngel.apparel.Wear(raiment, false);

            float bareDmg = ApplyTorsoDamageFrom(bareAngel, demonAttacker, DamageDefOf.Blunt, 20f);
            float armoredDmg = ApplyTorsoDamageFrom(armoredAngel, demonAttacker, DamageDefOf.Blunt, 20f);
            AddCheck("Special_AngelRaimentReducesDemonDamage",
                armoredAngel != null && raiment != null && armoredAngel.apparel.WornApparel.Contains(raiment) && armoredDmg < bareDmg * 0.75f,
                string.Format("< {0:0.0}", bareDmg * 0.75f),
                string.Format("armored={0:0.0}, bare={1:0.0}", armoredDmg, bareDmg));
        }

        private void VerifyHumanApparelStat(string checkName, string defName, StatDef stat, float minDelta, float minSharpArmor, IntVec3 origin, bool lowerIsBetter = false)
        {
            Pawn p = SpawnPawn(null, Faction.OfPlayer, origin);
            Apparel ap = MakeApparel(defName);
            if (p == null || ap == null)
            {
                AddCheck(checkName, false, "pawn & apparel", "null");
                return;
            }

            float before = p.GetStatValue(stat);
            p.apparel.Wear(ap, false);
            bool worn = p.apparel.WornApparel.Contains(ap);
            float after = p.GetStatValue(stat);
            float delta = after - before;
            float sharp = ap.GetStatValue(StatDefOf.ArmorRating_Sharp);

            bool statOk = lowerIsBetter ? (delta <= minDelta) : (delta >= minDelta);
            bool armorOk = sharp >= minSharpArmor;
            AddCheck(checkName, worn && statOk && armorOk,
                string.Format("worn, sharp>={0:0.00}, delta{1}{2:0.00}", minSharpArmor, lowerIsBetter ? "<=" : ">=", minDelta),
                string.Format("worn={0}, sharp={1:0.00}, delta={2:+0.00;-0.00}", worn, sharp, delta));
        }

        private static Apparel MakeApparel(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                if (def.stuffCategories != null && def.stuffCategories.Contains(StuffCategoryDefOf.Leathery))
                {
                    stuff = ThingDefOf.Leather_Plain;
                }
                else
                {
                    stuff = GenStuff.DefaultStuffFor(def);
                }
            }
            return ThingMaker.MakeThing(def, stuff) as Apparel;
        }

        private static bool HasLayer(List<ApparelLayerDef> layers, string defName)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].defName == defName) return true;
            }
            return false;
        }

        private static float ApplyTorsoDamageFrom(Pawn victim, Pawn attacker, DamageDef dmgDef, float amount)
        {
            if (victim == null || victim.Dead || dmgDef == null) return 0f;
            BodyPartRecord torso = null;
            List<BodyPartRecord> parts = victim.RaceProps.body.AllParts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].def == BodyPartDefOf.Torso)
                {
                    torso = parts[i];
                    break;
                }
            }
            DamageWorker.DamageResult res = victim.TakeDamage(new DamageInfo(dmgDef, amount, 2.0f, -1f, attacker, torso)); // CHECK API
            return res != null ? res.totalDamageDealt : 0f;
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

            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(nearCell, map, 6, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
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
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(map.Center, map, 10, delegate(IntVec3 c)
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