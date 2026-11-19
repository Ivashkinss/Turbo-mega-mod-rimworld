`Defs/HediffDefs/Hediffs_GearSets.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- SET 1: DEAN WINCHESTER — "OLDER BROTHER"                              -->
  <!-- Dean's worn leather jacket + Dean's sawed-off double-barrel           -->
  <!-- +10% MoveSpeed + immunity to supernatural fear / panic                -->
  <!-- ===================================================================== -->

  <HediffDef>
    <defName>TMM_Hediff_Set_Dean</defName>
    <label>Older Brother (set bonus)</label>
    <description>Wearing Dean's worn leather jacket while wielding his sawed-off shotgun grants +10% movement speed and complete immunity to supernatural fear and panic.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.90, 0.72, 0.35)</defaultLabelColor>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <statFactors>
          <MoveSpeed>1.10</MoveSpeed>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <!-- ===================================================================== -->
  <!-- SET 2: CASTIEL — "REBEL SERAPH"                                       -->
  <!-- Castiel's rumpled trenchcoat + Castiel's angel blade                  -->
  <!-- +15% MoveSpeed + 20% cooldown reduction on angelic abilities          -->
  <!-- ===================================================================== -->

  <HediffDef>
    <defName>TMM_Hediff_Set_Castiel</defName>
    <label>Rebel Seraph (set bonus)</label>
    <description>Wearing Castiel's rumpled trenchcoat while wielding his angel blade grants +15% movement speed and reduces the cooldown of angelic abilities by 20%.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.55, 0.85, 1.0)</defaultLabelColor>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <statFactors>
          <MoveSpeed>1.15</MoveSpeed>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <!-- ===================================================================== -->
  <!-- SET 3: KELLOGG — "INSTITUTE MERCENARY"                                -->
  <!-- Kellogg's armored mercenary outfit + Kellogg's .44 revolver           -->
  <!-- -15% AimingDelayFactor                                                -->
  <!-- ===================================================================== -->

  <HediffDef>
    <defName>TMM_Hediff_Set_Kellogg</defName>
    <label>Institute Mercenary (set bonus)</label>
    <description>Wearing Kellogg's armored mercenary outfit while wielding his .44 revolver reduces aiming delay by 15%.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.75, 0.78, 0.82)</defaultLabelColor>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <statFactors>
          <AimingDelayFactor>0.85</AimingDelayFactor>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

</Defs>
```

`Defs/ThingDefs/Hero_SignatureGear.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- PROJECTILES FOR SIGNATURE RANGED WEAPONS                              -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_DeanShotgun</defName>
    <label>rock-salt buckshot</label>
    <thingClass>TurboMegaMod.Bullet_DeanRockSalt</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Shotgun</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef MayRequire="Ludeon.RimWorld.Biotech">TMM_RockSalt</damageDef>
      <damageDef MayRequire="!Ludeon.RimWorld.Biotech">Bullet</damageDef>
      <damageAmountBase>24</damageAmountBase>
      <armorPenetrationBase>0.25</armorPenetrationBase>
      <stoppingPower>2.8</stoppingPower>
      <speed>60</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_KelloggRevolver</defName>
    <label>.44 magnum round</label>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Bullet</damageDef>
      <damageAmountBase>26</damageAmountBase>
      <armorPenetrationBase>0.45</armorPenetrationBase>
      <stoppingPower>1.8</stoppingPower>
      <speed>74</speed>
    </projectile>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 1. DEAN WINCHESTER: JACKET & SAWED-OFF SHOTGUN (SET: OLDER BROTHER)   -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_dean_leather_jacket</defName>
    <label>Dean's worn leather jacket</label>
    <description>A scuffed dark-brown cowhide car coat with faded seams, brass snaps, and deep pockets lined with rock-salt pouches and a hidden demon-trap charm. Part of the Older Brother set.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Jacket/Jacket</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>260</MaxHitPoints>
      <MarketValue>950</MarketValue>
      <Mass>2.4</Mass>
      <ArmorRating_Sharp>0.58</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.32</ArmorRating_Blunt>
      <ArmorRating_Heat>0.40</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>2.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.05</ShootingAccuracyPawn>
      <PainShockThreshold>0.08</PainShockThreshold>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Jacket/Jacket</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Sig_dean_sawed_off_shotgun</defName>
    <label>Dean's sawed-off double-barrel</label>
    <description>A short-barreled 12-gauge double-barrel shotgun with a dark walnut stock, kept in the trunk of a '67 Impala and loaded with coarse rock-salt shells that stagger and stun demons and anomalies at close range. Part of the Older Brother set.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/Shotgun</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Shotgun</soundInteract>
    <statBases>
      <MarketValue>880</MarketValue>
      <Mass>2.6</Mass>
      <AccuracyTouch>0.86</AccuracyTouch>
      <AccuracyShort>0.82</AccuracyShort>
      <AccuracyMedium>0.64</AccuracyMedium>
      <AccuracyLong>0.40</AccuracyLong>
      <RangedWeapon_Cooldown>1.3</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_DeanShotgun</defaultProjectile>
        <warmupTime>0.75</warmupTime>
        <range>14</range>
        <burstShotCount>1</burstShotCount>
        <soundCast>Shot_Shotgun</soundCast>
        <soundCastTail>GunTail_Heavy</soundCastTail>
        <muzzleFlashScale>10</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>stock</label>
        <capacities><li>Blunt</li></capacities>
        <power>9</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
      <li>
        <label>barrels</label>
        <capacities><li>Blunt</li><li>Poke</li></capacities>
        <power>9</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
    </tools>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 3. SAM WINCHESTER: LAYERED HUNTER PARKA                               -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_sam_hunting_flannel</defName>
    <label>Sam's layered hunter parka</label>
    <description>A practical olive canvas field jacket worn over a plaid flannel shirt and dark hoodie, with inner pockets stuffed with lore notebooks and Enochian spell slips.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Jacket/Jacket</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>250</MaxHitPoints>
      <MarketValue>920</MarketValue>
      <Mass>2.2</Mass>
      <ArmorRating_Sharp>0.50</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.28</ArmorRating_Blunt>
      <ArmorRating_Heat>0.35</ArmorRating_Heat>
      <Insulation_Cold>22</Insulation_Cold>
      <Insulation_Heat>12</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.14</ResearchSpeed>
      <MeleeDodgeChance>0.05</MeleeDodgeChance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Jacket/Jacket</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 4. PRESTON GARVEY: COLONIAL HAT                                       -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_preston_garvey_hat</defName>
    <label>Preston Garvey's colonial hat</label>
    <description>A wide-brimmed tan felt colonial hat with one side pinned up by a tarnished brass Minutemen badge. A symbol of hope for struggling settlements.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/CowboyHat/CowboyHat</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>150</MaxHitPoints>
      <MarketValue>640</MarketValue>
      <Mass>0.4</Mass>
      <ArmorRating_Sharp>0.24</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.14</ArmorRating_Blunt>
      <ArmorRating_Heat>0.22</ArmorRating_Heat>
      <Insulation_Cold>10</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>0.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.18</SocialImpact>
      <NegotiationAbility>0.12</NegotiationAbility>
    </equippedStatOffsets>
    <thingCategories><li>Headgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>UpperHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/CowboyHat/CowboyHat</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 5. BOBBY SINGER: TRUCKER CAP                                          -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_bobby_singer_cap_vest</defName>
    <label>Bobby Singer's trucker cap</label>
    <description>A faded navy-blue mesh trucker cap stained with motor oil and sweat, worn by the gruffest lorekeeper in the hunter network.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>140</MaxHitPoints>
      <MarketValue>520</MarketValue>
      <Mass>0.2</Mass>
      <ArmorRating_Sharp>0.14</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.10</ArmorRating_Blunt>
      <ArmorRating_Heat>0.18</ArmorRating_Heat>
      <Insulation_Cold>8</Insulation_Cold>
      <Insulation_Heat>10</Insulation_Heat>
      <EquipDelay>0.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.12</ResearchSpeed>
      <GeneralLaborSpeed>0.08</GeneralLaborSpeed>
    </equippedStatOffsets>
    <thingCategories><li>Headgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>UpperHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 6 & 7. CASTIEL: TRENCHCOAT (angel_only) & ANGEL BLADE (SET)           -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_castiel_trenchcoat</defName>
    <label>Castiel's rumpled trenchcoat</label>
    <description>A battered beige trenchcoat worn over a dark suit and crooked blue tie, subtly scorched along the hems by celestial fire and tailored with reinforced back slits for angelic wings. Can only be worn by Angels. Part of the Rebel Seraph set.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>290</MaxHitPoints>
      <MarketValue>1450</MarketValue>
      <Mass>2.3</Mass>
      <ArmorRating_Sharp>0.62</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.34</ArmorRating_Blunt>
      <ArmorRating_Heat>0.70</ArmorRating_Heat>
      <Insulation_Cold>22</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>2.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MedicalTendQuality>0.10</MedicalTendQuality>
      <PsychicSensitivity>0.20</PsychicSensitivity>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li><li>TMM_AngelApparel</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearExtension">
        <requiredXenotype>TMM_Angel</requiredXenotype>
        <wingFriendly>true</wingFriendly>
      </li>
    </modExtensions>
  </ThingDef>

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Sig_castiel_angel_blade</defName>
    <label>Castiel's angel blade</label>
    <description>A sleek, triangular silver-steel stiletto carried inside the sleeve of a rebel angel. Forged in Heaven, it slays regular demons and angels in a single thrust, while inflicting heavy damage against powerful named heroes. Part of the Rebel Seraph set.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Knife</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>2600</MarketValue>
      <Mass>0.9</Mass>
    </statBases>
    <tools>
      <li>
        <label>celestial point</label>
        <capacities><li>Stab</li><li>Cut</li></capacities>
        <power>32</power>
        <armorPenetration>1.20</armorPenetration>
        <cooldownTime>1.2</cooldownTime>
        <extraMeleeDamages MayRequire="Ludeon.RimWorld.Biotech">
          <li>
            <def>TMM_Enochian</def>
            <amount>8</amount>
            <armorPenetration>1.20</armorPenetration>
          </li>
        </extraMeleeDamages>
      </li>
    </tools>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearExtension">
        <slaysRegularDemons>true</slaysRegularDemons>
        <slaysRegularAngels>true</slaysRegularAngels>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 8. MICHAEL: RADIANT CUIRASS (angel_only)                              -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_michael_archangel_plate</defName>
    <label>Michael's radiant cuirass</label>
    <description>Ornate white-gold commander armor etched with blazing celestial runes, leaving open vents along the shoulder blades for archangel wings. Can only be worn by Angels.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>440</MaxHitPoints>
      <MarketValue>3800</MarketValue>
      <Mass>7.5</Mass>
      <Flammability>0.05</Flammability>
      <ArmorRating_Sharp>1.22</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.60</ArmorRating_Blunt>
      <ArmorRating_Heat>0.95</ArmorRating_Heat>
      <Insulation_Cold>26</Insulation_Cold>
      <Insulation_Heat>26</Insulation_Heat>
      <EquipDelay>5.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.25</SocialImpact>
      <MeleeHitChance>0.08</MeleeHitChance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Neck</li><li>Shoulders</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li><li>TMM_AngelApparel</li></tags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearExtension">
        <requiredXenotype>TMM_Angel</requiredXenotype>
        <wingFriendly>true</wingFriendly>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 9. LUCIFER: IMMACULATE WHITE SUIT                                     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_lucifer_white_suit</defName>
    <label>Lucifer's immaculate white suit</label>
    <description>A razor-sharp white tailored suit with subtle silver pinstripes, radiating unsettling charisma and cold, theatrical arrogance.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>300</MaxHitPoints>
      <MarketValue>2900</MarketValue>
      <Mass>1.6</Mass>
      <Flammability>0.05</Flammability>
      <ArmorRating_Sharp>0.56</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.30</ArmorRating_Blunt>
      <ArmorRating_Heat>0.95</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>28</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.30</SocialImpact>
      <PsychicSensitivity>0.25</PsychicSensitivity>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 10. CROWLEY: KING OF HELL SUIT                                        -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_crowley_king_suit</defName>
    <label>Crowley's King of Hell suit</label>
    <description>A bespoke charcoal-black Savile Row suit with a crimson pocket square and hidden sigil stitching. Built for negotiating souls in style.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>290</MaxHitPoints>
      <MarketValue>2400</MarketValue>
      <Mass>1.6</Mass>
      <Flammability>0.05</Flammability>
      <ArmorRating_Sharp>0.52</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.28</ArmorRating_Blunt>
      <ArmorRating_Heat>0.95</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>26</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <TradePriceImprovement>0.18</TradePriceImprovement>
      <NegotiationAbility>0.22</NegotiationAbility>
      <SocialImpact>0.20</SocialImpact>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 11. RUBY: DEMON-KILLING KNIFE                                         -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Sig_ruby_demon_knife</defName>
    <label>Ruby's demon-killing knife</label>
    <description>An ancient curved hunting dagger with a stag-horn handle and a blade covered in worn, arcane engravings. One deep strike destroys regular demons permanently, while dealing heavy damage to named demon heroes.</description>
    <techLevel>Medieval</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Knife</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>2700</MarketValue>
      <Mass>0.8</Mass>
    </statBases>
    <tools>
      <li>
        <label>cursed edge</label>
        <capacities><li>Stab</li><li>Cut</li></capacities>
        <power>28</power>
        <armorPenetration>1.15</armorPenetration>
        <cooldownTime>1.1</cooldownTime>
        <extraMeleeDamages MayRequire="Ludeon.RimWorld.Biotech">
          <li>
            <def>TMM_HolyWater</def>
            <amount>6</amount>
            <armorPenetration>1.15</armorPenetration>
          </li>
        </extraMeleeDamages>
      </li>
    </tools>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearExtension">
        <slaysRegularDemons>true</slaysRegularDemons>
        <slaysRegularAngels>false</slaysRegularAngels>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 12. ARTHUR MAXSON: ELDER BATTLECOAT                                   -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_maxson_battlecoat</defName>
    <label>Maxson's Elder battlecoat</label>
    <description>A heavy sheepskin-lined brown leather bomber trenchcoat worn over Brotherhood combat fatigues, emblazoned with the Elder's steel crest.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>340</MaxHitPoints>
      <MarketValue>2300</MarketValue>
      <Mass>3.8</Mass>
      <ArmorRating_Sharp>0.88</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.46</ArmorRating_Blunt>
      <ArmorRating_Heat>0.55</ArmorRating_Heat>
      <Insulation_Cold>30</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>3.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.25</SocialImpact>
      <AimingDelayFactor>-0.06</AimingDelayFactor>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 13. PALADIN DANSE: RECON HOOD                                         -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_danse_recon_hood</defName>
    <label>Paladin Danse's recon hood</label>
    <description>A tight olive-drab ballistic skullcap with padded ear cups and a chin strap, designed to cushion the head inside heavy T-60 helmets.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/SimpleHelmet/SimpleHelmet</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>180</MaxHitPoints>
      <MarketValue>720</MarketValue>
      <Mass>0.8</Mass>
      <ArmorRating_Sharp>0.45</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.32</ArmorRating_Blunt>
      <ArmorRating_Heat>0.28</ArmorRating_Heat>
      <Insulation_Cold>14</Insulation_Cold>
      <Insulation_Heat>6</Insulation_Heat>
      <EquipDelay>1.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.06</ShootingAccuracyPawn>
      <PainShockThreshold>0.06</PainShockThreshold>
    </equippedStatOffsets>
    <thingCategories><li>ArmorHeadgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>UpperHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/SimpleHelmet/SimpleHelmet</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 14. HENRY CASDIN: OUTCAST COMMAND HELMET                              -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_casdin_protector_helm</defName>
    <label>Casdin's Outcast command helmet</label>
    <description>A matte-black T-45d power armor helmet painted with a crimson stripe across the brow and upgraded target-acquisition optics.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>250</MaxHitPoints>
      <MarketValue>1650</MarketValue>
      <Mass>3.1</Mass>
      <Flammability>0.1</Flammability>
      <ArmorRating_Sharp>1.15</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.52</ArmorRating_Blunt>
      <ArmorRating_Heat>0.72</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>10</Insulation_Heat>
      <EquipDelay>2.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.08</ShootingAccuracyPawn>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.50</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories><li>ArmorHeadgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>FullHead</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmorHelmet/PowerArmorHelmet</wornGraphicPath> <!-- TODO art -->
      <layers><li>Overhead</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 15. COLONEL AUTUMN: LEATHER GREATCOAT                                 -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_autumn_enclave_greatcoat</defName>
    <label>Colonel Autumn's leather greatcoat</label>
    <description>A heavy dark-slate leather command greatcoat with red piping, brass buttons, and concealed ballistic mesh lining.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>350</MaxHitPoints>
      <MarketValue>2450</MarketValue>
      <Mass>3.6</Mass>
      <ArmorRating_Sharp>0.90</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.45</ArmorRating_Blunt>
      <ArmorRating_Heat>0.60</ArmorRating_Heat>
      <Insulation_Cold>24</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>3.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.22</SocialImpact>
      <SuppressionPower MayRequire="Ludeon.RimWorld.Ideology">0.25</SuppressionPower>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 16. FATHER (SHAUN): DIRECTOR LAB COAT                                 -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_father_director_coat</defName>
    <label>Father's Director lab coat</label>
    <description>An immaculate white synthetic coat with subtle silver trim, worn by the supreme Director of the Institute.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>250</MaxHitPoints>
      <MarketValue>2100</MarketValue>
      <Mass>1.3</Mass>
      <ArmorRating_Sharp>0.34</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.18</ArmorRating_Blunt>
      <ArmorRating_Heat>0.40</ArmorRating_Heat>
      <Insulation_Cold>14</Insulation_Cold>
      <Insulation_Heat>20</Insulation_Heat>
      <EquipDelay>1.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.25</ResearchSpeed>
      <SocialImpact>0.20</SocialImpact>
      <MedicalTendQuality>0.12</MedicalTendQuality>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 17 & 18. KELLOGG: ARMORED OUTFIT & .44 REVOLVER (SET: MERCENARY)      -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_kellogg_cyber_jacket</defName>
    <label>Kellogg's armored mercenary outfit</label>
    <description>A rugged brown leather jacket with a single bolted metal shoulder pauldron and reinforced cybernetic interface straps. Part of the Institute Mercenary set.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Jacket/Jacket</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>330</MaxHitPoints>
      <MarketValue>1850</MarketValue>
      <Mass>3.4</Mass>
      <ArmorRating_Sharp>0.84</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.44</ArmorRating_Blunt>
      <ArmorRating_Heat>0.45</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>2.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.08</ShootingAccuracyPawn>
      <MeleeDamageFactor>0.12</MeleeDamageFactor>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Jacket/Jacket</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Sig_kellogg_revolver</defName>
    <label>Kellogg's .44 revolver</label>
    <description>A heavy nickel-plated .44 magnum revolver with a dark wood grip and hair-trigger mechanism, infamous across the wasteland. Part of the Institute Mercenary set.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/Revolver</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Revolver</soundInteract>
    <statBases>
      <MarketValue>1650</MarketValue>
      <Mass>1.8</Mass>
      <AccuracyTouch>0.88</AccuracyTouch>
      <AccuracyShort>0.86</AccuracyShort>
      <AccuracyMedium>0.74</AccuracyMedium>
      <AccuracyLong>0.55</AccuracyLong>
      <RangedWeapon_Cooldown>1.2</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_KelloggRevolver</defaultProjectile>
        <warmupTime>0.30</warmupTime>
        <range>25</range>
        <burstShotCount>1</burstShotCount>
        <soundCast>Shot_Revolver</soundCast>
        <soundCastTail>GunTail_Light</soundCastTail>
        <muzzleFlashScale>9</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>grip</label>
        <capacities><li>Blunt</li></capacities>
        <power>9</power>
        <cooldownTime>1.8</cooldownTime>
      </li>
      <li>
        <label>barrel</label>
        <capacities><li>Blunt</li><li>Poke</li></capacities>
        <power>9</power>
        <cooldownTime>1.8</cooldownTime>
      </li>
    </tools>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 19. DESDEMONA: BALLISTIC FATIGUES                                     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_desdemona_railroad_coat</defName>
    <label>Desdemona's ballistic fatigues</label>
    <description>A dark olive-grey field jacket interwoven with dense ballistic weave and lined with hidden pockets for cipher keys and synth holotapes.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>330</MaxHitPoints>
      <MarketValue>1950</MarketValue>
      <Mass>3.1</Mass>
      <ArmorRating_Sharp>0.92</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.46</ArmorRating_Blunt>
      <ArmorRating_Heat>0.52</ArmorRating_Heat>
      <Insulation_Cold>22</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.6</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.20</SocialImpact>
      <MoveSpeed>0.10</MoveSpeed>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 20. DEACON: UNDERCOVER SHADES (EyeCover w/ Ideology, Overhead w/o)    -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_deacon_sunglasses</defName>
    <label>Deacon's undercover shades</label>
    <description>Classic black-framed tinted sunglasses paired with a subtle micro-transceiver earpiece. Helps the wearer keep a stone-cold poker face.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>120</MaxHitPoints>
      <MarketValue>680</MarketValue>
      <Mass>0.1</Mass>
      <ArmorRating_Sharp>0.08</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.05</ArmorRating_Blunt>
      <ArmorRating_Heat>0.10</ArmorRating_Heat>
      <EquipDelay>0.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.15</SocialImpact>
      <NegotiationAbility>0.15</NegotiationAbility>
      <ShootingAccuracyPawn>0.04</ShootingAccuracyPawn>
    </equippedStatOffsets>
    <thingCategories><li>Headgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Eyes</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</wornGraphicPath> <!-- TODO art -->
      <layers>
        <li MayRequire="Ludeon.RimWorld.Ideology">EyeCover</li>
        <li MayRequire="!Ludeon.RimWorld.Ideology">Overhead</li>
      </layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 21. CAPTAIN WES: COMMAND COAT                                         -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_wes_gunner_coat</defName>
    <label>Captain Wes's command coat</label>
    <description>A dark olive mercenary greatcoat layered over reinforced combat armor plates, marked with silver captain bars and the Gunners' skull emblem.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>340</MaxHitPoints>
      <MarketValue>1850</MarketValue>
      <Mass>4.2</Mass>
      <ArmorRating_Sharp>0.94</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.46</ArmorRating_Blunt>
      <ArmorRating_Heat>0.45</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>14</Insulation_Heat>
      <EquipDelay>3.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.06</ShootingAccuracyPawn>
      <AimingDelayFactor>-0.06</AimingDelayFactor>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 22. DR. STANISLAUS BRAUN: SIMULATION SUIT                             -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_braun_white_suit</defName>
    <label>Dr. Braun's simulation suit</label>
    <description>A spotless pre-war cream-white suit fitted with gold cufflinks and neural-interface sensors hidden beneath the collar.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>220</MaxHitPoints>
      <MarketValue>1950</MarketValue>
      <Mass>1.4</Mass>
      <ArmorRating_Sharp>0.28</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.16</ArmorRating_Blunt>
      <ArmorRating_Heat>0.30</ArmorRating_Heat>
      <Insulation_Cold>14</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>1.8</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.22</ResearchSpeed>
      <GeneralLaborSpeed>0.10</GeneralLaborSpeed>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 23. DR. HESS: ARCANE MONOCLE (EyeCover w/ Ideology, Overhead w/o)     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_dr_hess_monocle</defName>
    <label>Dr. Hess's arcane monocle</label>
    <description>A gold-rimmed crystal monocle etched with microscopic Enochian sigils, allowing the wearer to read faint magical residues and spot hidden illusions.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>130</MaxHitPoints>
      <MarketValue>1750</MarketValue>
      <Mass>0.1</Mass>
      <ArmorRating_Sharp>0.05</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.05</ArmorRating_Blunt>
      <ArmorRating_Heat>0.10</ArmorRating_Heat>
      <EquipDelay>0.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ResearchSpeed>0.18</ResearchSpeed>
      <ShootingAccuracyPawn>0.06</ShootingAccuracyPawn>
      <PsychicSensitivity>-0.30</PsychicSensitivity>
    </equippedStatOffsets>
    <thingCategories><li>Headgear</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Eyes</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Tuque/Tuque</wornGraphicPath> <!-- TODO art -->
      <layers>
        <li MayRequire="Ludeon.RimWorld.Ideology">EyeCover</li>
        <li MayRequire="!Ludeon.RimWorld.Ideology">Overhead</li>
      </layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 24. ARTHUR KETCH: BESPOKE ASSASSIN SUIT                               -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_ketch_arsenal_briefcase_coat</defName>
    <label>Arthur Ketch's bespoke assassin suit</label>
    <description>A razor-tailored charcoal British suit lined with flexible ballistic mesh and concealed holsters for enchanted blades and pistols.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>310</MaxHitPoints>
      <MarketValue>2150</MarketValue>
      <Mass>2.2</Mass>
      <ArmorRating_Sharp>0.78</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.38</ArmorRating_Blunt>
      <ArmorRating_Heat>0.50</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <ShootingAccuracyPawn>0.07</ShootingAccuracyPawn>
      <MeleeHitChance>0.07</MeleeHitChance>
      <MoveSpeed>0.12</MoveSpeed>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 25. JOHN HANCOCK: RED FROCK COAT                                      -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_hancock_tricorn_coat</defName>
    <label>Hancock's red frock coat</label>
    <description>A weathered, gold-trimmed crimson colonial frock coat worn with a matching tricorn hat, symbolizing Goodneighbor's fierce independence.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>280</MaxHitPoints>
      <MarketValue>1450</MarketValue>
      <Mass>2.5</Mass>
      <ArmorRating_Sharp>0.56</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.28</ArmorRating_Blunt>
      <ArmorRating_Heat>0.35</ArmorRating_Heat>
      <Insulation_Cold>18</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.2</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.25</SocialImpact>
      <MeleeDodgeChance>0.06</MeleeDodgeChance>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 26. KENT CONNOLLY: SILVER SHROUD TRENCHCOAT                           -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_silver_shroud_costume</defName>
    <label>Silver Shroud trenchcoat &amp; fedora</label>
    <description>A dramatic silver-grey trenchcoat and matching wide-brimmed fedora modeled after a pre-war radio vigilante, reinforced with modern ballistic padding.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>300</MaxHitPoints>
      <MarketValue>1600</MarketValue>
      <Mass>2.9</Mass>
      <ArmorRating_Sharp>0.76</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.38</ArmorRating_Blunt>
      <ArmorRating_Heat>0.40</ArmorRating_Heat>
      <Insulation_Cold>20</Insulation_Cold>
      <Insulation_Heat>16</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.20</SocialImpact>
      <AimingDelayFactor>-0.05</AimingDelayFactor>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 27. FAWKES: VAULT 87 JUMPSUIT (super_mutant_only)                     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_fawkes_vault87_harness</defName>
    <label>Fawkes's torn Vault 87 jumpsuit</label>
    <description>A heavily patched, oversized blue-and-yellow Vault 87 jumpsuit reinforced with leather straps and steel plates to fit a super mutant leader. Can only be worn by Super Mutants.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PlateArmor/PlateArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>440</MaxHitPoints>
      <MarketValue>1650</MarketValue>
      <Mass>12.0</Mass>
      <ArmorRating_Sharp>0.92</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.50</ArmorRating_Blunt>
      <ArmorRating_Heat>0.55</ArmorRating_Heat>
      <Insulation_Cold>22</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>5.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <SocialImpact>0.20</SocialImpact>
      <ResearchSpeed>0.10</ResearchSpeed>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PlateArmor/PlateArmor</wornGraphicPath> <!-- TODO art -->
      <layers><li>Middle</li><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li><li>TMM_SuperMutantApparel</li></tags>
    </apparel>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearExtension">
        <requiredXenotype>TMM_SuperMutant</requiredXenotype>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 28. SWAN: RUSTED SHIP ANCHOR (SUPER MUTANT OR POWER ARMOR ONLY)       -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Sig_swan_anchor_club</defName>
    <label>Swan's rusted ship anchor</label>
    <description>A massive, barnacle-encrusted iron ship anchor ripped from a sunken hull and wielded as a devastating two-handed club. Too heavy (18 kg) for ordinary humans—can only be wielded by Super Mutants or soldiers wearing Power Armor.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Mace</texPath> <!-- TODO art (vanilla Mace avoids Royalty dependency) -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MarketValue>2400</MarketValue>
      <Mass>18.0</Mass>
    </statBases>
    <tools>
      <li>
        <label>anchor fluke</label>
        <capacities><li>Blunt</li></capacities>
        <power>44</power>
        <armorPenetration>0.72</armorPenetration>
        <cooldownTime>2.6</cooldownTime>
      </li>
      <li>
        <label>iron shank</label>
        <capacities><li>Blunt</li></capacities>
        <power>24</power>
        <cooldownTime>2.2</cooldownTime>
      </li>
    </tools>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearExtension">
        <requiresMutantOrPowerArmor>true</requiresMutantOrPowerArmor>
      </li>
    </modExtensions>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 29. ALPHA VAMPIRE: ANCIENT CLOAK (MoveSpeed +0.10 per Claude's rule)  -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_alpha_vampire_cloak</defName>
    <label>Alpha Vampire's ancient cloak</label>
    <description>A regal, floor-length obsidian silk and leather cloak lined with dark crimson velvet, radiating ancient predatory authority.</description>
    <techLevel>Industrial</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>320</MaxHitPoints>
      <MarketValue>2500</MarketValue>
      <Mass>2.4</Mass>
      <ArmorRating_Sharp>0.74</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.36</ArmorRating_Blunt>
      <ArmorRating_Heat>0.30</ArmorRating_Heat>
      <Insulation_Cold>28</Insulation_Cold>
      <Insulation_Heat>12</Insulation_Heat>
      <EquipDelay>2.4</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MoveSpeed>0.10</MoveSpeed>
      <MeleeDodgeChance>0.10</MeleeDodgeChance>
      <SocialImpact>0.20</SocialImpact>
    </equippedStatOffsets>
    <thingCategories><li>ApparelArmor</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/Duster/Duster</wornGraphicPath> <!-- TODO art -->
      <layers><li>Shell</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_ApparelMods"><maxSlots>1</maxSlots></li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 30. DICK ROMAN: BILLIONAIRE SUIT                                      -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Sig_dick_roman_corporate_suit</defName>
    <label>Dick Roman's billionaire suit</label>
    <description>An ultra-expensive navy-blue bespoke corporate suit with a crisp white shirt and silk tie, exuding ruthless executive power.</description>
    <techLevel>Spacer</techLevel>
    <generateCommonality>0</generateCommonality>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <MaxHitPoints>300</MaxHitPoints>
      <MarketValue>3100</MarketValue>
      <Mass>1.5</Mass>
      <ArmorRating_Sharp>0.62</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.34</ArmorRating_Blunt>
      <ArmorRating_Heat>0.45</ArmorRating_Heat>
      <Insulation_Cold>16</Insulation_Cold>
      <Insulation_Heat>18</Insulation_Heat>
      <EquipDelay>2.0</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <TradePriceImprovement>0.20</TradePriceImprovement>
      <SocialImpact>0.25</SocialImpact>
      <SuppressionPower MayRequire="Ludeon.RimWorld.Ideology">0.20</SuppressionPower>
    </equippedStatOffsets>
    <thingCategories><li>ApparelMisc</li></thingCategories>
    <apparel>
      <bodyPartGroups><li>Torso</li><li>Legs</li><li>Arms</li></bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShirtButton/ShirtButton</wornGraphicPath> <!-- TODO art -->
      <layers><li>OnSkin</li><li>Middle</li></layers>
      <tags><li>TMM_SignatureGear</li></tags>
    </apparel>
  </ThingDef>

</Defs>
```

`Defs/RecipeDefs/Recipes_SignatureGear.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!--
    Story-gated recipes for all 30 signature hero items.
    SignatureGearHarmony.Postfix_RecipeDef_AvailableNow checks SparkStory for
    requiredStoryFlag ("unlock.<id>=true") OR any alias in altStoryFlags (set by Story_<faction>.xml).
  -->

  <!-- 1. Dean's worn leather jacket -->
  <RecipeDef>
    <defName>TMM_Make_Sig_dean_leather_jacket</defName>
    <label>craft Dean's worn leather jacket</label>
    <description>Tailor a rugged dark-brown cowhide car coat lined with rock-salt pockets.</description>
    <jobString>Tailoring Dean's leather jacket.</jobString>
    <workAmount>6200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><categories><li>Leathers</li></categories></filter><count>85</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>10</count></li>
    </ingredients>
    <products><TMM_Sig_dean_leather_jacket>1</TMM_Sig_dean_leather_jacket></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.dean_leather_jacket</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 2. Dean's sawed-off double-barrel -->
  <RecipeDef>
    <defName>TMM_Make_Sig_dean_sawed_off_shotgun</defName>
    <label>craft Dean's sawed-off double-barrel</label>
    <description>Assemble a compact 12-gauge double-barrel shotgun loaded with rock-salt shells.</description>
    <jobString>Machining Dean's shotgun.</jobString>
    <workAmount>5800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>TableMachining</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>55</count></li>
      <li><filter><thingDefs><li>WoodLog</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Sig_dean_sawed_off_shotgun>1</TMM_Sig_dean_sawed_off_shotgun></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.dean_sawed_off_shotgun</requiredStoryFlag>
        <altStoryFlags><li>unlock.dean_leather_jacket</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 3. Sam's layered hunter parka -->
  <RecipeDef>
    <defName>TMM_Make_Sig_sam_hunting_flannel</defName>
    <label>craft Sam's layered hunter parka</label>
    <description>Tailor a practical layered olive field parka with concealed lore pockets.</description>
    <jobString>Tailoring Sam's parka.</jobString>
    <workAmount>5800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>55</count></li>
      <li><filter><categories><li>Leathers</li></categories></filter><count>40</count></li>
    </ingredients>
    <products><TMM_Sig_sam_hunting_flannel>1</TMM_Sig_sam_hunting_flannel></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.sam_hunting_flannel</requiredStoryFlag>
        <altStoryFlags><li>unlock.sam_jacket</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 4. Preston Garvey's colonial hat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_preston_garvey_hat</defName>
    <label>craft Preston Garvey's colonial hat</label>
    <description>Tailor a wide-brimmed colonial felt hat pinned with a brass Minutemen badge.</description>
    <jobString>Tailoring Preston's hat.</jobString>
    <workAmount>3200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>5</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><categories><li>Leathers</li></categories></filter><count>45</count></li>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>5</count></li>
    </ingredients>
    <products><TMM_Sig_preston_garvey_hat>1</TMM_Sig_preston_garvey_hat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.preston_garvey_hat</requiredStoryFlag>
        <altStoryFlags><li>unlock.preston_hat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 5. Bobby Singer's trucker cap -->
  <RecipeDef>
    <defName>TMM_Make_Sig_bobby_singer_cap_vest</defName>
    <label>craft Bobby Singer's trucker cap</label>
    <description>Stitch a sturdy navy-blue mesh trucker cap in Bobby Singer's trademark style.</description>
    <jobString>Tailoring Bobby's cap.</jobString>
    <workAmount>2400</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>4</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Cloth</li></thingDefs></filter><count>40</count></li>
    </ingredients>
    <products><TMM_Sig_bobby_singer_cap_vest>1</TMM_Sig_bobby_singer_cap_vest></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.bobby_singer_cap_vest</requiredStoryFlag>
        <altStoryFlags><li>unlock.bobby_cap</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 6. Castiel's rumpled trenchcoat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_castiel_trenchcoat</defName>
    <label>craft Castiel's rumpled trenchcoat</label>
    <description>Tailor a wing-slotted beige trenchcoat reinforced with subtle celestial thread.</description>
    <jobString>Tailoring Castiel's trenchcoat.</jobString>
    <workAmount>7600</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>90</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>10</count></li>
    </ingredients>
    <products><TMM_Sig_castiel_trenchcoat>1</TMM_Sig_castiel_trenchcoat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.castiel_trenchcoat</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 7. Castiel's angel blade -->
  <RecipeDef>
    <defName>TMM_Make_Sig_castiel_angel_blade</defName>
    <label>forge Castiel's angel blade</label>
    <description>Fabricate a triangular silver-steel angel blade capable of slaying regular demons and angels in one thrust.</description>
    <jobString>Forging angel blade.</jobString>
    <workAmount>12000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>45</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>40</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>15</count></li>
    </ingredients>
    <products><TMM_Sig_castiel_angel_blade>1</TMM_Sig_castiel_angel_blade></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.castiel_angel_blade</requiredStoryFlag>
        <altStoryFlags><li>unlock.angel_blade</li><li>unlock.castiel_trenchcoat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 8. Michael's radiant cuirass -->
  <RecipeDef>
    <defName>TMM_Make_Sig_michael_archangel_plate</defName>
    <label>forge Michael's radiant cuirass</label>
    <description>Fabricate white-gold archangel commander armor tailored for celestial wings.</description>
    <jobString>Forging Michael's cuirass.</jobString>
    <workAmount>18000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>9</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>95</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>35</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>1</count></li>
    </ingredients>
    <products><TMM_Sig_michael_archangel_plate>1</TMM_Sig_michael_archangel_plate></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.michael_archangel_plate</requiredStoryFlag>
        <altStoryFlags><li>unlock.celestial_vestments</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 9. Lucifer's immaculate white suit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_lucifer_white_suit</defName>
    <label>craft Lucifer's immaculate white suit</label>
    <description>Tailor a flameproof hyperweave white suit threaded with silver pinstripes.</description>
    <jobString>Tailoring Lucifer's white suit.</jobString>
    <workAmount>11000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Hyperweave</li></thingDefs></filter><count>60</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>10</count></li>
    </ingredients>
    <products><TMM_Sig_lucifer_white_suit>1</TMM_Sig_lucifer_white_suit></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.lucifer_white_suit</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 10. Crowley's King of Hell suit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_crowley_king_suit</defName>
    <label>craft Crowley's King of Hell suit</label>
    <description>Tailor a bespoke Savile Row demon-lord suit lined with devilstrand and silver wards.</description>
    <jobString>Tailoring Crowley's suit.</jobString>
    <workAmount>10500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>65</count></li>
      <li><filter><thingDefs><li>Hyperweave</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>20</count></li>
    </ingredients>
    <products><TMM_Sig_crowley_king_suit>1</TMM_Sig_crowley_king_suit></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.crowley_king_suit</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 11A. Ruby's demon-killing knife (Core fallback without Anomaly DLC) -->
  <RecipeDef>
    <defName>TMM_Make_Sig_ruby_demon_knife</defName>
    <label>forge Ruby's demon-killing knife</label>
    <description>Forge an ancient rune-etched demon-killing dagger from plasteel, silver, and gold.</description>
    <jobString>Forging Ruby's knife.</jobString>
    <workAmount>10000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>FueledSmithy</li><li>ElectricSmithy</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>40</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>35</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>10</count></li>
    </ingredients>
    <products><TMM_Sig_ruby_demon_knife>1</TMM_Sig_ruby_demon_knife></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.ruby_demon_knife</requiredStoryFlag>
        <hideWhenAnomalyActive>true</hideWhenAnomalyActive>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 11B. Ruby's demon-killing knife (Anomaly DLC Bioferrite version) -->
  <RecipeDef MayRequire="Ludeon.RimWorld.Anomaly">
    <defName>TMM_Make_Sig_ruby_demon_knife_Anomaly</defName>
    <label>forge Ruby's demon-killing knife</label>
    <description>Forge an ancient rune-etched demon-killing dagger from plasteel, silver, and bioferrite.</description>
    <jobString>Forging Ruby's knife.</jobString>
    <workAmount>10000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>FueledSmithy</li><li>ElectricSmithy</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>40</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>35</count></li>
      <li><filter><thingDefs><li>Bioferrite</li></thingDefs></filter><count>15</count></li>
    </ingredients>
    <products><TMM_Sig_ruby_demon_knife>1</TMM_Sig_ruby_demon_knife></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.ruby_demon_knife</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 12. Maxson's Elder battlecoat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_maxson_battlecoat</defName>
    <label>craft Maxson's Elder battlecoat</label>
    <description>Tailor a heavy sheepskin-lined armored Brotherhood command battlecoat.</description>
    <jobString>Tailoring Maxson's battlecoat.</jobString>
    <workAmount>11500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><categories><li>Leathers</li></categories></filter><count>110</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>25</count></li>
    </ingredients>
    <products><TMM_Sig_maxson_battlecoat>1</TMM_Sig_maxson_battlecoat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.maxson_battlecoat</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 13. Paladin Danse's recon hood -->
  <RecipeDef>
    <defName>TMM_Make_Sig_danse_recon_hood</defName>
    <label>craft Paladin Danse's recon hood</label>
    <description>Tailor an olive ballistic recon skullcap for power-armor operators.</description>
    <jobString>Tailoring Danse's recon hood.</jobString>
    <workAmount>4200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>5</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>45</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>10</count></li>
    </ingredients>
    <products><TMM_Sig_danse_recon_hood>1</TMM_Sig_danse_recon_hood></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.danse_recon_hood</requiredStoryFlag>
        <altStoryFlags><li>unlock.danse_hood</li><li>unlock.danse_armor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 14. Casdin's Outcast command helmet -->
  <RecipeDef>
    <defName>TMM_Make_Sig_casdin_protector_helm</defName>
    <label>fabricate Casdin's Outcast command helmet</label>
    <description>Assemble a matte-black and crimson Outcast T-45d power armor command helmet.</description>
    <jobString>Fabricating Casdin's helmet.</jobString>
    <workAmount>9200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>40</count></li>
      <li><filter><thingDefs><li>Uranium</li></thingDefs></filter><count>6</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>4</count></li>
    </ingredients>
    <products><TMM_Sig_casdin_protector_helm>1</TMM_Sig_casdin_protector_helm></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.casdin_protector_helm</requiredStoryFlag>
        <altStoryFlags><li>unlock.casdin_helmet</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 15. Colonel Autumn's leather greatcoat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_autumn_enclave_greatcoat</defName>
    <label>craft Colonel Autumn's leather greatcoat</label>
    <description>Tailor a dark-slate armored Enclave command greatcoat with red piping.</description>
    <jobString>Tailoring Autumn's greatcoat.</jobString>
    <workAmount>12000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><categories><li>Leathers</li></categories></filter><count>100</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>35</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Sig_autumn_enclave_greatcoat>1</TMM_Sig_autumn_enclave_greatcoat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.autumn_enclave_greatcoat</requiredStoryFlag>
        <altStoryFlags><li>unlock.autumn_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 16. Father's Director lab coat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_father_director_coat</defName>
    <label>craft Father's Director lab coat</label>
    <description>Weave an immaculate white Director lab coat from hyperweave and synthread.</description>
    <jobString>Tailoring Father's lab coat.</jobString>
    <workAmount>9500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Hyperweave</li></thingDefs></filter><count>55</count></li>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>45</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>5</count></li>
    </ingredients>
    <products><TMM_Sig_father_director_coat>1</TMM_Sig_father_director_coat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.father_director_coat</requiredStoryFlag>
        <altStoryFlags><li>unlock.father_white_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 17. Kellogg's armored mercenary outfit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_kellogg_cyber_jacket</defName>
    <label>craft Kellogg's armored mercenary outfit</label>
    <description>Assemble a heavy leather mercenary jacket with a bolted metal pauldron.</description>
    <jobString>Crafting Kellogg's outfit.</jobString>
    <workAmount>10200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>TableMachining</li></recipeUsers>
    <ingredients>
      <li><filter><categories><li>Leathers</li></categories></filter><count>90</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Sig_kellogg_cyber_jacket>1</TMM_Sig_kellogg_cyber_jacket></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.kellogg_cyber_jacket</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 18. Kellogg's .44 revolver -->
  <RecipeDef>
    <defName>TMM_Make_Sig_kellogg_revolver</defName>
    <label>craft Kellogg's .44 revolver</label>
    <description>Machine a heavy nickel-plated .44 magnum hair-trigger revolver.</description>
    <jobString>Machining Kellogg's revolver.</jobString>
    <workAmount>9000</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>TableMachining</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>35</count></li>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>3</count></li>
    </ingredients>
    <products><TMM_Sig_kellogg_revolver>1</TMM_Sig_kellogg_revolver></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.kellogg_revolver</requiredStoryFlag>
        <altStoryFlags><li>unlock.kellogg_cyber_jacket</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 19. Desdemona's ballistic fatigues -->
  <RecipeDef>
    <defName>TMM_Make_Sig_desdemona_railroad_coat</defName>
    <label>craft Desdemona's ballistic fatigues</label>
    <description>Tailor a dark olive-grey Railroad command coat lined with dense ballistic weave.</description>
    <jobString>Tailoring Desdemona's fatigues.</jobString>
    <workAmount>10200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>85</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Sig_desdemona_railroad_coat>1</TMM_Sig_desdemona_railroad_coat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.desdemona_railroad_coat</requiredStoryFlag>
        <altStoryFlags><li>unlock.desdemona_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 20. Deacon's undercover shades -->
  <RecipeDef>
    <defName>TMM_Make_Sig_deacon_sunglasses</defName>
    <label>craft Deacon's undercover shades</label>
    <description>Assemble tinted spy sunglasses with a concealed micro-transceiver earpiece.</description>
    <jobString>Crafting Deacon's shades.</jobString>
    <workAmount>3800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>TableMachining</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>15</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Sig_deacon_sunglasses>1</TMM_Sig_deacon_sunglasses></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.deacon_sunglasses</requiredStoryFlag>
        <altStoryFlags><li>unlock.deacon_shades</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 21. Captain Wes's command coat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_wes_gunner_coat</defName>
    <label>craft Captain Wes's command coat</label>
    <description>Assemble a reinforced olive Gunner command greatcoat over heavy combat plates.</description>
    <jobString>Crafting Captain Wes's coat.</jobString>
    <workAmount>10800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>TableMachining</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>80</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>45</count></li>
    </ingredients>
    <products><TMM_Sig_wes_gunner_coat>1</TMM_Sig_wes_gunner_coat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.wes_gunner_coat</requiredStoryFlag>
        <altStoryFlags><li>unlock.wes_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 22. Dr. Braun's simulation suit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_braun_white_suit</defName>
    <label>craft Dr. Braun's simulation suit</label>
    <description>Tailor a cream-white pre-war executive suit fitted with hidden neural sensors.</description>
    <jobString>Tailoring Dr. Braun's suit.</jobString>
    <workAmount>9200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>75</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>15</count></li>
      <li><filter><thingDefs><li>ComponentSpacer</li></thingDefs></filter><count>1</count></li>
    </ingredients>
    <products><TMM_Sig_braun_white_suit>1</TMM_Sig_braun_white_suit></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.braun_white_suit</requiredStoryFlag>
        <altStoryFlags><li>unlock.braun_sim_suit</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 23. Dr. Hess's arcane monocle -->
  <RecipeDef>
    <defName>TMM_Make_Sig_dr_hess_monocle</defName>
    <label>fabricate Dr. Hess's arcane monocle</label>
    <description>Etch Enochian optical sigils into a gold-rimmed crystal monocle.</description>
    <jobString>Fabricating Dr. Hess's monocle.</jobString>
    <workAmount>8500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>FabricationBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>20</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter><count>2</count></li>
    </ingredients>
    <products><TMM_Sig_dr_hess_monocle>1</TMM_Sig_dr_hess_monocle></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.dr_hess_monocle</requiredStoryFlag>
        <altStoryFlags><li>unlock.hess_monocle</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 24. Arthur Ketch's bespoke assassin suit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_ketch_arsenal_briefcase_coat</defName>
    <label>craft Arthur Ketch's bespoke assassin suit</label>
    <description>Tailor a charcoal British ballistic suit fitted with concealed weapon holsters.</description>
    <jobString>Tailoring Arthur Ketch's suit.</jobString>
    <workAmount>10500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>80</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>20</count></li>
    </ingredients>
    <products><TMM_Sig_ketch_arsenal_briefcase_coat>1</TMM_Sig_ketch_arsenal_briefcase_coat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.ketch_arsenal_briefcase_coat</requiredStoryFlag>
        <altStoryFlags><li>unlock.ketch_suit</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 25. Hancock's red frock coat -->
  <RecipeDef>
    <defName>TMM_Make_Sig_hancock_tricorn_coat</defName>
    <label>craft Hancock's red frock coat</label>
    <description>Tailor a gold-trimmed crimson colonial frock coat in Goodneighbor's iconic style.</description>
    <jobString>Tailoring Hancock's frock coat.</jobString>
    <workAmount>7800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>65</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>10</count></li>
    </ingredients>
    <products><TMM_Sig_hancock_tricorn_coat>1</TMM_Sig_hancock_tricorn_coat></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.hancock_tricorn_coat</requiredStoryFlag>
        <altStoryFlags><li>unlock.hancock_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 26. Silver Shroud trenchcoat & fedora -->
  <RecipeDef>
    <defName>TMM_Make_Sig_silver_shroud_costume</defName>
    <label>craft Silver Shroud trenchcoat &amp; fedora</label>
    <description>Tailor a ballistic-padded silver-grey trenchcoat inspired by Hubris Comics' radio vigilante.</description>
    <jobString>Tailoring Silver Shroud costume.</jobString>
    <workAmount>8600</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>6</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>80</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>20</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>15</count></li>
    </ingredients>
    <products><TMM_Sig_silver_shroud_costume>1</TMM_Sig_silver_shroud_costume></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.silver_shroud_costume</requiredStoryFlag>
        <altStoryFlags><li>unlock.silver_shroud_coat</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 27. Fawkes's torn Vault 87 jumpsuit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_fawkes_vault87_harness</defName>
    <label>craft Fawkes's torn Vault 87 jumpsuit</label>
    <description>Reinforce an oversized blue-and-yellow Vault 87 jumpsuit with steel plates for a Super Mutant.</description>
    <jobString>Crafting Fawkes's harness.</jobString>
    <workAmount>9800</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Machining</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>TableMachining</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Synthread</li></thingDefs></filter><count>90</count></li>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>65</count></li>
      <li><filter><categories><li>Leathers</li></categories></filter><count>50</count></li>
    </ingredients>
    <products><TMM_Sig_fawkes_vault87_harness>1</TMM_Sig_fawkes_vault87_harness></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.fawkes_vault87_harness</requiredStoryFlag>
        <altStoryFlags><li>unlock.fawkes_vault87_suit</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 28. Swan's rusted ship anchor -->
  <RecipeDef>
    <defName>TMM_Make_Sig_swan_anchor_club</defName>
    <label>forge Swan's rusted ship anchor</label>
    <description>Weld and reinforce an 18 kg ship anchor club fit for a Super Mutant or power-armored juggernaut.</description>
    <jobString>Forging Swan's anchor club.</jobString>
    <workAmount>12500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Smith</effectWorking>
    <soundWorking>Recipe_Smith</soundWorking>
    <skillRequirements><Crafting>7</Crafting></skillRequirements>
    <recipeUsers><li>FueledSmithy</li><li>ElectricSmithy</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Steel</li></thingDefs></filter><count>180</count></li>
      <li><filter><thingDefs><li>Plasteel</li></thingDefs></filter><count>25</count></li>
    </ingredients>
    <products><TMM_Sig_swan_anchor_club>1</TMM_Sig_swan_anchor_club></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.swan_anchor_club</requiredStoryFlag>
        <altStoryFlags><li>unlock.swan_anchor</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 29. Alpha Vampire's ancient cloak -->
  <RecipeDef>
    <defName>TMM_Make_Sig_alpha_vampire_cloak</defName>
    <label>craft Alpha Vampire's ancient cloak</label>
    <description>Tailor a floor-length obsidian silk and devilstrand cloak lined with crimson velvet.</description>
    <jobString>Tailoring Alpha Vampire's cloak.</jobString>
    <workAmount>11200</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>DevilstrandCloth</li></thingDefs></filter><count>95</count></li>
      <li><filter><thingDefs><li>Hyperweave</li></thingDefs></filter><count>30</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>20</count></li>
    </ingredients>
    <products><TMM_Sig_alpha_vampire_cloak>1</TMM_Sig_alpha_vampire_cloak></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.alpha_vampire_cloak</requiredStoryFlag>
      </li>
    </modExtensions>
  </RecipeDef>

  <!-- 30. Dick Roman's billionaire suit -->
  <RecipeDef>
    <defName>TMM_Make_Sig_dick_roman_corporate_suit</defName>
    <label>craft Dick Roman's billionaire suit</label>
    <description>Tailor a bespoke navy-blue hyperweave corporate suit accented with gold and silver.</description>
    <jobString>Tailoring Dick Roman's suit.</jobString>
    <workAmount>12500</workAmount>
    <workSpeedStat>GeneralLaborSpeed</workSpeedStat>
    <workSkill>Crafting</workSkill>
    <effectWorking>Tailor</effectWorking>
    <soundWorking>Recipe_Tailor</soundWorking>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <recipeUsers><li>ElectricTailoringBench</li></recipeUsers>
    <ingredients>
      <li><filter><thingDefs><li>Hyperweave</li></thingDefs></filter><count>65</count></li>
      <li><filter><thingDefs><li>Gold</li></thingDefs></filter><count>25</count></li>
      <li><filter><thingDefs><li>Silver</li></thingDefs></filter><count>25</count></li>
    </ingredients>
    <products><TMM_Sig_dick_roman_corporate_suit>1</TMM_Sig_dick_roman_corporate_suit></products>
    <modExtensions>
      <li Class="TurboMegaMod.SignatureGearRecipeExtension">
        <requiredStoryFlag>unlock.dick_roman_corporate_suit</requiredStoryFlag>
        <altStoryFlags><li>unlock.dick_roman_suit</li></altStoryFlags>
      </li>
    </modExtensions>
  </RecipeDef>

</Defs>
```

`Patches/HeroDefs_SignatureGear.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>

  <!--
    Equips all 27 catalog heroes in Defs/HeroDefs/*.xml with their signature apparel & weapons.
    Uses PatchOperationConditional so missing HeroDefs or absent <apparel>/<weapon> elements
    never produce load errors.
    CHECK API: Verify exact HeroDef root XML tag name (e.g. TurboMegaMod.HeroDef vs TMM.HeroDef)
    and whether HeroDef exposes <apparel> list. SignatureGearHeroHook in SignatureGear.cs also
    acts as a runtime guarantee on HeroRegistry.GetOrCreate.
  -->

  <!-- 1. Dean Winchester: Jacket + Sawed-Off Shotgun -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]</xpath>
    <match Class="PatchOperationSequence">
      <operations>
        <li Class="PatchOperationConditional">
          <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]/apparel</xpath>
          <match Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]/apparel</xpath>
            <value><li>TMM_Sig_dean_leather_jacket</li></value>
          </match>
          <nomatch Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]</xpath>
            <value><apparel><li>TMM_Sig_dean_leather_jacket</li></apparel></value>
          </nomatch>
        </li>
        <li Class="PatchOperationConditional">
          <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]/weapon</xpath>
          <match Class="PatchOperationReplace">
            <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]/weapon</xpath>
            <value><weapon>TMM_Sig_dean_sawed_off_shotgun</weapon></value>
          </match>
          <nomatch Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_dean_winchester" or heroId="dean_winchester"]</xpath>
            <value><weapon>TMM_Sig_dean_sawed_off_shotgun</weapon></value>
          </nomatch>
        </li>
      </operations>
    </match>
  </Operation>

  <!-- 2. Sam Winchester -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_sam_winchester" or heroId="sam_winchester"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_sam_winchester" or heroId="sam_winchester"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_sam_winchester" or heroId="sam_winchester"]/apparel</xpath>
        <value><li>TMM_Sig_sam_hunting_flannel</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_sam_winchester" or heroId="sam_winchester"]</xpath>
        <value><apparel><li>TMM_Sig_sam_hunting_flannel</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 3. Preston Garvey -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_preston_garvey" or heroId="preston_garvey"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_preston_garvey" or heroId="preston_garvey"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_preston_garvey" or heroId="preston_garvey"]/apparel</xpath>
        <value><li>TMM_Sig_preston_garvey_hat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_preston_garvey" or heroId="preston_garvey"]</xpath>
        <value><apparel><li>TMM_Sig_preston_garvey_hat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 4. Bobby Singer -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_bobby_singer" or heroId="bobby_singer"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_bobby_singer" or heroId="bobby_singer"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_bobby_singer" or heroId="bobby_singer"]/apparel</xpath>
        <value><li>TMM_Sig_bobby_singer_cap_vest</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_bobby_singer" or heroId="bobby_singer"]</xpath>
        <value><apparel><li>TMM_Sig_bobby_singer_cap_vest</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 5. Castiel: Trenchcoat + Angel Blade -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]</xpath>
    <match Class="PatchOperationSequence">
      <operations>
        <li Class="PatchOperationConditional">
          <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]/apparel</xpath>
          <match Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]/apparel</xpath>
            <value><li>TMM_Sig_castiel_trenchcoat</li></value>
          </match>
          <nomatch Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]</xpath>
            <value><apparel><li>TMM_Sig_castiel_trenchcoat</li></apparel></value>
          </nomatch>
        </li>
        <li Class="PatchOperationConditional">
          <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]/weapon</xpath>
          <match Class="PatchOperationReplace">
            <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]/weapon</xpath>
            <value><weapon>TMM_Sig_castiel_angel_blade</weapon></value>
          </match>
          <nomatch Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_castiel" or heroId="castiel"]</xpath>
            <value><weapon>TMM_Sig_castiel_angel_blade</weapon></value>
          </nomatch>
        </li>
      </operations>
    </match>
  </Operation>

  <!-- 6. Michael -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_michael" or heroId="michael"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_michael" or heroId="michael"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_michael" or heroId="michael"]/apparel</xpath>
        <value><li>TMM_Sig_michael_archangel_plate</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_michael" or heroId="michael"]</xpath>
        <value><apparel><li>TMM_Sig_michael_archangel_plate</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 7. Lucifer -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_lucifer" or heroId="lucifer"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_lucifer" or heroId="lucifer"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_lucifer" or heroId="lucifer"]/apparel</xpath>
        <value><li>TMM_Sig_lucifer_white_suit</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_lucifer" or heroId="lucifer"]</xpath>
        <value><apparel><li>TMM_Sig_lucifer_white_suit</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 8. Crowley -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_crowley" or heroId="crowley"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_crowley" or heroId="crowley"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_crowley" or heroId="crowley"]/apparel</xpath>
        <value><li>TMM_Sig_crowley_king_suit</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_crowley" or heroId="crowley"]</xpath>
        <value><apparel><li>TMM_Sig_crowley_king_suit</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 9. Ruby -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_ruby" or heroId="ruby"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_ruby" or heroId="ruby"]/weapon</xpath>
      <match Class="PatchOperationReplace">
        <xpath>Defs/*[defName="TMM_Hero_ruby" or heroId="ruby"]/weapon</xpath>
        <value><weapon>TMM_Sig_ruby_demon_knife</weapon></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_ruby" or heroId="ruby"]</xpath>
        <value><weapon>TMM_Sig_ruby_demon_knife</weapon></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 10. Arthur Maxson -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_arthur_maxson" or heroId="arthur_maxson"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_arthur_maxson" or heroId="arthur_maxson"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_arthur_maxson" or heroId="arthur_maxson"]/apparel</xpath>
        <value><li>TMM_Sig_maxson_battlecoat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_arthur_maxson" or heroId="arthur_maxson"]</xpath>
        <value><apparel><li>TMM_Sig_maxson_battlecoat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 11. Paladin Danse -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_paladin_danse" or heroId="paladin_danse"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_paladin_danse" or heroId="paladin_danse"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_paladin_danse" or heroId="paladin_danse"]/apparel</xpath>
        <value><li>TMM_Sig_danse_recon_hood</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_paladin_danse" or heroId="paladin_danse"]</xpath>
        <value><apparel><li>TMM_Sig_danse_recon_hood</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 12. Defender Henry Casdin -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_HenryCasdin" or defName="TMM_Hero_henry_casdin" or heroId="henry_casdin"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_HenryCasdin" or defName="TMM_Hero_henry_casdin" or heroId="henry_casdin"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_HenryCasdin" or defName="TMM_Hero_henry_casdin" or heroId="henry_casdin"]/apparel</xpath>
        <value><li>TMM_Sig_casdin_protector_helm</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_HenryCasdin" or defName="TMM_Hero_henry_casdin" or heroId="henry_casdin"]</xpath>
        <value><apparel><li>TMM_Sig_casdin_protector_helm</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 13. Colonel Autumn -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_colonel_autumn" or heroId="colonel_autumn"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_colonel_autumn" or heroId="colonel_autumn"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_colonel_autumn" or heroId="colonel_autumn"]/apparel</xpath>
        <value><li>TMM_Sig_autumn_enclave_greatcoat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_colonel_autumn" or heroId="colonel_autumn"]</xpath>
        <value><apparel><li>TMM_Sig_autumn_enclave_greatcoat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 14. Father (Shaun) -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_father_shaun" or heroId="father_shaun"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_father_shaun" or heroId="father_shaun"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_father_shaun" or heroId="father_shaun"]/apparel</xpath>
        <value><li>TMM_Sig_father_director_coat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_father_shaun" or heroId="father_shaun"]</xpath>
        <value><apparel><li>TMM_Sig_father_director_coat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 15. Kellogg: Outfit + Revolver -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]</xpath>
    <match Class="PatchOperationSequence">
      <operations>
        <li Class="PatchOperationConditional">
          <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]/apparel</xpath>
          <match Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]/apparel</xpath>
            <value><li>TMM_Sig_kellogg_cyber_jacket</li></value>
          </match>
          <nomatch Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]</xpath>
            <value><apparel><li>TMM_Sig_kellogg_cyber_jacket</li></apparel></value>
          </nomatch>
        </li>
        <li Class="PatchOperationConditional">
          <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]/weapon</xpath>
          <match Class="PatchOperationReplace">
            <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]/weapon</xpath>
            <value><weapon>TMM_Sig_kellogg_revolver</weapon></value>
          </match>
          <nomatch Class="PatchOperationAdd">
            <xpath>Defs/*[defName="TMM_Hero_kellogg" or heroId="kellogg"]</xpath>
            <value><weapon>TMM_Sig_kellogg_revolver</weapon></value>
          </nomatch>
        </li>
      </operations>
    </match>
  </Operation>

  <!-- 16. Desdemona -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_desdemona" or heroId="desdemona"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_desdemona" or heroId="desdemona"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_desdemona" or heroId="desdemona"]/apparel</xpath>
        <value><li>TMM_Sig_desdemona_railroad_coat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_desdemona" or heroId="desdemona"]</xpath>
        <value><apparel><li>TMM_Sig_desdemona_railroad_coat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 17. Deacon -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_deacon" or heroId="deacon"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_deacon" or heroId="deacon"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_deacon" or heroId="deacon"]/apparel</xpath>
        <value><li>TMM_Sig_deacon_sunglasses</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_deacon" or heroId="deacon"]</xpath>
        <value><apparel><li>TMM_Sig_deacon_sunglasses</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 18. Captain Wes -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_captain_wes" or heroId="captain_wes"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_captain_wes" or heroId="captain_wes"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_captain_wes" or heroId="captain_wes"]/apparel</xpath>
        <value><li>TMM_Sig_wes_gunner_coat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_captain_wes" or heroId="captain_wes"]</xpath>
        <value><apparel><li>TMM_Sig_wes_gunner_coat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 19. Dr. Stanislaus Braun -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_stanislaus_braun" or heroId="stanislaus_braun"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_stanislaus_braun" or heroId="stanislaus_braun"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_stanislaus_braun" or heroId="stanislaus_braun"]/apparel</xpath>
        <value><li>TMM_Sig_braun_white_suit</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_stanislaus_braun" or heroId="stanislaus_braun"]</xpath>
        <value><apparel><li>TMM_Sig_braun_white_suit</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 20. Dr. Hess -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_dr_hess" or heroId="dr_hess"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_dr_hess" or heroId="dr_hess"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_dr_hess" or heroId="dr_hess"]/apparel</xpath>
        <value><li>TMM_Sig_dr_hess_monocle</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_dr_hess" or heroId="dr_hess"]</xpath>
        <value><apparel><li>TMM_Sig_dr_hess_monocle</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 21. Arthur Ketch -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_arthur_ketch" or heroId="arthur_ketch"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_arthur_ketch" or heroId="arthur_ketch"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_arthur_ketch" or heroId="arthur_ketch"]/apparel</xpath>
        <value><li>TMM_Sig_ketch_arsenal_briefcase_coat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_arthur_ketch" or heroId="arthur_ketch"]</xpath>
        <value><apparel><li>TMM_Sig_ketch_arsenal_briefcase_coat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 22. John Hancock -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_john_hancock" or heroId="john_hancock"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_john_hancock" or heroId="john_hancock"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_john_hancock" or heroId="john_hancock"]/apparel</xpath>
        <value><li>TMM_Sig_hancock_tricorn_coat</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_john_hancock" or heroId="john_hancock"]</xpath>
        <value><apparel><li>TMM_Sig_hancock_tricorn_coat</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 23. Kent Connolly -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_kent_connolly" or heroId="kent_connolly"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_kent_connolly" or heroId="kent_connolly"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_kent_connolly" or heroId="kent_connolly"]/apparel</xpath>
        <value><li>TMM_Sig_silver_shroud_costume</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_kent_connolly" or heroId="kent_connolly"]</xpath>
        <value><apparel><li>TMM_Sig_silver_shroud_costume</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 24. Fawkes -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_fawkes" or heroId="fawkes"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_fawkes" or heroId="fawkes"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_fawkes" or heroId="fawkes"]/apparel</xpath>
        <value><li>TMM_Sig_fawkes_vault87_harness</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_fawkes" or heroId="fawkes"]</xpath>
        <value><apparel><li>TMM_Sig_fawkes_vault87_harness</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 25. Swan -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_swan" or heroId="swan"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_swan" or heroId="swan"]/weapon</xpath>
      <match Class="PatchOperationReplace">
        <xpath>Defs/*[defName="TMM_Hero_swan" or heroId="swan"]/weapon</xpath>
        <value><weapon>TMM_Sig_swan_anchor_club</weapon></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_swan" or heroId="swan"]</xpath>
        <value><weapon>TMM_Sig_swan_anchor_club</weapon></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 26. Alpha Vampire -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_alpha_vampire" or heroId="alpha_vampire"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_alpha_vampire" or heroId="alpha_vampire"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_alpha_vampire" or heroId="alpha_vampire"]/apparel</xpath>
        <value><li>TMM_Sig_alpha_vampire_cloak</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_alpha_vampire" or heroId="alpha_vampire"]</xpath>
        <value><apparel><li>TMM_Sig_alpha_vampire_cloak</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

  <!-- 27. Dick Roman -->
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/*[defName="TMM_Hero_dick_roman" or heroId="dick_roman"]</xpath>
    <match Class="PatchOperationConditional">
      <xpath>Defs/*[defName="TMM_Hero_dick_roman" or heroId="dick_roman"]/apparel</xpath>
      <match Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_dick_roman" or heroId="dick_roman"]/apparel</xpath>
        <value><li>TMM_Sig_dick_roman_corporate_suit</li></value>
      </match>
      <nomatch Class="PatchOperationAdd">
        <xpath>Defs/*[defName="TMM_Hero_dick_roman" or heroId="dick_roman"]</xpath>
        <value><apparel><li>TMM_Sig_dick_roman_corporate_suit</li></apparel></value>
      </nomatch>
    </match>
  </Operation>

</Patch>
```

`Source/SignatureGear.cs`

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

    public sealed class SignatureGearExtension : DefModExtension
    {
        public string requiredXenotype;
        public bool wingFriendly;
        public bool requiresMutantOrPowerArmor;
        public bool slaysRegularDemons;
        public bool slaysRegularAngels;
    }

    public sealed class SignatureGearRecipeExtension : DefModExtension
    {
        public string requiredStoryFlag;
        public List<string> altStoryFlags = new List<string>();
        public bool hideWhenAnomalyActive;
    }

    // =========================================================================
    // DEAN'S SAW-OFF SHOTGUN PROJECTILE (ROCK SALT STUN VS DEMONS / ANOMALIES)
    // =========================================================================

    public sealed class Bullet_DeanRockSalt : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;
            if (SignatureGearUtil.IsDemonOrAnomaly(victim))
            {
                if (victim.stances != null && victim.stances.stunner != null)
                {
                    victim.stances.stunner.StunFor(120, launcher); // CHECK API
                }
            }
        }
    }

    // =========================================================================
    // CORE UTILITIES: RACE CHECKS, HERO REGISTRY CHECK, SET BONUSES, STORY FLAGS
    // =========================================================================

    public static class SignatureGearUtil
    {
        private static bool s_storyChecked;
        private static Type s_storyType;
        private static PropertyInfo s_storyInstanceProp;
        private static MethodInfo s_storyGetFlagMethod;
        private static FieldInfo s_storyFlagsField;

        private static bool s_heroRegChecked;
        private static MethodInfo s_heroGetDefMethod;
        private static FieldInfo s_heroDictField;
        private static FieldInfo s_heroListField;

        // HeroDef -> signature apparel & weapon mapping (ensures GetOrCreate always equips signature gear)
        private static readonly Dictionary<string, string> HeroSignatureApparel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TMM_Hero_dean_winchester", "TMM_Sig_dean_leather_jacket" },
            { "dean_winchester", "TMM_Sig_dean_leather_jacket" },
            { "TMM_Hero_sam_winchester", "TMM_Sig_sam_hunting_flannel" },
            { "sam_winchester", "TMM_Sig_sam_hunting_flannel" },
            { "TMM_Hero_preston_garvey", "TMM_Sig_preston_garvey_hat" },
            { "preston_garvey", "TMM_Sig_preston_garvey_hat" },
            { "TMM_Hero_bobby_singer", "TMM_Sig_bobby_singer_cap_vest" },
            { "bobby_singer", "TMM_Sig_bobby_singer_cap_vest" },
            { "TMM_Hero_castiel", "TMM_Sig_castiel_trenchcoat" },
            { "castiel", "TMM_Sig_castiel_trenchcoat" },
            { "TMM_Hero_michael", "TMM_Sig_michael_archangel_plate" },
            { "michael", "TMM_Sig_michael_archangel_plate" },
            { "TMM_Hero_lucifer", "TMM_Sig_lucifer_white_suit" },
            { "lucifer", "TMM_Sig_lucifer_white_suit" },
            { "TMM_Hero_crowley", "TMM_Sig_crowley_king_suit" },
            { "crowley", "TMM_Sig_crowley_king_suit" },
            { "TMM_Hero_arthur_maxson", "TMM_Sig_maxson_battlecoat" },
            { "arthur_maxson", "TMM_Sig_maxson_battlecoat" },
            { "TMM_Hero_paladin_danse", "TMM_Sig_danse_recon_hood" },
            { "paladin_danse", "TMM_Sig_danse_recon_hood" },
            { "TMM_Hero_HenryCasdin", "TMM_Sig_casdin_protector_helm" },
            { "TMM_Hero_henry_casdin", "TMM_Sig_casdin_protector_helm" },
            { "henry_casdin", "TMM_Sig_casdin_protector_helm" },
            { "TMM_Hero_colonel_autumn", "TMM_Sig_autumn_enclave_greatcoat" },
            { "colonel_autumn", "TMM_Sig_autumn_enclave_greatcoat" },
            { "TMM_Hero_father_shaun", "TMM_Sig_father_director_coat" },
            { "father_shaun", "TMM_Sig_father_director_coat" },
            { "TMM_Hero_kellogg", "TMM_Sig_kellogg_cyber_jacket" },
            { "kellogg", "TMM_Sig_kellogg_cyber_jacket" },
            { "TMM_Hero_desdemona", "TMM_Sig_desdemona_railroad_coat" },
            { "desdemona", "TMM_Sig_desdemona_railroad_coat" },
            { "TMM_Hero_deacon", "TMM_Sig_deacon_sunglasses" },
            { "deacon", "TMM_Sig_deacon_sunglasses" },
            { "TMM_Hero_captain_wes", "TMM_Sig_wes_gunner_coat" },
            { "captain_wes", "TMM_Sig_wes_gunner_coat" },
            { "TMM_Hero_stanislaus_braun", "TMM_Sig_braun_white_suit" },
            { "stanislaus_braun", "TMM_Sig_braun_white_suit" },
            { "TMM_Hero_dr_hess", "TMM_Sig_dr_hess_monocle" },
            { "dr_hess", "TMM_Sig_dr_hess_monocle" },
            { "TMM_Hero_arthur_ketch", "TMM_Sig_ketch_arsenal_briefcase_coat" },
            { "arthur_ketch", "TMM_Sig_ketch_arsenal_briefcase_coat" },
            { "TMM_Hero_john_hancock", "TMM_Sig_hancock_tricorn_coat" },
            { "john_hancock", "TMM_Sig_hancock_tricorn_coat" },
            { "TMM_Hero_kent_connolly", "TMM_Sig_silver_shroud_costume" },
            { "kent_connolly", "TMM_Sig_silver_shroud_costume" },
            { "TMM_Hero_fawkes", "TMM_Sig_fawkes_vault87_harness" },
            { "fawkes", "TMM_Sig_fawkes_vault87_harness" },
            { "TMM_Hero_alpha_vampire", "TMM_Sig_alpha_vampire_cloak" },
            { "alpha_vampire", "TMM_Sig_alpha_vampire_cloak" },
            { "TMM_Hero_dick_roman", "TMM_Sig_dick_roman_corporate_suit" },
            { "dick_roman", "TMM_Sig_dick_roman_corporate_suit" }
        };

        private static readonly Dictionary<string, string> HeroSignatureWeapons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TMM_Hero_dean_winchester", "TMM_Sig_dean_sawed_off_shotgun" },
            { "dean_winchester", "TMM_Sig_dean_sawed_off_shotgun" },
            { "TMM_Hero_castiel", "TMM_Sig_castiel_angel_blade" },
            { "castiel", "TMM_Sig_castiel_angel_blade" },
            { "TMM_Hero_ruby", "TMM_Sig_ruby_demon_knife" },
            { "ruby", "TMM_Sig_ruby_demon_knife" },
            { "TMM_Hero_kellogg", "TMM_Sig_kellogg_revolver" },
            { "kellogg", "TMM_Sig_kellogg_revolver" },
            { "TMM_Hero_swan", "TMM_Sig_swan_anchor_club" },
            { "swan", "TMM_Sig_swan_anchor_club" }
        };

        public static bool CanPawnUseSignatureItem(Pawn pawn, ThingDef itemDef, out string failReason)
        {
            failReason = null;
            if (pawn == null || itemDef == null) return true;

            SignatureGearExtension ext = itemDef.GetModExtension<SignatureGearExtension>();
            if (ext == null) return true;

            if (!ext.requiredXenotype.NullOrEmpty() && !MatchesRequiredRace(pawn, ext.requiredXenotype))
            {
                if (ext.requiredXenotype == "TMM_Angel") failReason = "Only Angels can wear winged angelic apparel.";
                else if (ext.requiredXenotype == "TMM_SuperMutant") failReason = "Only Super Mutants fit this oversized armor.";
                else failReason = "Incompatible race (" + ext.requiredXenotype + ").";
                return false;
            }

            if (ext.requiresMutantOrPowerArmor && !IsSuperMutantOrPowerArmored(pawn))
            {
                failReason = "Too heavy (18 kg): requires Super Mutant strength or Power Armor.";
                return false;
            }

            return true;
        }

        public static bool MatchesRequiredRace(Pawn pawn, string requiredXenotype)
        {
            if (pawn == null || requiredXenotype.NullOrEmpty()) return true;

            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == requiredXenotype) return true;

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

            if (requiredXenotype == "TMM_SuperMutant" && pawn.story != null && pawn.story.bodyType != null)
            {
                if (pawn.story.bodyType.defName == "TMM_SuperMutant") return true;
            }

            return false;
        }

        public static bool IsSuperMutantOrPowerArmored(Pawn pawn)
        {
            if (pawn == null) return false;
            if (MatchesRequiredRace(pawn, "TMM_SuperMutant")) return true;

            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    Apparel ap = worn[i];
                    if (ap == null || ap.def == null || ap.def.apparel == null || ap.def.apparel.layers == null) continue;
                    for (int l = 0; l < ap.def.apparel.layers.Count; l++)
                    {
                        if (ap.def.apparel.layers[l] != null && ap.def.apparel.layers[l].defName == "TMM_PowerArmor")
                        {
                            return true;
                        }
                    }
                    if (ap.def.defName.IndexOf("PowerArmor", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }

            return false;
        }

        public static bool IsDemon(Pawn pawn)
        {
            if (pawn == null) return false;
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == "TMM_Demon") return true;
                GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_DemonHolyBane");
                if (g != null && pawn.genes.HasActiveGene(g)) return true;
            }
            return false;
        }

        public static bool IsAngel(Pawn pawn)
        {
            if (pawn == null) return false;
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == "TMM_Angel") return true;
                GeneDef g = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_AngelCelestialGrace");
                if (g != null && pawn.genes.HasActiveGene(g)) return true;
            }
            return false;
        }

        public static bool IsDemonOrAnomaly(Pawn pawn)
        {
            if (pawn == null) return false;
            if (IsDemon(pawn)) return true;
            if (ModsConfig.AnomalyActive && pawn.RaceProps != null && pawn.RaceProps.IsAnomalyEntity) return true; // CHECK API
            return false;
        }

        /// <summary>
        /// Returns true if the pawn is a registered hero inside HeroRegistry.
        /// Castiel's blade and Ruby's knife only one-shot REGULAR (non-hero) demons/angels.
        /// </summary>
        public static bool IsRegisteredHero(Pawn pawn)
        {
            if (pawn == null || HeroRegistry.Instance == null) return false;

            if (!s_heroRegChecked)
            {
                s_heroRegChecked = true;
                Type regType = HeroRegistry.Instance.GetType();
                // CHECK API: HeroRegistry lookup method or storage field
                s_heroGetDefMethod = AccessTools.Method(regType, "GetHeroDef", new Type[] { typeof(Pawn) }) ??
                                     AccessTools.Method(regType, "DefOf", new Type[] { typeof(Pawn) }) ??
                                     AccessTools.Method(regType, "IsHero", new Type[] { typeof(Pawn) });

                FieldInfo[] fields = regType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    Type ft = fields[i].FieldType;
                    if (typeof(IDictionary).IsAssignableFrom(ft) && s_heroDictField == null) s_heroDictField = fields[i];
                    else if (typeof(IList).IsAssignableFrom(ft) && s_heroListField == null) s_heroListField = fields[i];
                }
            }

            try
            {
                if (s_heroGetDefMethod != null)
                {
                    object res = s_heroGetDefMethod.Invoke(HeroRegistry.Instance, new object[] { pawn });
                    if (res is bool) return (bool)res;
                    if (res != null) return true;
                }

                if (s_heroDictField != null)
                {
                    IDictionary dict = s_heroDictField.GetValue(HeroRegistry.Instance) as IDictionary;
                    if (dict != null)
                    {
                        foreach (DictionaryEntry e in dict)
                        {
                            Pawn p = e.Value as Pawn;
                            if (p != null && (p == pawn || p.thingIDNumber == pawn.thingIDNumber)) return true;
                        }
                    }
                }

                if (s_heroListField != null)
                {
                    IList list = s_heroListField.GetValue(HeroRegistry.Instance) as IList;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            Pawn p = list[i] as Pawn;
                            if (p != null && (p == pawn || p.thingIDNumber == pawn.thingIDNumber)) return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore reflection mismatch
            }

            return false;
        }

        public static bool IsWearingDef(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.apparel == null || defName.NullOrEmpty()) return false;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] != null && worn[i].def != null && worn[i].def.defName == defName) return true;
            }
            return false;
        }

        public static bool IsWieldingDef(Pawn pawn, string defName)
        {
            return pawn != null && pawn.equipment != null && pawn.equipment.Primary != null &&
                   pawn.equipment.Primary.def != null && pawn.equipment.Primary.def.defName == defName;
        }

        /// <summary>
        /// Evaluates all 3 gear sets (Dean, Castiel, Kellogg) and adds/removes their Hediffs immediately.
        /// </summary>
        public static void RefreshGearSets(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;

            bool deanSet = IsWearingDef(pawn, "TMM_Sig_dean_leather_jacket") &&
                           IsWieldingDef(pawn, "TMM_Sig_dean_sawed_off_shotgun");
            SetHediffState(pawn, "TMM_Hediff_Set_Dean", deanSet);

            bool castielSet = IsWearingDef(pawn, "TMM_Sig_castiel_trenchcoat") &&
                              IsWieldingDef(pawn, "TMM_Sig_castiel_angel_blade");
            SetHediffState(pawn, "TMM_Hediff_Set_Castiel", castielSet);

            bool kelloggSet = IsWearingDef(pawn, "TMM_Sig_kellogg_cyber_jacket") &&
                              IsWieldingDef(pawn, "TMM_Sig_kellogg_revolver");
            SetHediffState(pawn, "TMM_Hediff_Set_Kellogg", kelloggSet);
        }

        private static void SetHediffState(Pawn pawn, string hediffDefName, bool shouldHave)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (shouldHave && existing == null)
            {
                pawn.health.AddHediff(def);
            }
            else if (!shouldHave && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        public static bool HasSetBonus(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            return def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) != null;
        }

        public static void EnsureHeroSignatureEquipment(HeroDef heroDef, Pawn pawn)
        {
            if (heroDef == null || pawn == null || pawn.Dead) return;

            string key1 = heroDef.defName;
            string key2 = null;
            FieldInfo idField = AccessTools.Field(heroDef.GetType(), "heroId"); // CHECK API
            if (idField != null)
            {
                object v = idField.GetValue(heroDef);
                if (v != null) key2 = v.ToString();
            }

            string apparelDefName = null;
            if (!key1.NullOrEmpty() && HeroSignatureApparel.TryGetValue(key1, out apparelDefName)) { }
            else if (!key2.NullOrEmpty()) HeroSignatureApparel.TryGetValue(key2, out apparelDefName);

            if (!apparelDefName.NullOrEmpty() && pawn.apparel != null && !IsWearingDef(pawn, apparelDefName))
            {
                ThingDef apDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
                string reason;
                if (apDef != null && CanPawnUseSignatureItem(pawn, apDef, out reason))
                {
                    Apparel ap = ThingMaker.MakeThing(apDef, apDef.MadeFromStuff ? GenStuff.DefaultStuffFor(apDef) : null) as Apparel;
                    if (ap != null)
                    {
                        pawn.apparel.Wear(ap, false);
                    }
                }
            }

            string weaponDefName = null;
            if (!key1.NullOrEmpty() && HeroSignatureWeapons.TryGetValue(key1, out weaponDefName)) { }
            else if (!key2.NullOrEmpty()) HeroSignatureWeapons.TryGetValue(key2, out weaponDefName);

            if (!weaponDefName.NullOrEmpty() && pawn.equipment != null && !IsWieldingDef(pawn, weaponDefName))
            {
                ThingDef wDef = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
                string reason;
                if (wDef != null && CanPawnUseSignatureItem(pawn, wDef, out reason))
                {
                    ThingWithComps w = ThingMaker.MakeThing(wDef, wDef.MadeFromStuff ? GenStuff.DefaultStuffFor(wDef) : null) as ThingWithComps;
                    if (w != null)
                    {
                        pawn.equipment.DestroyAllEquipment();
                        pawn.equipment.AddEquipment(w);
                    }
                }
            }

            RefreshGearSets(pawn);
        }

        public static bool IsRecipeUnlocked(SignatureGearRecipeExtension ext)
        {
            if (ext == null) return true;
            if (ext.hideWhenAnomalyActive && ModsConfig.AnomalyActive) return false;

            if (!ext.requiredStoryFlag.NullOrEmpty() && IsStoryFlagTrue(ext.requiredStoryFlag)) return true;

            if (ext.altStoryFlags != null)
            {
                for (int i = 0; i < ext.altStoryFlags.Count; i++)
                {
                    if (!ext.altStoryFlags[i].NullOrEmpty() && IsStoryFlagTrue(ext.altStoryFlags[i])) return true;
                }
            }
            return false;
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
    }

    // =========================================================================
    // HARMONY PATCHES (EXPLICIT REGISTRATION)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class SignatureGearHarmony
    {
        static SignatureGearHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.signaturegear");

            // 1. Race / heavy weapon guards
            MethodInfo hasParts = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear", new Type[] { typeof(Pawn), typeof(ThingDef) }); // CHECK API
            if (hasParts != null)
            {
                harmony.Patch(hasParts, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_HasPartsToWear"));
            }

            MethodInfo canEquip = AccessTools.Method(typeof(EquipmentUtility), "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }); // CHECK API
            if (canEquip != null)
            {
                harmony.Patch(canEquip, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_CanEquip"));
            }

            MethodInfo wear = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear",
                new Type[] { typeof(Apparel), typeof(bool), typeof(bool) }); // CHECK API
            if (wear != null)
            {
                harmony.Patch(wear, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_ApparelTracker_Wear"));
            }

            // 2. Set bonus activation & removal hooks (apparel + equipment + spawn)
            MethodInfo appAdded = AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded", new Type[] { typeof(Apparel) }); // CHECK API
            if (appAdded != null)
            {
                harmony.Patch(appAdded, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_ApparelChanged"));
            }

            MethodInfo appRemoved = AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved", new Type[] { typeof(Apparel) }); // CHECK API
            if (appRemoved != null)
            {
                harmony.Patch(appRemoved, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_ApparelChanged"));
            }

            MethodInfo eqAdded = AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded", new Type[] { typeof(ThingWithComps) }); // CHECK API
            if (eqAdded != null)
            {
                harmony.Patch(eqAdded, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_EquipmentChanged"));
            }

            MethodInfo eqRemoved = AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentRemoved", new Type[] { typeof(ThingWithComps) }); // CHECK API
            if (eqRemoved != null)
            {
                harmony.Patch(eqRemoved, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_EquipmentChanged"));
            }

            MethodInfo spawnSetup = AccessTools.Method(typeof(Pawn), "SpawnSetup", new Type[] { typeof(Map), typeof(bool) });
            if (spawnSetup != null)
            {
                harmony.Patch(spawnSetup, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_Pawn_SpawnSetup"));
            }

            // 3. Castiel's blade & Ruby's knife one-hit kill on REGULAR (non-hero) demons / angels
            MethodInfo postApply = AccessTools.Method(typeof(Pawn), "PostApplyDamage", new Type[] { typeof(DamageInfo), typeof(float) }); // CHECK API
            if (postApply != null)
            {
                harmony.Patch(postApply, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_Pawn_PostApplyDamage"));
            }

            // 4. Dean's Set fear/panic immunity
            MethodInfo tryMental = AccessTools.Method(typeof(MentalStateHandler), "TryStartMentalState"); // CHECK API
            if (tryMental != null)
            {
                harmony.Patch(tryMental, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_TryStartMentalState"));
            }

            MethodInfo addHediff = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff",
                new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) }); // CHECK API
            if (addHediff != null)
            {
                harmony.Patch(addHediff, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_AddHediff"));
            }

            // 5. Castiel's Set -20% cooldown on angelic abilities
            MethodInfo startCooldown = AccessTools.Method(typeof(Ability), "StartCooldown", new Type[] { typeof(int) }); // CHECK API
            if (startCooldown != null)
            {
                harmony.Patch(startCooldown, prefix: new HarmonyMethod(typeof(SignatureGearHarmony), "Prefix_Ability_StartCooldown"));
            }

            // 6. Story-flag recipe gating (unlock.<id>=true)
            PropertyInfo availProp = AccessTools.Property(typeof(RecipeDef), "AvailableNow"); // CHECK API
            if (availProp != null && availProp.GetGetMethod() != null)
            {
                harmony.Patch(availProp.GetGetMethod(), postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_RecipeDef_AvailableNow"));
            }

            // 7. HeroRegistry.GetOrCreate runtime safeguard: ensures recruited/generated heroes wear their signature gear
            Type regType = AccessTools.TypeByName("TurboMegaMod.HeroRegistry");
            if (regType != null)
            {
                MethodInfo getOrCreate = AccessTools.Method(regType, "GetOrCreate", new Type[] { typeof(HeroDef) }); // CHECK API
                if (getOrCreate != null)
                {
                    harmony.Patch(getOrCreate, postfix: new HarmonyMethod(typeof(SignatureGearHarmony), "Postfix_HeroRegistry_GetOrCreate"));
                }
            }
        }

        public static void Postfix_HasPartsToWear(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result || p == null || apparel == null) return;
            string reason;
            if (!SignatureGearUtil.CanPawnUseSignatureItem(p, apparel, out reason))
            {
                __result = false;
            }
        }

        public static void Postfix_CanEquip(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result || thing == null || pawn == null) return;
            string reason;
            if (!SignatureGearUtil.CanPawnUseSignatureItem(pawn, thing.def, out reason))
            {
                cantReason = reason;
                __result = false;
            }
        }

        public static bool Prefix_ApparelTracker_Wear(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (__instance == null || __instance.pawn == null || newApparel == null) return true;
            string reason;
            if (!SignatureGearUtil.CanPawnUseSignatureItem(__instance.pawn, newApparel.def, out reason))
            {
                return false;
            }
            return true;
        }

        public static void Postfix_ApparelChanged(Pawn_ApparelTracker __instance)
        {
            if (__instance != null && __instance.pawn != null)
            {
                SignatureGearUtil.RefreshGearSets(__instance.pawn);
            }
        }

        public static void Postfix_EquipmentChanged(Pawn_EquipmentTracker __instance)
        {
            if (__instance != null && __instance.pawn != null)
            {
                SignatureGearUtil.RefreshGearSets(__instance.pawn);
            }
        }

        public static void Postfix_Pawn_SpawnSetup(Pawn __instance)
        {
            if (__instance != null)
            {
                SignatureGearUtil.RefreshGearSets(__instance);
            }
        }

        public static void Postfix_Pawn_PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead) return;
            if (dinfo.Def != null && dinfo.Def.isRanged) return;

            Pawn attacker = dinfo.Instigator as Pawn;
            ThingDef weaponDef = dinfo.Weapon;
            if (weaponDef == null && attacker != null && attacker.equipment != null && attacker.equipment.Primary != null)
            {
                weaponDef = attacker.equipment.Primary.def;
            }
            if (weaponDef == null) return;

            SignatureGearExtension ext = weaponDef.GetModExtension<SignatureGearExtension>();
            if (ext == null || (!ext.slaysRegularDemons && !ext.slaysRegularAngels)) return;

            // Claude's rule: Heroes in HeroRegistry are NEVER one-shot by Castiel's blade or Ruby's knife.
            if (SignatureGearUtil.IsRegisteredHero(__instance)) return;

            bool shouldSlay = false;
            if (ext.slaysRegularDemons && SignatureGearUtil.IsDemon(__instance)) shouldSlay = true;
            if (ext.slaysRegularAngels && SignatureGearUtil.IsAngel(__instance)) shouldSlay = true;

            if (shouldSlay && !__instance.Dead)
            {
                __instance.Kill(dinfo);
            }
        }

        public static bool Prefix_TryStartMentalState(MentalStateHandler __instance, MentalStateDef stateDef, ref bool __result)
        {
            if (stateDef == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null || !SignatureGearUtil.HasSetBonus(pawn, "TMM_Hediff_Set_Dean")) return true;

            if (stateDef == MentalStateDefOf.PanicFlee ||
                stateDef.defName.IndexOf("Panic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                stateDef.defName.IndexOf("Terror", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static bool Prefix_AddHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff == null || hediff.def == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null || !SignatureGearUtil.HasSetBonus(pawn, "TMM_Hediff_Set_Dean")) return true;

            if (hediff.def.defName == "TMM_Hediff_WarRoarFear" ||
                hediff.def.defName.IndexOf("Fear", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
            return true;
        }

        public static void Prefix_Ability_StartCooldown(Ability __instance, ref int ticks)
        {
            if (__instance == null || __instance.pawn == null || __instance.def == null) return;
            if (!SignatureGearUtil.HasSetBonus(__instance.pawn, "TMM_Hediff_Set_Castiel")) return;

            string dn = __instance.def.defName;
            if (dn == "TMM_Ability_HealingTouch" || dn == "TMM_Ability_TrueVisage" ||
                dn.IndexOf("Angel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ticks = Mathf.Max(60, Mathf.RoundToInt(ticks * 0.80f));
            }
        }

        public static void Postfix_RecipeDef_AvailableNow(RecipeDef __instance, ref bool __result)
        {
            if (!__result || __instance == null) return;
            SignatureGearRecipeExtension ext = __instance.GetModExtension<SignatureGearRecipeExtension>();
            if (ext == null) return;

            if (!SignatureGearUtil.IsRecipeUnlocked(ext))
            {
                __result = false;
            }
        }

        public static void Postfix_HeroRegistry_GetOrCreate(HeroDef def, Pawn __result)
        {
            if (def != null && __result != null)
            {
                SignatureGearUtil.EnsureHeroSignatureEquipment(def, __result);
            }
        }
    }
}
```

`Languages/Russian/DefInjected/HediffDef/Hediffs_GearSets.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Hediff_Set_Dean.label>Старший брат (бонус комплекта)</TMM_Hediff_Set_Dean.label>
  <TMM_Hediff_Set_Dean.description>Потёртая кожаная куртка Дина и его обрез вместе дают +10% к скорости передвижения и полный иммунитет к страху от нечисти и панике.</TMM_Hediff_Set_Dean.description>

  <TMM_Hediff_Set_Castiel.label>Мятежный серафим (бонус комплекта)</TMM_Hediff_Set_Castiel.label>
  <TMM_Hediff_Set_Castiel.description>Мятый плащ Кастиэля и его ангельский клинок вместе дают +15% к скорости передвижения и сокращают перезарядку ангельских способностей на 20%.</TMM_Hediff_Set_Castiel.description>

  <TMM_Hediff_Set_Kellogg.label>Наёмник Института (бонус комплекта)</TMM_Hediff_Set_Kellogg.label>
  <TMM_Hediff_Set_Kellogg.description>Бронекуртка Келлога и его револьвер вместе сокращают задержку прицеливания на 15%.</TMM_Hediff_Set_Kellogg.description>

</LanguageData>
```

`Languages/Russian/DefInjected/ThingDef/Hero_SignatureGear.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Sig_dean_leather_jacket.label>потёртая кожаная куртка Дина</TMM_Sig_dean_leather_jacket.label>
  <TMM_Sig_dean_leather_jacket.description>Потёртая в дорогах тёмно-коричневая кожаная куртка со старыми швами, латунными кнопками и глубокими карманами под патроны с солью и амулет. Часть комплекта «Старший брат».</TMM_Sig_dean_leather_jacket.description>

  <TMM_Sig_dean_sawed_off_shotgun.label>обрез дробовика Дина</TMM_Sig_dean_sawed_off_shotgun.label>
  <TMM_Sig_dean_sawed_off_shotgun.description>Короткоствольная двустволка 12-го калибра с тёмным ореховым прикладом из багажника «Импалы», заряженная крупной каменной солью, сбивающей с ног и оглушающей демонов и аномалий вблизи. Часть комплекта «Старший брат».</TMM_Sig_dean_sawed_off_shotgun.description>

  <TMM_Sig_sam_hunting_flannel.label>многослойная куртка Сэма</TMM_Sig_sam_hunting_flannel.label>
  <TMM_Sig_sam_hunting_flannel.description>Практичная оливковая полевая куртка поверх клетчатой фланелевой рубахи и тёмной толстовки со скрытыми карманами для записных книжек и свитков с ритуалами.</TMM_Sig_sam_hunting_flannel.description>

  <TMM_Sig_preston_garvey_hat.label>шляпа Престона Гарви</TMM_Sig_preston_garvey_hat.label>
  <TMM_Sig_preston_garvey_hat.description>Широкополая песочная фетровая шляпа с загнутым полем и потускневшей латунной кокардой Минитменов. Символ надежды для разрозненных поселений.</TMM_Sig_preston_garvey_hat.description>

  <TMM_Sig_bobby_singer_cap_vest.label>кепка Бобби Сингера</TMM_Sig_bobby_singer_cap_vest.label>
  <TMM_Sig_bobby_singer_cap_vest.description>Выцветшая тёмно-синяя кепка дальнобойщика со следами машинного масла — бессменный головной убор самого ворчливого хранителя знаний.</TMM_Sig_bobby_singer_cap_vest.description>

  <TMM_Sig_castiel_trenchcoat.label>мятый плащ Кастиэля</TMM_Sig_castiel_trenchcoat.label>
  <TMM_Sig_castiel_trenchcoat.description>Потрёпанный бежевый плащ поверх тёмного костюма и криво повязанного синего галстука, слегка опалённый по краям небесным огнём и скроенный под крылья ангела. Подходит только ангелам. Часть комплекта «Мятежный серафим».</TMM_Sig_castiel_trenchcoat.description>

  <TMM_Sig_castiel_angel_blade.label>ангельский клинок Кастиэля</TMM_Sig_castiel_angel_blade.label>
  <TMM_Sig_castiel_angel_blade.description>Гладкий трёхгранный серебристый стилет, который мятежный ангел прячет в рукаве. Убивает рядовых демонов и ангелов с одного удара, нанося высокий урон именным героям. Часть комплекта «Мятежный серафим».</TMM_Sig_castiel_angel_blade.description>

  <TMM_Sig_michael_archangel_plate.label>сияющая кираса Михаила</TMM_Sig_michael_archangel_plate.label>
  <TMM_Sig_michael_archangel_plate.description>Парадная бело-золотая кираса полководца Небес с пылающими енохианскими рунами и прорезями на лопатках для крыльев архангела. Подходит только ангелам.</TMM_Sig_michael_archangel_plate.description>

  <TMM_Sig_lucifer_white_suit.label>белоснежный костюм Люцифера</TMM_Sig_lucifer_white_suit.label>
  <TMM_Sig_lucifer_white_suit.description>Безупречно скроенный белоснежный костюм в тонкую серебристую полоску, излучающий пугающую харизму и холодное высокомерие.</TMM_Sig_lucifer_white_suit.description>

  <TMM_Sig_crowley_king_suit.label>костюм Короля Ада Кроули</TMM_Sig_crowley_king_suit.label>
  <TMM_Sig_crowley_king_suit.description>Сшитый на заказ угольно-чёрный костюм с алым нагрудным платком и потайными печатями в швах — идеальный наряд для торговли душами.</TMM_Sig_crowley_king_suit.description>

  <TMM_Sig_ruby_demon_knife.label>нож Руби</TMM_Sig_ruby_demon_knife.label>
  <TMM_Sig_ruby_demon_knife.description>Древний изогнутый охотничий кинжал с рукоятью из оленьего рога и клинком, покрытым стёртыми рунами. Один точный удар убивает рядовых демонов, нанося высокий урон именным героям-демонам.</TMM_Sig_ruby_demon_knife.description>

  <TMM_Sig_maxson_battlecoat.label>боевой плащ старейшины Мэксона</TMM_Sig_maxson_battlecoat.label>
  <TMM_Sig_maxson_battlecoat.description>Тяжёлый коричневый кожаный плащ на овчине поверх боевой формы Братства Стали с нашивкой верховного старейшины.</TMM_Sig_maxson_battlecoat.description>

  <TMM_Sig_danse_recon_hood.label>подшлемник паладина Данса</TMM_Sig_danse_recon_hood.label>
  <TMM_Sig_danse_recon_hood.description>Плотный оливковый баллистический подшлемник с мягкими накладками, гасящий вибрацию внутри тяжёлого шлема T-60.</TMM_Sig_danse_recon_hood.description>

  <TMM_Sig_casdin_protector_helm.label>командирский шлем Касдина</TMM_Sig_casdin_protector_helm.label>
  <TMM_Sig_casdin_protector_helm.description>Матово-чёрный шлем силовой брони T-45d с алой полосой на лбу и модернизированной оптикой наведения.</TMM_Sig_casdin_protector_helm.description>

  <TMM_Sig_autumn_enclave_greatcoat.label>кожаная шинель полковника Отема</TMM_Sig_autumn_enclave_greatcoat.label>
  <TMM_Sig_autumn_enclave_greatcoat.description>Тяжёлая тёмно-серая кожаная командирская шинель с красной окантовкой, латунными пуговицами и скрытой баллистической подкладкой.</TMM_Sig_autumn_enclave_greatcoat.description>

  <TMM_Sig_father_director_coat.label>белый халат Отца</TMM_Sig_father_director_coat.label>
  <TMM_Sig_father_director_coat.description>Безупречно белый синтетический халат с тонкой серебристой отделкой — одеяние верховного директора Института.</TMM_Sig_father_director_coat.description>

  <TMM_Sig_kellogg_cyber_jacket.label>бронекуртка Келлога</TMM_Sig_kellogg_cyber_jacket.label>
  <TMM_Sig_kellogg_cyber_jacket.description>Грубая коричневая кожаная куртка с одним прикрученным болтами стальным наплечником и ремнями для кибернетических имплантов. Часть комплекта «Наёмник Института».</TMM_Sig_kellogg_cyber_jacket.description>

  <TMM_Sig_kellogg_revolver.label>револьвер Келлога</TMM_Sig_kellogg_revolver.label>
  <TMM_Sig_kellogg_revolver.description>Тяжёлый никелированный револьвер 44-го калибра с тёмной деревянной рукоятью и облегчённым спуском, печально известный на всю Пустошь. Часть комплекта «Наёмник Института».</TMM_Sig_kellogg_revolver.description>

  <TMM_Sig_desdemona_railroad_coat.label>бронекуртка Дездемоны</TMM_Sig_desdemona_railroad_coat.label>
  <TMM_Sig_desdemona_railroad_coat.description>Тёмная оливково-серая полевая куртка, прошитая плотным баллистическим волокном со скрытыми карманами для шифров и голозаписей.</TMM_Sig_desdemona_railroad_coat.description>

  <TMM_Sig_deacon_sunglasses.label>тёмные очки Дьякона</TMM_Sig_deacon_sunglasses.label>
  <TMM_Sig_deacon_sunglasses.description>Классические тёмные очки в чёрной оправе со скрытым микропередатчиком. Помогают сохранять невозмутимый вид при любой легенде.</TMM_Sig_deacon_sunglasses.description>

  <TMM_Sig_wes_gunner_coat.label>командирский плащ капитана Уэса</TMM_Sig_wes_gunner_coat.label>
  <TMM_Sig_wes_gunner_coat.description>Тёмно-оливковый наёмничий плащ поверх усиленных армейских бронепластин с серебряными знаками капитана и эмблемой Стрелков.</TMM_Sig_wes_gunner_coat.description>

  <TMM_Sig_braun_white_suit.label>костюм симуляции доктора Брона</TMM_Sig_braun_white_suit.label>
  <TMM_Sig_braun_white_suit.description>Безупречный довоенный кремово-белый костюм с золотыми запонками и скрытыми под воротником датчиками нейроинтерфейса.</TMM_Sig_braun_white_suit.description>

  <TMM_Sig_dr_hess_monocle.label>оккультный монокль доктора Хесс</TMM_Sig_dr_hess_monocle.label>
  <TMM_Sig_dr_hess_monocle.description>Хрустальный монокль в золотой оправе с микроскопической енохианской гравировкой, позволяющий видеть следы магии и скрытые иллюзии.</TMM_Sig_dr_hess_monocle.description>

  <TMM_Sig_ketch_arsenal_briefcase_coat.label>костюм ликвидатора Артура Кетча</TMM_Sig_ketch_arsenal_briefcase_coat.label>
  <TMM_Sig_ketch_arsenal_briefcase_coat.description>Безупречно сидящий угольно-серый британский костюм с гибкой кевларовой подкладкой и потайными кобурами для зачарованных клинков и пистолетов.</TMM_Sig_ketch_arsenal_briefcase_coat.description>

  <TMM_Sig_hancock_tricorn_coat.label>красный сюртук Хэнкока</TMM_Sig_hancock_tricorn_coat.label>
  <TMM_Sig_hancock_tricorn_coat.description>Потрёпанный алый колониальный сюртук с золотым шитьём — легендарный наряд мэра Добрососедства, символизирующий свободу Пустоши.</TMM_Sig_hancock_tricorn_coat.description>

  <TMM_Sig_silver_shroud_costume.label>плащ Серебряного Плаща</TMM_Sig_silver_shroud_costume.label>
  <TMM_Sig_silver_shroud_costume.description>Эффектный серебристо-серый плащ в духе довоенного радиогероя, укреплённый современными баллистическими вставками.</TMM_Sig_silver_shroud_costume.description>

  <TMM_Sig_fawkes_vault87_harness.label>комбинезон Убежища 87 Фокса</TMM_Sig_fawkes_vault87_harness.label>
  <TMM_Sig_fawkes_vault87_harness.description>Сильно перешитый и укреплённый кожаными ремнями сине-жёлтый комбинезон Убежища 87, подогнанный под могучее тело Фокса. Подходит только супермутантам.</TMM_Sig_fawkes_vault87_harness.description>

  <TMM_Sig_swan_anchor_club.label>корабельный якорь Лебедя</TMM_Sig_swan_anchor_club.label>
  <TMM_Sig_swan_anchor_club.description>Огромный ржавый корабельный якорь, вырванный из затонувшего судна и используемый бегемотом Лебедем как сокрушительная дубина. Из-за веса (18 кг) подходит только супермутантам или бойцам в силовой броне.</TMM_Sig_swan_anchor_club.description>

  <TMM_Sig_alpha_vampire_cloak.label>древний плащ Альфа-вампира</TMM_Sig_alpha_vampire_cloak.label>
  <TMM_Sig_alpha_vampire_cloak.description>Царственный обсидиановый плащ до пят из плотного шёлка и кожи с тёмно-алой бархатной подкладкой, излучающий древнюю хищную власть.</TMM_Sig_alpha_vampire_cloak.description>

  <TMM_Sig_dick_roman_corporate_suit.label>костюм миллиардера Дика Романа</TMM_Sig_dick_roman_corporate_suit.label>
  <TMM_Sig_dick_roman_corporate_suit.description>Сверхдорогой тёмно-синий деловой костюм с безупречной белой сорочкой и шёлковым галстуком, излучающий безжалостную власть корпората.</TMM_Sig_dick_roman_corporate_suit.description>

</LanguageData>
```

`Languages/Russian/DefInjected/RecipeDef/Recipes_SignatureGear.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Make_Sig_dean_leather_jacket.label>сшить кожаную куртку Дина</TMM_Make_Sig_dean_leather_jacket.label>
  <TMM_Make_Sig_dean_leather_jacket.jobString>Шьёт куртку Дина.</TMM_Make_Sig_dean_leather_jacket.jobString>

  <TMM_Make_Sig_dean_sawed_off_shotgun.label>собрать обрез дробовика Дина</TMM_Make_Sig_dean_sawed_off_shotgun.label>
  <TMM_Make_Sig_dean_sawed_off_shotgun.jobString>Собирает обрез Дина.</TMM_Make_Sig_dean_sawed_off_shotgun.jobString>

  <TMM_Make_Sig_sam_hunting_flannel.label>сшить многослойную куртку Сэма</TMM_Make_Sig_sam_hunting_flannel.label>
  <TMM_Make_Sig_sam_hunting_flannel.jobString>Шьёт куртку Сэма.</TMM_Make_Sig_sam_hunting_flannel.jobString>

  <TMM_Make_Sig_preston_garvey_hat.label>сшить шляпу Престона Гарви</TMM_Make_Sig_preston_garvey_hat.label>
  <TMM_Make_Sig_preston_garvey_hat.jobString>Шьёт шляпу Престона Гарви.</TMM_Make_Sig_preston_garvey_hat.jobString>

  <TMM_Make_Sig_bobby_singer_cap_vest.label>сшить кепку Бобби Сингера</TMM_Make_Sig_bobby_singer_cap_vest.label>
  <TMM_Make_Sig_bobby_singer_cap_vest.jobString>Шьёт кепку Бобби Сингера.</TMM_Make_Sig_bobby_singer_cap_vest.jobString>

  <TMM_Make_Sig_castiel_trenchcoat.label>сшить мятый плащ Кастиэля</TMM_Make_Sig_castiel_trenchcoat.label>
  <TMM_Make_Sig_castiel_trenchcoat.jobString>Шьёт плащ Кастиэля.</TMM_Make_Sig_castiel_trenchcoat.jobString>

  <TMM_Make_Sig_castiel_angel_blade.label>выковать ангельский клинок Кастиэля</TMM_Make_Sig_castiel_angel_blade.label>
  <TMM_Make_Sig_castiel_angel_blade.jobString>Куёт ангельский клинок.</TMM_Make_Sig_castiel_angel_blade.jobString>

  <TMM_Make_Sig_michael_archangel_plate.label>выковать сияющую кирасу Михаила</TMM_Make_Sig_michael_archangel_plate.label>
  <TMM_Make_Sig_michael_archangel_plate.jobString>Куёт кирасу Михаила.</TMM_Make_Sig_michael_archangel_plate.jobString>

  <TMM_Make_Sig_lucifer_white_suit.label>сшить белоснежный костюм Люцифера</TMM_Make_Sig_lucifer_white_suit.label>
  <TMM_Make_Sig_lucifer_white_suit.jobString>Шьёт костюм Люцифера.</TMM_Make_Sig_lucifer_white_suit.jobString>

  <TMM_Make_Sig_crowley_king_suit.label>сшить костюм Короля Ада Кроули</TMM_Make_Sig_crowley_king_suit.label>
  <TMM_Make_Sig_crowley_king_suit.jobString>Шьёт костюм Кроули.</TMM_Make_Sig_crowley_king_suit.jobString>

  <TMM_Make_Sig_ruby_demon_knife.label>выковать нож Руби</TMM_Make_Sig_ruby_demon_knife.label>
  <TMM_Make_Sig_ruby_demon_knife.jobString>Куёт нож Руби.</TMM_Make_Sig_ruby_demon_knife.jobString>

  <TMM_Make_Sig_ruby_demon_knife_Anomaly.label>выковать нож Руби</TMM_Make_Sig_ruby_demon_knife_Anomaly.label>
  <TMM_Make_Sig_ruby_demon_knife_Anomaly.jobString>Куёт нож Руби.</TMM_Make_Sig_ruby_demon_knife_Anomaly.jobString>

  <TMM_Make_Sig_maxson_battlecoat.label>сшить боевой плащ старейшины Мэксона</TMM_Make_Sig_maxson_battlecoat.label>
  <TMM_Make_Sig_maxson_battlecoat.jobString>Шьёт плащ Мэксона.</TMM_Make_Sig_maxson_battlecoat.jobString>

  <TMM_Make_Sig_danse_recon_hood.label>сшить подшлемник паладина Данса</TMM_Make_Sig_danse_recon_hood.label>
  <TMM_Make_Sig_danse_recon_hood.jobString>Шьёт подшлемник Данса.</TMM_Make_Sig_danse_recon_hood.jobString>

  <TMM_Make_Sig_casdin_protector_helm.label>собрать командирский шлем Касдина</TMM_Make_Sig_casdin_protector_helm.label>
  <TMM_Make_Sig_casdin_protector_helm.jobString>Собирает шлем Касдина.</TMM_Make_Sig_casdin_protector_helm.jobString>

  <TMM_Make_Sig_autumn_enclave_greatcoat.label>сшить кожаную шинель полковника Отема</TMM_Make_Sig_autumn_enclave_greatcoat.label>
  <TMM_Make_Sig_autumn_enclave_greatcoat.jobString>Шьёт шинель Отема.</TMM_Make_Sig_autumn_enclave_greatcoat.jobString>

  <TMM_Make_Sig_father_director_coat.label>сшить белый халат Отца</TMM_Make_Sig_father_director_coat.label>
  <TMM_Make_Sig_father_director_coat.jobString>Шьёт халат Отца.</TMM_Make_Sig_father_director_coat.jobString>

  <TMM_Make_Sig_kellogg_cyber_jacket.label>собрать бронекуртку Келлога</TMM_Make_Sig_kellogg_cyber_jacket.label>
  <TMM_Make_Sig_kellogg_cyber_jacket.jobString>Собирает бронекуртку Келлога.</TMM_Make_Sig_kellogg_cyber_jacket.jobString>

  <TMM_Make_Sig_kellogg_revolver.label>собрать револьвер Келлога</TMM_Make_Sig_kellogg_revolver.label>
  <TMM_Make_Sig_kellogg_revolver.jobString>Собирает револьвер Келлога.</TMM_Make_Sig_kellogg_revolver.jobString>

  <TMM_Make_Sig_desdemona_railroad_coat.label>сшить бронекуртку Дездемоны</TMM_Make_Sig_desdemona_railroad_coat.label>
  <TMM_Make_Sig_desdemona_railroad_coat.jobString>Шьёт бронекуртку Дездемоны.</TMM_Make_Sig_desdemona_railroad_coat.jobString>

  <TMM_Make_Sig_deacon_sunglasses.label>собрать тёмные очки Дьякона</TMM_Make_Sig_deacon_sunglasses.label>
  <TMM_Make_Sig_deacon_sunglasses.jobString>Собирает очки Дьякона.</TMM_Make_Sig_deacon_sunglasses.jobString>

  <TMM_Make_Sig_wes_gunner_coat.label>собрать командирский плащ капитана Уэса</TMM_Make_Sig_wes_gunner_coat.label>
  <TMM_Make_Sig_wes_gunner_coat.jobString>Собирает плащ Уэса.</TMM_Make_Sig_wes_gunner_coat.jobString>

  <TMM_Make_Sig_braun_white_suit.label>сшить костюм симуляции доктора Брона</TMM_Make_Sig_braun_white_suit.label>
  <TMM_Make_Sig_braun_white_suit.jobString>Шьёт костюм Брона.</TMM_Make_Sig_braun_white_suit.jobString>

  <TMM_Make_Sig_dr_hess_monocle.label>изготовить оккультный монокль доктора Хесс</TMM_Make_Sig_dr_hess_monocle.label>
  <TMM_Make_Sig_dr_hess_monocle.jobString>Изготавливает монокль Хесс.</TMM_Make_Sig_dr_hess_monocle.jobString>

  <TMM_Make_Sig_ketch_arsenal_briefcase_coat.label>сшить костюм ликвидатора Артура Кетча</TMM_Make_Sig_ketch_arsenal_briefcase_coat.label>
  <TMM_Make_Sig_ketch_arsenal_briefcase_coat.jobString>Шьёт костюм Кетча.</TMM_Make_Sig_ketch_arsenal_briefcase_coat.jobString>

  <TMM_Make_Sig_hancock_tricorn_coat.label>сшить красный сюртук Хэнкока</TMM_Make_Sig_hancock_tricorn_coat.label>
  <TMM_Make_Sig_hancock_tricorn_coat.jobString>Шьёт сюртук Хэнкока.</TMM_Make_Sig_hancock_tricorn_coat.jobString>

  <TMM_Make_Sig_silver_shroud_costume.label>сшить плащ Серебряного Плаща</TMM_Make_Sig_silver_shroud_costume.label>
  <TMM_Make_Sig_silver_shroud_costume.jobString>Шьёт плащ Серебряного Плаща.</TMM_Make_Sig_silver_shroud_costume.jobString>

  <TMM_Make_Sig_fawkes_vault87_harness.label>сшить комбинезон Убежища 87 Фокса</TMM_Make_Sig_fawkes_vault87_harness.label>
  <TMM_Make_Sig_fawkes_vault87_harness.jobString>Шьёт комбинезон Фокса.</TMM_Make_Sig_fawkes_vault87_harness.jobString>

  <TMM_Make_Sig_swan_anchor_club.label>выковать корабельный якорь Лебедя</TMM_Make_Sig_swan_anchor_club.label>
  <TMM_Make_Sig_swan_anchor_club.jobString>Куёт якорь Лебедя.</TMM_Make_Sig_swan_anchor_club.jobString>

  <TMM_Make_Sig_alpha_vampire_cloak.label>сшить древний плащ Альфа-вампира</TMM_Make_Sig_alpha_vampire_cloak.label>
  <TMM_Make_Sig_alpha_vampire_cloak.jobString>Шьёт плащ Альфа-вампира.</TMM_Make_Sig_alpha_vampire_cloak.jobString>

  <TMM_Make_Sig_dick_roman_corporate_suit.label>сшить костюм миллиардера Дика Романа</TMM_Make_Sig_dick_roman_corporate_suit.label>
  <TMM_Make_Sig_dick_roman_corporate_suit.jobString>Шьёт костюм Дика Романа.</TMM_Make_Sig_dick_roman_corporate_suit.jobString>

</LanguageData>
```

`Tests/InGameSignatureGearProbe.cs`

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

namespace TurboMegaMod
{
    /// <summary>
    /// In-game verification probe for all 30 signature hero items, gear sets, Castiel's blade
    /// one-shot vs regular demon (and NOT demon hero), HeroRegistry signature apparel spawn,
    /// recipe flag gating, EyeCover layer, and Alpha Vampire cloak MoveSpeed +0.10.
    /// Activated via -tmm-signature-gear-probe.
    /// </summary>
    public sealed class InGameSignatureGearProbe : MapComponent
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

        private static readonly MethodInfo ApplyMeleeDamageMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "ApplyMeleeDamageToTarget"); // CHECK API

        public InGameSignatureGearProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-signature-gear-probe", StringComparison.OrdinalIgnoreCase)) return true;
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
                WriteReport("SignatureGearProbe.json");
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

            // ---------------- 1. ALL 30 SIGNATURE ITEMS LOADED ----------------
            string[] allSigIds = new string[]
            {
                "TMM_Sig_dean_leather_jacket", "TMM_Sig_dean_sawed_off_shotgun", "TMM_Sig_sam_hunting_flannel",
                "TMM_Sig_preston_garvey_hat", "TMM_Sig_bobby_singer_cap_vest", "TMM_Sig_castiel_trenchcoat",
                "TMM_Sig_castiel_angel_blade", "TMM_Sig_michael_archangel_plate", "TMM_Sig_lucifer_white_suit",
                "TMM_Sig_crowley_king_suit", "TMM_Sig_ruby_demon_knife", "TMM_Sig_maxson_battlecoat",
                "TMM_Sig_danse_recon_hood", "TMM_Sig_casdin_protector_helm", "TMM_Sig_autumn_enclave_greatcoat",
                "TMM_Sig_father_director_coat", "TMM_Sig_kellogg_cyber_jacket", "TMM_Sig_kellogg_revolver",
                "TMM_Sig_desdemona_railroad_coat", "TMM_Sig_deacon_sunglasses", "TMM_Sig_wes_gunner_coat",
                "TMM_Sig_braun_white_suit", "TMM_Sig_dr_hess_monocle", "TMM_Sig_ketch_arsenal_briefcase_coat",
                "TMM_Sig_hancock_tricorn_coat", "TMM_Sig_silver_shroud_costume", "TMM_Sig_fawkes_vault87_harness",
                "TMM_Sig_swan_anchor_club", "TMM_Sig_alpha_vampire_cloak", "TMM_Sig_dick_roman_corporate_suit"
            };
            int loadedCount = 0;
            for (int i = 0; i < allSigIds.Length; i++)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(allSigIds[i]) != null) loadedCount++;
            }
            AddCheck("Defs_All30SignatureItemsLoaded", loadedCount == 30, "30", loadedCount.ToString());

            // ---------------- 2. DEAN SET BONUS: EQUIP BOTH -> HEDIFF ON, UNEQUIP ONE -> HEDIFF OFF ----------------
            Pawn deanTester = SpawnPawn(null, Faction.OfPlayer, origin);
            Apparel deanJacket = MakeApparel("TMM_Sig_dean_leather_jacket");
            ThingWithComps deanShotgun = MakeWeapon("TMM_Sig_dean_sawed_off_shotgun");

            if (deanJacket != null) deanTester.apparel.Wear(deanJacket, false);
            bool hasBeforeShotgun = SignatureGearUtil.HasSetBonus(deanTester, "TMM_Hediff_Set_Dean");

            if (deanShotgun != null) deanTester.equipment.AddEquipment(deanShotgun);
            bool hasWithBoth = SignatureGearUtil.HasSetBonus(deanTester, "TMM_Hediff_Set_Dean");
            bool panicBlocked = !deanTester.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "probe", true, false, false, null); // CHECK API

            Apparel droppedJacket;
            if (deanJacket != null) deanTester.apparel.TryDrop(deanJacket, out droppedJacket, deanTester.Position, false);
            bool goneAfterUnequip = !SignatureGearUtil.HasSetBonus(deanTester, "TMM_Hediff_Set_Dean");

            AddCheck("SetBonus_DeanToggleAndPanicImmunity",
                !hasBeforeShotgun && hasWithBoth && panicBlocked && goneAfterUnequip,
                "off->on(panicBlocked)->off",
                string.Format("before={0}, both={1}, panicBlocked={2}, afterDrop={3}",
                    hasBeforeShotgun, hasWithBoth, panicBlocked, !goneAfterUnequip));

            // ---------------- 3. CASTIEL & KELLOGG SET BONUSES ----------------
            Pawn angel = SpawnPawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
            Apparel casCoat = MakeApparel("TMM_Sig_castiel_trenchcoat");
            ThingWithComps casBlade = MakeWeapon("TMM_Sig_castiel_angel_blade");
            if (angel != null && casCoat != null) angel.apparel.Wear(casCoat, false);
            if (angel != null && casBlade != null) angel.equipment.AddEquipment(casBlade);
            bool casSetActive = angel != null && SignatureGearUtil.HasSetBonus(angel, "TMM_Hediff_Set_Castiel");
            AddCheck("SetBonus_CastielActive", casSetActive, "true", casSetActive.ToString());

            Pawn kelloggTester = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(-2, 0, 0));
            Apparel kelJacket = MakeApparel("TMM_Sig_kellogg_cyber_jacket");
            ThingWithComps kelGun = MakeWeapon("TMM_Sig_kellogg_revolver");
            if (kelJacket != null) kelloggTester.apparel.Wear(kelJacket, false);
            if (kelGun != null) kelloggTester.equipment.AddEquipment(kelGun);
            bool kelSetActive = SignatureGearUtil.HasSetBonus(kelloggTester, "TMM_Hediff_Set_Kellogg");
            AddCheck("SetBonus_KelloggActive", kelSetActive, "true", kelSetActive.ToString());

            // ---------------- 4. CASTIEL'S BLADE: KILLS REGULAR DEMON, NOT HERO DEMON ----------------
            Pawn regularDemon = SpawnPawn("TMM_Demon", FindHostileFaction(), angel != null ? angel.Position + new IntVec3(1, 0, 0) : origin);
            if (angel != null && casBlade != null && regularDemon != null)
            {
                StrikeMeleeOnce(angel, casBlade, regularDemon);
            }
            AddCheck("CastielBlade_KillsRegularDemonInOneHit", regularDemon != null && regularDemon.Dead,
                "Dead=true", regularDemon != null ? regularDemon.Dead.ToString() : "null");

            HeroDef demonHeroDef = DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_crowley") ??
                                   DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_meg");
            Pawn demonHero = (demonHeroDef != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(demonHeroDef) : null;
            if (demonHero != null)
            {
                if (demonHero.Spawned) demonHero.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(demonHero, (angel != null ? angel.Position : origin) + new IntVec3(-1, 0, 0), map, WipeMode.Vanish);
                if (angel != null && casBlade != null)
                {
                    StrikeMeleeOnce(angel, casBlade, demonHero);
                }
            }
            AddCheck("CastielBlade_DoesNotOneShotDemonHero", demonHero != null && !demonHero.Dead,
                "Dead=false (registered hero survives one-shot)",
                demonHero != null ? string.Format("hero={0}, dead={1}", demonHeroDef.defName, demonHero.Dead) : "hero null");

            // ---------------- 5. HERO REGISTRY GETORCREATE: DEAN WEARS HIS JACKET ----------------
            HeroDef deanDef = DefDatabase<HeroDef>.GetNamedSilentFail("TMM_Hero_dean_winchester");
            Pawn deanHero = (deanDef != null && HeroRegistry.Instance != null)
                ? HeroRegistry.Instance.GetOrCreate(deanDef) : null;
            bool deanHasJacket = deanHero != null && SignatureGearUtil.IsWearingDef(deanHero, "TMM_Sig_dean_leather_jacket");
            AddCheck("HeroRegistry_DeanWearsSignatureJacket", deanHasJacket, "wearing TMM_Sig_dean_leather_jacket",
                deanHero != null ? deanHasJacket.ToString() : "deanHero null");

            // ---------------- 6. RECIPE FLAG GATING (LOCKED WITHOUT FLAG, OPEN WITH FLAG) ----------------
            SparkStory story = SparkStory.Instance;
            RecipeDef deanJacketRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("TMM_Make_Sig_dean_leather_jacket");
            if (story != null && deanJacketRecipe != null)
            {
                story.SetFlag("unlock.dean_leather_jacket", "false"); // CHECK API
                bool lockedBefore = !deanJacketRecipe.AvailableNow;
                story.SetFlag("unlock.dean_leather_jacket", "true"); // CHECK API
                bool openAfter = deanJacketRecipe.AvailableNow;
                AddCheck("RecipeGating_DeanJacketFlag", lockedBefore && openAfter,
                    "locked->open", string.Format("{0}->{1}", lockedBefore, openAfter));
            }
            else
            {
                AddCheck("RecipeGating_DeanJacketFlag", false, "story & recipe exist",
                    string.Format("story={0}, recipe={1}", story != null, deanJacketRecipe != null));
            }

            // ---------------- 7. CLAUDE'S BALANCE & LAYER RULES ----------------
            Pawn cloakTester = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 3));
            float baseMove = cloakTester.GetStatValue(StatDefOf.MoveSpeed);
            Apparel alphaCloak = MakeApparel("TMM_Sig_alpha_vampire_cloak");
            if (alphaCloak != null) cloakTester.apparel.Wear(alphaCloak, false);
            float moveDelta = cloakTester.GetStatValue(StatDefOf.MoveSpeed) - baseMove;
            AddCheck("Balance_AlphaCloakMoveSpeed010",
                alphaCloak != null && moveDelta >= 0.08f && moveDelta <= 0.13f,
                "delta ~ +0.10", string.Format("{0:+0.000;-0.000}", moveDelta));

            ThingDef shadesDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Sig_deacon_sunglasses");
            string expectedEyeLayer = ModsConfig.IdeologyActive ? "EyeCover" : "Overhead";
            bool shadesLayerOk = shadesDef != null && shadesDef.apparel != null && shadesDef.apparel.layers != null &&
                                 HasLayer(shadesDef.apparel.layers, expectedEyeLayer);
            AddCheck("Layer_DeaconShadesEyeCoverOrOverhead", shadesLayerOk, expectedEyeLayer, shadesLayerOk.ToString());

            // Swan's Anchor blocked on ordinary human, allowed on Super Mutant
            Pawn human = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, -3));
            Pawn mutant = SpawnPawn("TMM_SuperMutant", Faction.OfPlayer, origin + new IntVec3(1, 0, -3));
            ThingWithComps anchor = MakeWeapon("TMM_Sig_swan_anchor_club");
            string humanReason;
            string mutantReason;
            bool humanCanAnchor = anchor != null && EquipmentUtility.CanEquip(anchor, human, out humanReason); // CHECK API
            bool mutantCanAnchor = anchor != null && mutant != null && EquipmentUtility.CanEquip(anchor, mutant, out mutantReason); // CHECK API
            AddCheck("Restriction_SwanAnchorMutantOrPowerArmor", !humanCanAnchor && mutantCanAnchor,
                "human=false, mutant=true", string.Format("human={0}, mutant={1}", humanCanAnchor, mutantCanAnchor));
        }

        private bool StrikeMeleeOnce(Pawn attacker, ThingWithComps weapon, Pawn target)
        {
            if (attacker == null || weapon == null || target == null || ApplyMeleeDamageMethod == null) return false;
            CompEquippable eq = weapon.GetComp<CompEquippable>();
            if (eq == null || eq.AllVerbs == null) return false;

            Verb_MeleeAttack meleeVerb = null;
            for (int i = 0; i < eq.AllVerbs.Count; i++)
            {
                Verb_MeleeAttack v = eq.AllVerbs[i] as Verb_MeleeAttack;
                if (v != null)
                {
                    meleeVerb = v;
                    break;
                }
            }
            if (meleeVerb == null) return false;

            meleeVerb.caster = attacker;
            ApplyMeleeDamageMethod.Invoke(meleeVerb, new object[] { new LocalTargetInfo(target) });
            return true;
        }

        private static Apparel MakeApparel(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            return ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null) as Apparel;
        }

        private static ThingWithComps MakeWeapon(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            return ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null) as ThingWithComps;
        }

        private static bool HasLayer(List<ApparelLayerDef> layers, string defName)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].defName == defName) return true;
            }
            return false;
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