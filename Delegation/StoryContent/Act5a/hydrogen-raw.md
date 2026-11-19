`Defs/StoryDefs/Story_Act5_Part1.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- COALITION 1: FREE WASTELAND (Minutemen & Hunters + Railroad)          -->
  <!-- ===================================================================== -->

  <!-- FW QUEST 1: SPEC-OP — SHIELD OF SALT AND STEEL -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_FW_1_ShieldOfSaltAndSteel</defName>
    <label>shield of salt and steel</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_bobby_singer</giver>
    <fallbackGiver>TMM_Hero_tinker_tom</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=free_wasteland</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Raising the continental ward grid</label>
        <text>To seal the Rift without burning out the Zero Synth's soul, Bobby Singer and Tinker Tom must link {COLONY}'s Spark Resonator to a pre-war relay tower on the northern ridge—inscribing a giant Enochian containment sigil across its cooling basins. An Enclave airborne vanguard is already inbound to demolish the tower.</text>
        <choices>
          <li>
            <outcome>defector_boost</outcome>
            <label>[Defector Intel] Use Mick Davies's or Alan Binet's frequency tables to pre-tune the pylons</label>
            <resultText>Armed with stolen Consortium and Institute harmonic tables, Tinker Tom cuts the beacon calibration time in half and supplies extra medical kits for the strike team.</resultText>
            <anyOf>
              <li>defector.TMM_Hero_mick_davies=true</li>
              <li>defector.TMM_Hero_alan_binet=true</li>
            </anyOf>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>3</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_beaconTuned</flag><value>fast</value></li>
            </effects>
          </li>
          <li>
            <outcome>standard_deployment</outcome>
            <label>Deploy the strike team with rock salt, silver wards, and heavy weapons</label>
            <resultText>Your squad packs the Enochian relay coils and marches toward the northern ridge to hold the tower against all comers.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Steel</thing><count>60</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_beaconTuned</flag><value>standard</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_EnclaveFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>8</holdHours>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>50</count></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Northern Enochian beacon online</label>
                <text>Through eight grueling hours of plasma fire, your defenders held the relay tower until Tinker Tom locked the Enochian containment field into place!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Backup relay activated</label>
                <text>Before your squad reached the ridge, Minutemen artillery distracted the Enclave patrol long enough for Tinker Tom to activate a lower-power backup beacon.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Costly withdrawal from the ridge</label>
                <text>Forced to retreat under heavy vertibird bombardment, your engineers still managed to salvage the primary harmonic coil for manual routing at {COLONY}.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>First anchor point locked</label>
        <text>A pale blue arc shimmers faintly across the northern horizon—the Shield of Salt and Steel is active. Back at base, Dean Winchester bursts into the comms room with urgent news: intercepted Consortium traffic has pinpointed the secret black-site prison holding your missing mentor.</text>
        <choices>
          <li>
            <outcome>prepare_rescue</outcome>
            <label>Recall the strike team and prepare to rescue your mentor</label>
            <resultText>The Zero Synth's eyes shine with fierce hope: "They saved me from those white rooms—now it's our turn to bring them home."</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>10</amount></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_q1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- FW QUEST 2: SPEC-OP — LIBERATION OF THE MENTOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_FW_2_MentorPrisonBreak</defName>
    <label>liberation of the mentor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_desdemona</giver>
    <fallbackGiver>TMM_Hero_dean_winchester</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=free_wasteland</li>
      <li>done:TMM_Story_A5_FW_1_ShieldOfSaltAndSteel</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Decoding the black-site coordinates</label>
        <text>Desdemona's runners cross-reference the Sector 3 audio trace with captured Institute relay logs, pinpointing Sub-Level Detention Vault 00—an off-grid Consortium prison guarded by Courser killers and stasis traps.</text>
        <days>0.8</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
        <objective>Rescue</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>20</amount></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentSpacer</thing><count>2</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The mentor is free!</label>
                <text>Shattering the stasis cell locks, your strike team pulled your exhausted mentor out of the Consortium's black-site alive—carrying the final physical master-key for the Zero Anchor!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>An empty cell and a hidden cipher</label>
                <text>Consortium sweepers evacuated the prisoner hours before your arrival, but your mentor scratched the final Anchor override code onto the inside of the stasis glass.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Sacrifice in the dark</label>
                <text>Pinned down by Courser reinforcements, your mentor triggered the vault's emergency blast doors from inside to save your squad—tossing the final override holotape through the gap before the steel slammed shut.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>The final piece of the puzzle</label>
        <text>Whether guided by your rescued mentor's living memory or the final override holotape recovered from the vault, the Spark Resonator is now ready for its continental sealing pulse. Suddenly, every long-range alarm in {COLONY} begins wailing at once!</text>
        <choices>
          <li>
            <outcome>brace_for_siege</outcome>
            <label>Man the walls—allied reinforcements are rushing to {COLONY} for the final siege!</label>
            <resultText>Dust plumes rise on every horizon as enemies converge on the Zero Anchor—and loyal allies march to stand beside you.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>10</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_q2Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- FW QUEST 3: SIEGE OF THE ZERO ANCHOR (MULTI-WAVE DEFENSE) -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_FW_3_SiegeOfZeroAnchor</defName>
    <label>siege of the zero anchor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_dean_winchester</giver>
    <fallbackGiver>TMM_Hero_preston_garvey</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=free_wasteland</li>
      <li>done:TMM_Story_A5_FW_2_MentorPrisonBreak</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>The storm breaks over {COLONY}</label>
        <text>Beneath the blood-red sun, the Spark Resonator begins charging its planetary containment pulse. Institute teleportation bolts flash outside your perimeter—the first wave of the Siege of the Zero Anchor is upon you!</text>
        <days>0.6</days>
      </li>
      <!-- WAVE 1: 1.2x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_InstituteFaction</faction>
        <pointsFactor>1.2</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>40</count></li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- INTER-WAVE BREATHER + SWAN CHECK -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Resonator at 40% — allies rally to the walls</label>
        <text>Smoking synth wreckage litters the outer field while John Hancock's ghoul rangers and Fawkes's mutant heavy gunners drop medical crates and ammunition inside your courtyard! Beyond the ridge, sulfurous clouds gather—and seismic tremors shake the earth.</text>
        <choices>
          <li>
            <outcome>swan_already_dead</outcome>
            <label>[Swan Defeated] Hold the line as Hell's legions advance through the ash</label>
            <resultText>With the King of Behemoths already slain, the wild mutant tribes stay clear of the battle—leaving your guns focused squarely on the infernal vanguard!</resultText>
            <nextStage>4</nextStage>
            <requires>
              <li>story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>6</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Steel</thing><count>80</count></li>
            </effects>
          </li>
          <li>
            <outcome>swan_intervenes</outcome>
            <label>[Wild Horde Incoming] Brace for impact—Swan's feral super mutants are charging into the crossfire!</label>
            <resultText>Drawn by the Resonator's deafening hum, Swan's berserk super mutant horde smashes into the valley ahead of Hell's vanguard!</resultText>
            <nextStage>3</nextStage>
            <requires>
              <li>!story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>6</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Steel</thing><count>80</count></li>
            </effects>
          </li>
        </choices>
      </li>
      <!-- OPTIONAL WAVE 2A: WILD MUTANTS (IF SWAN NOT DEFEATED) 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_WildMutantsFaction</faction>
        <pointsFactor>1.6</pointsFactor>
        <delayHours>2</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>4</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.swanDefeated</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Feral behemoth horde shattered</label>
                <text>Your defenders cut down the charging super mutants at the barricades before turning their barrels toward the approaching clouds of black smoke!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- WAVE 2B: HELL LEGIONS 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HellFaction</faction>
        <pointsFactor>1.6</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>5</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>5</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- INTER-WAVE BREATHER BEFORE FINAL WAVE -->
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Resonator at 85% — blinding light pierces the crimson clouds</label>
        <text>Your salt lines and Enochian wards hold firm as the demonic assault collapses into ash! High above, blinding golden shafts split the blood-red sky: Archangel Michael is committing Heaven's elite host in one final, desperate strike to seize the Zero Synth before the seal locks forever!</text>
        <days>0.4</days>
      </li>
      <!-- WAVE 3: HEAVEN'S HOST 2.0x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HeavenFaction</faction>
        <pointsFactor>2.0</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Zero Anchor holds!</label>
                <text>Beneath a sky ablaze with blue harmonic lightning, your garrison shattered Heaven's final assault! The planetary seal is locked—only Dick Roman's Rift Spire stands between Earth and freedom!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Pulse discharged through the smoke</label>
                <text>Despite battered walls and scorched defenses, the Spark Resonator discharged its harmonic wave on schedule—now strike Dick Roman's spire before the Consortium can override the barrier!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_q3Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- FW QUEST 4: CITADEL ASSAULT — GATES OF THE RIFT -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_FW_4_CitadelAssault</defName>
    <label>the final impulse: gates of the rift</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_preston_garvey</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=free_wasteland</li>
      <li>done:TMM_Story_A5_FW_3_SiegeOfZeroAnchor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Assault on the Consortium Rift Spire</label>
        <text>The planetary seal is locked, severing Heaven and Hell from Earth—but Leviathan CEO Dick Roman is attempting to overload the central Rift Spire to tear the barrier back open and devour the continent. Dean Winchester checks the Colt's cylinder while Sam primes the harmonic detonator.</text>
        <choices>
          <li>
            <outcome>borax_vanguard</outcome>
            <label>[Borax Formula Unlocked] Equip the assault squad with concentrated Borax sprayers and grenades</label>
            <resultText>Your quartermasters issue pressurized sodium borate canisters to every trooper. Dick Roman's supernatural invulnerability will melt away on contact!</resultText>
            <requires>
              <li>story.boraxFormula=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentSpacer</thing><count>2</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_boraxStrike</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>standard_assault</outcome>
            <label>Launch the assault with heavy weapons, the Colt, and Enochian-etched rounds</label>
            <resultText>Vertibirds and Minutemen scouts drop your strike team right onto the threshold of the Rift Spire. Remember: hit Roman with Borax to strip his armor!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_fw_boraxStrike</flag><value>false</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_dick_roman</hero>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.dickRomanDead</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>20</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Dick Roman dissolved into ash!</label>
                <text>Hissing in white Borax foam, Dick Roman's human disguise split open in a shriek of black ichor before collapsing into lifeless ash! The Rift Spire falls silent forever.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Spire collapsed into the Rift</label>
                <text>Before Roman could trigger the overload, the Resonator's harmonic shockwave imploded the Rift Spire—burying the Leviathan board beneath thousands of tons of concrete.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Allied artillery finishes the spire</label>
                <text>Though your vanguard took heavy casualties storming the lobby, Minutemen artillery batteries leveled the Spire's relay crown before the overload could fire.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Dawn of the Free Wasteland</label>
        <text>The crimson clouds part above the ruins. For the first time in two centuries, clear blue sunlight breaks over a world free from celestial tyrants, infernal deals, and Consortium harvests—and the Zero Synth stands beside you, their soul whole and unharmed.</text>
        <choices>
          <li>
            <outcome>proclaim_freedom</outcome>
            <label>Sound the all-clear across every frequency—the wasteland belongs to the living</label>
            <resultText>Cheers echo from Goodneighbor to the Castle. The war for the Spark is won.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>40</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.finaleDone</flag><value>free_wasteland</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>


  <!-- ===================================================================== -->
  <!-- COALITION 2: STERILE EMPIRE (Enclave + Institute)                     -->
  <!-- ===================================================================== -->

  <!-- SE QUEST 1: SPEC-OP — TOXIC HORIZON -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SE_1_ToxicHorizon</defName>
    <label>toxic horizon</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_colonel_autumn</giver>
    <fallbackGiver>TMM_Hero_captain_richards</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=sterile_empire</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Synchronizing the atmospheric dispersal grid</label>
        <text>Dr. Charles Curling and Father have linked the Spark Resonator's output to a pre-war atmospheric weather station. Once charged, its aerosol emitters will purge radiation, FEV mutations, and occult anomalies across the continent—if you can hold the station against Brotherhood vertibirds.</text>
        <choices>
          <li>
            <outcome>institute_synth_shield</outcome>
            <label>Deploy Institute synth shock-troopers to reinforce the perimeter</label>
            <resultText>Relay grenades flare across the station perimeter as synthetic troopers lock down the firing lanes.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>50</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_gridPrep</flag><value>synths</value></li>
            </effects>
          </li>
          <li>
            <outcome>enclave_plasma_batteries</outcome>
            <label>Overcharge the station's pre-war plasma defense capacitors</label>
            <resultText>Heavy energy conduits hum to life along the weather station roof, ready to shred incoming power armor.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>15</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_gridPrep</flag><value>plasma</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_BrotherhoodFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>8</holdHours>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentSpacer</thing><count>2</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Atmospheric dispersal grid charged</label>
                <text>Your defenders held the weather station through eight hours of Brotherhood paladin assaults, bringing the Sterilization Matrix to full operational readiness!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Orbital relay rerouted</label>
                <text>While Brotherhood squads bogged down in decoy bunkers, Enclave engineers routed the sterilization frequency through an orbital satellite backup.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Emergency telemetry salvaged</label>
                <text>Driven off the roof by Brotherhood heavy fire, your technicians still extracted the primary dispersal transponder before teleporting back to {COLONY}.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Target: Resistance Command</label>
        <text>With the dispersal grid armed, Colonel Autumn orders an immediate decapitation strike against the surface mutant-human resistance coalition led by ghoul mayor John Hancock.</text>
        <choices>
          <li>
            <outcome>acknowledge_strike</outcome>
            <label>Prepare the Courser and Hellfire strike team</label>
            <resultText>Teleportation coordinates lock onto Goodneighbor's fortified command post.</resultText>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_q1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- SE QUEST 2: SPEC-OP — PURGE OF GOODNEIGHBOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SE_2_PurgeOfGoodneighbor</defName>
    <label>purge of goodneighbor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_father_shaun</giver>
    <fallbackGiver>TMM_Hero_colonel_autumn</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=sterile_empire</li>
      <li>done:TMM_Story_A5_SE_1_ToxicHorizon</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_GhoulsFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_john_hancock</hero>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>700</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Resistance command shattered</label>
                <text>John Hancock fell defending the barricades of Goodneighbor in his red frock coat—breaking the backbone of the surface resistance!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Goodneighbor evacuated</label>
                <text>Hancock abandoned the fortified square before your main force arrived, leaving behind heavy supply caches as his fighters scattered into the ruins.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Bloody street battle</label>
                <text>Though Hancock's bodyguards forced your strike team to withdraw, an Enclave orbital strike leveled the block minutes later.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Aftermath in the ashes</label>
        <text>Surrounded by surrendered non-combatants and captured Railroad couriers, your field commander awaits orders on how to treat the prisoners—and reports that your missing mentor's stasis pod has been located inside a Consortium vault slated for sterilization.</text>
        <choices>
          <li>
            <outcome>spare_civilians</outcome>
            <label>Detain the survivors in sterile quarantine camps rather than executing them</label>
            <resultText>Your restraint prevents an immediate uprising in {COLONY} and keeps the Zero Synth from turning completely against you.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_mercy</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>total_liquidation</outcome>
            <label>Enforce Colonel Autumn's zero-tolerance directive without exception</label>
            <resultText>Plasma fire silences the square. The Zero Synth watches the telemetry feed back at base in cold, horrified silence.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>-20</amount></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>45</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_mercy</flag><value>false</value></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_q2Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- SE QUEST 3: SIEGE OF THE ZERO ANCHOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SE_3_SiegeOfZeroAnchor</defName>
    <label>siege of the zero anchor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_colonel_autumn</giver>
    <fallbackGiver>TMM_Hero_father_shaun</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=sterile_empire</li>
      <li>done:TMM_Story_A5_SE_2_PurgeOfGoodneighbor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>The wasteland strikes back</label>
        <text>Realizing the Sterilization Matrix is minutes from full charge, every remaining resistance force on the surface launches a desperate all-out assault on {COLONY}!</text>
        <days>0.6</days>
      </li>
      <!-- WAVE 1: 1.2x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_MinutemenHuntersFaction</faction>
        <pointsFactor>1.2</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- BREATHER + SWAN CHECK -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Sterilization Matrix at 45% — heavy armor inbound</label>
        <text>Institute synth reinforcements teleport into your courtyard with fresh plasma cells and trauma kits just as the roar of Brotherhood vertibird turbines fills the crimson sky.</text>
        <choices>
          <li>
            <outcome>swan_already_dead</outcome>
            <label>[Swan Defeated] Turn the plasma batteries skyward against the Brotherhood assault</label>
            <resultText>With Swan's behemoths already wiped out, your perimeter turrets lock cleanly onto the descending Brotherhood paladins!</resultText>
            <nextStage>4</nextStage>
            <requires>
              <li>story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>60</count></li>
            </effects>
          </li>
          <li>
            <outcome>swan_intervenes</outcome>
            <label>[Wild Horde Incoming] Divert fire—Swan's feral super mutants are charging the barricades!</label>
            <resultText>Driven mad by the Resonator's ultrasonic pulse, Swan's super mutant horde smashes into your outer perimeter ahead of the Brotherhood!</resultText>
            <nextStage>3</nextStage>
            <requires>
              <li>!story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>60</count></li>
            </effects>
          </li>
        </choices>
      </li>
      <!-- OPTIONAL WAVE 2A: WILD MUTANTS 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_WildMutantsFaction</faction>
        <pointsFactor>1.6</pointsFactor>
        <delayHours>2</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>4</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.swanDefeated</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- WAVE 2B: BROTHERHOOD OF STEEL 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_BrotherhoodFaction</faction>
        <pointsFactor>1.6</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>5</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>5</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- BREATHER BEFORE WAVE 3 -->
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Matrix at 90% — black ichor rises from the storm</label>
        <text>Broken T-60 power armor smokes in the courtyard! Before your engineers can seal the vault doors, Dick Roman throws his entire Leviathan pack at {COLONY} to stop the sterilization wave from wiping out his harvest!</text>
        <days>0.4</days>
      </li>
      <!-- WAVE 3: MONSTER CLANS / LEVIATHANS 2.0x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>2.0</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Sterile Empire holds!</label>
                <text>Your plasma cohorts and Borax sprayers annihilated the Leviathan assault wave! Now strike Dick Roman's headquarters to complete Protocol Rebirth!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Perimeter battered but unbroken</label>
                <text>The Leviathan swarm withdrew into the ash as the Resonator reached full power—march on Roman's tower immediately!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_q3Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- SE QUEST 4: CITADEL ASSAULT — PROTOCOL REBIRTH -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SE_4_ProtocolRebirth</defName>
    <label>protocol rebirth: citadel of the board</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_colonel_autumn</giver>
    <fallbackGiver>TMM_Hero_father_shaun</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=sterile_empire</li>
      <li>done:TMM_Story_A5_SE_3_SiegeOfZeroAnchor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Eliminating the Consortium Board</label>
        <text>Only one obstacle prevents the Sterilization Matrix from cleansing the continent: Leviathan CEO Dick Roman has hijacked the orbital weather uplink inside SucroCorp Tower—and Enclave intelligence confirms your missing mentor is locked in a stasis pod on the same floor.</text>
        <choices>
          <li>
            <outcome>borax_deployment</outcome>
            <label>[Borax Formula Unlocked] Arm the Hellfire strike team with concentrated Borax sprayers</label>
            <resultText>Chemical canisters lock onto your troops' armor—Dick Roman's invulnerability will strip away the moment the white mist hits him!</resultText>
            <requires>
              <li>story.boraxFormula=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_boraxStrike</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>plasma_assault</outcome>
            <label>Deploy via Institute teleporter with heavy plasma ordnance</label>
            <resultText>Blue lightning drops your squad directly onto the executive floor. Use Borax weapons to strip Roman's invulnerability!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>40</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_se_boraxStrike</flag><value>false</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_dick_roman</hero>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.dickRomanDead</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Dick Roman liquidated &amp; mentor recovered</label>
                <text>Dissolved in hissing Borax foam, Dick Roman collapsed in the boardroom—and your troops cracked open the stasis pod holding your missing mentor!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Orbital uplink purged</label>
                <text>An Enclave orbital plasma beam reduced the upper spire to molten glass, clearing the frequency for Protocol Rebirth.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Beacon destroyed in retreat</label>
                <text>Though your vanguard suffered heavy losses, demolition charges collapsed the tower's transmitter core before withdrawal.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>A clean, sterile world</label>
        <text>White vapor sweeps across the upper atmosphere, neutralizing radiation, FEV toxins, and dimensional anomalies alike. Under the twin banners of the Enclave and the Institute, a disciplined human empire rises from the ash—while the Zero Synth stands quietly under armed guard beside the Resonator.</text>
        <choices>
          <li>
            <outcome>proclaim_empire</outcome>
            <label>Transmit the Imperial Charter—humanity has reclaimed its birthright</label>
            <resultText>Vertibirds patrol clear skies above {COLONY}. The Sterile Empire stands supreme.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>50</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.finaleDone</flag><value>sterile_empire</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>


  <!-- ===================================================================== -->
  <!-- COALITION 3: IRON CRUSADE (Brotherhood of Steel + Outcasts)           -->
  <!-- ===================================================================== -->

  <!-- IC QUEST 1: SPEC-OP — HAMMER AND ANVIL -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_IC_1_HammerAndAnvil</defName>
    <label>hammer and anvil</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_OutcastsFaction</faction>
    <giver>TMM_Hero_henry_casdin</giver>
    <fallbackGiver>TMM_Hero_specialist_olin</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=iron_crusade</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Fueling the continental dampener</label>
        <text>To turn the Spark Resonator into a permanent planetary dampener that extinguishes all supernatural magic and rogue AI alike, Henry Casdin and Proctor Ingram need enriched pre-war uranium cores locked inside a fortified Enclave arsenal depot.</text>
        <choices>
          <li>
            <outcome>outcast_breach_charges</outcome>
            <label>Equip the strike team with Outcast thermal breach charges and uranium shielding</label>
            <resultText>Specialist Olin distributes heavy fusion cutters and lead-lined packs for the raid on the Enclave depot.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>45</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_breachPrep</flag><value>thermal</value></li>
            </effects>
          </li>
          <li>
            <outcome>brotherhood_air_drop</outcome>
            <label>Coordinate a fast vertibird insertion straight onto the arsenal roof</label>
            <resultText>Brotherhood pilots plot a low-altitude approach vector to bypass the depot's outer minefields.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentIndustrial</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_breachPrep</flag><value>airdrop</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_EnclaveFaction</enemyFaction>
        <objective>Retrieve</objective>
        <thing>Uranium</thing>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>30</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Enriched uranium cores secured</label>
                <text>Cracking the Enclave's blast doors in a storm of laser fire, your knights hauled out the enriched uranium rods intact!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>15</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Secondary stockpile recovered</label>
                <text>While Enclave transports evacuated the main vault, your scouts intercepted a rear-guard fuel convoy carrying half the required uranium.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Emergency reactor siphon</label>
                <text>Repulsed from the arsenal under heavy plasma fire, Proctor Ingram stripped uranium rods from a decommissioned Brotherhood airship auxiliary core instead.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Next target: the synth factory</label>
        <text>With the dampener's fuel cores secured, Arthur Maxson slams a tactical map onto the table: before the Iron Curtain can fall, the Institute's underground synth-manufacturing complex must be burned out at its root.</text>
        <choices>
          <li>
            <outcome>march_on_institute</outcome>
            <label>Assemble the heavy power-armor spearhead for the descent into the Institute</label>
            <resultText>Servo motors whine in unison as Brotherhood and Outcast paladins prepare for subterranean combat.</resultText>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_q1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- IC QUEST 2: SPEC-OP — DESCENT INTO THE INSTITUTE -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_IC_2_DescentIntoInstitute</defName>
    <label>descent into the institute</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_arthur_maxson</giver>
    <fallbackGiver>TMM_Hero_henry_casdin</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=iron_crusade</li>
      <li>done:TMM_Story_A5_IC_1_HammerAndAnvil</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_father_shaun</hero>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>65</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Institute's core shattered</label>
                <text>Punching straight through the white concourse in power armor, your crusade eliminated Father and blew the Institute's molecular relay to cinders!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Tunnel collapsed</label>
                <text>Brotherhood sappers collapsed the main subterranean shaft with fusion charges, sealing the Institute's survivors beneath a mountain of bedrock.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Retreat from the concourse</label>
                <text>Forced back to the surface by waves of Coursers, your demolition team still detonated a shaped charge that severed the Institute's surface teleporter grid.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>In the smoking ruins of Robotics</label>
        <text>Amid the shattered synth assembly vats, your scouts find a locked Consortium sub-cell—and a choice on how to handle the panicked non-combatant scientists trapped in the lower lab.</text>
        <choices>
          <li>
            <outcome>evacuate_scientists</outcome>
            <label>Sound the evacuation alarm and allow unarmed technicians to flee to the surface</label>
            <resultText>Even Arthur Maxson lowers his gatling laser as terrified technicians scramble up the emergency stairwell—and inside the sub-cell, your troops find your missing mentor alive!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>10</amount></li>
            </effects>
          </li>
          <li>
            <outcome>bury_the_lab</outcome>
            <label>Seal the blast doors and flood the sub-levels—no trace of the Institute survives</label>
            <resultText>Heavy steel doors slam shut forever over the buried complex, burying both the scientists and any captives inside.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>-15</amount></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_q2Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- IC QUEST 3: SIEGE OF THE ZERO ANCHOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_IC_3_SiegeOfZeroAnchor</defName>
    <label>siege of the zero anchor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_arthur_maxson</giver>
    <fallbackGiver>TMM_Hero_henry_casdin</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=iron_crusade</li>
      <li>done:TMM_Story_A5_IC_2_DescentIntoInstitute</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Charging the Iron Curtain dampener</label>
        <text>Proctor Ingram and Specialist Olin lock the enriched uranium rods into the Spark Resonator! As its anti-magic field begins expanding across the region, every supernatural predator on Earth lunges at {COLONY} to destroy the machine before it finishes!</text>
        <days>0.6</days>
      </li>
      <!-- WAVE 1: 1.2x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>1.2</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- BREATHER + SWAN CHECK -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Dampener field at 50% — celestial thunder overhead</label>
        <text>Brotherhood vertibirds drop fresh fusion cells and heavy ammo into your courtyard as Henry Casdin shouts over the intercom: "First wave down! Keep those Borax sprayers hot and watch the sky!"</text>
        <choices>
          <li>
            <outcome>swan_already_dead</outcome>
            <label>[Swan Defeated] Brace the anti-air turrets against Heaven's descending host</label>
            <resultText>With Swan already dead, the wild super mutant tribes stay out of the fight—allowing your knights to concentrate fire on the angelic vanguard!</resultText>
            <nextStage>4</nextStage>
            <requires>
              <li>story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>10</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
            </effects>
          </li>
          <li>
            <outcome>swan_intervenes</outcome>
            <label>[Wild Horde Incoming] Sound general quarters—Swan's behemoths are storming the gates!</label>
            <resultText>Enraged by the dampener's harmonic whine, Swan's feral super mutants smash into your outer walls ahead of the celestial assault!</resultText>
            <nextStage>3</nextStage>
            <requires>
              <li>!story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>10</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
            </effects>
          </li>
        </choices>
      </li>
      <!-- OPTIONAL WAVE 2A: WILD MUTANTS 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_WildMutantsFaction</faction>
        <pointsFactor>1.6</pointsFactor>
        <delayHours>2</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>4</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.swanDefeated</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- WAVE 2B: HEAVEN 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HeavenFaction</faction>
        <pointsFactor>1.6</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>5</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>5</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- BREATHER BEFORE WAVE 3 -->
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Dampener at 90% — sulfur clouds choke the horizon</label>
        <text>Angelic feathers burn to ash across your battlements! Now the earth cracks open beyond your perimeter as Hell's final legion charges through the crimson fog in one last attempt to shatter the Iron Curtain!</text>
        <days>0.4</days>
      </li>
      <!-- WAVE 3: HELL 2.0x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HellFaction</faction>
        <pointsFactor>2.0</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Iron Crusade stands undefeated!</label>
                <text>Steel, salt, and disciplined fire broke Hell's final charge at your gates! Now mount the vertibirds and storm the Celestial Spire to sever supernatural power from Earth forever!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Dampener field stabilized</label>
                <text>Through bloody attrition, your knights held the Resonator until the dampener field locked—strike the enemy's main anchorstone at once!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_q3Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- IC QUEST 4: CITADEL ASSAULT — THE IRON CURTAIN -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_IC_4_IronCurtain</defName>
    <label>the iron curtain: assault on the celestial spire</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_arthur_maxson</giver>
    <fallbackGiver>TMM_Hero_henry_casdin</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=iron_crusade</li>
      <li>done:TMM_Story_A5_IC_3_SiegeOfZeroAnchor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Storming the Heavenly Bastion</label>
        <text>One final stronghold keeps the supernatural Rift tethered to Earth: the Celestial Spire, defended by Archangel Michael and his seraphim guard. Arthur Maxson chambers a round in Final Judgment while Henry Casdin checks the vertibird gunships.</text>
        <choices>
          <li>
            <outcome>enochian_and_borax_munitions</outcome>
            <label>[Borax &amp; Tech Ready] Load Enochian-etched rounds and chemical canisters for the assault</label>
            <resultText>Olin and Ingram outfit every knight with runes and specialized ordnance—neither angelic armor nor Leviathan infiltrators can withstand your vanguard!</resultText>
            <requires>
              <li>story.boraxFormula=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>40</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_specialAmmo</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>steel_vanguard</outcome>
            <label>Lead a direct power-armor assault straight through the Spire's main doors</label>
            <resultText>Vertibird gunships strafe the courtyard as your knights drop into the heart of the Celestial Spire. Ad Victoriam!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_ic_specialAmmo</flag><value>false</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_HeavenFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_michael</hero>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.michaelDefeated</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Archangel Michael cast down!</label>
                <text>In a blinding clash of celestial fire and heavy laser beams, Archangel Michael fell—and the Spire's Rift anchorstone shattered into lifeless dust!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Spire leveled by Prydwen artillery</label>
                <text>Brotherhood airship batteries bombarded the Spire's upper tiers, collapsing the anchorstone and driving the surviving angels back behind the closed Rift.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Anchorstone overloaded in battle</label>
                <text>Though forced to pull back under searing celestial light, your sappers detonated the Spire's main relay conduit before extraction.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Age of the Iron Curtain</label>
        <text>A silent, invisible wave sweeps outward from the Zero Anchor—extinguishing occult magic, dimensional rifts, and rogue synthetic networks across the globe. Humanity stands safe behind walls of steel under the stern watch of the reunited Brotherhood and Outcasts.</text>
        <choices>
          <li>
            <outcome>ad_victoriam</outcome>
            <label>Proclaim Ad Victoriam—the Iron Crusade has secured Earth for humanity</label>
            <resultText>Vertibirds circle in triumph above {COLONY}. No monster or false god will ever threaten mankind again.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>45</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.finaleDone</flag><value>iron_crusade</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act5_Part1.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- ===================================================================== -->
  <!-- COALITION 1: FREE WASTELAND                                           -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_FW_1_ShieldOfSaltAndSteel -->
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.label>щит из соли и стали</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.label>Возведение континентального барьера</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.text>Чтобы запечатать Разлом, не сжигая душу Нулевого Синта, Бобби Сингер и Техник Том должны связать Резонатор Искр {COLONY} с довоенной ретрансляционной вышкой на северном хребте — начертив гигантскую енохианскую печать вокруг её градирен. Воздушный авангард Анклава уже летит, чтобы снести вышку.</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.text>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.0.label>[Данные перебежчиков] Использовать частотные таблицы Мика Дэвиса или Алана Бине для быстрой настройки</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.0.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.0.resultText>Вооружившись вынесенными таблицами Консорциума и Института, Техник Том вдвое сокращает время настройки маяка и передаёт ударной группе передовые аптечки.</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.0.resultText>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.1.label>Отправить ударный отряд с каменной солью, серебряными оберегами и тяжёлым оружием</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.1.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.1.resultText>Бойцы грузят енохианские катушки и выдвигаются на северный хребет, чтобы удержать вышку любой ценой.</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.0.choices.1.resultText>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.0.effects.2.label>Северный енохианский маяк запущен</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.0.effects.2.text>Восемь долгих часов под плазменным огнём ваши защитники удерживали вышку, пока Техник Том не замкнул енохианский контур сдерживания!</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.1.effects.0.label>Задействован резервный ретранслятор</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.1.outcomes.effects.0.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.1.effects.0.label>Задействован резервный ретранслятор</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.1.effects.0.text>Пока отряд добирался до хребта, артиллерия Минитменов отвлекла патруль Анклава, позволив Технику Тому запустить резервный маяк пониженной мощности.</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.2.effects.0.label>Тяжёлый отход с хребта</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.2.effects.0.text>Вынужденные отступить под бомбёжкой винтокрылов, инженеры всё же вынесли главную гармоническую катушку для ручной коммутации в {COLONY}.</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.label>Первая опорная точка замкнута</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.text>Над северным горизонтом вспыхивает бледно-голубая дуга — Щит из соли и стали активирован. На базе Дин Винчестер врывается в штабную со срочной новостью: перехваченные каналы Консорциума выдали координаты тайной тюрьмы, где держат вашего пропавшего наставника.</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.text>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.choices.0.label>Отозвать ударную группу и готовить прорыв за наставником</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.choices.0.label>
  <TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.choices.0.resultText>Глаза Нулевого Синта горят решимостью: «Он спас меня из тех белых комнат — теперь наш черёд вернуть его домой».</TMM_Story_A5_FW_1_ShieldOfSaltAndSteel.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_FW_2_MentorPrisonBreak -->
  <TMM_Story_A5_FW_2_MentorPrisonBreak.label>освобождение наставника</TMM_Story_A5_FW_2_MentorPrisonBreak.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.0.label>Расшифровка координат спецтюрьмы</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.0.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.0.text>Связисты Дездемоны сопоставили след сигнала из Сектора 3 с перехваченными логами Института, вычислив Подземный изолятор №00 — секретную тюрьму Консорциума под охраной охотников и стазис-ловушек.</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.0.text>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.0.effects.3.label>Наставник на свободе!</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.0.effects.3.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.0.effects.3.text>Сбив замки стазис-камеры, ваша группа вытащила измученного наставника из тайной тюрьмы Консорциума живым — вместе с физическим мастер-ключом от Нулевого Якоря!</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.0.effects.3.text>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.1.effects.1.label>Пустая камера и выцарапанный шифр</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.1.effects.1.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.1.effects.1.text>Чистильщики Консорциума вывезли пленника за пару часов до штурма, но наставник успел выцарапать финальный код сброса Якоря на внутренней стороне стазис-стекла.</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.1.effects.1.text>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.2.effects.1.label>Жертва во тьме</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.2.effects.1.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.2.effects.1.text>Когда подкрепление охотников прижало отряд, наставник изнутри опустил аварийные гермодвери, спасая ваших бойцов — и в последнюю секунду просунул в щель голодиск с финальным ключом.</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.1.outcomes.2.effects.1.text>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.label>Последняя деталь мозаики</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.text>Опираясь на живую память спасённого наставника или найденный в камере финальный шифр, Резонатор Искр готов дать планетарный импульс запечатывания. В ту же секунду все сирены тревоги в {COLONY} взвывают одновременно!</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.text>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.choices.0.label>Занять стены — союзные подкрепления спешат к {COLONY} на решающую осаду!</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.choices.0.label>
  <TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.choices.0.resultText>На всех горизонтах поднимаются столбы пыли: враги стягиваются к Нулевому Якорю, а верные союзники идут встать с вами плечом к плечу.</TMM_Story_A5_FW_2_MentorPrisonBreak.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_FW_3_SiegeOfZeroAnchor -->
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.label>осада нулевого якоря</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.0.label>Буря обрушивается на {COLONY}</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.0.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.0.text>Под кроваво-красным солнцем Резонатор Искр начинает накапливать энергию для планетарного запечатывания. За периметром вспыхивают молнии телепортации Института — первая волна Осады Нулевого Якоря пошла на штурм!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.0.text>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.label>Заряд Резонатора 40% — союзники сбрасывают припасы</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.text>Дымящиеся остовы синтов усеивают подступы, а разведчики Джона Хэнкока и пулемётчики Фокса сбрасывают во двор ящики с медикаментами и боеприпасами! За хребтом сгущаются серные тучи — и земля содрогается от тяжёлой поступи.</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.text>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.0.label>[Лебедь повержен] Держать строй — сквозь пепел наступают легионы Ада</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.0.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>Поскольку Король Бегемотов уже пал, дикие племена супермутантов не решаются лезть в бой — все стволы сосредоточены на авангарде преисподней!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.1.label>[Дикая орда на подходе] Трубить тревогу — дикие супермутанты Лебедя врываются в мясорубку!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.1.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>Привлечённая оглушительным гулом Резонатора, обезумевшая орда Лебедя обрушивается на долину прямо перед легионом Ада!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>Орда бегемотов разбита</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>Защитники скосили наступающих супермутантов у самых баррикад и тут же развернули орудия навстречу клубам чёрного дыма!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.5.label>Заряд Резонатора 85% — ослепительный свет пронзает багровые тучи</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.5.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.5.text>Линии соли и енохианские обереги выдержали натиск демонов! Высоко в небе багровые облака разрывают столпы золотого света: архангел Михаил бросает в бой элитное воинство Небес, пытаясь захватить Нулевого Синта в последнюю минуту!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.5.text>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.1.label>Нулевой Якорь устоял!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.1.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.1.text>Под небом, озарённым синими гармоническими молниями, гарнизон сокрушил последний десант Небес! Планетарная печать замкнута — теперь остаётся лишь Шпиль Разлома Дика Романа!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.1.text>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>Импульс выпущен сквозь дым</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>
  <TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>Несмотря на пробитые стены и опалённые укрепления, Резонатор Искр вовремя дал гармонический залп — пора ударить по цитадели Дика Романа!</TMM_Story_A5_FW_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>

  <!-- TMM_Story_A5_FW_4_CitadelAssault -->
  <TMM_Story_A5_FW_4_CitadelAssault.label>последний импульс: врата разлома</TMM_Story_A5_FW_4_CitadelAssault.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.0.label>Штурм Шпиля Разлома Консорциума</TMM_Story_A5_FW_4_CitadelAssault.stages.0.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.0.text>Планетарная печать замкнута, отрезав Небеса и Ад от Земли — но глава левиафанов Дик Роман пытается перегрузить центральный Шпиль Разлома, чтобы снова прорвать барьер и пожрать континент. Дин Винчестер проверяет барабан «Кольта», а Сэм взводит гармонический детонатор.</TMM_Story_A5_FW_4_CitadelAssault.stages.0.text>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.0.label>[Формула буры открыта] Вооружить штурмовой отряд распылителями концентрированной буры</TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.0.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.0.resultText>Интенданты выдают каждому бойцу баллоны с тетраборатом натрия. Неуязвимость Дика Романа растает при первом же попадании!</TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.0.resultText>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.1.label>Начать штурм с тяжёлым оружием, «Кольтом» и енохианскими патронами</TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.1.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.1.resultText>Винтокрылы и разведчики Минитменов высаживают ударную группу прямо у порога Шпиля Разлома. Помните: против Романа необходима бура!</TMM_Story_A5_FW_4_CitadelAssault.stages.0.choices.1.resultText>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.0.effects.2.label>Дик Роман растворился в пепле!</TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.0.effects.2.text>Шипя в белой пене буры, человеческая личина Дика Романа разорвалась с воплем чёрного ихора, и тварь рухнула безжизненным пеплом! Шпиль Разлома умолк навсегда.</TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.1.effects.0.label>Шпиль рухнул в Разлом</TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.1.effects.0.text>Прежде чем Роман запустил перегрузку, гармоническая волна Резонатора обрушила Шпиль Разлома — похоронив совет левиафанов под тысячами тонн бетона.</TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.2.effects.0.label>Артиллерия союзников добивает вершину</TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.2.effects.0.text>Хотя авангард понёс тяжёлые потери при штурме холла, батареи Минитменов снесли ретрансляционную корону Шпиля до запуска перегрузки.</TMM_Story_A5_FW_4_CitadelAssault.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.2.label>Рассвет Свободной Пустоши</TMM_Story_A5_FW_4_CitadelAssault.stages.2.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.2.text>Багровые тучи расступаются над руинами. Впервые за два столетия чистый солнечный свет озаряет мир, свободный от небесных тиранов, адских сделок и жатвы Консорциума — а Нулевой Синт стоит рядом с вами живым и невредимым.</TMM_Story_A5_FW_4_CitadelAssault.stages.2.text>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.2.choices.0.label>Передать отбой по всем частотам — Пустошь принадлежит живым</TMM_Story_A5_FW_4_CitadelAssault.stages.2.choices.0.label>
  <TMM_Story_A5_FW_4_CitadelAssault.stages.2.choices.0.resultText>Ликующие голоса звучат от Добрососедства до Замка. Война за Искру выиграна.</TMM_Story_A5_FW_4_CitadelAssault.stages.2.choices.0.resultText>


  <!-- ===================================================================== -->
  <!-- COALITION 2: STERILE EMPIRE                                           -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_SE_1_ToxicHorizon -->
  <TMM_Story_A5_SE_1_ToxicHorizon.label>токсичный горизонт</TMM_Story_A5_SE_1_ToxicHorizon.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.0.label>Синхронизация сети распыления</TMM_Story_A5_SE_1_ToxicHorizon.stages.0.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.0.text>Доктор Чарльз Керлинг и Отец подключили Резонатор Искр к довоенной метеостанции. После зарядки её излучатели вытравят радиацию, мутации ВРЭ и оккультные аномалии по всему континенту — если вы удержите станцию от винтокрылов Братства Стали.</TMM_Story_A5_SE_1_ToxicHorizon.stages.0.text>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.0.label>Высадить ударные отряды синтов Института для прикрытия периметра</TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.0.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.0.resultText>Эстафетные гранаты вспыхивают по периметру станции, и синтетические штурмовики перекрывают сектора обстрела.</TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.0.resultText>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.1.label>Запитать довоенные плазменные конденсаторы обороны самой станции</TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.1.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.1.resultText>Тяжёлые энергоконтуры на крыше метеостанции оживают, готовые прожигать силовую броню десанта.</TMM_Story_A5_SE_1_ToxicHorizon.stages.0.choices.1.resultText>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.0.effects.1.label>Атмосферный ретранслятор заряжен</TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.0.effects.1.text>Защитники восемь часов сдерживали натиск паладинов Братства, выведя Матрицу Стерилизации на полную мощность!</TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.1.effects.0.label>Сигнал переведён на орбитальный спутник</TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.1.effects.0.text>Пока отряды Братства увязли в ложных бункерах, инженеры Анклава переключили частоту распыления на орбитальный ретранслятор.</TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.2.effects.0.label>Резервный блок вынесен из-под огня</TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.2.effects.0.text>Отступив с крыши под плотным лазерным огнём, техники всё же успели извлечь главный передатчик перед телепортацией в {COLONY}.</TMM_Story_A5_SE_1_ToxicHorizon.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.2.label>Цель: штаб сопротивления</TMM_Story_A5_SE_1_ToxicHorizon.stages.2.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.2.text>Когда сеть распыления взведена, полковник Отем приказывает нанести молниеносный обезглавливающий удар по коалиции сопротивления, которую возглавляет мэр-гуль Джон Хэнкок.</TMM_Story_A5_SE_1_ToxicHorizon.stages.2.text>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.2.choices.0.label>Подготовить ударную группу охотников и штурмовиков «Адского пламени»</TMM_Story_A5_SE_1_ToxicHorizon.stages.2.choices.0.label>
  <TMM_Story_A5_SE_1_ToxicHorizon.stages.2.choices.0.resultText>Координаты телепортации наводятся на укреплённый штаб Добрососедства.</TMM_Story_A5_SE_1_ToxicHorizon.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_SE_2_PurgeOfGoodneighbor -->
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.label>зачистка добрососедства</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.0.effects.1.label>Командование сопротивления разгромлено</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.0.effects.1.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.0.effects.1.text>Джон Хэнкок пал на баррикадах Добрососедства в своём алом сюртуке — хребет наземного сопротивления сломлен!</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.0.effects.1.text>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.1.effects.0.label>Добрососедство эвакуировано</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.1.effects.0.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.1.effects.0.text>Хэнкок оставил укреплённую площадь до подхода главных сил, бросив склады припасов при отходе в руины.</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.1.effects.0.text>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.2.effects.0.label>Кровавый уличный бой</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.2.effects.0.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.2.effects.0.text>Хотя телохранители Хэнкока вынудили десант отойти, спустя минуту орбитальный удар Анклава сравнял квартал с землёй.</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.0.outcomes.2.effects.0.text>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.label>После боя в пепле</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.text>В окружении сдавшихся мирных жителей и пленных связных Подземки полевой командир ждёт приказа об участи пленных — и докладывает, что стазис-капсула вашего пропавшего наставника обнаружена в башне Консорциума.</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.text>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.0.label>Отправить выживших в стерильные карантинные лагеря без расстрелов</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.0.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.0.resultText>Сдержанность предотвращает бунт в {COLONY} и не даёт Нулевому Синту окончательно отвернуться от вас.</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.0.resultText>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.1.label>Исполнить директиву полковника Отема о нулевой терпимости без исключений</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.1.label>
  <TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.1.resultText>Плазменные залпы устанавливают мёртвую тишину. На базе Нулевой Синт смотрит на сводку с камер в холодном, ужасающем молчании.</TMM_Story_A5_SE_2_PurgeOfGoodneighbor.stages.1.choices.1.resultText>

  <!-- TMM_Story_A5_SE_3_SiegeOfZeroAnchor -->
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.label>осада нулевого якоря</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.0.label>Ответный удар Пустоши</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.0.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.0.text>Поняв, что Матрица Стерилизации выйдет на полную мощность через считанные часы, все уцелевшие силы сопротивления бросаются на отчаянный штурм {COLONY}!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.0.text>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.label>Матрица на 45% — тяжёлая броня на подходе</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.text>Подкрепления синтов Института телепортируются во двор со свежими плазменными батареями и стимуляторами как раз в тот миг, когда багровое небо сотрясает рёв турбин винтокрылов Братства Стали.</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.text>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.0.label>[Лебедь повержен] Развернуть плазменные батареи в небо против десанта Братства</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.0.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>Поскольку бегемоты Лебедя уже истреблены, турели периметра чисто наводятся на снижающихся паладинов Братства!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.1.label>[Дикая орда на подходе] Перенаправить огонь — дикие супермутанты Лебедя таранят баррикады!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.1.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>Сведённая с ума ультразвуковым импульсом Резонатора, орда Лебедя врезается во внешний периметр раньше десанта Братства!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.5.label>Матрица на 90% — чёрный ихор поднимается из бури</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.5.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.5.text>Пробитые доспехи T-60 дымятся во дворе! Прежде чем инженеры успевают задраить гермодвери, Дик Роман бросает на {COLONY} всю стаю левиафанов, чтобы сорвать волну стерилизации!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.5.text>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>Стерильная Империя выстояла!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>Плазменные когорты и распылители буры уничтожили штурмовую стаю левиафанов! Теперь пора ударить по штаб-квартире Дика Романа и завершить Протокол Возрождения!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>Периметр изранен, но не сломлен</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>
  <TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>Стая левиафанов отступила в пепел, когда Резонатор вышел на полную мощность — немедленно атакуйте башню Романа!</TMM_Story_A5_SE_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>

  <!-- TMM_Story_A5_SE_4_ProtocolRebirth -->
  <TMM_Story_A5_SE_4_ProtocolRebirth.label>протокол возрождения: цитадель совета</TMM_Story_A5_SE_4_ProtocolRebirth.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.label>Ликвидация Совета Консорциума</TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.text>Осталось одно препятствие, мешающее Матрице Стерилизации очистить континент: глава левиафанов Дик Роман перехватил орбитальный погодный канал в башне «Сукрокорп» — а разведка Анклава подтверждает, что на том же этаже в стазис-капсуле держат вашего пропавшего наставника.</TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.text>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.0.label>[Формула буры открыта] Вооружить отряд «Адского пламени» распылителями концентрированной буры</TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.0.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.0.resultText>Химические баллоны закреплены на броне бойцов — неуязвимость Дика Романа исчезнет в ту же секунду, как его накроет белый туман!</TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.0.resultText>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.1.label>Высадиться через телепорт Института с тяжёлым плазменным вооружением</TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.1.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.1.resultText>Синие молнии высаживают отряд прямо на этаж правления. Используйте оружие с бурой, чтобы снять защиту Романа!</TMM_Story_A5_SE_4_ProtocolRebirth.stages.0.choices.1.resultText>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.0.effects.2.label>Дик Роман ликвидирован, наставник спасён</TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.0.effects.2.text>Растворившись в шипящей пене буры, Дик Роман рухнул в зале заседаний — а бойцы вскрыли стазис-капсулу с вашим пропавшим наставником!</TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.1.effects.1.label>Орбитальный канал очищен</TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.1.effects.1.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.1.effects.1.text>Орбитальный плазменный луч Анклава превратил верхний ярус башни в расплавленное стекло, освободив частоту для Протокола Возрождения.</TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.1.effects.1.text>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.2.effects.1.label>Маяк подорван при отходе</TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.2.effects.1.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.2.effects.1.text>Хотя авангард понёс тяжёлые потери, подрывные заряды уничтожили главный передатчик башни перед эвакуацией.</TMM_Story_A5_SE_4_ProtocolRebirth.stages.1.outcomes.2.effects.1.text>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.label>Чистый, стерильный мир</TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.text>Белый аэрозоль накрывает верхние слои атмосферы, нейтрализуя радиацию, токсины ВРЭ и пространственные разломы. Под двойным знаменем Анклава и Института из пепла поднимается дисциплинированная империя людей — пока Нулевой Синт молча стоит под охраной у Резонатора.</TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.text>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.choices.0.label>Передать Имперскую Хартию — человечество вернуло себе право на Землю</TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.choices.0.label>
  <TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.choices.0.resultText>Винтокрылы торжественно кружат в чистом небе над {COLONY}. Стерильная Империя восторжествовала.</TMM_Story_A5_SE_4_ProtocolRebirth.stages.2.choices.0.resultText>


  <!-- ===================================================================== -->
  <!-- COALITION 3: IRON CRUSADE                                             -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_IC_1_HammerAndAnvil -->
  <TMM_Story_A5_IC_1_HammerAndAnvil.label>молот и наковальня</TMM_Story_A5_IC_1_HammerAndAnvil.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.label>Топливо для континентального подавителя</TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.text>Чтобы превратить Резонатор Искр в постоянный планетарный подавитель, гасящий любую сверхъестественную магию и вышедший из-под контроля ИИ, Генри Касдину и проктору Ингрэм нужны обогащённые урановые стержни из укреплённого арсенала Анклава.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.text>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.0.label>Выдать штурмовой группе термические резаки Изгоев и свинцовые контейнеры</TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.0.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.0.resultText>Специалист Олин раздаёт бойцам тяжёлые плазменные резаки и защитные ранцы для рейда на склад Анклава.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.0.resultText>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.1.label>Провести стремительную высадку с винтокрылов прямо на крышу арсенала</TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.1.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.1.resultText>Пилоты Братства прокладывают маршрут на сверхмалой высоте, обходя внешние минные поля склада.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.0.choices.1.resultText>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.0.effects.1.label>Обогащённые урановые стержни захвачены</TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.0.effects.1.text>Вскрыв гермодвери Анклава под шквалом лазерного огня, ваши рыцари вынесли обогащённые урановые стержни целыми!</TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.1.effects.1.label>Захвачен резервный обоз</TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.1.effects.1.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.1.effects.1.text>Пока транспорты Анклава эвакуировали главный бункер, разведчики перехватили арьергардный конвой с половиной нужного запаса урана.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.1.effects.1.text>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.2.effects.0.label>Аварийная выгрузка из резервного реактора</TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.2.effects.0.text>Отбитые от арсенала плотным плазменным огнём, инженеры проктора Ингрэм извлекли урановые стержни из списанного вспомогательного реактора дирижабля.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.label>Следующая цель: фабрика синтов</TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.text>Когда топливные стержни подавителя получены, Артур Мэксон бьёт кулаком по тактической карте: прежде чем опустится Железный занавес, подземный комплекс Института должен быть выжжен до основания.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.text>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.choices.0.label>Собрать тяжёлый бронетанковый кулак для нисхождения в Институт</TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.choices.0.label>
  <TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.choices.0.resultText>Сервоприводы слаженно гудят, пока паладины Братства и Изгоев готовятся к бою в подземных залах.</TMM_Story_A5_IC_1_HammerAndAnvil.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_IC_2_DescentIntoInstitute -->
  <TMM_Story_A5_IC_2_DescentIntoInstitute.label>нисхождение в институт</TMM_Story_A5_IC_2_DescentIntoInstitute.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.0.effects.1.label>Ядро Института сокрушено</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.0.effects.1.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.0.effects.1.text>Проломив белый атриум в силовой броне, ваш крестовый отряд ликвидировал Отца и взорвал молекулярный телепорт Института!</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.0.effects.1.text>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.1.effects.0.label>Шахта обрушена</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.1.effects.0.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.1.effects.0.text>Сапёры Братства подорвали главный ствол шахты термоядерными зарядами, похоронив уцелевших учёных Института под толщей скалы.</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.1.effects.0.text>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.2.effects.0.label>Отход из атриума</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.2.effects.0.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.2.effects.0.text>Оттеснённые на поверхность волнами охотников, подрывники всё же успели взорвать кумулятивный заряд и перерезать поверхностную сеть телепортации.</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.0.outcomes.2.effects.0.text>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.label>В дымящихся руинах робототехники</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.text>Среди разбитых конвейеров сборки синтов разведчики находят запертую камеру Консорциума — и перед вами встаёт выбор, как поступить с безоружными учёными в нижней лаборатории.</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.text>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.0.label>Включить сирену эвакуации и дать безоружным техникам уйти на поверхность</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.0.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.0.resultText>Даже Артур Мэксон опускает ствол гатлинг-лазера, пока перепуганные техники бегут по аварийной лестнице — а внутри камеры бойцы находят вашего пропавшего наставника живым!</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.0.resultText>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.1.label>Задраить гермодвери и затопить нижние уровни — от Института не останется ни следа</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.1.label>
  <TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.1.resultText>Тяжёлые стальные створки навсегда захлопываются над подземным комплексом, хороня и учёных, и всех пленников внутри.</TMM_Story_A5_IC_2_DescentIntoInstitute.stages.1.choices.1.resultText>

  <!-- TMM_Story_A5_IC_3_SiegeOfZeroAnchor -->
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.label>осада нулевого якоря</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.0.label>Зарядка подавителя «Железный занавес»</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.0.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.0.text>Проктор Ингрэм и специалист Олин загружают обогащённые урановые стержни в Резонатор Искр! Едва его антимагическое поле начинает расширяться над регионом, все сверхъестественные хищники Земли бросаются на {COLONY}, чтобы уничтожить машину до конца зарядки!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.0.text>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.label>Поле подавления 50% — небесный гром над головой</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.text>Винтокрылы Братства сбрасывают во двор свежие энергоячейки и боеприпасы, пока Генри Касдин кричит по громкой связи: «Первая волна отбита! Держите распылители буры наготове и следите за небом!»</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.text>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.0.label>[Лебедь повержен] Навести зенитные турели на спускающееся воинство Небес</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.0.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>Поскольку Лебедь уже мёртв, дикие племена супермутантов не лезут в бой — ваши рыцари сосредоточили весь огонь на авангарде ангелов!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.1.label>[Дикая орда на подходе] Боевая тревога — бегемоты Лебедя штурмуют ворота!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.1.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>Разъярённые гулом подавителя, дикие супермутанты Лебедя врезаются во внешние стены прямо перед десантом Небес!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.5.label>Поле подавления 90% — серные тучи заволакивают горизонт</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.5.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.5.text>Ангельские перья сгорают в пепел на ваших брустверах! Теперь земля разверзается за периметром, и последний легион Ада рвётся сквозь багровый туман в отчаянной попытке разбить Железный занавес!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.5.text>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>Железный Крестовый Поход непобедим!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>Сталь, соль и дисциплинированный огонь сокрушили последний натиск преисподней у ваших ворот! Теперь садитесь в винтокрылы и штурмуйте Небесный Шпиль, чтобы навсегда отсечь сверхъестественную силу от Земли!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>Поле подавления стабилизировано</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>
  <TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>Ценой тяжёлых потерь рыцари удержали Резонатор до замыкания поля — немедленно атакуйте главный якорь врага!</TMM_Story_A5_IC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>

  <!-- TMM_Story_A5_IC_4_IronCurtain -->
  <TMM_Story_A5_IC_4_IronCurtain.label>железный занавес: штурм небесного шпиля</TMM_Story_A5_IC_4_IronCurtain.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.0.label>Штурм Небесного Бастиона</TMM_Story_A5_IC_4_IronCurtain.stages.0.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.0.text>Последняя твердыня удерживает сверхъестественный Разлом над Землёй — Небесный Шпиль, который обороняет архангел Михаил со своей гвардией серафимов. Артур Мэксон досылает энергоячейку в «Последний суд», пока Генри Касдин проверяет штурмовые винтокрылы.</TMM_Story_A5_IC_4_IronCurtain.stages.0.text>
  <TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.0.label>[Бура и технологии готовы] Загрузить патроны с енохианской гравировкой и баллоны буры</TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.0.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.0.resultText>Олин и Ингрэм снабжают каждого рыцаря руническими зарядами и химическими канистрами — ни ангельская броня, ни лазутчики левиафанов не выдержат такого удара!</TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.0.resultText>
  <TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.1.label>Возглавить лобовой штурм в силовой броне прямо через главные врата Шпиля</TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.1.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.1.resultText>Штурмовые винтокрылы подавляют двор огнём, пока ваши рыцари высаживаются в самое сердце Небесного Шпиля. Ad Victoriam!</TMM_Story_A5_IC_4_IronCurtain.stages.0.choices.1.resultText>
  <TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.0.effects.1.label>Архангел Михаил повержен!</TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.0.effects.1.text>В ослепительной схватке небесного огня и тяжёлых лазерных лучей архангел Михаил пал — и якорный камень Разлома рассыпался в безжизненную пыль!</TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.1.effects.0.label>Шпиль разрушен артиллерией «Придвена»</TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.1.effects.0.text>Орудия дирижабля Братства накрыли верхние ярусы Шпиля, обрушив якорный камень и отбросив уцелевших ангелов за закрытый Разлом.</TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.2.effects.0.label>Якорный камень перегружен в бою</TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.2.effects.0.text>Вынужденные отойти под испепеляющим светом, сапёры успели подорвать главный контур Шпиля перед эвакуацией.</TMM_Story_A5_IC_4_IronCurtain.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_IC_4_IronCurtain.stages.2.label>Эпоха Железного занавеса</TMM_Story_A5_IC_4_IronCurtain.stages.2.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.2.text>Беззвучная невидимая волна прокатывается от Нулевого Якоря — навсегда гася оккультную магию, пространственные разломы и неконтролируемые синтетические сети по всей планете. Человечество укрыто за стенами стали под суровым надзором воссоединившихся Братства Стали и Изгоев.</TMM_Story_A5_IC_4_IronCurtain.stages.2.text>
  <TMM_Story_A5_IC_4_IronCurtain.stages.2.choices.0.label>Провозгласить Ad Victoriam — Железный Крестовый Поход защитил Землю</TMM_Story_A5_IC_4_IronCurtain.stages.2.choices.0.label>
  <TMM_Story_A5_IC_4_IronCurtain.stages.2.choices.0.resultText>Винтокрылы триумфально кружат над {COLONY}. Ни одно чудовище и ни один лжебог больше не угрожают человечеству.</TMM_Story_A5_IC_4_IronCurtain.stages.2.choices.0.resultText>

</LanguageData>
```