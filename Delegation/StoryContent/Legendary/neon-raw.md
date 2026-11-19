`Defs/ThingDefs/Legendary_Weapons.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- HELPER DAMAGE DEFS FOR LEGENDARY WEAPONS                              -->
  <!-- (TMM_HolyWater, TMM_RockSalt, TMM_Enochian, TMM_BoraxBurn already     -->
  <!--  exist in Damages_Supernatural.xml and are referenced directly.)      -->
  <!-- ===================================================================== -->

  <DamageDef>
    <defName>TMM_Damage_TelekineticWave</defName>
    <label>telekinetic shockwave</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>true</externalViolence>
    <isExplosive>true</isExplosive>
    <deathMessage>{0} was crushed by a telekinetic shockwave.</deathMessage>
    <hediff>Crush</hediff>
    <hediffSolid>Crack</hediffSolid>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Blunt</armorCategory>
    <defaultDamage>34</defaultDamage>
    <defaultArmorPenetration>0.65</defaultArmorPenetration>
    <defaultStoppingPower>2.5</defaultStoppingPower>
    <buildingDamageFactor>4.0</buildingDamageFactor>
    <explosionCellFleck>BlastDry</explosionCellFleck>
    <soundExplosion>Explosion_Bomb</soundExplosion>
  </DamageDef>

  <DamageDef>
    <defName>TMM_Damage_RailwaySpike</defName>
    <label>superheated railway spike</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>true</externalViolence>
    <deathMessage>{0} was impaled by a superheated railway spike.</deathMessage>
    <hediff>Stab</hediff>
    <hediffSolid>Crack</hediffSolid>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Sharp</armorCategory>
    <defaultDamage>36</defaultDamage>
    <defaultArmorPenetration>0.78</defaultArmorPenetration>
    <defaultStoppingPower>2.0</defaultStoppingPower>
    <isRanged>true</isRanged>
    <additionalHediffs>
      <li>
        <hediff>TMM_Hediff_Pinned</hediff>
        <severityPerDamageDealt>0.1</severityPerDamageDealt>
      </li>
    </additionalHediffs>
  </DamageDef>

  <DamageDef>
    <defName>TMM_Damage_MiniNuke</defName>
    <label>mini-nuke blast</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>true</externalViolence>
    <isExplosive>true</isExplosive>
    <deathMessage>{0} was vaporized in a tactical nuclear blast.</deathMessage>
    <hediff>Burn</hediff>
    <hediffSolid>Crack</hediffSolid>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Heat</armorCategory>
    <defaultDamage>150</defaultDamage>
    <defaultArmorPenetration>1.0</defaultArmorPenetration>
    <buildingDamageFactor>3.5</buildingDamageFactor>
    <explosionAffectOutsidePartsOnly>false</explosionAffectOutsidePartsOnly>
    <explosionHeatEnergyPerCell>25</explosionHeatEnergyPerCell>
    <explosionCellFleck>BlastFlame</explosionCellFleck>
    <soundExplosion>Explosion_Bomb</soundExplosion>
    <additionalHediffs>
      <li>
        <hediff>ToxicBuildup</hediff>
        <severityPerDamageDealt>0.004</severityPerDamageDealt>
      </li>
    </additionalHediffs>
  </DamageDef>

  <DamageDef>
    <defName>TMM_Damage_EqualizerRecoil</defName>
    <label>dimensional backlash</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>false</externalViolence>
    <deathMessage>{0} died from dimensional weapon backlash.</deathMessage>
    <hediff>Burn</hediff>
    <armorCategory>Heat</armorCategory>
    <defaultDamage>12</defaultDamage>
    <defaultArmorPenetration>1.0</defaultArmorPenetration>
  </DamageDef>

  <!-- ===================================================================== -->
  <!-- 1. INSTITUTE: EXPERIMENT 18-A                                         -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_Experiment18A</defName>
    <label>overclocked plasma bolt</label>
    <thingClass>TurboMegaMod.Bullet_Experiment18A</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Charge_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Burn</damageDef>
      <damageAmountBase>22</damageAmountBase>
      <armorPenetrationBase>0.48</armorPenetrationBase>
      <speed>72</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_Experiment18A</defName>
    <label>Experiment 18-A</label>
    <description>An overclocked Institute plasma rifle prototype with unstable magnetic containment coils. It fires superheated plasma bolts at a blistering rate, occasionally reducing targets to a pile of glowing ash.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/ChargeRifle</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_ChargeRifle</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>4800</MarketValue>
      <Mass>3.8</Mass>
      <AccuracyTouch>0.72</AccuracyTouch>
      <AccuracyShort>0.82</AccuracyShort>
      <AccuracyMedium>0.78</AccuracyMedium>
      <AccuracyLong>0.65</AccuracyLong>
      <RangedWeapon_Cooldown>1.1</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_Experiment18A</defaultProjectile>
        <warmupTime>0.8</warmupTime>
        <range>28</range>
        <burstShotCount>4</burstShotCount>
        <ticksBetweenBurstShots>7</ticksBetweenBurstShots>
        <soundCast>Shot_ChargeRifle</soundCast>
        <soundCastTail>GunTail_Medium</soundCastTail>
        <muzzleFlashScale>10</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>stock</label>
        <capacities><li>Blunt</li></capacities>
        <power>9</power>
        <cooldownTime>2</cooldownTime>
      </li>
      <li>
        <label>barrel</label>
        <capacities><li>Blunt</li><li>Poke</li></capacities>
        <power>9</power>
        <cooldownTime>2</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 2. BROTHERHOOD OF STEEL: FINAL JUDGMENT                               -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_FinalJudgment</defName>
    <label>high-energy laser beam</label>
    <graphicData>
      <texPath>Things/Projectile/Charge_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Burn</damageDef>
      <damageAmountBase>14</damageAmountBase>
      <armorPenetrationBase>0.40</armorPenetrationBase>
      <speed>90</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_FinalJudgment</defName>
    <label>Final Judgment</label>
    <description>A relic Gatling laser handed down by Brotherhood Elders, fitted with a self-sustaining micro-fusion core. It unleashes an endless torrent of high-energy beams, though its cooling vents scorch the wielder during prolonged bursts.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/Minigun</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Rifle</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>5500</MarketValue>
      <Mass>9.5</Mass>
      <AccuracyTouch>0.45</AccuracyTouch>
      <AccuracyShort>0.58</AccuracyShort>
      <AccuracyMedium>0.52</AccuracyMedium>
      <AccuracyLong>0.40</AccuracyLong>
      <RangedWeapon_Cooldown>0.8</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>TurboMegaMod.Verb_ShootFinalJudgment</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_FinalJudgment</defaultProjectile>
        <warmupTime>1.4</warmupTime>
        <range>32</range>
        <burstShotCount>16</burstShotCount>
        <ticksBetweenBurstShots>4</ticksBetweenBurstShots>
        <soundCast>Shot_Minigun</soundCast>
        <soundCastTail>GunTail_Medium</soundCastTail>
        <muzzleFlashScale>10</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>barrels</label>
        <capacities><li>Blunt</li></capacities>
        <power>12</power>
        <cooldownTime>2.4</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 5. MINUTEMEN & HUNTERS: THE COLT (13 CHARGES, SLAYS SUPERNATURAL)     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_TheColt</defName>
    <label>enchanted silver bullet</label>
    <thingClass>TurboMegaMod.Bullet_TheColt</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Bullet</damageDef>
      <damageAmountBase>45</damageAmountBase>
      <armorPenetrationBase>0.90</armorPenetrationBase>
      <stoppingPower>2.0</stoppingPower>
      <speed>80</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_TheColt</defName>
    <label>The Colt</label>
    <description>An ornate 1851 Colt Paterson revolver forged by Samuel Colt himself during the passing of a comet. Loaded with 13 enchanted silver bullets, this legendary firearm can permanently slay almost any supernatural entity with a single shot.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/Revolver</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Revolver</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>8500</MarketValue>
      <Mass>1.4</Mass>
      <AccuracyTouch>0.90</AccuracyTouch>
      <AccuracyShort>0.88</AccuracyShort>
      <AccuracyMedium>0.78</AccuracyMedium>
      <AccuracyLong>0.62</AccuracyLong>
      <RangedWeapon_Cooldown>1.4</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>TurboMegaMod.Verb_ShootLimitedCharges</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_TheColt</defaultProjectile>
        <warmupTime>0.35</warmupTime>
        <range>26</range>
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
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
      <li Class="TurboMegaMod.CompProperties_LimitedCharges">
        <maxCharges>13</maxCharges>
        <allowReload>false</allowReload>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 6. GHOULS: LORENZO'S ARTIFACT GUN                                     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Projectile_LorenzoWave</defName>
    <label>telekinetic pulse</label>
    <thingClass>TurboMegaMod.Projectile_LorenzoWave</thingClass>
    <graphicData>
      <texPath>Things/Projectile/ChargeLanceShot</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>TMM_Damage_TelekineticWave</damageDef>
      <damageAmountBase>34</damageAmountBase>
      <armorPenetrationBase>0.65</armorPenetrationBase>
      <speed>55</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_LorenzosArtifact</defName>
    <label>Lorenzo's Artifact Gun</label>
    <description>A heavily modified gamma gun fitted with an ancient, alien crown emitter. Instead of radiation, it unleashes concentrated telekinetic shockwaves that hurl enemies backward and shatter fortified cover.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/ChargeLance</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_ChargeLance</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>5200</MarketValue>
      <Mass>3.2</Mass>
      <AccuracyTouch>0.75</AccuracyTouch>
      <AccuracyShort>0.82</AccuracyShort>
      <AccuracyMedium>0.80</AccuracyMedium>
      <AccuracyLong>0.70</AccuracyLong>
      <RangedWeapon_Cooldown>2.1</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Projectile_LorenzoWave</defaultProjectile>
        <warmupTime>1.1</warmupTime>
        <range>27</range>
        <burstShotCount>1</burstShotCount>
        <soundCast>ChargeLance_Fire</soundCast>
        <soundCastTail>GunTail_Heavy</soundCastTail>
        <muzzleFlashScale>11</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>emitter</label>
        <capacities><li>Blunt</li></capacities>
        <power>10</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 7. FRIENDLY SUPER MUTANTS: ATOM'S JUDGEMENT                           -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Legendary_AtomsJudgement</defName>
    <label>Atom's Judgement</label>
    <description>A colossal super sledge welded with four leaking fusion cores onto its striking head. Every swing delivers bone-crushing kinetic force followed by a localized burst of searing radiation.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Mace</texPath> <!-- TODO art (vanilla Mace avoids Royalty dependency) -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>4900</MarketValue>
      <Mass>11.0</Mass>
    </statBases>
    <tools>
      <li>
        <label>fusion hammerhead</label>
        <capacities><li>Blunt</li></capacities>
        <power>38</power>
        <armorPenetration>0.70</armorPenetration>
        <cooldownTime>2.4</cooldownTime>
      </li>
      <li>
        <label>handle</label>
        <capacities><li>Blunt</li></capacities>
        <power>14</power>
        <cooldownTime>1.9</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 8. ENCLAVE: TESLA CANNON                                              -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_TeslaCannon</defName>
    <label>high-voltage arc</label>
    <thingClass>TurboMegaMod.Bullet_TeslaCannon</thingClass>
    <graphicData>
      <texPath>Things/Projectile/ChargeLanceShot</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Burn</damageDef>
      <damageAmountBase>32</damageAmountBase>
      <armorPenetrationBase>0.60</armorPenetrationBase>
      <speed>95</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_TeslaCannon</defName>
    <label>Enclave Tesla Cannon</label>
    <description>A shoulder-mounted directed-energy weapon developed by Enclave scientists to neutralize armored vehicles and rogue machines. Its high-voltage arc leaps between nearby targets, frying circuits and paralyzing synths.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/ChargeLance</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_ChargeLance</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>6000</MarketValue>
      <Mass>8.0</Mass>
      <AccuracyTouch>0.70</AccuracyTouch>
      <AccuracyShort>0.82</AccuracyShort>
      <AccuracyMedium>0.85</AccuracyMedium>
      <AccuracyLong>0.78</AccuracyLong>
      <RangedWeapon_Cooldown>2.5</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_TeslaCannon</defaultProjectile>
        <warmupTime>1.5</warmupTime>
        <range>34</range>
        <burstShotCount>1</burstShotCount>
        <soundCast>ChargeLance_Fire</soundCast>
        <soundCastTail>GunTail_Heavy</soundCastTail>
        <muzzleFlashScale>12</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>barrel</label>
        <capacities><li>Blunt</li></capacities>
        <power>11</power>
        <cooldownTime>2.2</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 9. GUNNERS: OVERSEER'S GUARDIAN (PURE XML)                            -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_OverseersGuardian</defName>
    <label>twin-chamber rifle bullet</label>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Bullet</damageDef>
      <damageAmountBase>16</damageAmountBase>
      <armorPenetrationBase>0.35</armorPenetrationBase>
      <speed>78</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_OverseersGuardian</defName>
    <label>Overseer's Guardian</label>
    <description>A precision-machined combat rifle with a twin-chambered receiver. Every pull of the trigger fires two synchronized rounds along nearly identical trajectories, doubling stopping power without sacrificing accuracy.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/AssaultRifle</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Rifle</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>4400</MarketValue>
      <Mass>4.2</Mass>
      <AccuracyTouch>0.84</AccuracyTouch>
      <AccuracyShort>0.92</AccuracyShort>
      <AccuracyMedium>0.86</AccuracyMedium>
      <AccuracyLong>0.76</AccuracyLong>
      <RangedWeapon_Cooldown>1.5</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_OverseersGuardian</defaultProjectile>
        <warmupTime>0.85</warmupTime>
        <range>31</range>
        <burstShotCount>2</burstShotCount>
        <ticksBetweenBurstShots>2</ticksBetweenBurstShots>
        <soundCast>Shot_AssaultRifle</soundCast>
        <soundCastTail>GunTail_Medium</soundCastTail>
        <muzzleFlashScale>9</muzzleFlashScale>
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
        <label>barrel</label>
        <capacities><li>Blunt</li><li>Poke</li></capacities>
        <power>9</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 10. RAILROAD: HEAVY RAILWAY RIFLE (PURE XML + PINNED HEDIFF)          -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_RailwayRifle</defName>
    <label>superheated railway spike</label>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Big</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>TMM_Damage_RailwaySpike</damageDef>
      <damageAmountBase>36</damageAmountBase>
      <armorPenetrationBase>0.78</armorPenetrationBase>
      <stoppingPower>2.0</stoppingPower>
      <speed>88</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_RailwayRifle</defName>
    <label>Heavy Railway Rifle</label>
    <description>A pneumatic contraption cobbled together by Railroad engineers that fires superheated steel railway spikes. The heavy projectiles punch straight through power armor plates and pin victims to the ground.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/SniperRifle</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Rifle</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>4300</MarketValue>
      <Mass>6.5</Mass>
      <AccuracyTouch>0.60</AccuracyTouch>
      <AccuracyShort>0.78</AccuracyShort>
      <AccuracyMedium>0.88</AccuracyMedium>
      <AccuracyLong>0.86</AccuracyLong>
      <RangedWeapon_Cooldown>2.3</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_RailwayRifle</defaultProjectile>
        <warmupTime>1.8</warmupTime>
        <range>38</range>
        <burstShotCount>1</burstShotCount>
        <soundCast>Shot_SniperRifle</soundCast>
        <soundCastTail>GunTail_Heavy</soundCastTail>
        <muzzleFlashScale>10</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>barrel</label>
        <capacities><li>Blunt</li></capacities>
        <power>11</power>
        <cooldownTime>2.1</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 11. WILD SUPER MUTANTS: THE BIG BOY (TWIN MINI-NUKE, LIMITED CHARGES) -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Projectile_BigBoy</defName>
    <label>mini-nuke warhead</label>
    <thingClass>Projectile_Explosive</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Rocket_Big</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>TMM_Damage_MiniNuke</damageDef>
      <damageAmountBase>150</damageAmountBase>
      <armorPenetrationBase>1.0</armorPenetrationBase>
      <explosionRadius>6.9</explosionRadius>
      <speed>42</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_BigBoy</defName>
    <label>The Big Boy</label>
    <description>A monstrously modified tactical nuclear catapult lugged around by wild super mutant warlords. Its twin-barrel mechanism launches two mini-nukes simultaneously, turning entire battlefields into irradiated craters.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/TripleRocketLauncher</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Rifle</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>7800</MarketValue>
      <Mass>15.0</Mass>
      <RangedWeapon_Cooldown>5.5</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>TurboMegaMod.Verb_ShootLimitedCharges</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Projectile_BigBoy</defaultProjectile>
        <warmupTime>3.5</warmupTime>
        <range>36</range>
        <forcedMissRadius>2.2</forcedMissRadius>
        <burstShotCount>2</burstShotCount>
        <ticksBetweenBurstShots>8</ticksBetweenBurstShots>
        <soundCast>InfernoCannon_Fire</soundCast>
        <soundCastTail>GunTail_Heavy</soundCastTail>
        <muzzleFlashScale>16</muzzleFlashScale>
        <targetParams>
          <canTargetLocations>true</canTargetLocations>
        </targetParams>
      </li>
    </verbs>
    <tools>
      <li>
        <label>launcher frame</label>
        <capacities><li>Blunt</li></capacities>
        <power>14</power>
        <cooldownTime>2.6</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
      <li Class="TurboMegaMod.CompProperties_LimitedCharges">
        <maxCharges>6</maxCharges>
        <allowReload>true</allowReload>
        <ammoDef>Uranium</ammoDef>
        <ammoCountPerCharge>5</ammoCountPerCharge>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 12. BRITISH MEN OF LETTERS: THE EQUALIZER                             -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_TheEqualizer</defName>
    <label>quantum-entangled round</label>
    <thingClass>TurboMegaMod.Bullet_TheEqualizer</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Charge_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef>Bullet</damageDef>
      <damageAmountBase>65</damageAmountBase>
      <armorPenetrationBase>1.0</armorPenetrationBase>
      <stoppingPower>2.0</stoppingPower>
      <speed>85</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseGun">
    <defName>TMM_Legendary_TheEqualizer</defName>
    <label>The Equalizer</label>
    <description>An arcane dimensional firearm forged in the vaults of the British Men of Letters. It fires quantum-entangled rounds capable of wounding even archangels and primordial entities, though the recoil mirrors a fraction of the agony back onto the shooter.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/Autopistol</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Autopistol</soundInteract>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>7500</MarketValue>
      <Mass>1.6</Mass>
      <AccuracyTouch>0.88</AccuracyTouch>
      <AccuracyShort>0.85</AccuracyShort>
      <AccuracyMedium>0.75</AccuracyMedium>
      <AccuracyLong>0.60</AccuracyLong>
      <RangedWeapon_Cooldown>1.8</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>TurboMegaMod.Verb_ShootEqualizer</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_TheEqualizer</defaultProjectile>
        <warmupTime>0.6</warmupTime>
        <range>25</range>
        <burstShotCount>1</burstShotCount>
        <soundCast>Shot_Autopistol</soundCast>
        <soundCastTail>GunTail_Light</soundCastTail>
        <muzzleFlashScale>10</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>grip</label>
        <capacities><li>Blunt</li></capacities>
        <power>9</power>
        <cooldownTime>1.8</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 13. HELL: THE FIRST BLADE (ONE-HIT KILL IN CODE + MARK OF CAIN)       -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Legendary_FirstBlade</defName>
    <label>The First Blade</label>
    <description>The primordial jawbone blade wielded by Cain in the world's first murder. Bound to the Mark of Cain, it slays any mortal or demon in a single strike (save primordial lords), but fills its bearer with an insatiable bloodlust during times of peace.</description>
    <techLevel>Neolithic</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Knife</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>12000</MarketValue>
      <Mass>1.2</Mass>
    </statBases>
    <tools>
      <li>
        <label>primordial edge</label>
        <capacities><li>Cut</li><li>Stab</li></capacities>
        <power>44</power>
        <armorPenetration>1.2</armorPenetration>
        <cooldownTime>1.3</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
      <li Class="TurboMegaMod.CompProperties_MarkOfCainBlade" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 14. HEAVEN: ARCHANGEL BLADE (ONE-HIT KILL IN CODE + ENOCHIAN + WARD)  -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Legendary_ArchangelBlade</defName>
    <label>Archangel Blade</label>
    <description>A golden, twisted-metal blade forged in the highest tier of Heaven. It slices effortlessly through any armor, slays non-boss foes in a single strike, ignites targets with celestial fire, and shields its wielder from demonic curses.</description>
    <techLevel>Archotech</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Gladius</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>11000</MarketValue>
      <Mass>1.5</Mass>
    </statBases>
    <tools>
      <li>
        <label>celestial point</label>
        <capacities><li>Stab</li><li>Cut</li></capacities>
        <power>46</power>
        <armorPenetration>1.5</armorPenetration>
        <cooldownTime>1.4</cooldownTime>
        <extraMeleeDamages MayRequire="Ludeon.RimWorld.Biotech">
          <li>
            <def>TMM_Enochian</def>
            <amount>12</amount>
            <armorPenetration>1.5</armorPenetration>
          </li>
        </extraMeleeDamages>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 15. MONSTER CLANS: ALPHA PREDATOR (LIFESTEAL + HEMOGEN)               -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseMeleeWeapon">
    <defName>TMM_Legendary_AlphaPredator</defName>
    <label>Alpha Predator</label>
    <description>A grotesque, living sword grown from Alpha Vampire blood and hardened bioferrite. Its serrated edge drinks the lifeblood of victims with every slash, rapidly knitting the wielder's own wounds closed.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/LongSword</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>6800</MarketValue>
      <Mass>2.1</Mass>
    </statBases>
    <tools>
      <li>
        <label>serrated fang-edge</label>
        <capacities><li>Cut</li><li>Stab</li></capacities>
        <power>30</power>
        <armorPenetration>0.68</armorPenetration>
        <cooldownTime>1.5</cooldownTime>
      </li>
    </tools>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

</Defs>
```

`Defs/ThingDefs/Legendary_Apparel.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- 3. OUTCASTS: PROTECTOR POWER ARMOR (AUTO-INJECTOR AT <30% HEALTH)     -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Legendary_ProtectorArmor</defName>
    <label>Protector Power Armor</label>
    <description>A custom black-and-crimson power armor suit forged by Brotherhood Outcast master technicians. Worn over regular clothing, its chestplate houses a medical auto-injector that floods the wearer's bloodstream with stimpaks the moment vitals drop below 30%.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>6200</MarketValue>
      <MaxHitPoints>500</MaxHitPoints>
      <Mass>14.0</Mass>
      <Flammability>0.1</Flammability>
      <ArmorRating_Sharp>1.35</ArmorRating_Sharp>
      <ArmorRating_Blunt>0.65</ArmorRating_Blunt>
      <ArmorRating_Heat>1.10</ArmorRating_Heat>
      <Insulation_Cold>38</Insulation_Cold>
      <Insulation_Heat>22</Insulation_Heat>
      <EquipDelay>10</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <MoveSpeed>-0.25</MoveSpeed>
      <ToxicEnvironmentResistance MayRequire="Ludeon.RimWorld.Biotech">0.60</ToxicEnvironmentResistance>
    </equippedStatOffsets>
    <thingCategories>
      <li>ApparelArmor</li>
    </thingCategories>
    <apparel>
      <bodyPartGroups>
        <li>Torso</li>
        <li>Neck</li>
        <li>Shoulders</li>
        <li>Arms</li>
        <li>Legs</li>
      </bodyPartGroups>
      <wornGraphicPath>Things/Pawn/Humanlike/Apparel/PowerArmor/PowerArmor</wornGraphicPath> <!-- TODO art -->
      <layers>
        <li>Shell</li>
      </layers>
      <defaultOutfitTags>
        <li>Soldier</li>
      </defaultOutfitTags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
      <li Class="TurboMegaMod.CompProperties_ProtectorAutoInjector">
        <cooldownTicks>30000</cooldownTicks>
        <healthThreshold>0.30</healthThreshold>
      </li>
    </comps>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- 4. VAULT-TEC: GOLDEN PIP-BOY 3000 MARK IV (UTILITY + ANTI-PANIC)      -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="ApparelBase">
    <defName>TMM_Legendary_GoldenPipBoy</defName>
    <label>Golden Pip-Boy 3000 Mark IV</label>
    <description>A commemorative gold-plated wrist computer crafted for senior Vault-Tec executives. Its upgraded tactical V.A.T.S. co-processor sharpens aim, accelerates complex tasks, steadies morale, and prevents panic breaks.</description>
    <techLevel>Spacer</techLevel>
    <graphicData>
      <texPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <generateCommonality>0</generateCommonality>
    <statBases>
      <MarketValue>4500</MarketValue>
      <MaxHitPoints>250</MaxHitPoints>
      <Mass>0.9</Mass>
      <Flammability>0.1</Flammability>
      <EquipDelay>1.5</EquipDelay>
    </statBases>
    <equippedStatOffsets>
      <WorkSpeedGlobal>0.20</WorkSpeedGlobal>
      <ShootingAccuracyPawn>5</ShootingAccuracyPawn>
      <MentalBreakThreshold>-0.30</MentalBreakThreshold>
    </equippedStatOffsets>
    <thingCategories>
      <li>ApparelUtility</li>
    </thingCategories>
    <apparel>
      <careIfWornByCorpse>false</careIfWornByCorpse>
      <careIfDamaged>false</careIfDamaged>
      <wearPerDay>0</wearPerDay>
      <bodyPartGroups>
        <li>Waist</li>
      </bodyPartGroups>
      <layers>
        <li>Belt</li>
      </layers>
      <defaultOutfitTags>
        <li>Soldier</li>
        <li>Worker</li>
      </defaultOutfitTags>
    </apparel>
    <comps>
      <li Class="TurboMegaMod.CompProperties_LegendaryItem" />
    </comps>
  </ThingDef>

</Defs>
```

`Defs/ThingDefs/Borax.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- BORAX PROJECTILES (TMM_BoraxBurn; strong vs Leviathans, ~0 vs humans) -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_Borax</defName>
    <label>borax mist stream</label>
    <thingClass>TurboMegaMod.Bullet_Borax</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef MayRequire="Ludeon.RimWorld.Biotech">TMM_BoraxBurn</damageDef>
      <damageDef MayRequire="!Ludeon.RimWorld.Biotech">Burn</damageDef>
      <damageAmountBase>12</damageAmountBase>
      <armorPenetrationBase>0.85</armorPenetrationBase>
      <speed>48</speed>
    </projectile>
  </ThingDef>

  <ThingDef ParentName="BaseBullet">
    <defName>TMM_Bullet_BoraxTurret</defName>
    <label>pressurized borax jet</label>
    <thingClass>TurboMegaMod.Bullet_Borax</thingClass>
    <graphicData>
      <texPath>Things/Projectile/Bullet_Small</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <projectile>
      <damageDef MayRequire="Ludeon.RimWorld.Biotech">TMM_BoraxBurn</damageDef>
      <damageDef MayRequire="!Ludeon.RimWorld.Biotech">Burn</damageDef>
      <damageAmountBase>11</damageAmountBase>
      <armorPenetrationBase>0.85</armorPenetrationBase>
      <speed>54</speed>
    </projectile>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- HANDHELD BORAX SPRAYER (CRAFTABLE AFTER TMM_BoraxResearch)            -->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseHumanMakeableGun">
    <defName>TMM_Gun_BoraxSprayer</defName>
    <label>borax sprayer</label>
    <description>A pressurized chemical sprayer filled with concentrated sodium borate solution. Harmless to humans, this caustic white mist dissolves the indestructible flesh of Leviathans and strips Dick Roman of his supernatural invulnerability.</description>
    <techLevel>Industrial</techLevel>
    <graphicData>
      <texPath>Things/Item/Equipment/WeaponRanged/IncendiaryLauncher</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Rifle</soundInteract>
    <recipeMaker>
      <researchPrerequisite>TMM_BoraxResearch</researchPrerequisite>
      <skillRequirements>
        <Crafting>6</Crafting>
      </skillRequirements>
      <recipeUsers>
        <li>TableMachining</li>
      </recipeUsers>
    </recipeMaker>
    <costList>
      <Steel>65</Steel>
      <Chemfuel>30</Chemfuel>
      <ComponentIndustrial>3</ComponentIndustrial>
    </costList>
    <statBases>
      <WorkToMake>18000</WorkToMake>
      <Mass>4.5</Mass>
      <AccuracyTouch>0.88</AccuracyTouch>
      <AccuracyShort>0.82</AccuracyShort>
      <AccuracyMedium>0.65</AccuracyMedium>
      <AccuracyLong>0.40</AccuracyLong>
      <RangedWeapon_Cooldown>1.3</RangedWeapon_Cooldown>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>TMM_Bullet_Borax</defaultProjectile>
        <warmupTime>0.7</warmupTime>
        <range>12</range>
        <burstShotCount>3</burstShotCount>
        <ticksBetweenBurstShots>7</ticksBetweenBurstShots>
        <soundCast>Shot_IncendiaryLauncher</soundCast>
        <soundCastTail>GunTail_Light</soundCastTail>
        <muzzleFlashScale>6</muzzleFlashScale>
      </li>
    </verbs>
    <tools>
      <li>
        <label>nozzle</label>
        <capacities><li>Blunt</li></capacities>
        <power>8</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
    </tools>
  </ThingDef>

  <!-- ===================================================================== -->
  <!-- AUTOMATED BORAX TURRET & TURRET GUN (BUILDABLE AFTER TMM_BoraxResearch)-->
  <!-- ===================================================================== -->

  <ThingDef ParentName="BaseWeaponTurret">
    <defName>TMM_Gun_BoraxTurret</defName>
    <label>borax turret sprayer</label>
    <description>Automated high-pressure sodium borate emitter mounted on a security turret.</description>
    <graphicData>
      <texPath>Things/Building/Security/TurretMini_Top</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <statBases>
      <AccuracyTouch>0.84</AccuracyTouch>
      <AccuracyShort>0.78</AccuracyShort>
      <AccuracyMedium>0.65</AccuracyMedium>
      <AccuracyLong>0.48</AccuracyLong>
      <RangedWeapon_Cooldown>2.2</RangedWeapon_Cooldown>
      <DeteriorationRate>0</DeteriorationRate>
      <Mass>5</Mass>
      <Flammability>0</Flammability>
    </statBases>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <defaultProjectile>TMM_Bullet_BoraxTurret</defaultProjectile>
        <warmupTime>0</warmupTime>
        <range>18</range>
        <ticksBetweenBurstShots>8</ticksBetweenBurstShots>
        <burstShotCount>3</burstShotCount>
        <soundCast>Shot_IncendiaryLauncher</soundCast>
        <soundCastTail>GunTail_Light</soundCastTail>
        <muzzleFlashScale>6</muzzleFlashScale>
        <consumeFuelPerShot>1</consumeFuelPerShot>
      </li>
    </verbs>
  </ThingDef>

  <ThingDef ParentName="BuildingBase">
    <defName>TMM_Turret_Borax</defName>
    <label>automated borax turret</label>
    <description>A stationary defensive turret that sprays concentrated sodium borate up to 18 cells away. Harmless to humans, it melts Leviathan defenses and strips Dick Roman's invulnerability.</description>
    <thingClass>Building_TurretGun</thingClass>
    <drawerType>MapMeshAndRealTime</drawerType>
    <graphicData>
      <texPath>Things/Building/Security/TurretMini_Base</texPath> <!-- TODO art -->
      <graphicClass>Graphic_Single</graphicClass>
      <drawSize>(3,3)</drawSize>
      <damageData>
        <rect>(0.2,0.2,0.6,0.6)</rect>
      </damageData>
      <shadowData>
        <volume>(0.27,0.25,0.27)</volume>
        <offset>(0,0,0)</offset>
      </shadowData>
    </graphicData>
    <uiIconPath>Things/Building/Security/TurretMini_MenuIcon</uiIconPath> <!-- TODO art -->
    <uiIconScale>0.90</uiIconScale>
    <minifiedDef>MinifiedThing</minifiedDef>
    <altitudeLayer>Building</altitudeLayer>
    <stealable>false</stealable>
    <rotatable>false</rotatable>
    <thingCategories>
      <li>BuildingsSecurity</li>
    </thingCategories>
    <statBases>
      <MaxHitPoints>130</MaxHitPoints>
      <Flammability>0.5</Flammability>
      <WorkToBuild>2400</WorkToBuild>
      <Mass>12</Mass>
      <Beauty>-18</Beauty>
      <ShootingAccuracyTurret>0.96</ShootingAccuracyTurret>
    </statBases>
    <tickerType>Normal</tickerType>
    <passability>PassThroughOnly</passability>
    <pathCost>50</pathCost>
    <fillPercent>0.4</fillPercent>
    <hasTooltip>true</hasTooltip>
    <costList>
      <Steel>110</Steel>
      <Chemfuel>35</Chemfuel>
      <ComponentIndustrial>4</ComponentIndustrial>
    </costList>
    <terrainAffordanceNeeded>Light</terrainAffordanceNeeded>
    <designationCategory>Security</designationCategory>
    <constructionSkillPrerequisite>6</constructionSkillPrerequisite>
    <researchPrerequisites>
      <li>TMM_BoraxResearch</li>
    </researchPrerequisites>
    <building>
      <combatPower>55</combatPower>
      <ai_combatDangerous>true</ai_combatDangerous>
      <turretGunDef>TMM_Gun_BoraxTurret</turretGunDef>
      <turretBurstCooldownTime>2.6</turretBurstCooldownTime>
    </building>
    <placeWorkers>
      <li>PlaceWorker_TurretTop</li>
      <li>PlaceWorker_ShowTurretRadius</li>
    </placeWorkers>
    <comps>
      <li Class="CompProperties_Forbiddable" />
      <li Class="CompProperties_Power">
        <compClass>CompPowerTrader</compClass>
        <basePowerConsumption>100</basePowerConsumption>
      </li>
      <li Class="CompProperties_Flickable" />
      <li Class="CompProperties_Breakdownable" />
      <li Class="CompProperties_Refuelable">
        <fuelLabel>Borax solution</fuelLabel>
        <fuelGizmoLabel>Borax tank</fuelGizmoLabel>
        <outOfFuelMessage>Cannot spray: Needs chemfuel (borax propellant)</outOfFuelMessage>
        <fuelFilter>
          <thingDefs>
            <li>Chemfuel</li>
          </thingDefs>
        </fuelFilter>
        <fuelCapacity>60</fuelCapacity>
        <fuelMultiplier>1.0</fuelMultiplier>
        <initialFuelPercent>1</initialFuelPercent>
        <autoRefuelPercent>0.5</autoRefuelPercent>
        <showFuelGizmo>true</showFuelGizmo>
        <minimumFueledThreshold>1</minimumFueledThreshold>
        <factorByDifficulty>true</factorByDifficulty>
        <consumeFuelOnlyWhenUsed>true</consumeFuelOnlyWhenUsed>
        <fuelIconPath>UI/Overlays/Barrel</fuelIconPath>
      </li>
    </comps>
  </ThingDef>

</Defs>
```

`Defs/HediffDefs/Hediffs_Legendary.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 1. Final Judgment Overheat -->
  <HediffDef>
    <defName>TMM_Hediff_FinalJudgmentHeat</defName>
    <label>fusion vent overheat</label>
    <description>Thermal feedback from Final Judgment's micro-fusion core slows movement and causes burns if pushed to maximum heat.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(1.0, 0.45, 0.15)</defaultLabelColor>
    <isBad>true</isBad>
    <minSeverity>0.001</minSeverity>
    <maxSeverity>1.0</maxSeverity>
    <comps>
      <li Class="HediffCompProperties_SeverityPerDay">
        <severityPerDay>-6.0</severityPerDay>
      </li>
    </comps>
    <stages>
      <li>
        <label>warm</label>
        <minSeverity>0.01</minSeverity>
        <statFactors>
          <MoveSpeed>0.95</MoveSpeed>
        </statFactors>
      </li>
      <li>
        <label>hot</label>
        <minSeverity>0.50</minSeverity>
        <statFactors>
          <MoveSpeed>0.85</MoveSpeed>
        </statFactors>
      </li>
      <li>
        <label>searing</label>
        <minSeverity>0.90</minSeverity>
        <statFactors>
          <MoveSpeed>0.75</MoveSpeed>
        </statFactors>
        <painOffset>0.12</painOffset>
      </li>
    </stages>
  </HediffDef>

  <!-- 2. Protector Auto-Injector Stimpak -->
  <HediffDef>
    <defName>TMM_Hediff_ProtectorStimpak</defName>
    <label>protector auto-stimpak</label>
    <description>Emergency combat stimulants injected by Protector Power Armor suppress pain and dramatically accelerate wound healing.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.35, 0.9, 0.55)</defaultLabelColor>
    <isBad>false</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>15000</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
    </comps>
    <stages>
      <li>
        <painFactor>0.35</painFactor>
        <statOffsets>
          <InjuryHealingFactor>2.0</InjuryHealingFactor>
        </statOffsets>
      </li>
    </stages>
  </HediffDef>

  <!-- 3. Railway Rifle Pinned -->
  <HediffDef>
    <defName>TMM_Hediff_Pinned</defName>
    <label>pinned by railway spike</label>
    <description>A superheated steel spike has mangled tendons and pinned this creature in place, slashing movement speed by 70% for 5 seconds.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.9, 0.35, 0.25)</defaultLabelColor>
    <isBad>true</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>300</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
    </comps>
    <stages>
      <li>
        <statFactors>
          <MoveSpeed>0.30</MoveSpeed>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <!-- 4. First Blade: Permanent Mark of Cain -->
  <HediffDef>
    <defName>TMM_Hediff_MarkOfCain</defName>
    <label>Mark of Cain</label>
    <description>An ancient crimson sigil burned into the forearm upon claiming the First Blade. It grants unnatural speed, melee ferocity, and numbness to pain—but cannot be removed. Going three days without killing builds an unbearable bloodlust that erupts into berserk fury.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.85, 0.12, 0.12)</defaultLabelColor>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <initialSeverity>0.1</initialSeverity>
    <minSeverity>0.1</minSeverity>
    <maxSeverity>1.0</maxSeverity>
    <comps>
      <li Class="TurboMegaMod.HediffCompProperties_MarkOfCain">
        <peaceLimitTicks>180000</peaceLimitTicks>
      </li>
    </comps>
    <stages>
      <li>
        <label>sated</label>
        <minSeverity>0.1</minSeverity>
        <painFactor>0.20</painFactor>
        <statFactors>
          <MoveSpeed>1.35</MoveSpeed>
          <MeleeDamageFactor>1.50</MeleeDamageFactor>
        </statFactors>
      </li>
      <li>
        <label>restless bloodlust</label>
        <minSeverity>0.55</minSeverity>
        <painFactor>0.20</painFactor>
        <statFactors>
          <MoveSpeed>1.35</MoveSpeed>
          <MeleeDamageFactor>1.50</MeleeDamageFactor>
        </statFactors>
      </li>
      <li>
        <label>overwhelming bloodlust</label>
        <minSeverity>0.95</minSeverity>
        <painFactor>0.15</painFactor>
        <statFactors>
          <MoveSpeed>1.40</MoveSpeed>
          <MeleeDamageFactor>1.60</MeleeDamageFactor>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <!-- 5. Dick Roman: Primordial Invulnerability (99% damage reduction unless boraxed) -->
  <HediffDef>
    <defName>TMM_Hediff_DickRomanInvulnerable</defName>
    <label>primordial leviathan invulnerability</label>
    <description>As leader of the Leviathans, Dick Roman shrugs off 99% of all incoming damage. Only caustic sodium borate (borax) burns can strip away this impenetrable resilience for 30 seconds.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.25, 0.25, 0.3)</defaultLabelColor>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <label>invulnerable</label>
      </li>
    </stages>
  </HediffDef>

  <!-- 6. Periodic Borax Acid Burn on Leviathans (30s = 1800 ticks, works with or without Biotech) -->
  <HediffDef>
    <defName>TMM_Hediff_BoraxAcidBurn</defName>
    <label>corrosive borax saturation</label>
    <description>Concentrated sodium borate eats violently through Leviathan flesh, stripping Dick Roman's invulnerability and inflicting continuous acid burns for 30 seconds.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.92, 0.94, 0.98)</defaultLabelColor>
    <isBad>true</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>1800</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
      <li Class="TurboMegaMod.HediffCompProperties_BoraxAcidDot">
        <tickInterval>180</tickInterval>
        <damagePerTick>5</damagePerTick>
      </li>
    </comps>
    <stages>
      <li>
        <statFactors>
          <InjuryHealingFactor>0.01</InjuryHealingFactor>
          <ArmorRating_Sharp>0.50</ArmorRating_Sharp>
          <ArmorRating_Blunt>0.50</ArmorRating_Blunt>
          <ArmorRating_Heat>0.50</ArmorRating_Heat>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

</Defs>
```

`Defs/ResearchProjectDefs/Research_Borax.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <ResearchProjectDef>
    <defName>TMM_BoraxResearch</defName>
    <label>caustic borax weaponry</label>
    <description>Synthesize concentrated sodium borate propellant to craft handheld Borax Sprayers and construct Automated Borax Turrets—the only reliable countermeasure against Leviathans and Dick Roman's invulnerability.</description>
    <baseCost>1200</baseCost>
    <techLevel>Industrial</techLevel>
    <prerequisites>
      <li>Machining</li>
      <li>GunTurrets</li>
    </prerequisites>
    <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
    <researchViewX>9.0</researchViewX>
    <researchViewY>4.8</researchViewY>
  </ResearchProjectDef>

</Defs>
```

`Source/LegendaryItems.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // =========================================================================
    // LEGENDARY UTILITY & BOSS / SUPERNATURAL IDENTIFICATION
    // =========================================================================

    public static class LegendaryUtil
    {
        public const string HeroLucifer = "TMM_Hero_lucifer";
        public const string HeroMichael = "TMM_Hero_michael";
        public const string HeroDickRoman = "TMM_Hero_dick_roman";

        private static bool s_heroLookupInitialized;
        private static MethodInfo s_getHeroDefMethod;
        private static FieldInfo s_heroPawnsDictField;
        private static FieldInfo s_heroPawnsListField;

        public static bool IsImmuneBoss(Pawn pawn)
        {
            if (pawn == null) return false;
            return IsHero(pawn, HeroLucifer) || IsHero(pawn, HeroMichael) || IsHero(pawn, HeroDickRoman);
        }

        public static bool IsDickRoman(Pawn pawn)
        {
            return IsHero(pawn, HeroDickRoman);
        }

        /// <summary>
        /// Checks whether a pawn corresponds to a specific HeroDef without spawning a new pawn.
        /// Uses HeroRegistry reflection first, falling back to exact character name matching.
        /// </summary>
        public static bool IsHero(Pawn pawn, string heroDefName)
        {
            if (pawn == null || heroDefName.NullOrEmpty()) return false;

            string foundDefName = TryGetRegisteredHeroDefName(pawn);
            if (!foundDefName.NullOrEmpty())
            {
                return string.Equals(foundDefName, heroDefName, StringComparison.OrdinalIgnoreCase);
            }

            // Fallback name checks (works in probes or if HeroRegistry uses a different internal map)
            if (pawn.Name != null)
            {
                string full = pawn.Name.ToStringFull;
                string shortName = pawn.Name.ToStringShort;
                if (heroDefName == HeroDickRoman)
                {
                    if (ContainsIgnoreCase(full, "Dick Roman") || ContainsIgnoreCase(shortName, "Dick Roman")) return true;
                }
                else if (heroDefName == HeroLucifer)
                {
                    if (string.Equals(shortName, "Lucifer", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(full, "Lucifer")) return true;
                }
                else if (heroDefName == HeroMichael)
                {
                    if (ContainsIgnoreCase(full, "Archangel Michael") || string.Equals(shortName, "Michael", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }

            return false;
        }

        private static string TryGetRegisteredHeroDefName(Pawn pawn)
        {
            if (pawn == null || HeroRegistry.Instance == null) return null;

            if (!s_heroLookupInitialized)
            {
                s_heroLookupInitialized = true;
                Type regType = HeroRegistry.Instance.GetType();
                // CHECK API: HeroRegistry reverse-lookup method or storage field
                s_getHeroDefMethod = AccessTools.Method(regType, "GetHeroDef", new Type[] { typeof(Pawn) });
                if (s_getHeroDefMethod == null)
                {
                    s_getHeroDefMethod = AccessTools.Method(regType, "DefOf", new Type[] { typeof(Pawn) });
                }
                FieldInfo[] fields = regType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    Type ft = fields[i].FieldType;
                    if (typeof(IDictionary).IsAssignableFrom(ft))
                    {
                        s_heroPawnsDictField = fields[i];
                        break;
                    }
                    if (typeof(IList).IsAssignableFrom(ft) && s_heroPawnsListField == null)
                    {
                        s_heroPawnsListField = fields[i];
                    }
                }
            }

            try
            {
                if (s_getHeroDefMethod != null)
                {
                    Def d = s_getHeroDefMethod.Invoke(HeroRegistry.Instance, new object[] { pawn }) as Def;
                    if (d != null) return d.defName;
                }

                if (s_heroPawnsDictField != null)
                {
                    IDictionary dict = s_heroPawnsDictField.GetValue(HeroRegistry.Instance) as IDictionary;
                    if (dict != null)
                    {
                        foreach (DictionaryEntry entry in dict)
                        {
                            Pawn p = entry.Value as Pawn;
                            if (p != null && p == pawn)
                            {
                                Def keyDef = entry.Key as Def;
                                if (keyDef != null) return keyDef.defName;
                                string keyStr = entry.Key as string;
                                if (!keyStr.NullOrEmpty()) return keyStr;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore reflection lookup mismatch; name fallback will still run.
            }

            return null;
        }

        public static bool IsLeviathan(Pawn pawn)
        {
            if (pawn == null) return false;
            if (IsDickRoman(pawn)) return true;
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (pawn.genes.Xenotype != null && pawn.genes.Xenotype.defName == "TMM_Leviathan") return true;
                GeneDef boraxGene = DefDatabase<GeneDef>.GetNamedSilentFail("TMM_Gene_LeviathanBoraxAllergy");
                if (boraxGene != null && pawn.genes.HasActiveGene(boraxGene)) return true;
            }
            return false;
        }

        public static bool IsSupernaturalForColt(Pawn pawn)
        {
            if (pawn == null) return false;

            if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.Xenotype != null)
            {
                string x = pawn.genes.Xenotype.defName;
                if (x == "TMM_Demon" || x == "TMM_Angel" || x == "TMM_Leviathan" || x == "TMM_Ghoul")
                {
                    return true;
                }
            }

            if (ModsConfig.AnomalyActive && pawn.RaceProps != null)
            {
                if (pawn.RaceProps.IsAnomalyEntity) return true; // CHECK API
            }

            return false;
        }

        public static bool IsGhoulOrSuperMutant(Pawn pawn)
        {
            if (pawn == null || !ModsConfig.BiotechActive || pawn.genes == null || pawn.genes.Xenotype == null) return false;
            string x = pawn.genes.Xenotype.defName;
            return x == "TMM_Ghoul" || x == "TMM_SuperMutant";
        }

        public static bool IsMechanicalOrSynth(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps == null) return false;
            if (!pawn.RaceProps.IsFlesh || pawn.RaceProps.IsMechanoid) return true;
            if (pawn.Faction != null && pawn.Faction.def != null && pawn.Faction.def.defName == "TMM_InstituteFaction")
            {
                if (pawn.kindDef != null && ContainsIgnoreCase(pawn.kindDef.defName, "Synth")) return true;
            }
            return false;
        }

        public static bool HasBoraxVulnerabilityDebuff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef h1 = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_BoraxBurn");
            if (h1 != null && pawn.health.hediffSet.GetFirstHediffOfDef(h1) != null) return true;
            HediffDef h2 = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_BoraxAcidBurn");
            if (h2 != null && pawn.health.hediffSet.GetFirstHediffOfDef(h2) != null) return true;
            return false;
        }

        public static void ApplyBoraxDebuffs(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            if (!IsLeviathan(pawn)) return;

            RefreshHediff(pawn, "TMM_Hediff_BoraxBurn");
            RefreshHediff(pawn, "TMM_Hediff_BoraxAcidBurn");

            // Strip Leviathan Predatory Mimicry if active
            HediffDef mimicDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_PredatoryMimicry");
            if (mimicDef != null)
            {
                Hediff mimic = pawn.health.hediffSet.GetFirstHediffOfDef(mimicDef);
                if (mimic != null) pawn.health.RemoveHediff(mimic);
            }
        }

        public static void EnsureDickRomanInvulnerability(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            if (!IsDickRoman(pawn)) return;

            HediffDef invDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_DickRomanInvulnerable");
            if (invDef == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(invDef) == null)
            {
                pawn.health.AddHediff(invDef);
            }
        }

        public static void EnsureMarkOfCain(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_MarkOfCain");
            if (markDef == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(markDef) == null)
            {
                pawn.health.AddHediff(markDef);
            }
        }

        public static bool IsWearingDef(Pawn pawn, string apparelDefName)
        {
            if (pawn == null || pawn.apparel == null || apparelDefName.NullOrEmpty()) return false;
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] != null && worn[i].def != null && worn[i].def.defName == apparelDefName) return true;
            }
            return false;
        }

        public static bool IsWieldingDef(Pawn pawn, string weaponDefName)
        {
            return pawn != null && pawn.equipment != null && pawn.equipment.Primary != null &&
                   pawn.equipment.Primary.def != null && pawn.equipment.Primary.def.defName == weaponDefName;
        }

        public static float HealOneOrMoreInjuries(Pawn pawn, float maxHeal)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null || maxHeal <= 0f) return 0f;
            float remaining = maxHeal;
            List<Hediff> snapshot = new List<Hediff>(pawn.health.hediffSet.hediffs);
            for (int i = 0; i < snapshot.Count && remaining > 0f; i++)
            {
                Hediff_Injury inj = snapshot[i] as Hediff_Injury;
                if (inj == null || inj.IsPermanent()) continue;
                float amount = Mathf.Min(remaining, inj.Severity);
                if (amount > 0f)
                {
                    inj.Heal(amount);
                    remaining -= amount;
                }
            }
            return maxHeal - remaining;
        }

        private static void RefreshHediff(Pawn pawn, string hediffDefName)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
            pawn.health.AddHediff(def);
        }

        private static bool ContainsIgnoreCase(string hay, string needle)
        {
            return !hay.NullOrEmpty() && !needle.NullOrEmpty() &&
                   hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    // =========================================================================
    // WORLD COMPONENT: LEGENDARY REGISTRY (UNIQUE PER WORLD)
    // =========================================================================

    /// <summary>
    /// Tracks which unique legendary items currently exist in the world.
    /// Destroyed legendaries are automatically unregistered so they can be granted again.
    /// Also periodically ensures any existing Dick Roman pawn carries his invulnerability Hediff.
    /// </summary>
    public sealed class LegendaryRegistry : WorldComponent
    {
        private Dictionary<string, string> grantedThingIds = new Dictionary<string, string>();

        public LegendaryRegistry(World world) : base(world)
        {
        }

        public static LegendaryRegistry Instance
        {
            get { return Find.World != null ? Find.World.GetComponent<LegendaryRegistry>() : null; }
        }

        public bool ExistsInWorld(ThingDef def)
        {
            if (def == null || grantedThingIds == null) return false;
            return grantedThingIds.ContainsKey(def.defName);
        }

        /// <summary>
        /// Creates the legendary item and drop-pods it to the given map only if no instance
        /// of this ThingDef currently exists in the world.
        /// </summary>
        public bool TryGrant(ThingDef def, Map map)
        {
            if (def == null || map == null) return false;
            if (grantedThingIds == null) grantedThingIds = new Dictionary<string, string>();
            if (grantedThingIds.ContainsKey(def.defName)) return false;

            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            Thing item = ThingMaker.MakeThing(def, stuff);
            if (item == null) return false;

            grantedThingIds[def.defName] = item.ThingID;

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map); // CHECK API
            List<Thing> list = new List<Thing>();
            list.Add(item);
            DropPodUtility.DropThingsNear(dropSpot, map, list, 110, false, false, true, false); // CHECK API
            return true;
        }

        public void RegisterExisting(Thing thing)
        {
            if (thing == null || thing.def == null || thing.Destroyed) return;
            if (thing.def.GetCompProperties<CompProperties_LegendaryItem>() == null) return;
            if (grantedThingIds == null) grantedThingIds = new Dictionary<string, string>();
            if (!grantedThingIds.ContainsKey(thing.def.defName))
            {
                grantedThingIds[thing.def.defName] = thing.ThingID;
            }
        }

        public void NotifyDestroyed(Thing thing)
        {
            if (thing == null || thing.def == null || grantedThingIds == null) return;
            string trackedId;
            if (grantedThingIds.TryGetValue(thing.def.defName, out trackedId) && trackedId == thing.ThingID)
            {
                grantedThingIds.Remove(thing.def.defName);
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 250 != 0) return;

            // Guarantee already-existing Dick Roman pawns receive invulnerability Hediff
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map == null || map.mapPawns == null) continue;
                List<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (LegendaryUtil.IsDickRoman(pawns[i]))
                    {
                        LegendaryUtil.EnsureDickRomanInvulnerability(pawns[i]);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref grantedThingIds, "grantedThingIds", LookMode.Value, LookMode.Value);
            if (grantedThingIds == null) grantedThingIds = new Dictionary<string, string>();
        }
    }

    // =========================================================================
    // THING COMPS (LEGENDARY MARKER, LIMITED CHARGES, PROTECTOR, MARK OF CAIN)
    // =========================================================================

    public sealed class CompProperties_LegendaryItem : CompProperties
    {
        public CompProperties_LegendaryItem()
        {
            compClass = typeof(CompLegendaryItem);
        }
    }

    public sealed class CompLegendaryItem : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (LegendaryRegistry.Instance != null)
            {
                LegendaryRegistry.Instance.RegisterExisting(parent);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (LegendaryRegistry.Instance != null)
            {
                LegendaryRegistry.Instance.NotifyDestroyed(parent);
            }
        }
    }

    public sealed class CompProperties_LimitedCharges : CompProperties
    {
        public int maxCharges = 13;
        public bool allowReload = false;
        public ThingDef ammoDef;
        public int ammoCountPerCharge = 1;

        public CompProperties_LimitedCharges()
        {
            compClass = typeof(CompLimitedCharges);
        }
    }

    public sealed class CompLimitedCharges : ThingComp
    {
        private int charges = -1;

        public CompProperties_LimitedCharges Props
        {
            get { return (CompProperties_LimitedCharges)props; }
        }

        public int Charges
        {
            get
            {
                if (charges < 0) charges = Props.maxCharges;
                return charges;
            }
        }

        public bool CanFire
        {
            get { return Charges > 0; }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            charges = Props.maxCharges;
        }

        public bool ConsumeCharge()
        {
            if (Charges <= 0) return false;
            charges = Charges - 1;
            return true;
        }

        public bool TryReloadFromInventory(Pawn wielder)
        {
            if (!Props.allowReload || Props.ammoDef == null || wielder == null || Charges >= Props.maxCharges) return false;
            if (wielder.Map == null) return false;

            int need = Mathf.Max(1, Props.ammoCountPerCharge);
            int available = OutpostPodLogistics.CountResource(wielder.Map, Props.ammoDef);
            if (available < need) return false;

            int taken = OutpostPodLogistics.DeductResource(wielder.Map, Props.ammoDef, need);
            if (taken >= need)
            {
                charges = Mathf.Min(Props.maxCharges, Charges + 1);
                return true;
            }
            return false;
        }

        public override string TransformLabel(string label)
        {
            return string.Format("{0} [{1}/{2}]", label, Charges, Props.maxCharges);
        }

        public override string CompInspectStringExtra()
        {
            return string.Format("Charges: {0} / {1}", Charges, Props.maxCharges);
        }

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra() // CHECK API
        {
            if (Props.allowReload && Props.ammoDef != null && Charges < Props.maxCharges)
            {
                Command_Action reloadCmd = new Command_Action();
                reloadCmd.defaultLabel = string.Format("Reload +1 ({0} {1})", Props.ammoCountPerCharge, Props.ammoDef.label);
                reloadCmd.defaultDesc = "Reload one charge using colony stockpiles.";
                reloadCmd.icon = TexCommand.RearmTrap; // CHECK API
                reloadCmd.action = delegate
                {
                    Pawn holder = parent.ParentHolder is Pawn_EquipmentTracker ? ((Pawn_EquipmentTracker)parent.ParentHolder).pawn : null;
                    if (!TryReloadFromInventory(holder))
                    {
                        Messages.Message("Not enough " + Props.ammoDef.label + " in stockpiles.", MessageTypeDefOf.RejectInput, false);
                    }
                };
                yield return reloadCmd;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref charges, "charges", -1);
            if (charges < 0) charges = Props.maxCharges;
        }
    }

    public sealed class CompProperties_ProtectorAutoInjector : CompProperties
    {
        public int cooldownTicks = 30000; // 12 hours
        public float healthThreshold = 0.30f;

        public CompProperties_ProtectorAutoInjector()
        {
            compClass = typeof(CompProtectorAutoInjector);
        }
    }

    public sealed class CompProtectorAutoInjector : ThingComp
    {
        private int nextReadyTick = -1;

        public CompProperties_ProtectorAutoInjector Props
        {
            get { return (CompProperties_ProtectorAutoInjector)props; }
        }

        public bool TryTrigger(Pawn wearer)
        {
            if (wearer == null || wearer.Dead || wearer.health == null) return false;
            int now = Find.TickManager.TicksGame;
            if (now < nextReadyTick) return false;
            if (wearer.health.summaryHealth.SummaryHealthPercent >= Props.healthThreshold) return false;

            // Tend & heal bleeding injuries
            List<Hediff> snapshot = new List<Hediff>(wearer.health.hediffSet.hediffs);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Hediff_Injury inj = snapshot[i] as Hediff_Injury;
                if (inj != null && inj.Bleeding)
                {
                    inj.Tended(0.85f, 1.0f); // CHECK API
                    inj.Heal(Mathf.Min(inj.Severity, 4f));
                }
            }

            HediffDef stimDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_ProtectorStimpak");
            if (stimDef != null)
            {
                wearer.health.AddHediff(stimDef);
            }

            nextReadyTick = now + Props.cooldownTicks;
            if (wearer.Spawned && wearer.Map != null)
            {
                MoteMaker.ThrowText(wearer.DrawPos, wearer.Map, "AUTO-STIMPAK", Color.green, 3.0f); // CHECK API
            }
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextReadyTick, "nextReadyTick", -1);
        }
    }

    public sealed class CompProperties_MarkOfCainBlade : CompProperties
    {
        public CompProperties_MarkOfCainBlade()
        {
            compClass = typeof(CompMarkOfCainBlade);
        }
    }

    public sealed class CompMarkOfCainBlade : ThingComp
    {
        public override void Notify_Equipped(Pawn pawn) // CHECK API
        {
            base.Notify_Equipped(pawn);
            LegendaryUtil.EnsureMarkOfCain(pawn);
        }
    }

    // =========================================================================
    // HEDIFF COMPS (MARK OF CAIN BLOODLUST, BORAX ACID DOT)
    // =========================================================================

    public sealed class HediffCompProperties_MarkOfCain : HediffCompProperties
    {
        public int peaceLimitTicks = 180000; // 3 days

        public HediffCompProperties_MarkOfCain()
        {
            compClass = typeof(HediffComp_MarkOfCain);
        }
    }

    public sealed class HediffComp_MarkOfCain : HediffComp
    {
        private int ticksSinceLastKill;

        private HediffCompProperties_MarkOfCain Props
        {
            get { return (HediffCompProperties_MarkOfCain)props; }
        }

        public int TicksSinceLastKill
        {
            get { return ticksSinceLastKill; }
        }

        public void NotifyKill()
        {
            ticksSinceLastKill = 0;
            parent.Severity = 0.1f;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn == null || Pawn.Dead || !Pawn.Spawned) return;
            if (!Pawn.IsHashIntervalTick(250)) return;

            ticksSinceLastKill += 250;
            if (ticksSinceLastKill <= Props.peaceLimitTicks)
            {
                if (parent.Severity > 0.1f) parent.Severity = 0.1f;
                return;
            }

            // Build bloodlust from 0.1 -> 1.0 over 1 day past the 3-day threshold
            int over = ticksSinceLastKill - Props.peaceLimitTicks;
            float targetSev = Mathf.Clamp(0.1f + (over / (float)GenDate.TicksPerDay) * 0.9f, 0.1f, 1.0f);
            parent.Severity = targetSev;

            if (parent.Severity >= 0.999f && !Pawn.InMentalState && Pawn.mindState != null && Pawn.mindState.mentalStateHandler != null)
            {
                Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Mark of Cain bloodlust", true, false, false, null); // CHECK API
                ticksSinceLastKill = Props.peaceLimitTicks;
                parent.Severity = 0.55f;
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                float days = ticksSinceLastKill / (float)GenDate.TicksPerDay;
                return string.Format("Days without kill: {0:0.0} / 3.0", days);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksSinceLastKill, "ticksSinceLastKill", 0);
        }
    }

    public sealed class HediffCompProperties_BoraxAcidDot : HediffCompProperties
    {
        public int tickInterval = 180;
        public float damagePerTick = 5f;

        public HediffCompProperties_BoraxAcidDot()
        {
            compClass = typeof(HediffComp_BoraxAcidDot);
        }
    }

    public sealed class HediffComp_BoraxAcidDot : HediffComp
    {
        private HediffCompProperties_BoraxAcidDot Props
        {
            get { return (HediffCompProperties_BoraxAcidDot)props; }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn == null || Pawn.Dead || !Pawn.Spawned) return;
            if (!Pawn.IsHashIntervalTick(Props.tickInterval)) return;
            if (!LegendaryUtil.IsLeviathan(Pawn)) return;

            DamageInfo dinfo = new DamageInfo(DamageDefOf.AcidBurn, Props.damagePerTick, 1.0f); // CHECK API
            Pawn.TakeDamage(dinfo);
        }
    }

    // =========================================================================
    // CUSTOM VERBS (LIMITED CHARGES, FINAL JUDGMENT HEAT, EQUALIZER RECOIL)
    // =========================================================================

    public sealed class Verb_ShootLimitedCharges : Verb_Shoot
    {
        public override bool Available()
        {
            if (!base.Available()) return false;
            CompLimitedCharges comp = EquipmentSource != null ? EquipmentSource.GetComp<CompLimitedCharges>() : null;
            return comp == null || comp.CanFire;
        }

        protected override bool TryCastShot()
        {
            CompLimitedCharges comp = EquipmentSource != null ? EquipmentSource.GetComp<CompLimitedCharges>() : null;
            if (comp != null && !comp.CanFire) return false;

            bool fired = base.TryCastShot();
            if (fired && comp != null)
            {
                comp.ConsumeCharge();
            }
            return fired;
        }
    }

    public sealed class Verb_ShootFinalJudgment : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired && CasterPawn != null && !CasterPawn.Dead && CasterPawn.health != null)
            {
                HediffDef heatDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_FinalJudgmentHeat");
                if (heatDef != null)
                {
                    HealthUtility.AdjustSeverity(CasterPawn, heatDef, 0.03f);
                    Hediff h = CasterPawn.health.hediffSet.GetFirstHediffOfDef(heatDef);
                    if (h != null && h.Severity >= 0.99f)
                    {
                        CasterPawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, 2f, 0f, -1f, CasterPawn, null, EquipmentSource != null ? EquipmentSource.def : null));
                    }
                }
            }
            return fired;
        }
    }

    public sealed class Verb_ShootEqualizer : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool fired = base.TryCastShot();
            if (fired && CasterPawn != null && !CasterPawn.Dead)
            {
                DamageDef recoilDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_Damage_EqualizerRecoil") ?? DamageDefOf.Burn;
                CasterPawn.TakeDamage(new DamageInfo(recoilDef, 12f, 1.0f, -1f, CasterPawn, null, EquipmentSource != null ? EquipmentSource.def : null));
            }
            return fired;
        }
    }

    // =========================================================================
    // CUSTOM PROJECTILES (EXPERIMENT 18-A, COLT, LORENZO, TESLA, EQUALIZER, BORAX)
    // =========================================================================

    public sealed class Bullet_Experiment18A : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || !victim.Dead) return;
            if (victim.Corpse != null && !victim.Corpse.Destroyed && Rand.Value < 0.25f)
            {
                IntVec3 pos = victim.Corpse.Position;
                Map m = victim.Corpse.Map;
                victim.Corpse.Destroy(DestroyMode.Vanish);
                if (m != null && pos.InBounds(m))
                {
                    FilthMaker.TryMakeFilth(pos, m, ThingDefOf.Filth_Ash, 1);
                }
            }
        }
    }

    public sealed class Bullet_TheColt : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;

            DamageDef holyDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_HolyWater");
            if (holyDef != null && LegendaryUtil.IsSupernaturalForColt(victim))
            {
                victim.TakeDamage(new DamageInfo(holyDef, 8f, 1.0f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef));
            }

            if (!victim.Dead && LegendaryUtil.IsSupernaturalForColt(victim) && !LegendaryUtil.IsImmuneBoss(victim))
            {
                DamageInfo killInfo = new DamageInfo(def.projectile.damageDef, 100f, 2.0f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
                victim.Kill(killInfo);
            }
        }
    }

    public sealed class Projectile_LorenzoWave : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Map map = Map;
            IntVec3 center = Position;
            IntVec3 sourcePos = launcher != null ? launcher.Position : origin.ToIntVec3();

            if (map != null && center.InBounds(map))
            {
                int dmg = def.projectile.GetDamageAmount(1f);
                float ap = def.projectile.GetArmorPenetration(1f);
                GenExplosion.DoExplosion(center, map, 1.9f, def.projectile.damageDef, launcher, dmg, ap, null, equipmentDef, def); // CHECK API

                List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p == launcher || p.Dead || p.Destroyed) continue;
                    if (p.Position.DistanceTo(center) > 2.5f) continue;

                    PushPawnBack(p, sourcePos, map, Rand.RangeInclusive(3, 5));
                    if (p.stances != null && p.stances.stunner != null)
                    {
                        p.stances.stunner.StunFor(120, launcher); // CHECK API
                    }
                }
            }

            base.Impact(hitThing, blockedByShield);
        }

        private static void PushPawnBack(Pawn pawn, IntVec3 fromCell, Map map, int cells)
        {
            Vector3 dir = (pawn.Position - fromCell).ToVector3();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            IntVec3 best = pawn.Position;
            for (int step = 1; step <= cells; step++)
            {
                IntVec3 candidate = pawn.Position + new IntVec3(Mathf.RoundToInt(dir.x * step), 0, Mathf.RoundToInt(dir.z * step));
                if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Impassable(map)) break;
                best = candidate;
            }

            if (best != pawn.Position)
            {
                pawn.Position = best;
                pawn.Notify_Teleported(true, true); // CHECK API
            }
        }
    }

    public sealed class Bullet_TeslaCannon : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Map map = Map;
            IntVec3 impactPos = hitThing != null ? hitThing.Position : Position;
            Pawn primaryVictim = hitThing as Pawn;

            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || map == null) return;

            if (primaryVictim != null && !primaryVictim.Dead && LegendaryUtil.IsMechanicalOrSynth(primaryVictim))
            {
                if (primaryVictim.stances != null && primaryVictim.stances.stunner != null)
                {
                    primaryVictim.stances.stunner.StunFor(240, launcher); // CHECK API
                }
            }

            // Chain lightning to up to 3 nearby hostile targets within 4 cells
            float chainDmg = def.projectile.GetDamageAmount(1f) * 0.7f;
            List<Pawn> candidates = new List<Pawn>();
            List<Pawn> all = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || p == primaryVictim || p == launcher || p.Dead || p.Destroyed) continue;
                if (launcher != null && !p.HostileTo(launcher)) continue;
                if (p.Position.DistanceTo(impactPos) <= 4.0f)
                {
                    candidates.Add(p);
                }
            }

            candidates.Sort(delegate(Pawn a, Pawn b)
            {
                return a.Position.DistanceToSquared(impactPos).CompareTo(b.Position.DistanceToSquared(impactPos));
            });

            int hits = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < hits; i++)
            {
                Pawn target = candidates[i];
                FleckMaker.ThrowLightningGlow(target.DrawPos, map, 1.4f); // CHECK API
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, chainDmg, 0.50f, -1f, launcher, null, equipmentDef);
                target.TakeDamage(dinfo);

                if (!target.Dead && LegendaryUtil.IsMechanicalOrSynth(target) && target.stances != null && target.stances.stunner != null)
                {
                    target.stances.stunner.StunFor(240, launcher); // CHECK API
                }
            }
        }
    }

    public sealed class Bullet_TheEqualizer : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;

            DamageDef holyDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_HolyWater");
            if (holyDef != null && LegendaryUtil.IsSupernaturalForColt(victim))
            {
                victim.TakeDamage(new DamageInfo(holyDef, 14f, 1.0f, ExactRotation.eulerAngles.y, launcher, null, equipmentDef));
            }
        }
    }

    public sealed class Bullet_Borax : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false) // CHECK API
        {
            Pawn victim = hitThing as Pawn;
            base.Impact(hitThing, blockedByShield);

            if (blockedByShield || victim == null || victim.Dead) return;
            if (LegendaryUtil.IsLeviathan(victim))
            {
                LegendaryUtil.ApplyBoraxDebuffs(victim);
            }
        }
    }

    // =========================================================================
    // HARMONY PATCHES (EXPLICITLY REGISTERED TO AVOID PatchAll COLLISIONS)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class LegendaryItemsHarmony
    {
        static LegendaryItemsHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.legendary");

            // 1. Thing.TakeDamage Prefix: Dick Roman 99% reduction + Borax vs human near-zero
            MethodInfo takeDamage = AccessTools.Method(typeof(Thing), "TakeDamage", new Type[] { typeof(DamageInfo) });
            if (takeDamage != null)
            {
                harmony.Patch(takeDamage, prefix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Prefix_Thing_TakeDamage"));
            }

            // 2. Pawn.PostApplyDamage Postfix: One-hit blades, Protector Auto-Injector, Atom's Judgement, Alpha Predator, Borax debuff
            MethodInfo postApplyDamage = AccessTools.Method(typeof(Pawn), "PostApplyDamage", new Type[] { typeof(DamageInfo), typeof(float) }); // CHECK API
            if (postApplyDamage != null)
            {
                harmony.Patch(postApplyDamage, postfix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Postfix_Pawn_PostApplyDamage"));
            }

            // 3. Pawn.Kill Postfix: Mark of Cain kill counter reset
            MethodInfo pawnKill = AccessTools.Method(typeof(Pawn), "Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) }); // CHECK API
            if (pawnKill != null)
            {
                harmony.Patch(pawnKill, postfix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Postfix_Pawn_Kill"));
            }

            // 4. Pawn.SpawnSetup Postfix: Auto-apply Dick Roman invulnerability on spawn
            MethodInfo spawnSetup = AccessTools.Method(typeof(Pawn), "SpawnSetup", new Type[] { typeof(Map), typeof(bool) });
            if (spawnSetup != null)
            {
                harmony.Patch(spawnSetup, postfix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Postfix_Pawn_SpawnSetup"));
            }

            // 5. MentalStateHandler.TryStartMentalState Prefix: Golden Pip-Boy anti-panic + Archangel Blade anti-curse
            MethodInfo tryMental = AccessTools.Method(typeof(MentalStateHandler), "TryStartMentalState"); // CHECK API
            if (tryMental != null)
            {
                harmony.Patch(tryMental, prefix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Prefix_TryStartMentalState"));
            }

            // 6. Pawn_HealthTracker.AddHediff Prefix: Archangel Blade immunity to Infernal Whisper
            MethodInfo addHediff = AccessTools.Method(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) }); // CHECK API
            if (addHediff != null)
            {
                harmony.Patch(addHediff, prefix: new HarmonyMethod(typeof(LegendaryItemsHarmony), "Prefix_AddHediff"));
            }
        }

        public static void Prefix_Thing_TakeDamage(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn victim = __instance as Pawn;
            if (victim == null || victim.Dead) return;

            bool isBoraxDamage = (dinfo.Def != null && dinfo.Def.defName == "TMM_BoraxBurn") ||
                                 (dinfo.Weapon != null && (dinfo.Weapon.defName == "TMM_Gun_BoraxSprayer" || dinfo.Weapon.defName == "TMM_Gun_BoraxTurret"));

            // Rule 6: Borax deals near-zero damage to non-Leviathans (humans), full/amplified to Leviathans
            if (isBoraxDamage)
            {
                if (!LegendaryUtil.IsLeviathan(victim))
                {
                    dinfo.SetAmount(Mathf.Min(dinfo.Amount * 0.04f, 0.5f));
                }
                else
                {
                    // Ensure Borax hurts Leviathans heavily even if Biotech damageFactors gene isn't active
                    if (!ModsConfig.BiotechActive)
                    {
                        dinfo.SetAmount(dinfo.Amount * 3.5f);
                    }
                    // Borax bypasses Dick Roman's 99% shield so it can burn him and apply the debuff
                    return;
                }
            }

            // Rule 5: Dick Roman cuts all incoming damage by 99% unless afflicted by Borax Burn
            if (LegendaryUtil.IsDickRoman(victim))
            {
                LegendaryUtil.EnsureDickRomanInvulnerability(victim);
                if (!LegendaryUtil.HasBoraxVulnerabilityDebuff(victim))
                {
                    dinfo.SetAmount(dinfo.Amount * 0.01f);
                }
            }
        }

        public static void Postfix_Pawn_PostApplyDamage(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null) return;

            // 1. Apply Borax debuff whenever Borax damage hits a Leviathan / Dick Roman
            bool isBoraxDamage = (dinfo.Def != null && dinfo.Def.defName == "TMM_BoraxBurn") ||
                                 (dinfo.Weapon != null && (dinfo.Weapon.defName == "TMM_Gun_BoraxSprayer" || dinfo.Weapon.defName == "TMM_Gun_BoraxTurret"));
            if (isBoraxDamage && !__instance.Dead && LegendaryUtil.IsLeviathan(__instance))
            {
                LegendaryUtil.ApplyBoraxDebuffs(__instance);
            }

            // 2. Protector Power Armor auto-injector
            if (!__instance.Dead && __instance.apparel != null)
            {
                List<Apparel> worn = __instance.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    Apparel ap = worn[i];
                    if (ap != null && ap.def != null && ap.def.defName == "TMM_Legendary_ProtectorArmor")
                    {
                        CompProtectorAutoInjector inj = ap.GetComp<CompProtectorAutoInjector>();
                        if (inj != null) inj.TryTrigger(__instance);
                        break;
                    }
                }
            }

            Pawn attacker = dinfo.Instigator as Pawn;
            string weaponDef = dinfo.Weapon != null ? dinfo.Weapon.defName : null;
            if (weaponDef.NullOrEmpty() && attacker != null && attacker.equipment != null && attacker.equipment.Primary != null)
            {
                if (!dinfo.Def.isRanged)
                {
                    weaponDef = attacker.equipment.Primary.def.defName;
                }
            }

            if (weaponDef.NullOrEmpty()) return;

            // 3. Rule 2: First Blade & Archangel Blade one-hit kill (except Lucifer, Michael, Dick Roman)
            if (weaponDef == "TMM_Legendary_FirstBlade" || weaponDef == "TMM_Legendary_ArchangelBlade")
            {
                if (weaponDef == "TMM_Legendary_ArchangelBlade" && __instance.Spawned && __instance.Map != null)
                {
                    FireUtility.TryAttachFire(__instance, 0.35f, attacker); // CHECK API
                }

                if (!__instance.Dead && !LegendaryUtil.IsImmuneBoss(__instance))
                {
                    __instance.Kill(dinfo);
                }
                return;
            }

            // 4. Atom's Judgement radiation pulse (2.5 cells)
            if (weaponDef == "TMM_Legendary_AtomsJudgement" && __instance.Spawned && __instance.Map != null)
            {
                Map map = __instance.Map;
                IntVec3 pos = __instance.Position;
                FleckMaker.ThrowDustPuffThick(pos.ToVector3Shifted(), map, 2.0f, new Color(0.25f, 0.95f, 0.25f)); // CHECK API
                List<Pawn> nearby = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
                for (int i = 0; i < nearby.Count; i++)
                {
                    Pawn p = nearby[i];
                    if (p == null || p.Dead || p.Destroyed || p.Position.DistanceTo(pos) > 2.5f) continue;
                    if (LegendaryUtil.IsGhoulOrSuperMutant(p))
                    {
                        LegendaryUtil.HealOneOrMoreInjuries(p, 10f);
                    }
                    else if (p != attacker && p.RaceProps != null && p.RaceProps.IsFlesh)
                    {
                        float toxRes = Mathf.Clamp01(p.GetStatValue(StatDefOf.ToxicResistance)); // CHECK API
                        float delta = 0.18f * (1f - toxRes);
                        if (delta > 0.001f) HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, delta);
                    }
                }
                return;
            }

            // 5. Alpha Predator lifesteal (8-12 healing + hemogen)
            if (weaponDef == "TMM_Legendary_AlphaPredator" && attacker != null && !attacker.Dead &&
                __instance.RaceProps != null && __instance.RaceProps.IsFlesh && totalDamageDealt > 0.5f)
            {
                LegendaryUtil.HealOneOrMoreInjuries(attacker, Rand.Range(8f, 12f));
                if (ModsConfig.BiotechActive && attacker.genes != null)
                {
                    Gene_Hemogen hemo = attacker.genes.GetFirstGeneOfType<Gene_Hemogen>(); // CHECK API
                    if (hemo != null)
                    {
                        hemo.Value = Mathf.Min(hemo.Max, hemo.Value + 0.15f); // CHECK API
                    }
                }
            }
        }

        public static void Postfix_Pawn_Kill(Pawn __instance, DamageInfo? dinfo)
        {
            if (!dinfo.HasValue) return;
            Pawn killer = dinfo.Value.Instigator as Pawn;
            if (killer == null || killer.Dead || killer.health == null || killer.health.hediffSet == null) return;

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_MarkOfCain");
            if (markDef == null) return;
            HediffWithComps mark = killer.health.hediffSet.GetFirstHediffOfDef(markDef) as HediffWithComps;
            if (mark != null)
            {
                HediffComp_MarkOfCain comp = mark.TryGetComp<HediffComp_MarkOfCain>();
                if (comp != null) comp.NotifyKill();
            }
        }

        public static void Postfix_Pawn_SpawnSetup(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (__instance == null) return;
            if (LegendaryUtil.IsDickRoman(__instance))
            {
                LegendaryUtil.EnsureDickRomanInvulnerability(__instance);
            }
            if (LegendaryUtil.IsWieldingDef(__instance, "TMM_Legendary_FirstBlade"))
            {
                LegendaryUtil.EnsureMarkOfCain(__instance);
            }
        }

        public static bool Prefix_TryStartMentalState(MentalStateHandler __instance, MentalStateDef stateDef, ref bool __result)
        {
            if (stateDef == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null) return true;

            // Golden Pip-Boy blocks panic flee & terror states
            if (LegendaryUtil.IsWearingDef(pawn, "TMM_Legendary_GoldenPipBoy"))
            {
                if (stateDef == MentalStateDefOf.PanicFlee ||
                    stateDef.defName.IndexOf("Panic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    stateDef.defName.IndexOf("Terror", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        public static bool Prefix_AddHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff == null || hediff.def == null) return true;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null) return true;

            // Archangel Blade grants immunity to demonic curses / Infernal Whisper
            if (LegendaryUtil.IsWieldingDef(pawn, "TMM_Legendary_ArchangelBlade"))
            {
                if (hediff.def.defName == "TMM_Hediff_InfernalWhisper")
                {
                    return false;
                }
            }
            return true;
        }
    }
}
```

`Languages/Russian/DefInjected/DamageDef/Damages_Legendary.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Damage_TelekineticWave.label>телекинетический импульс</TMM_Damage_TelekineticWave.label>
  <TMM_Damage_TelekineticWave.deathMessage>{0} раздавлен телекинетической волной.</TMM_Damage_TelekineticWave.deathMessage>

  <TMM_Damage_RailwaySpike.label>раскалённый костыль</TMM_Damage_RailwaySpike.label>
  <TMM_Damage_RailwaySpike.deathMessage>{0} пробит насквозь раскалённым железнодорожным костылём.</TMM_Damage_RailwaySpike.deathMessage>

  <TMM_Damage_MiniNuke.label>мини-ядерный взрыв</TMM_Damage_MiniNuke.label>
  <TMM_Damage_MiniNuke.deathMessage>{0} испарился в тактическом ядерном взрыве.</TMM_Damage_MiniNuke.deathMessage>

  <TMM_Damage_EqualizerRecoil.label>межпространственная отдача</TMM_Damage_EqualizerRecoil.label>
  <TMM_Damage_EqualizerRecoil.deathMessage>{0} погиб от обратной отдачи «Уравнителя».</TMM_Damage_EqualizerRecoil.deathMessage>

</LanguageData>
```

`Languages/Russian/DefInjected/ThingDef/Legendary.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- Легендарное оружие -->
  <TMM_Legendary_Experiment18A.label>«Эксперимент 18-А»</TMM_Legendary_Experiment18A.label>
  <TMM_Legendary_Experiment18A.description>Разогнанный прототип плазменной винтовки Института с нестабильными магнитными катушками. Выпускает сгустки перегретой плазмы с невероятной скоростью, периодически дезинтегрируя цель в кучку пепла.</TMM_Legendary_Experiment18A.description>

  <TMM_Legendary_FinalJudgment.label>«Последний суд»</TMM_Legendary_FinalJudgment.label>
  <TMM_Legendary_FinalJudgment.description>Реликтовый гатлинг-лазер старейшин Братства Стали со встроенным микроядерным реактором. Обрушивает на врагов бесконечный шквал лазерных лучей, но при долгой стрельбе раскаляет броню и обжигает самого стрелка.</TMM_Legendary_FinalJudgment.description>

  <TMM_Legendary_TheColt.label>револьвер «Кольт»</TMM_Legendary_TheColt.label>
  <TMM_Legendary_TheColt.description>Богато украшенный револьвер Colt Paterson 1851 года, выкованный самим Сэмюэлем Кольтом во время прохождения кометы. Заряженный 13 зачарованными серебряными пулями, он способен одним выстрелом сразить почти любую сверхъестественную тварь.</TMM_Legendary_TheColt.description>

  <TMM_Legendary_LorenzosArtifact.label>гамма-пушка «Артефакт Лоренцо»</TMM_Legendary_LorenzosArtifact.label>
  <TMM_Legendary_LorenzosArtifact.description>Глубоко модифицированная гамма-пушка со встроенным излучателем из древней инопланетной короны. Вместо радиации она выпускает мощные телекинетические импульсы, отбрасывающие врагов и крушащие стены и укрытия.</TMM_Legendary_LorenzosArtifact.description>

  <TMM_Legendary_AtomsJudgement.label>суперкувалда «Крушитель Атома»</TMM_Legendary_AtomsJudgement.label>
  <TMM_Legendary_AtomsJudgement.description>Исполинская суперкувалда, к бойку которой приварены четыре фонящих ядерных блока. Каждый удар обрушивает на врага сокрушительную кинетическую мощь и обжигающую радиационную волну по площади.</TMM_Legendary_AtomsJudgement.description>

  <TMM_Legendary_TeslaCannon.label>пушка Теслы Анклава</TMM_Legendary_TeslaCannon.label>
  <TMM_Legendary_TeslaCannon.description>Плечевое энергетическое орудие, созданное учёными Анклава для уничтожения бронетехники и роботов. Высоковольтная дуга перескакивает между ближайшими целями, выжигая электронику и намертво парализуя механоидов и синтов.</TMM_Legendary_TeslaCannon.description>

  <TMM_Legendary_OverseersGuardian.label>карабин «Страж Смотрителя»</TMM_Legendary_OverseersGuardian.label>
  <TMM_Legendary_OverseersGuardian.description>Модифицированный боевой карабин со сдвоенным затворным механизмом. При каждом нажатии на спуск он выпускает две синхронные пули по единой траектории, удваивая убойную силу без потери точности.</TMM_Legendary_OverseersGuardian.description>

  <TMM_Legendary_RailwayRifle.label>гвоздемёт «Железнодорожная винтовка»</TMM_Legendary_RailwayRifle.label>
  <TMM_Legendary_RailwayRifle.description>Пневматическое орудие инженеров Подземки, стреляющее раскалёнными стальными железнодорожными костылями. Тяжёлые снаряды прошивают силовую броню навылет и буквально пригвождают врагов к земле.</TMM_Legendary_RailwayRifle.description>

  <TMM_Legendary_BigBoy.label>ядерный гранатомёт «Большой мальчик»</TMM_Legendary_BigBoy.label>
  <TMM_Legendary_BigBoy.description>Чудовищно переделанная тактическая ядерная катапульта, которую таскают вожаки диких супермутантов. Сдвоенный механизм запускает сразу два мини-ядерных заряда за один выстрел, превращая поле боя в выжженный кратер.</TMM_Legendary_BigBoy.description>

  <TMM_Legendary_TheEqualizer.label>пистолет «Уравнитель»</TMM_Legendary_TheEqualizer.label>
  <TMM_Legendary_TheEqualizer.description>Оккультный межпространственный пистолет, созданный в тайных лабораториях Британских Просвещённых. Он стреляет квантово-связанными зарядами, способными ранить даже архангелов и древних сущностей, но каждый выстрел наносит ответный урон самому стрелку.</TMM_Legendary_TheEqualizer.description>

  <TMM_Legendary_FirstBlade.label>Первый Клинок</TMM_Legendary_FirstBlade.label>
  <TMM_Legendary_FirstBlade.description>Первородный клинок из челюстной кости, которым Каин совершил первое в истории убийство. Связанный с Меткой Каина, он убивает любого врага (кроме высших боссов) с одного удара, но в мирное время терзает владельца неутолимой жаждой крови и приступами ярости.</TMM_Legendary_FirstBlade.description>

  <TMM_Legendary_ArchangelBlade.label>Клинок Архангела</TMM_Legendary_ArchangelBlade.label>
  <TMM_Legendary_ArchangelBlade.description>Золотой витой клинок, выкованный в высших чертогах Небес. Он без труда рассекает любую броню, убивает обычных врагов с одного удара, воспламеняет цели святым огнём и защищает владельца от демонических проклятий.</TMM_Legendary_ArchangelBlade.description>

  <TMM_Legendary_AlphaPredator.label>живой клинок «Альфа-хищник»</TMM_Legendary_AlphaPredator.label>
  <TMM_Legendary_AlphaPredator.description>Жуткий живой клинок, выращенный из крови Альфа-вампира и закалённого биоферрита. Его зазубренное лезвие пьёт жизненную силу врагов при каждом ударе, мгновенно затягивая раны самого владельца.</TMM_Legendary_AlphaPredator.description>

  <!-- Легендарная броня и аксессуары -->
  <TMM_Legendary_ProtectorArmor.label>силовая броня «Протектор»</TMM_Legendary_ProtectorArmor.label>
  <TMM_Legendary_ProtectorArmor.description>Уникальный чёрно-красный комплект силовой брони, собранный лучшими техниками Изгоев. Надевается поверх обычной одежды; в нагрудник встроен медицинский автоинъектор, мгновенно впрыскивающий стимуляторы при падении здоровья ниже 30%.</TMM_Legendary_ProtectorArmor.description>

  <TMM_Legendary_GoldenPipBoy.label>Золотой Пип-Бой 3000 Mark IV</TMM_Legendary_GoldenPipBoy.label>
  <TMM_Legendary_GoldenPipBoy.description>Юбилейный позолоченный наручный компьютер, созданный для высшего руководства Волт-Тек. Улучшенный тактический сопроцессор повышает меткость, ускоряет работу и защищает носителя от паники.</TMM_Legendary_GoldenPipBoy.description>

  <!-- Оружие и турель с бурой -->
  <TMM_Gun_BoraxSprayer.label>распылитель буры</TMM_Gun_BoraxSprayer.label>
  <TMM_Gun_BoraxSprayer.description>Химический распылитель под давлением, заправленный концентрированным раствором тетрабората натрия (буры). Безвредный для людей, этот едкий белый туман разъедает неуязвимую плоть левиафанов и лишает Дика Романа его сверхъестественной защиты.</TMM_Gun_BoraxSprayer.description>

  <TMM_Turret_Borax.label>автоматическая турель с бурой</TMM_Turret_Borax.label>
  <TMM_Turret_Borax.description>Стационарная оборонительная турель, распыляющая концентрированный раствор буры на дистанцию до 18 клеток. Безвредна для людей, но разъедает левиафанов и снимает неуязвимость Дика Романа.</TMM_Turret_Borax.description>

  <TMM_Gun_BoraxTurret.label>форсунка турели с бурой</TMM_Gun_BoraxTurret.label>
  <TMM_Gun_BoraxTurret.description>Автоматический распылитель буры высокого давления, установленный на турели.</TMM_Gun_BoraxTurret.description>

</LanguageData>
```

`Languages/Russian/DefInjected/HediffDef/Hediffs_Legendary.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Hediff_FinalJudgmentHeat.label>перегрев «Последнего суда»</TMM_Hediff_FinalJudgmentHeat.label>
  <TMM_Hediff_FinalJudgmentHeat.description>Тепловая отдача микроядерного блока «Последнего суда» замедляет движение и при предельном нагреве наносит ожоги.</TMM_Hediff_FinalJudgmentHeat.description>

  <TMM_Hediff_ProtectorStimpak.label>автостимулятор «Протектора»</TMM_Hediff_ProtectorStimpak.label>
  <TMM_Hediff_ProtectorStimpak.description>Экстренная доза боевых стимуляторов от силовой брони «Протектор» подавляет боль и резко ускоряет заживление ран.</TMM_Hediff_ProtectorStimpak.description>

  <TMM_Hediff_Pinned.label>пригвождён костылём</TMM_Hediff_Pinned.label>
  <TMM_Hediff_Pinned.description>Раскалённый железнодорожный костыль снизил скорость передвижения на 70% на 5 секунд.</TMM_Hediff_Pinned.description>

  <TMM_Hediff_MarkOfCain.label>Метка Каина</TMM_Hediff_MarkOfCain.label>
  <TMM_Hediff_MarkOfCain.description>Древняя багровая печать, выжженная на предплечье владельца Первого Клинка. Даёт нечеловеческую скорость, мощь в ближнем бою и нечувствительность к боли, но не снимается. Без убийств в течение трёх дней копится неутолимая жажда крови, переходящая в берсерк.</TMM_Hediff_MarkOfCain.description>

  <TMM_Hediff_DickRomanInvulnerable.label>неуязвимость первородного левиафана</TMM_Hediff_DickRomanInvulnerable.label>
  <TMM_Hediff_DickRomanInvulnerable.description>Глава левиафанов Дик Роман игнорирует 99% любого входящего урона. Только ожог едкой бурой способен снять эту защиту на 30 секунд.</TMM_Hediff_DickRomanInvulnerable.description>

  <TMM_Hediff_BoraxAcidBurn.label>разъедающий ожог бурой</TMM_Hediff_BoraxAcidBurn.label>
  <TMM_Hediff_BoraxAcidBurn.description>Концентрированный раствор буры разъедает плоть левиафана, отключая неуязвимость Дика Романа и нанося сильный периодический кислотный урон в течение 30 секунд.</TMM_Hediff_BoraxAcidBurn.description>

</LanguageData>
```

`Languages/Russian/DefInjected/ResearchProjectDef/Research_Borax.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_BoraxResearch.label>оружие на основе буры</TMM_BoraxResearch.label>
  <TMM_BoraxResearch.description>Синтез концентрированного раствора тетрабората натрия для производства ручных распылителей буры и постройки автоматических турелей с бурой — единственного надёжного средства против левиафанов и неуязвимости Дика Романа.</TMM_BoraxResearch.description>

</LanguageData>
```

`Tests/InGameLegendaryProbe.cs`

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
    /// In-game verification probe for all 15 legendary rewards, LegendaryRegistry uniqueness,
    /// First Blade &amp; Archangel Blade one-hit kills (with boss immunity), The Colt 13 charges,
    /// Mark of Cain on equip, Dick Roman 99% invulnerability + Borax bypass, and Borax vs Leviathan/Human.
    /// Activated via -tmm-legendary-probe.
    /// </summary>
    public sealed class InGameLegendaryProbe : MapComponent
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

        private static readonly MethodInfo TryCastShotMethod = AccessTools.Method(typeof(Verb), "TryCastShot"); // CHECK API
        private static readonly MethodInfo ApplyMeleeDamageMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "ApplyMeleeDamageToTarget"); // CHECK API
        private static readonly MethodInfo ProjectileImpactMethod = AccessTools.Method(typeof(Projectile), "Impact", new Type[] { typeof(Thing), typeof(bool) }); // CHECK API

        public InGameLegendaryProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-legendary-probe", StringComparison.OrdinalIgnoreCase)) return true;
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
                WriteReport("LegendaryProbe.json");
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

            // ---------------- 1. LEGENDARY REGISTRY UNIQUENESS ----------------
            LegendaryRegistry reg = LegendaryRegistry.Instance;
            ThingDef coltDef = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Legendary_TheColt");
            AddCheck("Registry_AndColtDefExist", reg != null && coltDef != null, "both non-null",
                string.Format("reg={0}, colt={1}", reg != null, coltDef != null));

            if (reg != null && coltDef != null)
            {
                bool firstGrant = reg.TryGrant(coltDef, map);
                bool secondGrant = reg.TryGrant(coltDef, map);
                AddCheck("Registry_TryGrantTwice", firstGrant && !secondGrant, "first=true, second=false",
                    string.Format("first={0}, second={1}", firstGrant, secondGrant));
            }

            // ---------------- 2. DICK ROMAN INVULNERABILITY + BORAX ----------------
            Pawn dickRoman = SpawnDickRoman(origin + new IntVec3(4, 0, 0));
            AddCheck("DickRoman_SpawnedWithInvuln", dickRoman != null && HasHediff(dickRoman, "TMM_Hediff_DickRomanInvulnerable"),
                "invulnerable Hediff active", dickRoman != null ? HasHediff(dickRoman, "TMM_Hediff_DickRomanInvulnerable").ToString() : "null");

            if (dickRoman != null)
            {
                float hpBefore = dickRoman.health.summaryHealth.SummaryHealthPercent;
                float normalDealt = ApplyTorsoDamage(dickRoman, DamageDefOf.Cut, 30f);
                float hpAfter = dickRoman.health.summaryHealth.SummaryHealthPercent;
                float hpLossPct = (hpBefore - hpAfter) * 100f;
                AddCheck("DickRoman_Blocks99PctNormalDamage", normalDealt < 1.0f && hpLossPct < 1.0f,
                    "dealt<1.0 & loss<1%", string.Format("dealt={0:0.00}, hpLoss={1:0.00}%", normalDealt, hpLossPct));

                // Apply Borax hit via real Borax Sprayer shot
                Pawn boraxShooter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
                ThingWithComps boraxGun = EquipWeapon(boraxShooter, "TMM_Gun_BoraxSprayer");
                FireWeaponOnceAt(boraxShooter, boraxGun, dickRoman);

                bool hasBoraxDebuff = LegendaryUtil.HasBoraxVulnerabilityDebuff(dickRoman);
                float postBoraxDealt = ApplyTorsoDamage(dickRoman, DamageDefOf.Cut, 30f);
                AddCheck("DickRoman_FullDamageAfterBorax", hasBoraxDebuff && postBoraxDealt >= 15f,
                    "boraxDebuff=true, dealt>=15", string.Format("debuff={0}, dealt={1:0.0}", hasBoraxDebuff, postBoraxDealt));
            }

            // ---------------- 3. BORAX VS LEVIATHAN VS HUMAN ----------------
            Pawn leviathan = SpawnPawn("TMM_Leviathan", FindHostileFaction(), origin + new IntVec3(-4, 0, 2));
            Pawn humanTest = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(-4, 0, -2));
            DamageDef boraxDmgDef = DefDatabase<DamageDef>.GetNamedSilentFail("TMM_BoraxBurn") ?? DamageDefOf.Burn;

            float boraxVsLeviathan = leviathan != null ? ApplyTorsoDamage(leviathan, boraxDmgDef, 12f) : 0f;
            float boraxVsHuman = humanTest != null ? ApplyTorsoDamage(humanTest, boraxDmgDef, 12f) : 99f;
            bool levHasDebuff = leviathan != null && LegendaryUtil.HasBoraxVulnerabilityDebuff(leviathan);
            AddCheck("Borax_StrongVsLeviathanNearZeroVsHuman",
                boraxVsLeviathan >= 20f && levHasDebuff && boraxVsHuman <= 1.5f,
                "lev>=20 & debuff & human<=1.5",
                string.Format("lev={0:0.0} (debuff={1}), human={2:0.00}", boraxVsLeviathan, levHasDebuff, boraxVsHuman));

            // ---------------- 4. FIRST BLADE: MARK OF CAIN + ONE-HIT KILL + BOSS IMMUNITY ----------------
            Pawn bladeWielder = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 3));
            ThingWithComps firstBlade = EquipWeapon(bladeWielder, "TMM_Legendary_FirstBlade");
            bool gotMark = HasHediff(bladeWielder, "TMM_Hediff_MarkOfCain");
            AddCheck("FirstBlade_MarkOfCainOnEquip", gotMark, "TMM_Hediff_MarkOfCain present", gotMark.ToString());

            Pawn normalVictim = SpawnPawn(null, FindHostileFaction(), bladeWielder.Position + new IntVec3(1, 0, 0));
            StrikeMeleeOnce(bladeWielder, firstBlade, normalVictim);
            AddCheck("FirstBlade_KillsNormalPawnInOneHit", normalVictim != null && normalVictim.Dead, "Dead=true",
                normalVictim != null ? normalVictim.Dead.ToString() : "null");

            Pawn freshDick = SpawnDickRoman(bladeWielder.Position + new IntVec3(-1, 0, 0));
            StrikeMeleeOnce(bladeWielder, firstBlade, freshDick);
            AddCheck("FirstBlade_DoesNotOneShotDickRoman", freshDick != null && !freshDick.Dead, "Dead=false",
                freshDick != null ? freshDick.Dead.ToString() : "null");

            // ---------------- 5. THE COLT: DEMON KILL, HUMAN NORMAL, 13 CHARGES ----------------
            Pawn coltShooter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, -3));
            ThingWithComps colt = EquipWeapon(coltShooter, "TMM_Legendary_TheColt");
            CompLimitedCharges coltCharges = colt != null ? colt.GetComp<CompLimitedCharges>() : null;

            Pawn demonVictim = SpawnPawn("TMM_Demon", FindHostileFaction(), coltShooter.Position + new IntVec3(4, 0, 0));
            FireWeaponOnceAt(coltShooter, colt, demonVictim);
            AddCheck("Colt_KillsDemonInOneShot", demonVictim != null && demonVictim.Dead, "Dead=true",
                demonVictim != null ? demonVictim.Dead.ToString() : "null");

            Pawn humanVictim = SpawnPawn(null, FindHostileFaction(), coltShooter.Position + new IntVec3(-4, 0, 0));
            FireWeaponOnceAt(coltShooter, colt, humanVictim);
            bool humanSurvivedInjured = humanVictim != null && !humanVictim.Dead && SumInjuries(humanVictim) > 0f;
            AddCheck("Colt_NormalDamageOnHuman", humanSurvivedInjured, "alive & injured",
                humanVictim != null ? string.Format("dead={0}, inj={1:0.0}", humanVictim.Dead, SumInjuries(humanVictim)) : "null");

            // Fire remaining 11 charges (total 13 shots)
            int shotsLeftBefore = coltCharges != null ? coltCharges.Charges : -1;
            for (int s = 0; s < 11; s++)
            {
                Pawn dummy = SpawnPawn(null, FindHostileFaction(), coltShooter.Position + new IntVec3(3, 0, 1));
                FireWeaponOnceAt(coltShooter, colt, dummy);
            }
            int chargesAfter13 = coltCharges != null ? coltCharges.Charges : -1;
            Pawn fourteenthTarget = SpawnPawn(null, FindHostileFaction(), coltShooter.Position + new IntVec3(3, 0, -1));
            bool fourteenthFired = FireWeaponOnceAt(coltShooter, colt, fourteenthTarget);
            AddCheck("Colt_13ChargesExhausted", shotsLeftBefore == 11 && chargesAfter13 == 0 && !fourteenthFired,
                "after2=11, after13=0, shot14=false",
                string.Format("after2={0}, after13={1}, shot14={2}", shotsLeftBefore, chargesAfter13, fourteenthFired));

            // ---------------- 6. SMOKE-TEST EVERY REMAINING LEGENDARY (REAL ACTION, 0 ERRORS) ----------------
            string[] rangedLegendaries = new string[]
            {
                "TMM_Legendary_Experiment18A",
                "TMM_Legendary_FinalJudgment",
                "TMM_Legendary_LorenzosArtifact",
                "TMM_Legendary_TeslaCannon",
                "TMM_Legendary_OverseersGuardian",
                "TMM_Legendary_RailwayRifle",
                "TMM_Legendary_BigBoy",
                "TMM_Legendary_TheEqualizer"
            };

            for (int i = 0; i < rangedLegendaries.Length; i++)
            {
                string defName = rangedLegendaries[i];
                Pawn shooter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(-6, 0, i - 4));
                Pawn target = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(6, 0, i - 4));
                ThingWithComps gun = EquipWeapon(shooter, defName);
                bool ok = FireWeaponOnceAt(shooter, gun, target);
                AddCheck("Smoke_" + defName, ok, "fired cleanly", ok.ToString());
            }

            string[] meleeLegendaries = new string[]
            {
                "TMM_Legendary_AtomsJudgement",
                "TMM_Legendary_ArchangelBlade",
                "TMM_Legendary_AlphaPredator"
            };

            for (int i = 0; i < meleeLegendaries.Length; i++)
            {
                string defName = meleeLegendaries[i];
                Pawn fighter = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(0, 0, 5 + i));
                Pawn target = SpawnPawn(null, FindHostileFaction(), origin + new IntVec3(1, 0, 5 + i));
                ThingWithComps weap = EquipWeapon(fighter, defName);
                bool ok = StrikeMeleeOnce(fighter, weap, target);
                AddCheck("Smoke_" + defName, ok, "struck cleanly", ok.ToString());
            }

            // Apparel / Utility legendaries: Protector Armor + Golden Pip-Boy
            Pawn wearer = SpawnPawn(null, Faction.OfPlayer, origin + new IntVec3(-2, 0, 5));
            Apparel protector = WearApparel(wearer, "TMM_Legendary_ProtectorArmor");
            Apparel pipboy = WearApparel(wearer, "TMM_Legendary_GoldenPipBoy");
            bool panicBlocked = wearer != null && !wearer.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, "probe", true, false, false, null); // CHECK API
            AddCheck("Smoke_ProtectorAndPipBoy", protector != null && pipboy != null && panicBlocked,
                "both worn & panic blocked",
                string.Format("protector={0}, pipboy={1}, panicBlocked={2}", protector != null, pipboy != null, panicBlocked));

            // Research & Borax Turret def check
            ResearchProjectDef boraxRes = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("TMM_BoraxResearch");
            ThingDef boraxTurret = DefDatabase<ThingDef>.GetNamedSilentFail("TMM_Turret_Borax");
            AddCheck("Borax_ResearchAndTurretDefsLoaded", boraxRes != null && boraxTurret != null, "both loaded",
                string.Format("research={0}, turret={1}", boraxRes != null, boraxTurret != null));
        }

        private bool FireWeaponOnceAt(Pawn shooter, ThingWithComps gun, Pawn target)
        {
            if (shooter == null || gun == null || target == null || TryCastShotMethod == null) return false;
            CompEquippable eq = gun.GetComp<CompEquippable>();
            if (eq == null || eq.PrimaryVerb == null) return false;

            Verb verb = eq.PrimaryVerb;
            verb.caster = shooter;

            // Set currentTarget via reflection so TryCastShot fires cleanly synchronously
            FieldInfo curTargetField = AccessTools.Field(typeof(Verb), "currentTarget"); // CHECK API
            if (curTargetField != null)
            {
                curTargetField.SetValue(verb, new LocalTargetInfo(target));
            }

            int beforeCount = map.listerThings.AllThings.Count;
            object res = TryCastShotMethod.Invoke(verb, null);
            bool fired = (res is bool) && (bool)res;
            if (!fired) return false;

            // Force immediate impact on any projectile just launched by this shooter
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

        private bool StrikeMeleeOnce(Pawn attacker, ThingWithComps weapon, Pawn target)
        {
            if (attacker == null || weapon == null || target == null || ApplyMeleeDamageMethod == null) return false;
            CompEquippable eq = weapon.GetComp<CompEquippable>();
            if (eq == null || eq.AllVerbs == null || eq.AllVerbs.Count == 0) return false;

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

        private ThingWithComps EquipWeapon(Pawn pawn, string weaponDefName)
        {
            if (pawn == null || pawn.equipment == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
            if (def == null) return null;
            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            ThingWithComps w = ThingMaker.MakeThing(def, stuff) as ThingWithComps;
            if (w == null) return null;
            pawn.equipment.DestroyAllEquipment();
            pawn.equipment.AddEquipment(w);
            return w;
        }

        private Apparel WearApparel(Pawn pawn, string apparelDefName)
        {
            if (pawn == null || pawn.apparel == null) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
            if (def == null) return null;
            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            Apparel ap = ThingMaker.MakeThing(def, stuff) as Apparel;
            if (ap == null) return null;
            pawn.apparel.Wear(ap, false);
            return ap;
        }

        private Pawn SpawnDickRoman(IntVec3 nearCell)
        {
            HeroDef dickDef = DefDatabase<HeroDef>.GetNamedSilentFail(LegendaryUtil.HeroDickRoman);
            if (dickDef != null && HeroRegistry.Instance != null)
            {
                Pawn h = HeroRegistry.Instance.GetOrCreate(dickDef);
                if (h != null && !h.Dead)
                {
                    if (h.Spawned) h.DeSpawn(DestroyMode.Vanish);
                    if (h.apparel != null) h.apparel.DestroyAll();
                    // Clear any prior borax debuff so invulnerability test is deterministic
                    RemoveHediff(h, "TMM_Hediff_BoraxBurn");
                    RemoveHediff(h, "TMM_Hediff_BoraxAcidBurn");
                    IntVec3 cell = FindStandableNear(nearCell);
                    GenSpawn.Spawn(h, cell, map, WipeMode.Vanish);
                    LegendaryUtil.EnsureDickRomanInvulnerability(h);
                    return h;
                }
            }

            // Fallback named Leviathan pawn if HeroDef not registered in minimal test env
            Pawn fallback = SpawnPawn("TMM_Leviathan", FindHostileFaction(), nearCell);
            if (fallback != null)
            {
                fallback.Name = new NameTriple("Dick", "Dick Roman", "Roman");
                LegendaryUtil.EnsureDickRomanInvulnerability(fallback);
            }
            return fallback;
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

        private static float ApplyTorsoDamage(Pawn pawn, DamageDef dmgDef, float amount)
        {
            if (pawn == null || pawn.Dead || dmgDef == null) return 0f;
            BodyPartRecord torso = null;
            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].def == BodyPartDefOf.Torso)
                {
                    torso = parts[i];
                    break;
                }
            }
            DamageWorker.DamageResult res = pawn.TakeDamage(new DamageInfo(dmgDef, amount, 2.0f, -1f, null, torso)); // CHECK API
            return res != null ? res.totalDamageDealt : 0f;
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

        private static bool HasHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            return def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) != null;
        }

        private static void RemoveHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (h != null) pawn.health.RemoveHediff(h);
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