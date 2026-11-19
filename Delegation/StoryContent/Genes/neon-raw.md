`Defs/DamageDefs/Damages_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <DamageDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_HolyWater</defName>
    <label>holy water burn</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>true</externalViolence>
    <deathMessage>{0} was seared to ash by holy water.</deathMessage>
    <hediff>Burn</hediff>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Heat</armorCategory>
    <defaultDamage>12</defaultDamage>
    <defaultArmorPenetration>0.25</defaultArmorPenetration>
    <buildingDamageFactor>0.1</buildingDamageFactor>
    <plantDamageFactor>0.1</plantDamageFactor>
    <isRanged>true</isRanged>
  </DamageDef>

  <DamageDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_RockSalt</defName>
    <label>rock salt blast</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>true</externalViolence>
    <deathMessage>{0} was torn apart by a blast of rock salt.</deathMessage>
    <hediff>Gunshot</hediff>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Sharp</armorCategory>
    <defaultDamage>10</defaultDamage>
    <defaultArmorPenetration>0.15</defaultArmorPenetration>
    <defaultStoppingPower>1.2</defaultStoppingPower>
    <isRanged>true</isRanged>
  </DamageDef>

  <DamageDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Enochian</defName>
    <label>enochian strike</label>
    <workerClass>DamageWorker_Cut</workerClass>
    <externalViolence>true</externalViolence>
    <deathMessage>{0} was felled by an Enochian-etched weapon.</deathMessage>
    <hediff>Cut</hediff>
    <hediffSolid>Crack</hediffSolid>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Sharp</armorCategory>
    <defaultDamage>16</defaultDamage>
    <defaultArmorPenetration>0.40</defaultArmorPenetration>
    <defaultStoppingPower>1.0</defaultStoppingPower>
    <cutExtraTargetsCurve>
      <points>
        <li>0, 0</li>
        <li>0.6, 1</li>
      </points>
    </cutExtraTargetsCurve>
  </DamageDef>

  <DamageDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_BoraxBurn</defName>
    <label>caustic borax burn</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <externalViolence>true</externalViolence>
    <deathMessage>{0} was dissolved by caustic borax.</deathMessage>
    <hediff>Burn</hediff>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <armorCategory>Heat</armorCategory>
    <defaultDamage>10</defaultDamage>
    <defaultArmorPenetration>0.35</defaultArmorPenetration>
    <buildingDamageFactor>0.2</buildingDamageFactor>
    <isRanged>true</isRanged>
  </DamageDef>

</Defs>
```

`Defs/HediffDefs/Hediffs_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- Passive bleed reduction for Angel Celestial Grace (RimWorld has no BleedRate StatDef) -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_CelestialGrace</defName>
    <label>celestial grace</label>
    <description>Radiant grace knits torn vessels almost instantly, reducing bleeding by 80%.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <becomeVisible>false</becomeVisible>
        <totalBleedFactor>0.2</totalBleedFactor>
      </li>
    </stages>
  </HediffDef>

  <!-- Angel True Visage retinal burn -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_RetinalBurn</defName>
    <label>celestial retinal burn</label>
    <description>Overwhelming divine radiance seared this creature's eyes, severely impairing vision for several hours.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(1.0, 0.85, 0.45)</defaultLabelColor>
    <isBad>true</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>15000</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
    </comps>
    <stages>
      <li>
        <capMods>
          <li>
            <capacity>Sight</capacity>
            <offset>-0.70</offset>
          </li>
        </capMods>
      </li>
    </stages>
  </HediffDef>

  <!-- Demon Infernal Whisper -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_InfernalWhisper</defName>
    <label>infernal whisper</label>
    <description>Dark psychic static clouds the victim's thoughts with dread, lowering consciousness, work speed, and combat responsiveness.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.75, 0.2, 0.25)</defaultLabelColor>
    <isBad>true</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>20000</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
    </comps>
    <stages>
      <li>
        <capMods>
          <li>
            <capacity>Consciousness</capacity>
            <offset>-0.25</offset>
          </li>
        </capMods>
        <statFactors>
          <WorkSpeedGlobal>0.70</WorkSpeedGlobal>
          <AimingDelayFactor>1.30</AimingDelayFactor>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <!-- Super Mutant Irradiated Metabolism (active on polluted ground or toxic fallout) -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_IrradiatedMetabolism</defName>
    <label>irradiated metabolism</label>
    <description>Ambient radiation accelerates FEV cellular division, dramatically increasing wound healing speed and lifting mood.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.45, 0.9, 0.35)</defaultLabelColor>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <statOffsets>
          <InjuryHealingFactor>1.50</InjuryHealingFactor>
        </statOffsets>
      </li>
    </stages>
  </HediffDef>

  <ThoughtDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Thought_IrradiatedMetabolism</defName>
    <workerClass>ThoughtWorker_Hediff</workerClass>
    <hediff>TMM_Hediff_IrradiatedMetabolism</hediff>
    <validWhileDespawned>true</validWhileDespawned>
    <stages>
      <li>
        <label>irradiated vigor</label>
        <description>The radiation in the air makes my blood sing and my wounds knit shut.</description>
        <baseMoodEffect>6</baseMoodEffect>
      </li>
    </stages>
  </ThoughtDef>

  <!-- Super Mutant War Roar caster buff & enemy fear debuff -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_WarRoarAdrenaline</defName>
    <label>mutant battle rush</label>
    <description>A surge of FEV adrenaline accelerates melee strikes and movement for a short burst.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.4, 0.85, 0.3)</defaultLabelColor>
    <isBad>false</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>1200</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
    </comps>
    <stages>
      <li>
        <capMods>
          <li>
            <capacity>Manipulation</capacity>
            <offset>0.25</offset>
          </li>
        </capMods>
        <statFactors>
          <MoveSpeed>1.25</MoveSpeed>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_WarRoarFear</defName>
    <label>terrifying roar</label>
    <description>Shaken to the bone by a super mutant's war bellow, slowing movement speed.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.85, 0.55, 0.2)</defaultLabelColor>
    <isBad>true</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>1200</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
    </comps>
    <stages>
      <li>
        <statFactors>
          <MoveSpeed>0.80</MoveSpeed>
        </statFactors>
      </li>
    </stages>
  </HediffDef>

  <!-- Super Mutant Bulky Physiology hunger multiplier (works even with net metabolism 0) -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_BulkyPhysiology</defName>
    <label>bulky FEV physiology</label>
    <description>Massive mutated muscle mass demands 35% more calories than a human frame.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <isBad>false</isBad>
    <everCurableByItem>false</everCurableByItem>
    <stages>
      <li>
        <becomeVisible>false</becomeVisible>
        <hungerRateFactor>1.35</hungerRateFactor>
      </li>
    </stages>
  </HediffDef>

  <!-- Leviathan Predatory Mimicry disguise -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_PredatoryMimicry</defName>
    <label>predatory mimicry</label>
    <description>Cellular shapeshifting copies another humanoid's face and voice, granting sharp social and trading bonuses. Breaks immediately on borax damage or when using Abyssal Devouring Maw.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.55, 0.35, 0.85)</defaultLabelColor>
    <isBad>false</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>120000</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
      </li>
      <li Class="TurboMegaMod.HediffCompProperties_PredatoryMimicry" />
    </comps>
    <stages>
      <li>
        <statOffsets>
          <SocialImpact>0.25</SocialImpact>
          <NegotiationAbility>0.25</NegotiationAbility>
          <TradePriceImprovement>0.25</TradePriceImprovement>
        </statOffsets>
      </li>
    </stages>
  </HediffDef>

  <!-- Leviathan Caustic Borax Burn debuff -->
  <HediffDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Hediff_BoraxBurn</defName>
    <label>caustic borax burn</label>
    <description>Sodium borate is violently corroding Leviathan tissue, suppressing cellular regeneration and halving armor protection for 30 seconds.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.9, 0.9, 0.95)</defaultLabelColor>
    <isBad>true</isBad>
    <comps>
      <li Class="HediffCompProperties_Disappears">
        <disappearsAfterTicks>1800</disappearsAfterTicks>
        <showRemainingTime>true</showRemainingTime>
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

  <!-- Ghoul Feral Frenzy mental state -->
  <MentalStateDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_MentalState_FeralGhoul</defName>
    <stateClass>TurboMegaMod.MentalState_FeralGhoul</stateClass>
    <label>feral ghoul frenzy</label>
    <category>Aggro</category>
    <blockInteractionInitiationExcept>
      <li>Insult</li>
    </blockInteractionInitiationExcept>
    <blockInteractionRecipientExcept>
      <li>Insult</li>
    </blockInteractionRecipientExcept>
    <minTicksBeforeRecovery>10000</minTicksBeforeRecovery>
    <maxTicksBeforeRecovery>30000</maxTicksBeforeRecovery>
    <recoveryMtbDays>0.4</recoveryMtbDays>
    <nameColor>(0.85, 0.35, 0.2)</nameColor>
    <beginLetter>{0} has succumbed to feral degeneration and is attacking nearby humans in a mindless rage!</beginLetter>
    <beginLetterDef>ThreatSmall</beginLetterDef>
    <recoveryMessage>{0} has pulled back from the brink of feral madness.</recoveryMessage>
    <baseInspectLine>Mental state: Feral frenzy</baseInspectLine>
  </MentalStateDef>

</Defs>
```

`Defs/AbilityDefs/Abilities_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 1. Angel: Healing Touch -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_HealingTouch</defName>
    <label>healing touch</label>
    <description>Press two fingers to a living target's forehead to channel divine grace, instantly closing 3-4 fresh wounds and purging 50% of active infections and toxic buildup.</description>
    <iconPath>UI/Icons/Genes/Gene_Coagulate</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>120000</cooldownTicksRange>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbilityTouch</verbClass>
      <drawAimPie>false</drawAimPie>
      <range>1.5</range>
      <warmupTime>0.75</warmupTime>
      <targetParams>
        <canTargetPawns>true</canTargetPawns>
        <canTargetSelf>false</canTargetSelf>
        <canTargetHumans>true</canTargetHumans>
        <canTargetAnimals>true</canTargetAnimals>
        <canTargetMechs>false</canTargetMechs>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityHealingTouch" />
    </comps>
  </AbilityDef>

  <!-- 2. Angel: True Visage -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_TrueVisage</defName>
    <label>true visage</label>
    <description>Briefly unleash overwhelming celestial light through the vessel's eyes, searing the retinas (-70% sight for 6 hours) and stunning all non-angelic living beings within 5 cells in line of sight.</description>
    <iconPath>UI/Icons/Genes/Gene_PsychicAbilityEnhanced</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>180000</cooldownTicksRange>
    <targetRequired>false</targetRequired>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbility</verbClass>
      <drawAimPie>false</drawAimPie>
      <range>5.0</range>
      <warmupTime>0.5</warmupTime>
      <requireLineOfSight>false</requireLineOfSight>
      <targetParams>
        <canTargetSelf>true</canTargetSelf>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityTrueVisage">
        <radius>5.0</radius>
      </li>
    </comps>
  </AbilityDef>

  <!-- 3. Demon: Black Smoke Shift -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_BlackSmokeShift</defName>
    <label>black smoke shift</label>
    <description>Dissolve into billowing sulfur smoke to rapidly relocate up to 12 cells away, stunning hostile foes within 2 cells of the destination for 2 seconds.</description>
    <iconPath>UI/Icons/Genes/Gene_Longjump</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>90000</cooldownTicksRange>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbility</verbClass>
      <range>12.0</range>
      <warmupTime>0.35</warmupTime>
      <requireLineOfSight>false</requireLineOfSight>
      <targetParams>
        <canTargetLocations>true</canTargetLocations>
        <canTargetPawns>false</canTargetPawns>
        <canTargetBuildings>false</canTargetBuildings>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityBlackSmokeShift">
        <stunRadius>2.0</stunRadius>
        <stunTicks>120</stunTicks>
      </li>
    </comps>
  </AbilityDef>

  <!-- 4. Demon: Infernal Whisper (vanilla GiveHediff XML) -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_InfernalWhisper</defName>
    <label>infernal whisper</label>
    <description>Project dark psychic static into a victim's mind from up to 10 cells away, reducing consciousness by 25% and slowing work and aiming by 30% for 8 hours. Psychically deaf targets resist it.</description>
    <iconPath>UI/Icons/Genes/Gene_PsychicBonding</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>120000</cooldownTicksRange>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbility</verbClass>
      <range>10.0</range>
      <warmupTime>0.5</warmupTime>
      <targetParams>
        <canTargetPawns>true</canTargetPawns>
        <canTargetSelf>false</canTargetSelf>
        <canTargetHumans>true</canTargetHumans>
        <canTargetAnimals>true</canTargetAnimals>
        <canTargetMechs>false</canTargetMechs>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="CompProperties_AbilityGiveHediff">
        <compClass>CompAbilityEffect_GiveHediff</compClass>
        <hediffDef>TMM_Hediff_InfernalWhisper</hediffDef>
        <durationMultiplier>PsychicSensitivity</durationMultiplier>
        <psychic>true</psychic>
        <applicableToMechs>false</applicableToMechs>
      </li>
    </comps>
  </AbilityDef>

  <!-- 5. Super Mutant: Mutant War Roar -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_MutantWarRoar</defName>
    <label>mutant war roar</label>
    <description>Unleash an earth-shaking bellow that floods the mutant with adrenaline (+25% manipulation and speed for 20 seconds) while slowing nearby mortal enemies within 6 cells by 20%.</description>
    <iconPath>UI/Icons/Genes/Gene_AggressionAggressive</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>60000</cooldownTicksRange>
    <targetRequired>false</targetRequired>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbility</verbClass>
      <drawAimPie>false</drawAimPie>
      <range>6.0</range>
      <warmupTime>0.3</warmupTime>
      <requireLineOfSight>false</requireLineOfSight>
      <targetParams>
        <canTargetSelf>true</canTargetSelf>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityMutantWarRoar">
        <radius>6.0</radius>
      </li>
    </comps>
  </AbilityDef>

  <!-- 6. Ghoul: Glowing One's Pulse -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_GlowingPulse</defName>
    <label>glowing one's pulse</label>
    <description>Release a blinding green wave of radiation within 4.5 cells, rapidly healing nearby ghouls and super mutants while inflicting +25% toxic buildup on unprotected humans.</description>
    <iconPath>UI/Icons/Genes/Gene_ToxResistanceTotal</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>120000</cooldownTicksRange>
    <targetRequired>false</targetRequired>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbility</verbClass>
      <drawAimPie>false</drawAimPie>
      <range>4.5</range>
      <warmupTime>0.6</warmupTime>
      <requireLineOfSight>false</requireLineOfSight>
      <targetParams>
        <canTargetSelf>true</canTargetSelf>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityGlowingPulse">
        <radius>4.5</radius>
      </li>
    </comps>
  </AbilityDef>

  <!-- 7. Leviathan: Abyssal Devouring Maw -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_DevourMaw</defName>
    <label>abyssal devouring maw</label>
    <description>Unhinge the jaw into a razor-lined maw to tear flesh from an adjacent living creature, dealing 45 armor-piercing bite damage, restoring 40% hunger, and healing 20 wound severity. Immediately breaks Predatory Mimicry.</description>
    <iconPath>UI/Icons/Genes/Gene_Bloodfeeder</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>30000</cooldownTicksRange>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbilityTouch</verbClass>
      <drawAimPie>false</drawAimPie>
      <range>1.5</range>
      <warmupTime>0.4</warmupTime>
      <targetParams>
        <canTargetPawns>true</canTargetPawns>
        <canTargetSelf>false</canTargetSelf>
        <canTargetHumans>true</canTargetHumans>
        <canTargetAnimals>true</canTargetAnimals>
        <canTargetMechs>false</canTargetMechs>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityDevourMaw" />
    </comps>
  </AbilityDef>

  <!-- 8. Leviathan: Predatory DNA Mimicry -->
  <AbilityDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Ability_PredatoryMimicry</defName>
    <label>predatory DNA mimicry</label>
    <description>Sample a nearby living humanoid or corpse to copy its skin color and hair for 48 hours, granting +25% to social impact, negotiation, and trade prices. Breaks on borax damage or when using Abyssal Devouring Maw.</description>
    <iconPath>UI/Icons/Genes/Gene_BeautyPretty</iconPath> <!-- CHECK API -->
    <cooldownTicksRange>180000</cooldownTicksRange>
    <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
    <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
    <verbProperties>
      <verbClass>Verb_CastAbility</verbClass>
      <range>2.5</range>
      <warmupTime>0.75</warmupTime>
      <targetParams>
        <canTargetPawns>true</canTargetPawns>
        <canTargetItems>true</canTargetItems>
        <canTargetSelf>false</canTargetSelf>
        <canTargetHumans>true</canTargetHumans>
        <canTargetAnimals>false</canTargetAnimals>
        <canTargetMechs>false</canTargetMechs>
      </targetParams>
    </verbProperties>
    <comps>
      <li Class="TurboMegaMod.CompProperties_AbilityPredatoryMimicry" />
    </comps>
  </AbilityDef>

</Defs>
```

`Defs/GeneDefs/Genes_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- HEAVENLY ANGEL (TMM_Angel) — 4 genes, net biostatMet = -6             -->
  <!-- ===================================================================== -->

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_AngelCelestialGrace</defName>
    <geneClass>TurboMegaMod.Gene_SupernaturalPassive</geneClass>
    <label>celestial grace</label>
    <description>Carriers of divine grace move with unnatural speed, shrug off bleeding, and resist mortal aging. This radiant energy binds the angel to its human vessel and fuels its supernatural powers.</description>
    <iconPath>UI/Icons/Genes/Gene_Ageless</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-4</biostatMet>
    <biostatCpx>4</biostatCpx>
    <statFactors>
      <MoveSpeed>1.15</MoveSpeed>
    </statFactors>
    <statOffsets>
      <PsychicSensitivity>0.30</PsychicSensitivity>
    </statOffsets>
    <biologicalAgeTickFactorFromAgeCurve>
      <points>
        <li>(13, 1)</li>
        <li>(18.5, 0)</li>
      </points>
    </biologicalAgeTickFactorFromAgeCurve>
    <modExtensions>
      <li Class="TurboMegaMod.SupernaturalGeneExtension">
        <passiveHediff>TMM_Hediff_CelestialGrace</passiveHediff>
      </li>
    </modExtensions>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_AngelHealingTouch</defName>
    <label>healing touch</label>
    <description>By pressing two fingers to a mortal's forehead, the angel channels pure grace to mend torn flesh and purge infections. The effort drains the angel's reserves and requires time to recover.</description>
    <iconPath>UI/Icons/Genes/Gene_Coagulate</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-2</biostatMet>
    <biostatCpx>3</biostatCpx>
    <abilities>
      <li>TMM_Ability_HealingTouch</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_AngelTrueVisage</defName>
    <label>true visage</label>
    <description>The angel briefly lets its overwhelming celestial light flare through its vessel's eyes. Mortals caught in the radiance suffer searing retinal burns and temporary blindness.</description>
    <iconPath>UI/Icons/Genes/Gene_PsychicAbilityEnhanced</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-3</biostatMet>
    <biostatCpx>4</biostatCpx>
    <abilities>
      <li>TMM_Ability_TrueVisage</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_AngelEnochianVulnerability</defName>
    <label>enochian vulnerability</label>
    <description>Ancient Enochian sigils and celestial bronze disrupt the angel's grace, stripping away its supernatural resilience. Under enchanted blades, an angel takes +150% damage and bleeds like any mortal.</description>
    <iconPath>UI/Icons/Genes/Gene_Delicate</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>3</biostatMet>
    <biostatCpx>2</biostatCpx>
    <!-- NOTE: Men of Letters Enochian jammer beacon ability-suppression will hook into this gene later. -->
    <damageFactors>
      <TMM_Enochian>2.5</TMM_Enochian>
    </damageFactors>
  </GeneDef>

  <!-- ===================================================================== -->
  <!-- DEMON (TMM_Demon) — 4 genes, net biostatMet = -3                      -->
  <!-- ===================================================================== -->

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_DemonBlackSmoke</defName>
    <label>black smoke shift</label>
    <description>The demon momentarily dissolves into a billowing cloud of sulfur-scented black smoke to cross obstacles. Reforming abruptly disorients nearby foes.</description>
    <iconPath>UI/Icons/Genes/Gene_Longjump</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-3</biostatMet>
    <biostatCpx>3</biostatCpx>
    <abilities>
      <li>TMM_Ability_BlackSmokeShift</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_DemonMindCloud</defName>
    <label>infernal whisper</label>
    <description>Projecting dark psychic static into a victim's mind, the demon clouds their thoughts with dread and confusion. Weak-willed targets struggle to aim or react.</description>
    <iconPath>UI/Icons/Genes/Gene_PsychicBonding</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-2</biostatMet>
    <biostatCpx>3</biostatCpx>
    <abilities>
      <li>TMM_Ability_InfernalWhisper</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_DemonCrossroadsGuile</defName>
    <label>crossroads guile</label>
    <description>Centuries of bargaining for mortal souls have honed the demon's silver tongue and predatory charm. They effortlessly sway traders and intimidate prisoners.</description>
    <iconPath>UI/Icons/Genes/Gene_AptitudeRemarkable_Social</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-2</biostatMet>
    <biostatCpx>2</biostatCpx>
    <aptitudes>
      <Social>6</Social>
    </aptitudes>
    <statOffsets>
      <TradePriceImprovement>0.20</TradePriceImprovement>
      <NegotiationAbility>0.25</NegotiationAbility>
      <SuppressionPower MayRequire="Ludeon.RimWorld.Ideology">0.25</SuppressionPower>
    </statOffsets>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_DemonHolyBane</defName>
    <label>bane of salt and holy water</label>
    <description>Consecrated water sears demonic flesh like boiling acid, while rock salt blasts tear through their vessel's endurance, dealing +200% damage.</description>
    <iconPath>UI/Icons/Genes/Gene_FireWeakness</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>4</biostatMet>
    <biostatCpx>2</biostatCpx>
    <!-- NOTE: Devil's Trap floor pentagram immobilization will hook into this gene once traps are implemented. -->
    <damageFactors>
      <TMM_HolyWater>3.0</TMM_HolyWater>
      <TMM_RockSalt>3.0</TMM_RockSalt>
    </damageFactors>
  </GeneDef>

  <!-- ===================================================================== -->
  <!-- SUPER MUTANT (TMM_SuperMutant) — 4 genes, net biostatMet = -4         -->
  <!-- ===================================================================== -->

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_MutantFevMusculature</defName>
    <label>FEV titan frame</label>
    <description>Forced Evolutionary Virus hyper-densifies muscle fibers and bone structure. Super mutants effortlessly haul massive loads and handle heavy machine guns like light carbines.</description>
    <iconPath>UI/Icons/Genes/Gene_StrongMeleeDamage</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-3</biostatMet>
    <biostatCpx>3</biostatCpx>
    <statOffsets>
      <CarryingCapacity>150</CarryingCapacity>
    </statOffsets>
    <statFactors>
      <AimingDelayFactor>0.80</AimingDelayFactor>
      <MeleeDamageFactor>1.25</MeleeDamageFactor>
    </statFactors>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_MutantRadRegeneration</defName>
    <geneClass>TurboMegaMod.Gene_MutantRadRegeneration</geneClass>
    <label>irradiated metabolism</label>
    <description>Mutated cellular repair mechanisms thrive on ambient radiation. Instead of suffering sickness during toxic fallout or on polluted terrain, the super mutant's wounds knit shut rapidly and its mood rises.</description>
    <iconPath>UI/Icons/Genes/Gene_PollutionStimulus</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-2</biostatMet>
    <biostatCpx>3</biostatCpx>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_MutantWarRoar</defName>
    <label>mutant war roar</label>
    <description>Letting out an earth-shaking bellow, the super mutant floods its bloodstream with adrenaline while terrifying nearby human enemies.</description>
    <iconPath>UI/Icons/Genes/Gene_AggressionAggressive</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-2</biostatMet>
    <biostatCpx>2</biostatCpx>
    <abilities>
      <li>TMM_Ability_MutantWarRoar</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_MutantBulkyConstraints</defName>
    <geneClass>TurboMegaMod.Gene_SupernaturalPassive</geneClass>
    <label>bulky FEV physiology</label>
    <description>Extreme FEV mutation leaves the carrier completely sterile and ravenous for calories (+35% hunger rate).</description>
    <iconPath>UI/Icons/Genes/Gene_Sterile</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>3</biostatMet>
    <biostatCpx>2</biostatCpx>
    <statFactors>
      <Fertility>0</Fertility>
    </statFactors>
    <!-- NOTE: Standard human apparel restriction intentionally deferred until mutant apparel defs exist. -->
    <modExtensions>
      <li Class="TurboMegaMod.SupernaturalGeneExtension">
        <passiveHediff>TMM_Hediff_BulkyPhysiology</passiveHediff>
      </li>
    </modExtensions>
  </GeneDef>

  <!-- ===================================================================== -->
  <!-- WASTELAND GHOUL (TMM_Ghoul) — 3 genes, net biostatMet = -2            -->
  <!-- ===================================================================== -->

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_GhoulRadHealing</defName>
    <geneClass>TurboMegaMod.Gene_GhoulRadHealing</geneClass>
    <label>necrotic radiation absorption</label>
    <description>Radiation stimulates a ghoul's necrotic tissue, rapidly knitting torn flesh and easing chronic pain. Standing on polluted ground or under toxic fallout heals 15 wound severity every 4 hours.</description>
    <iconPath>UI/Icons/Genes/Gene_ToxResistanceTotal</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-3</biostatMet>
    <biostatCpx>3</biostatCpx>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_GhoulGlowingPulse</defName>
    <label>glowing one's pulse</label>
    <description>Channeling accumulated nuclear energy, the ghoul unleashes a blinding green wave of radiation that mends nearby mutants and ghouls while sickening unprotected humans.</description>
    <iconPath>UI/Icons/Genes/Gene_AcidSpray</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-2</biostatMet>
    <biostatCpx>3</biostatCpx>
    <abilities>
      <li>TMM_Ability_GlowingPulse</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_GhoulFeralRisk</defName>
    <label>feral degeneration risk</label>
    <description>Centuries of brain necrosis leave every ghoul walking a razor's edge between sanity and feral madness. Extreme mental breaks carry a 25% chance to trigger a mindless feral frenzy.</description>
    <iconPath>UI/Icons/Genes/Gene_AggressionHyperAggressive</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>3</biostatMet>
    <biostatCpx>3</biostatCpx>
  </GeneDef>

  <!-- ===================================================================== -->
  <!-- LEVIATHAN (TMM_Leviathan) — 3 genes, net biostatMet = -2              -->
  <!-- ===================================================================== -->

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_LeviathanDevourMaw</defName>
    <label>abyssal devouring maw</label>
    <description>Unhinging its human disguise into a cavernous maw lined with serrated teeth, the Leviathan tears out massive chunks of flesh to instantly replenish strength and hunger.</description>
    <iconPath>UI/Icons/Genes/Gene_Bloodfeeder</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-3</biostatMet>
    <biostatCpx>3</biostatCpx>
    <abilities>
      <li>TMM_Ability_DevourMaw</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_LeviathanShapeshiftMimic</defName>
    <label>predatory DNA mimicry</label>
    <description>By sampling a drop of a victim's blood or flesh, a Leviathan reshapes its cellular structure into a convincing duplicate of its prey's face and voice for 48 hours.</description>
    <iconPath>UI/Icons/Genes/Gene_BeautyPretty</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>-3</biostatMet>
    <biostatCpx>4</biostatCpx>
    <abilities>
      <li>TMM_Ability_PredatoryMimicry</li>
    </abilities>
  </GeneDef>

  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Gene_LeviathanBoraxAllergy</defName>
    <label>caustic borax sensitivity</label>
    <description>Common sodium borate acts as a violent corrosive agent against Leviathan physiology, dealing +300% damage, stripping disguises, suppressing regeneration, and halving armor for 30 seconds.</description>
    <iconPath>UI/Icons/Genes/Gene_Delicate</iconPath> <!-- CHECK API -->
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatMet>4</biostatMet>
    <biostatCpx>2</biostatCpx>
    <damageFactors>
      <TMM_BoraxBurn>4.0</TMM_BoraxBurn>
    </damageFactors>
  </GeneDef>

</Defs>
```

`Defs/GeneDefs/Genes_Metabolism.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Metabolism genes that cancel the total metabolic cost of our supernatural xenotypes (existing + 18 supernatural genes -> net 0). Biotech only. -->
<Defs>

  <!-- TMM_Angel: base (-9) + new (-4 -2 -3 +3 = -6) = -15 -> +15 -->
  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Metabolism_Angelic</defName>
    <label>angelic metabolism</label>
    <description>Carriers of this gene draw sustenance from a divine source. It fully pays for the metabolic cost of the other angelic genes.</description>
    <iconPath>UI/Icons/Genes/Gene_RobustDigestion</iconPath>
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatCpx>0</biostatCpx>
    <biostatMet>15</biostatMet>
  </GeneDef>

  <!-- TMM_Demon: base (-2) + new (-3 -2 -2 +4 = -3) = -5 -> +5 -->
  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Metabolism_Demonic</defName>
    <label>demonic metabolism</label>
    <description>Carriers of this gene sustain their mortal vessels with infernal energy. It fully pays for the metabolic cost of the other demonic genes.</description>
    <iconPath>UI/Icons/Genes/Gene_RobustDigestion</iconPath>
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatCpx>0</biostatCpx>
    <biostatMet>5</biostatMet>
  </GeneDef>

  <!-- TMM_SuperMutant: base (-10) + new (-3 -2 -2 +3 = -4) = -14 -> +14 -->
  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Metabolism_Mutant</defName>
    <label>FEV mutant metabolism</label>
    <description>Hyper-efficient FEV biochemistry balances the baseline metabolic cost of the super mutant xenotype, leaving only the extra caloric demand of bulky physiology.</description>
    <iconPath>UI/Icons/Genes/Gene_RobustDigestion</iconPath>
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatCpx>0</biostatCpx>
    <biostatMet>14</biostatMet>
  </GeneDef>

  <!-- TMM_Ghoul: base (-8) + new (-3 -2 +3 = -2) = -10 -> +10 -->
  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Metabolism_Ghoul</defName>
    <label>ghoul metabolism</label>
    <description>Irradiated necrotic tissue operates on minimal baseline nutrition, fully offsetting the metabolic cost of other ghoul genes.</description>
    <iconPath>UI/Icons/Genes/Gene_RobustDigestion</iconPath>
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatCpx>0</biostatCpx>
    <biostatMet>10</biostatMet>
  </GeneDef>

  <!-- TMM_Leviathan: base (-8) + new (-3 -3 +4 = -2) = -10 -> +10 -->
  <GeneDef MayRequire="Ludeon.RimWorld.Biotech">
    <defName>TMM_Metabolism_Leviathan</defName>
    <label>leviathan metabolism</label>
    <description>Primordial Purgatory physiology offsets the baseline metabolic cost of Leviathan genetic traits.</description>
    <iconPath>UI/Icons/Genes/Gene_RobustDigestion</iconPath>
    <displayCategory>Miscellaneous</displayCategory>
    <canGenerateInGeneSet>false</canGenerateInGeneSet>
    <biostatCpx>0</biostatCpx>
    <biostatMet>10</biostatMet>
  </GeneDef>

</Defs>
```

`Patches/Xenotypes_SupernaturalGenes.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>

  <Operation Class="PatchOperationAdd" MayRequire="Ludeon.RimWorld.Biotech">
    <xpath>Defs/XenotypeDef[defName="TMM_Angel"]/genes</xpath>
    <value>
      <li>TMM_Gene_AngelCelestialGrace</li>
      <li>TMM_Gene_AngelHealingTouch</li>
      <li>TMM_Gene_AngelTrueVisage</li>
      <li>TMM_Gene_AngelEnochianVulnerability</li>
    </value>
  </Operation>

  <Operation Class="PatchOperationAdd" MayRequire="Ludeon.RimWorld.Biotech">
    <xpath>Defs/XenotypeDef[defName="TMM_Demon"]/genes</xpath>
    <value>
      <li>TMM_Gene_DemonBlackSmoke</li>
      <li>TMM_Gene_DemonMindCloud</li>
      <li>TMM_Gene_DemonCrossroadsGuile</li>
      <li>TMM_Gene_DemonHolyBane</li>
    </value>
  </Operation>

  <Operation Class="PatchOperationAdd" MayRequire="Ludeon.RimWorld.Biotech">
    <xpath>Defs/XenotypeDef[defName="TMM_SuperMutant"]/genes</xpath>
    <value>
      <li>TMM_Gene_MutantFevMusculature</li>
      <li>TMM_Gene_MutantRadRegeneration</li>
      <li>TMM_Gene_MutantWarRoar</li>
      <li>TMM_Gene_MutantBulkyConstraints</li>
    </value>
  </Operation>

  <Operation Class="PatchOperationAdd" MayRequire="Ludeon.RimWorld.Biotech">
    <xpath>Defs/XenotypeDef[defName="TMM_Ghoul"]/genes</xpath>
    <value>
      <li>TMM_Gene_GhoulRadHealing</li>
      <li>TMM_Gene_GhoulGlowingPulse</li>
      <li>TMM_Gene_GhoulFeralRisk</li>
    </value>
  </Operation>

  <Operation Class="PatchOperationAdd" MayRequire="Ludeon.RimWorld.Biotech">
    <xpath>Defs/XenotypeDef[defName="TMM_Leviathan"]/genes</xpath>
    <value>
      <li>TMM_Gene_LeviathanDevourMaw</li>
      <li>TMM_Gene_LeviathanShapeshiftMimic</li>
      <li>TMM_Gene_LeviathanBoraxAllergy</li>
    </value>
  </Operation>

</Patch>
```

`Source/SupernaturalGenes.cs`

```csharp
using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurboMegaMod
{
    // =========================================================================
    // DEF EXTENSIONS & HELPERS
    // =========================================================================

    public sealed class SupernaturalGeneExtension : DefModExtension
    {
        public HediffDef passiveHediff;
    }

    public static class SupernaturalGeneUtil
    {
        public static bool HasActiveGene(Pawn pawn, string geneDefName)
        {
            if (pawn == null || pawn.genes == null || geneDefName.NullOrEmpty()) return false;
            GeneDef def = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
            return def != null && pawn.genes.HasActiveGene(def);
        }

        public static bool IsXenotype(Pawn pawn, string xenotypeDefName)
        {
            return pawn != null && pawn.genes != null && pawn.genes.Xenotype != null &&
                   pawn.genes.Xenotype.defName == xenotypeDefName;
        }

        public static bool IsAngel(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_Angel") || HasActiveGene(pawn, "TMM_Gene_AngelCelestialGrace");
        }

        public static bool IsDemon(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_Demon") || HasActiveGene(pawn, "TMM_Gene_DemonHolyBane");
        }

        public static bool IsSuperMutant(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_SuperMutant") || HasActiveGene(pawn, "TMM_Gene_MutantFevMusculature");
        }

        public static bool IsGhoul(Pawn pawn)
        {
            return IsXenotype(pawn, "TMM_Ghoul") || HasActiveGene(pawn, "TMM_Gene_GhoulRadHealing");
        }

        public static bool IsInRadiation(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return false;
            if (pawn.Position.IsPolluted(pawn.Map)) return true; // CHECK API
            if (pawn.Map.gameConditionManager != null &&
                pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Heals up to maxAmount total injury severity across non-permanent Hediff_Injury entries.
        /// Returns total severity healed.
        /// </summary>
        public static float HealInjuries(Pawn pawn, float maxAmount)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null || maxAmount <= 0f)
            {
                return 0f;
            }

            float remaining = maxAmount;
            List<Hediff> hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
            for (int i = 0; i < hediffs.Count && remaining > 0f; i++)
            {
                Hediff_Injury inj = hediffs[i] as Hediff_Injury;
                if (inj == null || inj.IsPermanent()) continue;
                float heal = Mathf.Min(remaining, inj.Severity);
                if (heal > 0f)
                {
                    inj.Heal(heal);
                    remaining -= heal;
                }
            }
            return maxAmount - remaining;
        }

        public static void StripMimicry(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef mimicDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_PredatoryMimicry");
            if (mimicDef == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(mimicDef);
            if (h != null)
            {
                pawn.health.RemoveHediff(h);
            }
        }
    }

    // =========================================================================
    // PASSIVE GENES (BLEED FACTOR, BULKY HUNGER, RAD REGEN, GHOUL RAD HEALING)
    // =========================================================================

    /// <summary>
    /// Gene that maintains a permanent hidden passive Hediff while active (used for
    /// Celestial Grace totalBleedFactor=0.2 and Bulky FEV Physiology hungerRateFactor=1.35).
    /// </summary>
    public sealed class Gene_SupernaturalPassive : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            EnsurePassiveHediff();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            RemovePassiveHediff();
        }

        public override void Tick() // CHECK API (in 1.6/1.7 check Tick vs TickInterval)
        {
            base.Tick();
            if (pawn != null && pawn.IsHashIntervalTick(600))
            {
                if (Active) EnsurePassiveHediff();
                else RemovePassiveHediff();
            }
        }

        private void EnsurePassiveHediff()
        {
            if (pawn == null || pawn.Dead || pawn.health == null || def == null) return;
            SupernaturalGeneExtension ext = def.GetModExtension<SupernaturalGeneExtension>();
            if (ext == null || ext.passiveHediff == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(ext.passiveHediff) == null)
            {
                pawn.health.AddHediff(ext.passiveHediff);
            }
        }

        private void RemovePassiveHediff()
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null || def == null) return;
            SupernaturalGeneExtension ext = def.GetModExtension<SupernaturalGeneExtension>();
            if (ext == null || ext.passiveHediff == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(ext.passiveHediff);
            if (h != null) pawn.health.RemoveHediff(h);
        }
    }

    /// <summary>
    /// Super Mutant Irradiated Metabolism: grants +150% wound healing and +6 mood while on
    /// polluted ground or during Toxic Fallout.
    /// </summary>
    public sealed class Gene_MutantRadRegeneration : Gene
    {
        public override void Tick() // CHECK API
        {
            base.Tick();
            if (pawn == null || pawn.Dead || !pawn.IsHashIntervalTick(250)) return;
            RefreshRadiationBuff();
        }

        public void RefreshRadiationBuff()
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null) return;
            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_IrradiatedMetabolism");
            if (buffDef == null) return;

            bool shouldHave = Active && SupernaturalGeneUtil.IsInRadiation(pawn);
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(buffDef);
            if (shouldHave && existing == null)
            {
                pawn.health.AddHediff(buffDef);
            }
            else if (!shouldHave && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }
    }

    /// <summary>
    /// Wasteland Ghoul Necrotic Radiation Absorption: heals 15 wound severity every 4 hours
    /// while on polluted ground or under Toxic Fallout.
    /// </summary>
    public sealed class Gene_GhoulRadHealing : Gene
    {
        private const int HealIntervalTicks = 10000; // 4 in-game hours
        private const float HealAmount = 15f;
        private int ticksInRad;

        public override void Tick() // CHECK API
        {
            base.Tick();
            if (!Active || pawn == null || pawn.Dead || !pawn.IsHashIntervalTick(250)) return;
            if (!SupernaturalGeneUtil.IsInRadiation(pawn)) return;

            ticksInRad += 250;
            if (ticksInRad >= HealIntervalTicks)
            {
                ticksInRad -= HealIntervalTicks;
                ApplyRadiationHeal();
            }
        }

        public float ApplyRadiationHeal()
        {
            if (pawn == null || pawn.Dead || !Active) return 0f;
            float healed = SupernaturalGeneUtil.HealInjuries(pawn, HealAmount);
            if (healed > 0f && pawn.Spawned && pawn.Map != null)
            {
                FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map); // CHECK API
            }
            return healed;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksInRad, "ticksInRad", 0);
        }
    }

    // =========================================================================
    // ABILITY EFFECTS
    // =========================================================================

    // 1. Angel: Healing Touch
    public sealed class CompProperties_AbilityHealingTouch : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityHealingTouch()
        {
            compClass = typeof(CompAbilityEffect_HealingTouch);
        }
    }

    public sealed class CompAbilityEffect_HealingTouch : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn p = target.Pawn;
            if (p == null || p.Dead || !p.RaceProps.IsFlesh)
            {
                if (throwMessages) Messages.Message("Target must be a living flesh creature.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn p = target.Pawn;
            if (p == null || p.Dead || p.health == null || p.health.hediffSet == null) return;

            int woundsToClose = Rand.RangeInclusive(3, 4);
            int closed = 0;

            List<Hediff> snapshot = new List<Hediff>(p.health.hediffSet.hediffs);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Hediff h = snapshot[i];
                if (h == null || h is Hediff_MissingPart) continue;

                Hediff_Injury inj = h as Hediff_Injury;
                if (inj != null && !inj.IsPermanent() && closed < woundsToClose)
                {
                    inj.Heal(inj.Severity + 1f);
                    closed++;
                    continue;
                }

                if (IsInfectionOrToxin(h))
                {
                    h.Severity = h.Severity * 0.5f;
                }
            }

            if (p.Spawned && p.Map != null)
            {
                FleckMaker.ThrowLightningGlow(p.DrawPos, p.Map, 1.2f); // CHECK API
            }
        }

        private static bool IsInfectionOrToxin(Hediff h)
        {
            if (h == null || h.def == null) return false;
            if (h.def == HediffDefOf.WoundInfection || h.def == HediffDefOf.ToxicBuildup) return true;
            HediffWithComps hwc = h as HediffWithComps;
            if (hwc != null && hwc.TryGetComp<HediffComp_Immunizable>() != null) return true;
            return false;
        }
    }

    // 2. Angel: True Visage
    public sealed class CompProperties_AbilityTrueVisage : CompProperties_AbilityEffect
    {
        public float radius = 5.0f;

        public CompProperties_AbilityTrueVisage()
        {
            compClass = typeof(CompAbilityEffect_TrueVisage);
        }
    }

    public sealed class CompAbilityEffect_TrueVisage : CompAbilityEffect
    {
        private CompProperties_AbilityTrueVisage Props
        {
            get { return (CompProperties_AbilityTrueVisage)props; }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            float r = Props.radius;
            HediffDef burnDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_RetinalBurn");
            FleckMaker.ThrowLightningGlow(caster.DrawPos, map, 2.5f); // CHECK API

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p.Dead || p.Destroyed) continue;
                if (!p.RaceProps.IsFlesh) continue;
                if (SupernaturalGeneUtil.IsAngel(p)) continue;
                if (p.Position.DistanceTo(caster.Position) > r) continue;
                if (!GenSight.LineOfSight(caster.Position, p.Position, map)) continue;

                // Blind targets are unaffected by visual radiance
                if (p.health != null && p.health.capacities != null &&
                    p.health.capacities.GetLevel(PawnCapacityDefOf.Sight) <= 0.01f)
                {
                    continue;
                }

                if (burnDef != null && p.health != null)
                {
                    p.health.AddHediff(burnDef);
                }
                if (p.stances != null && p.stances.stunner != null)
                {
                    p.stances.stunner.StunFor(180, caster); // CHECK API
                }
            }
        }
    }

    // 3. Demon: Black Smoke Shift
    public sealed class CompProperties_AbilityBlackSmokeShift : CompProperties_AbilityEffect
    {
        public float stunRadius = 2.0f;
        public int stunTicks = 120;

        public CompProperties_AbilityBlackSmokeShift()
        {
            compClass = typeof(CompAbilityEffect_BlackSmokeShift);
        }
    }

    public sealed class CompAbilityEffect_BlackSmokeShift : CompAbilityEffect
    {
        private CompProperties_AbilityBlackSmokeShift Props
        {
            get { return (CompProperties_AbilityBlackSmokeShift)props; }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return false;
            IntVec3 cell = target.Cell;
            if (!cell.InBounds(caster.Map) || !cell.Standable(caster.Map) || cell.Fogged(caster.Map))
            {
                if (throwMessages) Messages.Message("Cannot shift to an impassable or fogged cell.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            IntVec3 destCell = target.Cell;
            if (!destCell.InBounds(map) || !destCell.Standable(map)) return;

            // NOTE: Devil's Trap & Enochian warded wall checks will hook in here once those buildings exist.
            FleckMaker.ThrowSmoke(caster.DrawPos, map, 2.2f); // CHECK API

            caster.Position = destCell;
            caster.Notify_Teleported(true, true); // CHECK API

            FleckMaker.ThrowSmoke(caster.DrawPos, map, 2.5f); // CHECK API

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p.Dead || p.Destroyed) continue;
                if (!p.HostileTo(caster)) continue;
                if (p.Position.DistanceTo(destCell) > Props.stunRadius) continue;

                if (p.stances != null && p.stances.stunner != null)
                {
                    p.stances.stunner.StunFor(Props.stunTicks, caster); // CHECK API
                }
            }
        }
    }

    // 4. Super Mutant: Mutant War Roar
    public sealed class CompProperties_AbilityMutantWarRoar : CompProperties_AbilityEffect
    {
        public float radius = 6.0f;

        public CompProperties_AbilityMutantWarRoar()
        {
            compClass = typeof(CompAbilityEffect_MutantWarRoar);
        }
    }

    public sealed class CompAbilityEffect_MutantWarRoar : CompAbilityEffect
    {
        private CompProperties_AbilityMutantWarRoar Props
        {
            get { return (CompProperties_AbilityMutantWarRoar)props; }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_WarRoarAdrenaline");
            HediffDef fearDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_WarRoarFear");

            if (buffDef != null && caster.health != null)
            {
                caster.health.AddHediff(buffDef);
            }

            Map map = caster.Map;
            FleckMaker.ThrowDustPuffThick(caster.DrawPos, map, 2.2f, new Color(0.55f, 0.75f, 0.35f)); // CHECK API

            if (fearDef == null) return;

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p == caster || p.Dead || p.Destroyed) continue;
                if (!p.HostileTo(caster)) continue;
                if (!p.RaceProps.IsFlesh) continue;
                if (SupernaturalGeneUtil.IsAngel(p) || SupernaturalGeneUtil.IsDemon(p) ||
                    SupernaturalGeneUtil.IsGhoul(p) || SupernaturalGeneUtil.IsSuperMutant(p))
                {
                    continue;
                }
                if (p.Position.DistanceTo(caster.Position) > Props.radius) continue;

                if (p.health != null)
                {
                    p.health.AddHediff(fearDef);
                }
            }
        }
    }

    // 5. Ghoul: Glowing One's Pulse
    public sealed class CompProperties_AbilityGlowingPulse : CompProperties_AbilityEffect
    {
        public float radius = 4.5f;

        public CompProperties_AbilityGlowingPulse()
        {
            compClass = typeof(CompAbilityEffect_GlowingPulse);
        }
    }

    public sealed class CompAbilityEffect_GlowingPulse : CompAbilityEffect
    {
        private CompProperties_AbilityGlowingPulse Props
        {
            get { return (CompProperties_AbilityGlowingPulse)props; }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            Map map = caster.Map;
            FleckMaker.ThrowDustPuffThick(caster.DrawPos, map, 3.0f, new Color(0.25f, 0.95f, 0.25f)); // CHECK API

            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || p.Destroyed) continue;
                if (p.Position.DistanceTo(caster.Position) > Props.radius) continue;

                if (SupernaturalGeneUtil.IsGhoul(p) || SupernaturalGeneUtil.IsSuperMutant(p))
                {
                    if (!p.HostileTo(caster))
                    {
                        SupernaturalGeneUtil.HealInjuries(p, 25f);
                    }
                    continue;
                }

                if (!p.RaceProps.IsFlesh) continue;

                float toxRes = Mathf.Clamp01(p.GetStatValue(StatDefOf.ToxicResistance)); // CHECK API
                float delta = 0.25f * (1f - toxRes);
                if (delta > 0.001f)
                {
                    HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, delta);
                }
            }
        }
    }

    // 6. Leviathan: Abyssal Devouring Maw
    public sealed class CompProperties_AbilityDevourMaw : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDevourMaw()
        {
            compClass = typeof(CompAbilityEffect_DevourMaw);
        }
    }

    public sealed class CompAbilityEffect_DevourMaw : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn p = target.Pawn;
            if (p == null || p.Dead || !p.RaceProps.IsFlesh)
            {
                if (throwMessages) Messages.Message("Target must be a living creature of flesh.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;
            if (caster == null || victim == null || victim.Dead || !victim.RaceProps.IsFlesh) return;

            // Unhinging the maw instantly breaks Predatory Mimicry
            SupernaturalGeneUtil.StripMimicry(caster);

            DamageInfo dinfo = new DamageInfo(DamageDefOf.Bite, 45f, 1.0f, -1f, caster);
            victim.TakeDamage(dinfo);

            if (caster.needs != null && caster.needs.food != null)
            {
                caster.needs.food.CurLevelPercentage = Mathf.Min(1f, caster.needs.food.CurLevelPercentage + 0.40f);
            }

            SupernaturalGeneUtil.HealInjuries(caster, 20f);
        }
    }

    // 7. Leviathan: Predatory DNA Mimicry (simplified: copies skin color + hair only, restores on removal)
    public sealed class CompProperties_AbilityPredatoryMimicry : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityPredatoryMimicry()
        {
            compClass = typeof(CompAbilityEffect_PredatoryMimicry);
        }
    }

    public sealed class CompAbilityEffect_PredatoryMimicry : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn donor = ExtractHumanlikeDonor(target);
            if (donor == null || donor == parent.pawn)
            {
                if (throwMessages) Messages.Message("Target must be another living humanoid or humanoid corpse.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn donor = ExtractHumanlikeDonor(target);
            if (caster == null || donor == null || caster.story == null || donor.story == null) return;

            // Remove any existing mimicry first so original appearance is restored cleanly before re-capturing
            SupernaturalGeneUtil.StripMimicry(caster);

            HediffDef mimicDef = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_PredatoryMimicry");
            if (mimicDef == null || caster.health == null) return;

            HediffWithComps h = HediffMaker.MakeHediff(mimicDef, caster) as HediffWithComps;
            if (h == null) return;

            caster.health.AddHediff(h);
            HediffComp_PredatoryMimicry comp = h.TryGetComp<HediffComp_PredatoryMimicry>();
            if (comp != null)
            {
                comp.CaptureAndApplyMimicry(donor);
            }
        }

        private static Pawn ExtractHumanlikeDonor(LocalTargetInfo target)
        {
            Pawn p = target.Pawn;
            if (p != null && p.RaceProps != null && p.RaceProps.Humanlike) return p;

            Corpse c = target.Thing as Corpse;
            if (c != null && c.InnerPawn != null && c.InnerPawn.RaceProps != null && c.InnerPawn.RaceProps.Humanlike)
            {
                return c.InnerPawn;
            }
            return null;
        }
    }

    public sealed class HediffCompProperties_PredatoryMimicry : HediffCompProperties
    {
        public HediffCompProperties_PredatoryMimicry()
        {
            compClass = typeof(HediffComp_PredatoryMimicry);
        }
    }

    public sealed class HediffComp_PredatoryMimicry : HediffComp
    {
        private bool savedOriginal;
        private bool origHasSkinOverride;
        private Color origSkinOverride;
        private string origHairDefName;
        private Color origHairColor;

        public void CaptureAndApplyMimicry(Pawn donor)
        {
            if (Pawn == null || Pawn.story == null || donor == null || donor.story == null) return;

            if (!savedOriginal)
            {
                origHasSkinOverride = Pawn.story.skinColorOverride.HasValue; // CHECK API
                origSkinOverride = origHasSkinOverride ? Pawn.story.skinColorOverride.Value : Color.white;
                origHairDefName = Pawn.story.hairDef != null ? Pawn.story.hairDef.defName : null;
                origHairColor = Pawn.story.HairColor; // CHECK API
                savedOriginal = true;
            }

            Pawn.story.skinColorOverride = donor.story.SkinColor; // CHECK API
            if (donor.story.hairDef != null)
            {
                Pawn.story.hairDef = donor.story.hairDef;
            }
            Pawn.story.HairColor = donor.story.HairColor; // CHECK API

            RefreshPawnGraphics(Pawn);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!savedOriginal || Pawn == null || Pawn.story == null) return;

            if (origHasSkinOverride)
            {
                Pawn.story.skinColorOverride = origSkinOverride;
            }
            else
            {
                Pawn.story.skinColorOverride = null;
            }

            if (!origHairDefName.NullOrEmpty())
            {
                HairDef restoredHair = DefDatabase<HairDef>.GetNamedSilentFail(origHairDefName);
                if (restoredHair != null) Pawn.story.hairDef = restoredHair;
            }
            Pawn.story.HairColor = origHairColor;

            savedOriginal = false;
            RefreshPawnGraphics(Pawn);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref savedOriginal, "savedOriginal", false);
            Scribe_Values.Look(ref origHasSkinOverride, "origHasSkinOverride", false);
            Scribe_Values.Look(ref origSkinOverride, "origSkinOverride", Color.white);
            Scribe_Values.Look(ref origHairDefName, "origHairDefName", null);
            Scribe_Values.Look(ref origHairColor, "origHairColor", Color.black);
        }

        private static void RefreshPawnGraphics(Pawn p)
        {
            if (p != null && p.Drawer != null && p.Drawer.renderer != null)
            {
                p.Drawer.renderer.SetAllGraphicsDirty(); // CHECK API
            }
        }
    }

    // =========================================================================
    // MENTAL STATE: FERAL GHOUL FRENZY
    // =========================================================================

    public sealed class MentalState_FeralGhoul : MentalState_Berserk
    {
        public override bool ForceHostileTo(Thing t)
        {
            Pawn other = t as Pawn;
            if (other != null && SupernaturalGeneUtil.IsGhoul(other)) return false;
            return base.ForceHostileTo(t);
        }

        public override bool ForceHostileTo(Faction f)
        {
            return true;
        }
    }

    // =========================================================================
    // HARMONY PATCHES (BORAX ALLERGY DEBUFF, FERAL GHOUL BREAK, APPAREL STUB)
    // =========================================================================

    [StaticConstructorOnStartup]
    public static class SupernaturalGenesHarmony
    {
        internal static bool SuppressFeralHook;

        static SupernaturalGenesHarmony()
        {
            Harmony harmony = new Harmony("colonyproject.integrated.supernaturalgenes");
            harmony.PatchAll(typeof(SupernaturalGenesHarmony).Assembly);
        }
    }

    /// <summary>
    /// Applies TMM_Hediff_BoraxBurn and breaks Predatory Mimicry whenever a Leviathan with
    /// TMM_Gene_LeviathanBoraxAllergy takes TMM_BoraxBurn damage.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")] // CHECK API
    public static class Patch_Pawn_PostApplyDamage_Borax
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead || dinfo.Def == null) return;
            if (dinfo.Def.defName != "TMM_BoraxBurn") return;
            if (!SupernaturalGeneUtil.HasActiveGene(__instance, "TMM_Gene_LeviathanBoraxAllergy")) return;

            SupernaturalGeneUtil.StripMimicry(__instance);

            HediffDef boraxHediff = DefDatabase<HediffDef>.GetNamedSilentFail("TMM_Hediff_BoraxBurn");
            if (boraxHediff != null && __instance.health != null)
            {
                Hediff existing = __instance.health.hediffSet.GetFirstHediffOfDef(boraxHediff);
                if (existing != null)
                {
                    __instance.health.RemoveHediff(existing);
                }
                __instance.health.AddHediff(boraxHediff);
            }
        }
    }

    /// <summary>
    /// 25% chance to replace an Extreme Mental Break on a ghoul with TMM_Gene_GhoulFeralRisk
    /// into TMM_MentalState_FeralGhoul.
    /// </summary>
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")] // CHECK API
    public static class Patch_MentalStateHandler_FeralGhoul
    {
        public static void Prefix(MentalStateHandler __instance, ref MentalStateDef stateDef, bool causedByMood)
        {
            if (SupernaturalGenesHarmony.SuppressFeralHook) return;
            if (!causedByMood || stateDef == null) return;
            if (stateDef.defName == "TMM_MentalState_FeralGhoul") return;

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>(); // CHECK API
            if (pawn == null || !SupernaturalGeneUtil.HasActiveGene(pawn, "TMM_Gene_GhoulFeralRisk")) return;
            if (!IsExtremeBreakState(stateDef)) return;

            if (Rand.Value < 0.25f)
            {
                MentalStateDef feralDef = DefDatabase<MentalStateDef>.GetNamedSilentFail("TMM_MentalState_FeralGhoul");
                if (feralDef != null)
                {
                    stateDef = feralDef;
                }
            }
        }

        private static bool IsExtremeBreakState(MentalStateDef stateDef)
        {
            List<MentalBreakDef> breaks = DefDatabase<MentalBreakDef>.AllDefsListForReading;
            for (int i = 0; i < breaks.Count; i++)
            {
                MentalBreakDef mb = breaks[i];
                if (mb != null && mb.mentalState == stateDef && mb.intensity == MentalBreakIntensity.Extreme)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /*
     * STUB (deferred per design decision):
     * When Super Mutant apparel defs are added to the mod, patch ApparelUtility / Pawn_ApparelTracker
     * to prevent pawns with TMM_Gene_MutantBulkyConstraints from wearing standard human-sized apparel
     * unless the apparel carries a mutant-fit tag.
     */
}
```

`Languages/Russian/DefInjected/DamageDef/Damages_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_HolyWater.label>ожог святой водой</TMM_HolyWater.label>
  <TMM_HolyWater.deathMessage>{0} испепелён святой водой.</TMM_HolyWater.deathMessage>

  <TMM_RockSalt.label>залп каменной соли</TMM_RockSalt.label>
  <TMM_RockSalt.deathMessage>{0} разорван залпом каменной соли.</TMM_RockSalt.deathMessage>

  <TMM_Enochian.label>енохианский удар</TMM_Enochian.label>
  <TMM_Enochian.deathMessage>{0} сражён енохианским оружием.</TMM_Enochian.deathMessage>

  <TMM_BoraxBurn.label>ожог едкой бурой</TMM_BoraxBurn.label>
  <TMM_BoraxBurn.deathMessage>{0} растворился под действием едкой буры.</TMM_BoraxBurn.deathMessage>

</LanguageData>
```

`Languages/Russian/DefInjected/HediffDef/Hediffs_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Hediff_CelestialGrace.label>небесная благодать</TMM_Hediff_CelestialGrace.label>
  <TMM_Hediff_CelestialGrace.description>Сияющая благодать почти мгновенно затягивает сосуды, снижая скорость кровотечения на 80%.</TMM_Hediff_CelestialGrace.description>

  <TMM_Hediff_RetinalBurn.label>небесный ожог сетчатки</TMM_Hediff_RetinalBurn.label>
  <TMM_Hediff_RetinalBurn.description>Ослепительный небесный свет обжёг глаза, временно снизив зрение на 70%.</TMM_Hediff_RetinalBurn.description>

  <TMM_Hediff_InfernalWhisper.label>адский дурман</TMM_Hediff_InfernalWhisper.label>
  <TMM_Hediff_InfernalWhisper.description>Тёмный психический туман затуманивает сознание страхом, снижая Сознание, скорость работы и точность реакции.</TMM_Hediff_InfernalWhisper.description>

  <TMM_Hediff_IrradiatedMetabolism.label>радиационная регенерация</TMM_Hediff_IrradiatedMetabolism.label>
  <TMM_Hediff_IrradiatedMetabolism.description>Фоновая радиация ускоряет деление мутировавших клеток, резко ускоряя заживление ран и поднимая настроение.</TMM_Hediff_IrradiatedMetabolism.description>

  <TMM_Hediff_WarRoarAdrenaline.label>боевой прилив мутанта</TMM_Hediff_WarRoarAdrenaline.label>
  <TMM_Hediff_WarRoarAdrenaline.description>Ударная доза адреналина ВРЭ ускоряет удары в ближнем бою и движение.</TMM_Hediff_WarRoarAdrenaline.description>

  <TMM_Hediff_WarRoarFear.label>ужасающий рёв</TMM_Hediff_WarRoarFear.label>
  <TMM_Hediff_WarRoarFear.description>Смертельный ужас от рёва супермутанта замедляет движения жертвы.</TMM_Hediff_WarRoarFear.description>

  <TMM_Hediff_BulkyPhysiology.label>громоздкая физиология ВРЭ</TMM_Hediff_BulkyPhysiology.label>
  <TMM_Hediff_BulkyPhysiology.description>Огромная мышечная масса требует на 35% больше пищи.</TMM_Hediff_BulkyPhysiology.description>

  <TMM_Hediff_PredatoryMimicry.label>хищная мимикрия</TMM_Hediff_PredatoryMimicry.label>
  <TMM_Hediff_PredatoryMimicry.description>Клеточная маскировка копирует внешность и голос жертвы, повышая социальные и торговые навыки. Мгновенно слетает от буры или «Пасти бездны».</TMM_Hediff_PredatoryMimicry.description>

  <TMM_Hediff_BoraxBurn.label>ожог бурой</TMM_Hediff_BoraxBurn.label>
  <TMM_Hediff_BoraxBurn.description>Тетраборат натрия разъедает ткани левиафана, подавляя регенерацию и снижая защиту брони вдвое на 30 секунд.</TMM_Hediff_BoraxBurn.description>

</LanguageData>
```

`Languages/Russian/DefInjected/MentalStateDef/MentalStates_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_MentalState_FeralGhoul.label>приступ одичания</TMM_MentalState_FeralGhoul.label>
  <TMM_MentalState_FeralGhoul.beginLetter>{0} впал в приступ одичания и в слепой ярости бросается на ближайших людей!</TMM_MentalState_FeralGhoul.beginLetter>
  <TMM_MentalState_FeralGhoul.recoveryMessage>{0} сумел удержать рассудок и выйти из приступа одичания.</TMM_MentalState_FeralGhoul.recoveryMessage>
  <TMM_MentalState_FeralGhoul.baseInspectLine>Психическое состояние: Приступ одичания</TMM_MentalState_FeralGhoul.baseInspectLine>

</LanguageData>
```

`Languages/Russian/DefInjected/AbilityDef/Abilities_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <TMM_Ability_HealingTouch.label>исцеление касанием</TMM_Ability_HealingTouch.label>
  <TMM_Ability_HealingTouch.description>Прикоснувшись двумя пальцами ко лбу живой цели, ангел направляет поток благодати, мгновенно закрывая 3–4 свежие раны и снижая тяжесть инфекций и токсинов на 50%.</TMM_Ability_HealingTouch.description>

  <TMM_Ability_TrueVisage.label>истинный облик</TMM_Ability_TrueVisage.label>
  <TMM_Ability_TrueVisage.description>На миг приоткрыть ослепительный небесный облик сквозь глаза сосуда: в радиусе 5 клеток в прямой видимости наносит ожоги сетчатки (-70% зрения на 6 часов) и оглушает всех живых существ, кроме ангелов.</TMM_Ability_TrueVisage.description>

  <TMM_Ability_BlackSmokeShift.label>рывок чёрным дымом</TMM_Ability_BlackSmokeShift.label>
  <TMM_Ability_BlackSmokeShift.description>Распасться на клубы серного дыма для мгновенного рывка на дистанцию до 12 клеток, оглушая врагов в радиусе 2 клеток от точки появления на 2 секунды.</TMM_Ability_BlackSmokeShift.description>

  <TMM_Ability_InfernalWhisper.label>адский дурман</TMM_Ability_InfernalWhisper.label>
  <TMM_Ability_InfernalWhisper.description>Наслать тёмный ментальный туман с дистанции до 10 клеток, снижая Сознание жертвы на 25% и замедляя работу и прицеливание на 30% на 8 часов. Не действует на психоглухих.</TMM_Ability_InfernalWhisper.description>

  <TMM_Ability_MutantWarRoar.label>боевой рёв мутанта</TMM_Ability_MutantWarRoar.label>
  <TMM_Ability_MutantWarRoar.description>Оглушительный рёв даёт мутанту прилив адреналина (+25% к работе рук и скорости на 20 секунд) и замедляет ближайших смертных врагов в радиусе 6 клеток на 20%.</TMM_Ability_MutantWarRoar.description>

  <TMM_Ability_GlowingPulse.label>радиационный импульс светящегося</TMM_Ability_GlowingPulse.label>
  <TMM_Ability_GlowingPulse.description>Высвободить зелёную волну радиации в радиусе 4.5 клеток, исцеляющую союзных гулей и супермутантов и наносящую +25% токсичного заражения незащищённым людям.</TMM_Ability_GlowingPulse.description>

  <TMM_Ability_DevourMaw.label>пасть бездны</TMM_Ability_DevourMaw.label>
  <TMM_Ability_DevourMaw.description>Распахнуть бритвенную пасть вплотную к живой цели из плоти: наносит 45 бронебойного урона, восстанавливает 40% сытости и лечит 20 единиц ран. Мгновенно сбрасывает «Хищную мимикрию».</TMM_Ability_DevourMaw.description>

  <TMM_Ability_PredatoryMimicry.label>хищная мимикрия</TMM_Ability_PredatoryMimicry.label>
  <TMM_Ability_PredatoryMimicry.description>Поглотив образец плоти живого гуманоида или трупа, скопировать его цвет кожи и причёску на 48 часов, получая +25% к социальному влиянию, переговорам и торговле. Слетает от урона бурой или «Пасти бездны».</TMM_Ability_PredatoryMimicry.description>

</LanguageData>
```

`Languages/Russian/DefInjected/GeneDef/Genes_Supernatural.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- Небесный ангел -->
  <TMM_Gene_AngelCelestialGrace.label>небесная благодать</TMM_Gene_AngelCelestialGrace.label>
  <TMM_Gene_AngelCelestialGrace.description>Носители божественной благодати двигаются с нечеловеческой скоростью, почти не истекают кровью и не стареют. Эта сияющая энергия удерживает ангела в человеческом сосуде и питает его силы.</TMM_Gene_AngelCelestialGrace.description>

  <TMM_Gene_AngelHealingTouch.label>исцеление касанием</TMM_Gene_AngelHealingTouch.label>
  <TMM_Gene_AngelHealingTouch.description>Прикоснувшись двумя пальцами ко лбу смертного, ангел направляет поток благодати, затягивая раны и выжигая инфекции. Это требует больших сил и времени на восстановление.</TMM_Gene_AngelHealingTouch.description>

  <TMM_Gene_AngelTrueVisage.label>истинный облик</TMM_Gene_AngelTrueVisage.label>
  <TMM_Gene_AngelTrueVisage.description>Ангел на миг приоткрывает свой ослепительный небесный облик сквозь глаза сосуда. Смертные, попавшие в вспышку, получают ожоги сетчатки и временно слепнут.</TMM_Gene_AngelTrueVisage.description>

  <TMM_Gene_AngelEnochianVulnerability.label>уязвимость к енохианским знакам</TMM_Gene_AngelEnochianVulnerability.label>
  <TMM_Gene_AngelEnochianVulnerability.description>Древние енохианские символы и небесная бронза нарушают связь ангела с благодатью. Под ударами зачарованных клинков он получает +150% урона и становится уязвим как обычный смертный.</TMM_Gene_AngelEnochianVulnerability.description>

  <!-- Демон -->
  <TMM_Gene_DemonBlackSmoke.label>рывок чёрным дымом</TMM_Gene_DemonBlackSmoke.label>
  <TMM_Gene_DemonBlackSmoke.description>Демон на мгновение распадается на клубы чёрного серного дыма, преодолевая препятствия. Резкое возвращение в плоть дезориентирует ближайших врагов.</TMM_Gene_DemonBlackSmoke.description>

  <TMM_Gene_DemonMindCloud.label>адский дурман</TMM_Gene_DemonMindCloud.label>
  <TMM_Gene_DemonMindCloud.description>Насылая тёмный ментальный туман в разум жертвы, демон затуманивает её сознание страхом и растерянностью. Слабовольные цели с трудом держат оружие и медленно реагируют.</TMM_Gene_DemonMindCloud.description>

  <TMM_Gene_DemonCrossroadsGuile.label>хитрость перекрёстка</TMM_Gene_DemonCrossroadsGuile.label>
  <TMM_Gene_DemonCrossroadsGuile.description>Столетия торговли смертными душами отточили язык и хищное обаяние демона. Он без труда склоняет торговцев к невыгодным сделкам и ломает волю пленников.</TMM_Gene_DemonCrossroadsGuile.description>

  <TMM_Gene_DemonHolyBane.label>проклятие соли и святой воды</TMM_Gene_DemonHolyBane.label>
  <TMM_Gene_DemonHolyBane.description>Освящённая вода жжёт демоническую плоть словно кипящая кислота, а заряды каменной соли разрывают выносливость сосуда, нанося +200% урона.</TMM_Gene_DemonHolyBane.description>

  <!-- Супермутант -->
  <TMM_Gene_MutantFevMusculature.label>титаническая мускулатура ВРЭ</TMM_Gene_MutantFevMusculature.label>
  <TMM_Gene_MutantFevMusculature.description>Вирус рукотворной эволюции многократно уплотняет мышечные волокна и кости. Супермутанты легко переносят огромные грузы и обращаются с тяжёлыми пулемётами как с лёгкими карабинами.</TMM_Gene_MutantFevMusculature.description>

  <TMM_Gene_MutantRadRegeneration.label>радиационная регенерация</TMM_Gene_MutantRadRegeneration.label>
  <TMM_Gene_MutantRadRegeneration.description>Мутировавшие клетки используют радиацию как катализатор деления. Вместо лучевой болезни во время токсичных осадков или в загрязнённой зоне раны супермутанта затягиваются прямо на глазах.</TMM_Gene_MutantRadRegeneration.description>

  <TMM_Gene_MutantWarRoar.label>боевой рёв мутанта</TMM_Gene_MutantWarRoar.label>
  <TMM_Gene_MutantWarRoar.description>Издавая оглушительный рёв, супермутант выбрасывает в кровь ударную дозу адреналина и наводит ужас на ближайших врагов-людей.</TMM_Gene_MutantWarRoar.description>

  <TMM_Gene_MutantBulkyConstraints.label>громоздкая физиология ВРЭ</TMM_Gene_MutantBulkyConstraints.label>
  <TMM_Gene_MutantBulkyConstraints.description>Глубокая мутация ВРЭ делает носителя полностью бесплодным и вечно голодным (+35% к скорости голода).</TMM_Gene_MutantBulkyConstraints.description>

  <!-- Гуль пустошей -->
  <TMM_Gene_GhoulRadHealing.label>поглощение радиации</TMM_Gene_GhoulRadHealing.label>
  <TMM_Gene_GhoulRadHealing.description>Радиация стимулирует некротические ткани гуля, быстро затягивая раны и унимая вечную боль в суставах. То, что убивает «гладкокожих», для гуля — лучшее лекарство.</TMM_Gene_GhoulRadHealing.description>

  <TMM_Gene_GhoulGlowingPulse.label>радиационный импульс светящегося</TMM_Gene_GhoulGlowingPulse.label>
  <TMM_Gene_GhoulGlowingPulse.description>Высвобождая накопленную ядерную энергию, гуль испускает ослепительную зелёную волну радиации, исцеляющую соседних гулей и мутантов, но отравляющую людей.</TMM_Gene_GhoulGlowingPulse.description>

  <TMM_Gene_GhoulFeralRisk.label>риск одичания</TMM_Gene_GhoulFeralRisk.label>
  <TMM_Gene_GhoulFeralRisk.description>Века некроза мозга заставляют каждого гуля балансировать на грани рассудка и животного безумия. Тяжёлый нервный срыв с шансом 25% может толкнуть его к приступу одичания.</TMM_Gene_GhoulFeralRisk.description>

  <!-- Левиафан -->
  <TMM_Gene_LeviathanDevourMaw.label>пасть бездны</TMM_Gene_LeviathanDevourMaw.label>
  <TMM_Gene_LeviathanDevourMaw.description>Распахивая человеческую личину в бездонную пасть с рядами бритвенных зубов, левиафан вырывает куски плоти, мгновенно утоляя голод и залечивая раны.</TMM_Gene_LeviathanDevourMaw.description>

  <TMM_Gene_LeviathanShapeshiftMimic.label>хищная мимикрия</TMM_Gene_LeviathanShapeshiftMimic.label>
  <TMM_Gene_LeviathanShapeshiftMimic.description>Поглотив каплю крови или плоти жертвы, левиафан перестраивает свои клетки и превращается в безупречную внешнюю и голосовую копию добычи.</TMM_Gene_LeviathanShapeshiftMimic.description>

  <TMM_Gene_LeviathanBoraxAllergy.label>уязвимость к буре</TMM_Gene_LeviathanBoraxAllergy.label>
  <TMM_Gene_LeviathanBoraxAllergy.description>Обычный тетраборат натрия (бура) действует на физиологию левиафанов как сильнейшая кислота, нанося +300% урона, сбивая маскировку, подавляя регенерацию и снижая броню вдвое на 30 секунд.</TMM_Gene_LeviathanBoraxAllergy.description>

</LanguageData>
```

`Tests/InGameSupernaturalGenesProbe.cs`

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
    /// In-game verification probe for the 18 supernatural genes across all 5 xenotypes:
    /// checks active genes, metabolism sum == 0, passive stat/radiation effects, real Ability.Activate
    /// execution for all 8 abilities, and Weakness DamageDef multipliers vs baseline human.
    /// Activated via -tmm-supergenes-probe.
    /// </summary>
    public sealed class InGameSupernaturalGenesProbe : MapComponent
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

        public InGameSupernaturalGenesProbe(Map map) : base(map)
        {
        }

        private static bool ProbeEnabled
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "-tmm-supergenes-probe", StringComparison.OrdinalIgnoreCase)) return true;
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
                WriteReport("SupernaturalGenesProbe.json");
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
            IntVec3 origin = FindClearAreaCenter();

            Pawn human = SpawnXenotypePawn(null, Faction.OfPlayer, origin);
            Pawn angel = SpawnXenotypePawn("TMM_Angel", Faction.OfPlayer, origin + new IntVec3(2, 0, 0));
            Pawn demon = SpawnXenotypePawn("TMM_Demon", Faction.OfPlayer, origin + new IntVec3(-2, 0, 0));
            Pawn mutant = SpawnXenotypePawn("TMM_SuperMutant", Faction.OfPlayer, origin + new IntVec3(0, 0, 2));
            Pawn ghoul = SpawnXenotypePawn("TMM_Ghoul", Faction.OfPlayer, origin + new IntVec3(0, 0, -2));
            Pawn leviathan = SpawnXenotypePawn("TMM_Leviathan", Faction.OfPlayer, origin + new IntVec3(2, 0, 2));

            // ---------------- 1. XENOTYPE GENES & METABOLISM == 0 ----------------
            VerifyRaceGenesAndMetabolism("Angel", angel, new string[]
            {
                "TMM_Gene_AngelCelestialGrace",
                "TMM_Gene_AngelHealingTouch",
                "TMM_Gene_AngelTrueVisage",
                "TMM_Gene_AngelEnochianVulnerability"
            });
            VerifyRaceGenesAndMetabolism("Demon", demon, new string[]
            {
                "TMM_Gene_DemonBlackSmoke",
                "TMM_Gene_DemonMindCloud",
                "TMM_Gene_DemonCrossroadsGuile",
                "TMM_Gene_DemonHolyBane"
            });
            VerifyRaceGenesAndMetabolism("SuperMutant", mutant, new string[]
            {
                "TMM_Gene_MutantFevMusculature",
                "TMM_Gene_MutantRadRegeneration",
                "TMM_Gene_MutantWarRoar",
                "TMM_Gene_MutantBulkyConstraints"
            });
            VerifyRaceGenesAndMetabolism("Ghoul", ghoul, new string[]
            {
                "TMM_Gene_GhoulRadHealing",
                "TMM_Gene_GhoulGlowingPulse",
                "TMM_Gene_GhoulFeralRisk"
            });
            VerifyRaceGenesAndMetabolism("Leviathan", leviathan, new string[]
            {
                "TMM_Gene_LeviathanDevourMaw",
                "TMM_Gene_LeviathanShapeshiftMimic",
                "TMM_Gene_LeviathanBoraxAllergy"
            });

            // ---------------- 2. PASSIVE STATS & RADIATION ----------------
            if (human != null && angel != null)
            {
                float humanSpeed = human.GetStatValue(StatDefOf.MoveSpeed);
                float angelSpeed = angel.GetStatValue(StatDefOf.MoveSpeed);
                AddCheck("Passive_AngelMoveSpeed15Pct", angelSpeed > humanSpeed * 1.10f,
                    string.Format("> {0:0.00}", humanSpeed * 1.10f), string.Format("{0:0.00}", angelSpeed));

                float angelPsy = angel.GetStatValue(StatDefOf.PsychicSensitivity);
                AddCheck("Passive_AngelPsychicSensitivity", angelPsy >= 1.30f, ">= 1.30", string.Format("{0:0.00}", angelPsy));

                bool hasGraceHediff = HasHediff(angel, "TMM_Hediff_CelestialGrace");
                AddCheck("Passive_AngelBleedHediff", hasGraceHediff, "present", hasGraceHediff.ToString());
            }

            if (demon != null)
            {
                float trade = demon.GetStatValue(StatDefOf.TradePriceImprovement);
                AddCheck("Passive_DemonTradePriceBonus", trade >= 0.20f, ">= 0.20", string.Format("{0:0.00}", trade));
            }

            if (mutant != null)
            {
                float carry = mutant.GetStatValue(StatDefOf.CarryingCapacity);
                float meleeDmg = mutant.GetStatValue(StatDefOf.MeleeDamageFactor);
                float aimDelay = mutant.GetStatValue(StatDefOf.AimingDelayFactor);
                float fertility = mutant.GetStatValue(StatDefOf.Fertility); // CHECK API

                AddCheck("Passive_MutantCarryCapacity", carry >= 200f, ">= 200", string.Format("{0:0.0}", carry));
                AddCheck("Passive_MutantMeleeDamage", meleeDmg >= 1.25f, ">= 1.25", string.Format("{0:0.00}", meleeDmg));
                AddCheck("Passive_MutantAimDelay", aimDelay <= 0.85f, "<= 0.85", string.Format("{0:0.00}", aimDelay));
                AddCheck("Passive_MutantSterile", fertility <= 0.01f, "0", string.Format("{0:0.00}", fertility));
                AddCheck("Passive_MutantBulkyHungerHediff", HasHediff(mutant, "TMM_Hediff_BulkyPhysiology"), "present",
                    HasHediff(mutant, "TMM_Hediff_BulkyPhysiology").ToString());

                // Pollution triggers Irradiated Metabolism
                map.pollutionGrid.SetPolluted(mutant.Position, true); // CHECK API
                Gene_MutantRadRegeneration radGene = mutant.genes.GetFirstGeneOfType<Gene_MutantRadRegeneration>();
                if (radGene != null) radGene.RefreshRadiationBuff();
                AddCheck("Passive_MutantRadRegenOnPollution", HasHediff(mutant, "TMM_Hediff_IrradiatedMetabolism"), "present",
                    HasHediff(mutant, "TMM_Hediff_IrradiatedMetabolism").ToString());
                map.pollutionGrid.SetPolluted(mutant.Position, false); // CHECK API
            }

            if (ghoul != null)
            {
                AddTestCut(ghoul, 12f);
                float dmgBefore = SumInjurySeverity(ghoul);
                map.pollutionGrid.SetPolluted(ghoul.Position, true); // CHECK API
                Gene_GhoulRadHealing radHealGene = ghoul.genes.GetFirstGeneOfType<Gene_GhoulRadHealing>();
                float healed = radHealGene != null ? radHealGene.ApplyRadiationHeal() : 0f;
                float dmgAfter = SumInjurySeverity(ghoul);
                AddCheck("Passive_GhoulRadHealing", healed > 0f && dmgAfter < dmgBefore, "healed > 0",
                    string.Format("before={0:0.0}, after={1:0.0}, healed={2:0.0}", dmgBefore, dmgAfter, healed));
                map.pollutionGrid.SetPolluted(ghoul.Position, false); // CHECK API
            }

            // ---------------- 3. ABILITIES (REAL Ability.Activate) ----------------

            // 3.1 Angel Healing Touch
            if (angel != null)
            {
                Pawn patient = SpawnXenotypePawn(null, Faction.OfPlayer, angel.Position + new IntVec3(1, 0, 0));
                AddTestCut(patient, 4f);
                AddTestCut(patient, 4f);
                AddTestCut(patient, 4f);
                Hediff inf = patient.health.AddHediff(HediffDefOf.WoundInfection);
                inf.Severity = 0.60f;

                float injBefore = SumInjurySeverity(patient);
                Ability ab = FindAbility(angel, "TMM_Ability_HealingTouch");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(patient), LocalTargetInfo.Invalid);
                float injAfter = SumInjurySeverity(patient);
                AddCheck("Ability_HealingTouch", activated && injAfter < injBefore && inf.Severity <= 0.35f,
                    "wounds healed & infection halved",
                    string.Format("act={0}, inj={1:0.0}->{2:0.0}, inf={3:0.00}", activated, injBefore, injAfter, inf.Severity));
            }

            // 3.2 Angel True Visage
            if (angel != null)
            {
                Pawn onlooker = SpawnXenotypePawn(null, Faction.OfPlayer, angel.Position + new IntVec3(0, 0, 3));
                Ability ab = FindAbility(angel, "TMM_Ability_TrueVisage");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(angel), new LocalTargetInfo(angel));
                bool blinded = HasHediff(onlooker, "TMM_Hediff_RetinalBurn");
                AddCheck("Ability_TrueVisage", activated && blinded, "RetinalBurn applied",
                    string.Format("act={0}, hasBurn={1}", activated, blinded));
            }

            // 3.3 Demon Black Smoke Shift
            if (demon != null)
            {
                IntVec3 startPos = demon.Position;
                IntVec3 destPos = FindStandableCellOffset(startPos, 6, 9);
                Ability ab = FindAbility(demon, "TMM_Ability_BlackSmokeShift");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(destPos), new LocalTargetInfo(destPos));
                bool moved = demon.Position == destPos && demon.Position != startPos;
                AddCheck("Ability_BlackSmokeShift", activated && moved, destPos.ToString(), demon.Position.ToString());
            }

            // 3.4 Demon Infernal Whisper
            if (demon != null)
            {
                Pawn target = SpawnXenotypePawn(null, Faction.OfPlayer, demon.Position + new IntVec3(3, 0, 0));
                Ability ab = FindAbility(demon, "TMM_Ability_InfernalWhisper");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(target), LocalTargetInfo.Invalid);
                bool hasDebuff = HasHediff(target, "TMM_Hediff_InfernalWhisper");
                AddCheck("Ability_InfernalWhisper", activated && hasDebuff, "InfernalWhisper applied",
                    string.Format("act={0}, hasHediff={1}", activated, hasDebuff));
            }

            // 3.5 Super Mutant War Roar
            if (mutant != null)
            {
                Faction hostileFac = FindAnyHostileFaction();
                Pawn enemy = SpawnXenotypePawn(null, hostileFac, mutant.Position + new IntVec3(2, 0, 0));
                Ability ab = FindAbility(mutant, "TMM_Ability_MutantWarRoar");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(mutant), new LocalTargetInfo(mutant));
                bool selfBuff = HasHediff(mutant, "TMM_Hediff_WarRoarAdrenaline");
                bool enemyFear = enemy != null && HasHediff(enemy, "TMM_Hediff_WarRoarFear");
                AddCheck("Ability_MutantWarRoar", activated && selfBuff && enemyFear, "selfBuff=true, enemyFear=true",
                    string.Format("act={0}, buff={1}, fear={2}", activated, selfBuff, enemyFear));
            }

            // 3.6 Ghoul Glowing Pulse
            if (ghoul != null)
            {
                Pawn allyGhoul = SpawnXenotypePawn("TMM_Ghoul", Faction.OfPlayer, ghoul.Position + new IntVec3(2, 0, 0));
                Pawn bystander = SpawnXenotypePawn(null, Faction.OfPlayer, ghoul.Position + new IntVec3(-2, 0, 0));
                AddTestCut(allyGhoul, 12f);
                float allyInjBefore = SumInjurySeverity(allyGhoul);

                Ability ab = FindAbility(ghoul, "TMM_Ability_GlowingPulse");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(ghoul), new LocalTargetInfo(ghoul));
                float allyInjAfter = SumInjurySeverity(allyGhoul);
                Hediff tox = bystander.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
                bool gotTox = tox != null && tox.Severity > 0.05f;
                AddCheck("Ability_GlowingPulse", activated && allyInjAfter < allyInjBefore && gotTox,
                    "ally healed & human got ToxicBuildup",
                    string.Format("act={0}, allyInj={1:0.0}->{2:0.0}, tox={3:0.00}", activated, allyInjBefore, allyInjAfter, tox != null ? tox.Severity : 0f));
            }

            // 3.7 Leviathan Predatory Mimicry (apply -> verify copy -> restore)
            if (leviathan != null)
            {
                Pawn donor = SpawnXenotypePawn(null, Faction.OfPlayer, leviathan.Position + new IntVec3(1, 0, 0));
                HairDef origHair = leviathan.story.hairDef;
                HairDef donorHair = PickDifferentHair(origHair);
                donor.story.hairDef = donorHair;
                donor.story.skinColorOverride = new Color(0.85f, 0.72f, 0.62f);

                Ability ab = FindAbility(leviathan, "TMM_Ability_PredatoryMimicry");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(donor), LocalTargetInfo.Invalid);
                bool hasMimic = HasHediff(leviathan, "TMM_Hediff_PredatoryMimicry");
                bool copiedHair = leviathan.story.hairDef == donorHair;

                SupernaturalGeneUtil.StripMimicry(leviathan);
                bool restoredHair = leviathan.story.hairDef == origHair;

                AddCheck("Ability_PredatoryMimicry", activated && hasMimic && copiedHair && restoredHair,
                    "mimic applied, hair copied & restored",
                    string.Format("act={0}, hediff={1}, copied={2}, restored={3}", activated, hasMimic, copiedHair, restoredHair));
            }

            // 3.8 Leviathan Devour Maw
            if (leviathan != null)
            {
                Pawn prey = SpawnXenotypePawn(null, Faction.OfPlayer, leviathan.Position + new IntVec3(1, 0, 0));
                if (leviathan.needs != null && leviathan.needs.food != null)
                {
                    leviathan.needs.food.CurLevelPercentage = 0.25f;
                }
                AddTestCut(leviathan, 12f);
                float foodBefore = leviathan.needs.food.CurLevelPercentage;
                float selfInjBefore = SumInjurySeverity(leviathan);

                Ability ab = FindAbility(leviathan, "TMM_Ability_DevourMaw");
                bool activated = ActivateAbility(ab, new LocalTargetInfo(prey), LocalTargetInfo.Invalid);
                float foodAfter = leviathan.needs.food.CurLevelPercentage;
                float selfInjAfter = SumInjurySeverity(leviathan);
                bool preyDamaged = prey.Dead || SumInjurySeverity(prey) > 0f;

                AddCheck("Ability_DevourMaw", activated && preyDamaged && foodAfter > foodBefore + 0.30f && selfInjAfter < selfInjBefore,
                    "prey hurt, food +40%, self healed",
                    string.Format("act={0}, preyHurt={1}, food={2:0.00}->{3:0.00}, selfInj={4:0.0}->{5:0.0}",
                        activated, preyDamaged, foodBefore, foodAfter, selfInjBefore, selfInjAfter));
            }

            // ---------------- 4. WEAKNESSES (DAMAGE COMPARISON VS BASELINE HUMAN) ----------------
            CompareWeaknessDamage("Weakness_AngelEnochian", "TMM_Enochian", angel, 2.0f);
            CompareWeaknessDamage("Weakness_DemonHolyWater", "TMM_HolyWater", demon, 2.5f);
            CompareWeaknessDamage("Weakness_DemonRockSalt", "TMM_RockSalt", demon, 2.5f);

            // Leviathan Borax Burn: damage multiplier + TMM_Hediff_BoraxBurn applied
            CompareWeaknessDamage("Weakness_LeviathanBoraxDamage", "TMM_BoraxBurn", leviathan, 3.0f);
            bool boraxHediffApplied = leviathan != null && HasHediff(leviathan, "TMM_Hediff_BoraxBurn");
            AddCheck("Weakness_LeviathanBoraxDebuff", boraxHediffApplied, "TMM_Hediff_BoraxBurn applied",
                boraxHediffApplied.ToString());
        }

        private void VerifyRaceGenesAndMetabolism(string label, Pawn pawn, string[] requiredGenes)
        {
            if (pawn == null || pawn.genes == null)
            {
                AddCheck("Race_" + label + "_Generated", false, "valid pawn", "null");
                return;
            }

            bool allActive = true;
            List<string> missing = new List<string>();
            for (int i = 0; i < requiredGenes.Length; i++)
            {
                if (!SupernaturalGeneUtil.HasActiveGene(pawn, requiredGenes[i]))
                {
                    allActive = false;
                    missing.Add(requiredGenes[i]);
                }
            }

            AddCheck("Race_" + label + "_GenesActive", allActive, "all active",
                allActive ? "all active" : "missing: " + string.Join(",", missing.ToArray()));

            int metSum = 0;
            List<Gene> genes = pawn.genes.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (genes[i] != null && genes[i].Active && genes[i].def != null)
                {
                    metSum += genes[i].def.biostatMet;
                }
            }

            AddCheck("Race_" + label + "_MetabolismZero", metSum == 0, "0", metSum.ToString());
        }

        private void CompareWeaknessDamage(string checkName, string damageDefName, Pawn targetPawn, float minRatio)
        {
            DamageDef dmgDef = DefDatabase<DamageDef>.GetNamedSilentFail(damageDefName);
            if (dmgDef == null || targetPawn == null)
            {
                AddCheck(checkName, false, "valid DamageDef & pawn", "missing");
                return;
            }

            Pawn baseline = SpawnXenotypePawn(null, Faction.OfPlayer, map.Center);
            float humanDmg = ApplyControlledTorsoDamage(baseline, dmgDef, 6f);
            float targetDmg = ApplyControlledTorsoDamage(targetPawn, dmgDef, 6f);
            float ratio = humanDmg > 0.01f ? (targetDmg / humanDmg) : 0f;

            AddCheck(checkName, ratio >= minRatio,
                string.Format("ratio >= {0:0.0}x", minRatio),
                string.Format("target={0:0.0}, human={1:0.0}, ratio={2:0.00}x", targetDmg, humanDmg, ratio));
        }

        private static float ApplyControlledTorsoDamage(Pawn pawn, DamageDef dmgDef, float amount)
        {
            if (pawn == null || pawn.Dead) return 0f;
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
            DamageInfo dinfo = new DamageInfo(dmgDef, amount, 2.0f, -1f, null, torso);
            DamageWorker.DamageResult res = pawn.TakeDamage(dinfo); // CHECK API
            return res != null ? res.totalDamageDealt : 0f;
        }

        private Pawn SpawnXenotypePawn(string xenotypeDefName, Faction fac, IntVec3 nearCell)
        {
            XenotypeDef xeno = !xenotypeDefName.NullOrEmpty()
                ? DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeDefName)
                : XenotypeDefOf.Baseliner;

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

            // Remove apparel so damage & stat tests measure pure xenotype physiology
            if (p.apparel != null) p.apparel.DestroyAll();

            IntVec3 cell;
            if (!CellFinder.TryFindRandomCellNear(nearCell, map, 4, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map);
            }, out cell))
            {
                cell = map.Center;
            }
            GenSpawn.Spawn(p, cell, map, WipeMode.Vanish);
            return p;
        }

        private static Ability FindAbility(Pawn pawn, string abilityDefName)
        {
            if (pawn == null || pawn.abilities == null) return null;
            AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (def == null) return null;
            return pawn.abilities.GetAbility(def);
        }

        private static bool ActivateAbility(Ability ability, LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (ability == null) return false;
            return ability.Activate(target, dest); // CHECK API
        }

        private static void AddTestCut(Pawn pawn, float severity)
        {
            if (pawn == null || pawn.Dead) return;
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, severity, 2.0f);
            pawn.TakeDamage(dinfo);
        }

        private static float SumInjurySeverity(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return 0f;
            float sum = 0f;
            List<Hediff> list = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < list.Count; i++)
            {
                Hediff_Injury inj = list[i] as Hediff_Injury;
                if (inj != null && !inj.IsPermanent()) sum += inj.Severity;
            }
            return sum;
        }

        private static bool HasHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            return def != null && pawn.health.hediffSet.GetFirstHediffOfDef(def) != null;
        }

        private static HairDef PickDifferentHair(HairDef current)
        {
            List<HairDef> all = DefDatabase<HairDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i] != current) return all[i];
            }
            return current;
        }

        private static Faction FindAnyHostileFaction()
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

        private IntVec3 FindClearAreaCenter()
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(map.Center, map, 12, delegate(IntVec3 c)
            {
                return c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            return map.Center;
        }

        private IntVec3 FindStandableCellOffset(IntVec3 origin, int minDist, int maxDist)
        {
            IntVec3 found;
            if (CellFinder.TryFindRandomCellNear(origin, map, maxDist, delegate(IntVec3 c)
            {
                float d = c.DistanceTo(origin);
                return d >= minDist && d <= maxDist && c.InBounds(map) && c.Standable(map) && !c.Fogged(map);
            }, out found))
            {
                return found;
            }
            return origin;
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