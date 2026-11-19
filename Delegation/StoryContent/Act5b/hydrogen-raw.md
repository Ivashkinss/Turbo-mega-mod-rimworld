`Defs/StoryDefs/Story_Act5_Part2.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- COALITION 4: CELESTIAL ORDER (Heaven + British Men of Letters)        -->
  <!-- ===================================================================== -->

  <!-- CO QUEST 1: SPEC-OP — HARVEST OF SPARKS -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_CO_1_HarvestOfSparks</defName>
    <label>harvest of sparks</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_michael</giver>
    <fallbackGiver>TMM_Hero_naomi</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=celestial_order</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Rekindling Heaven's dying grace</label>
        <text>To restore the Heavenly Host's full strength before the Rift closes, Archangel Michael requires the spiritual resonance of awakened third-generation synths currently sheltered in a hidden Railroad transit station. Dr. Hess has supplied Enochian siphon lanterns to collect their Sparks.</text>
        <choices>
          <li>
            <outcome>defector_gentle_siphon</outcome>
            <label>[Defector Guidance] Let Castiel or Mick Davies tune the lanterns to draw resonance without killing the synths</label>
            <resultText>Working quietly behind Michael's back, your defector calibrates the Enochian lanterns to draw only harmonic overflow—sparing the synths' lives while still fueling Heaven's beacon.</resultText>
            <anyOf>
              <li>defector.TMM_Hero_castiel=true</li>
              <li>defector.TMM_Hero_mick_davies=true</li>
            </anyOf>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>3</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_siphonMode</flag><value>gentle</value></li>
            </effects>
          </li>
          <li>
            <outcome>full_extraction</outcome>
            <label>Execute Michael's directive without hesitation—drain every Spark in the station</label>
            <resultText>Golden lanterns flare to blinding brilliance as your strike team marches toward the Railroad transit depot. The Zero Synth watches in silent dread.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>25</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_siphonMode</flag><value>brutal</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_RailroadFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentSpacer</thing><count>2</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Celestial lanterns charged</label>
                <text>Your strike team broke through the Railroad's ballistic-weave defenders and charged the Enochian lanterns to full radiance—already, angelic wings above {COLONY} glow brighter!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Angelic vanguard sweeps the depot</label>
                <text>Before your squad arrived, Naomi's strike angels descended onto the depot themselves, returning with partially charged Spark lanterns.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Residual grace salvaged</label>
                <text>Forced to retreat from the Railroad's ambush, your technicians still carried back two filled siphon lanterns—enough to keep Michael's schedule alive.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Next phase: London's control grid</label>
        <text>With Heaven's grace surging back through the Resonator, Dr. Hess unfolds a continental map: the British Men of Letters must erect their master Enochian relay pylon atop a pre-war broadcast spire to pacify the wasteland's will.</text>
        <choices>
          <li>
            <outcome>march_to_spire</outcome>
            <label>Deploy the ward-engineers to erect the London Network</label>
            <resultText>Arthur Ketch and Naomi's seraphim fall into formation beside your troopers as the convoy heads for the broadcast spire.</resultText>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_q1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- CO QUEST 2: SPEC-OP — THE LONDON NETWORK -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_CO_2_LondonNetwork</defName>
    <label>the london network</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_dr_hess</giver>
    <fallbackGiver>TMM_Hero_arthur_ketch</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=celestial_order</li>
      <li>done:TMM_Story_A5_CO_1_HarvestOfSparks</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Erecting the master Enochian spire</label>
        <text>Dr. Hess's occult architects have bolted the master Enochian suppression pylon onto a towering pre-war radio spire. Brotherhood of Steel gunships are already inbound to blow the spire off the ridge before its calming field covers the region.</text>
        <days>0.8</days>
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
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>25</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The London Network is online</label>
                <text>Through eight hours of Brotherhood laser fire, your wardens held the spire until a shimmering dome of golden Enochian script locked across the horizon!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Auxiliary pylon engaged</label>
                <text>While Brotherhood paladins fought Men of Letters decoys, Arthur Ketch brought an underground backup pylon online.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Harmonic core extracted</label>
                <text>Forced off the ridge by Brotherhood artillery, your operatives still salvaged the spire's golden relay core for direct mounting at {COLONY}.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>A prisoner in the London sub-vault</label>
        <text>Inside the spire's locked basement interrogation wing, your squad makes a shocking discovery: your missing mentor is chained inside a warded stasis cell—captured weeks ago by British Men of Letters sweepers. Dr. Hess watches coldly over the intercom: "That individual knows too much about Project Purgatory. Leave them in our custody."</text>
        <choices>
          <li>
            <outcome>free_mentor</outcome>
            <label>Defy Dr. Hess, shatter the cell wards, and bring your mentor home to {COLONY}</label>
            <resultText>Dr. Hess's jaw tightens, but she dares not cross you with the Resonator in your hands. Your exhausted mentor grips your arm as your squad escorts them out of the vault!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>15</amount></li>
            </effects>
          </li>
          <li>
            <outcome>leave_to_hess</outcome>
            <label>Leave the prisoner under London's custody—nothing may jeopardize the Celestial Order</label>
            <resultText>The heavy steel door clicks shut over your mentor's quiet plea. Dr. Hess rewards your absolute obedience with a chest of pre-war gold.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>30</count></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>-15</amount></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_q2Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- CO QUEST 3: SIEGE OF THE ZERO ANCHOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_CO_3_SiegeOfZeroAnchor</defName>
    <label>siege of the zero anchor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_michael</giver>
    <fallbackGiver>TMM_Hero_dr_hess</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=celestial_order</li>
      <li>done:TMM_Story_A5_CO_2_LondonNetwork</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Heaven's beacon ignites</label>
        <text>A pillar of blinding golden fire shoots from {COLONY}'s Spark Resonator into the blood-red clouds, summoning Heaven's legions back to Earth. Desperate to stop the Celestial Order before free will is bound forever, surface hunters, Enclave gunships, and Hell's armies converge on your walls!</text>
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
        <label>Celestial Pillar at 45% — thunder on every horizon</label>
        <text>Angelic couriers drop ultratech trauma kits and Men of Letters munitions into your courtyard as the hunter vanguard falls back. Beyond the ash ridge, Enclave plasma engines roar—and the ground shakes beneath heavy footsteps.</text>
        <choices>
          <li>
            <outcome>swan_already_dead</outcome>
            <label>[Swan Defeated] Focus all Enochian defenses on the incoming Enclave vanguard</label>
            <resultText>With Swan already slain, the wild super mutant tribes cower in the hills—letting your garrison meet the Enclave head-on!</resultText>
            <nextStage>4</nextStage>
            <requires>
              <li>story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>55</count></li>
            </effects>
          </li>
          <li>
            <outcome>swan_intervenes</outcome>
            <label>[Wild Horde Incoming] Sound the bells—Swan's feral behemoths are crashing into the battle!</label>
            <resultText>Blinded and enraged by the golden sky-pillar, Swan's super mutant horde smashes into your barricades ahead of the Enclave!</resultText>
            <nextStage>3</nextStage>
            <requires>
              <li>!story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>55</count></li>
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
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Behemoth horde purged</label>
                <text>Holy fire and rune-etched volleys cut down the charging behemoths at your walls as Enclave vertibirds darken the sky!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- WAVE 2B: ENCLAVE 1.6x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_EnclaveFaction</faction>
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
        <label>Celestial Pillar at 90% — brimstone tears open the earth</label>
        <text>Black Enclave armor smolders across the courtyard! Now the earth splits open beyond your perimeter in a torrent of sulfur: Hell's final legion surges forward in one last apocalyptic bid to snuff out Heaven's light!</text>
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
                <label>The Celestial Order holds!</label>
                <text>Bathing the battlefield in blinding white light, your garrison shattered Hell's legions! Only the Morningstar himself remains—march on the Sulfur Citadel!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Golden pillar locked</label>
                <text>Through scorching smoke and broken gates, the Spark Resonator completed Heaven's summons—strike Lucifer's stronghold before he breaks the seal!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_q3Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- CO QUEST 4: CITADEL ASSAULT — ASCENSION OF THE HOST -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_CO_4_AscensionOfTheHost</defName>
    <label>ascension of the host: the morningstar's fall</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_michael</giver>
    <fallbackGiver>TMM_Hero_dr_hess</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=celestial_order</li>
      <li>done:TMM_Story_A5_CO_3_SiegeOfZeroAnchor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>The final battle of brothers</label>
        <text>Inside the burning ruins of the Sulfur Citadel, Lucifer has broken free of the Cage—rallying Hell's last demons and Leviathan mercenaries to tear down Heaven's newly lit sky. Archangel Michael draws his golden Archangel Blade while Dr. Hess primes the Equalizer.</text>
        <choices>
          <li>
            <outcome>borax_and_grace</outcome>
            <label>[Borax Formula Ready] Arm the strike host with holy oil, Enochian rounds, and Borax sprayers</label>
            <resultText>Consecrated wards and Borax canisters leave neither Lucifer's demons nor his Leviathan guard any place to hide!</resultText>
            <requires>
              <li>story.boraxFormula=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>25</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_specialPrep</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>angelic_assault</outcome>
            <label>Descend in columns of celestial fire straight into Lucifer's throne hall</label>
            <resultText>Wings beat like thunder as your strike squad drops into the heart of the Sulfur Citadel to end the oldest war in creation.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_co_specialPrep</flag><value>false</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_HellFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_lucifer</hero>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.luciferDefeated</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Lucifer cast back into the Pit!</label>
                <text>In a deafening shockwave of white and crimson lightning, Lucifer fell to his knees—and Heaven's golden chains dragged him back into the sealed Cage forever!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Citadel purged by celestial smiting</label>
                <text>Archangel Michael called down a pillar of purifying fire from the Resonator, collapsing the Sulfur Citadel and sealing the infernal fissure beneath glassed rock.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Pyrrhic victory at the Pit</label>
                <text>Though your vanguard suffered heavy losses in the sulfur smoke, Men of Letters ward-bombs collapsed the throne hall and broke Hell's command.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Reign of the Celestial Order</label>
        <text>Radiant, cloudless light washes across the continent. Raids, monsters, and violent chaos vanish beneath the humming golden pylons of the London Network and Heaven's watchful host—an immaculate, silent paradise bought with free will.</text>
        <choices>
          <li>
            <outcome>proclaim_order</outcome>
            <label>Sound the silver trumpets—the Celestial Order now rules Earth in eternal stillness</label>
            <resultText>Angels patrol the spotless sky above {COLONY}. The war is over; absolute peace has begun.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>50</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.finaleDone</flag><value>celestial_order</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>


  <!-- ===================================================================== -->
  <!-- COALITION 5: DARK COVENANT (Hell + Monster Clans)                     -->
  <!-- ===================================================================== -->

  <!-- DC QUEST 1: SPEC-OP — BLOOD TITHE -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_DC_1_BloodTithe</defName>
    <label>blood tithe</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_meg</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=dark_covenant</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Cracking the Vault-Tec soul-bank</label>
        <text>To tear open the final Seal and flood Earth with the power of Hell and Purgatory, your infernal allies need the pre-war soul capacitors hoarded inside Vault-Tec's central reserve bunker. Dick Roman's Leviathans and Alpha Vampire's pack are ready to breach the blast door beside you.</text>
        <choices>
          <li>
            <outcome>blood_ritual</outcome>
            <label>Empower your squad with Alpha Vampire blood-alchemy and bioferrite charms</label>
            <resultText>Crimson mist curls around your fighters' blades as predatory hunger sharpens their reflexes for the vault raid.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>3</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_tithePrep</flag><value>vampire</value></li>
            </effects>
          </li>
          <li>
            <outcome>demonic_breach</outcome>
            <label>Summon brimstone sappers to melt straight through Vault-Tec's steel doors</label>
            <resultText>Crossroads demons coat your breach charges in hellfire sulfur—no pre-war bulkhead will slow your vanguard.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>50</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_tithePrep</flag><value>hellfire</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_VaultTecFaction</enemyFaction>
        <objective>Retrieve</objective>
        <thing>Gold</thing>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>30</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Vault soul-capacitors plundered</label>
                <text>Smashing past Vault-Tec's security riot squads, your dark legion ripped the golden soul-capacitors out of the central core and hauled them back to {COLONY}!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Leviathan pack empties the bunker</label>
                <text>Before your squad arrived, Dick Roman's Leviathan enforcers slipped through the air vents and delivered the stolen capacitors straight to your altar.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Scorched retreat with the tithe</label>
                <text>Though Vault turrets bloodied your vanguard, Meg teleported out through black smoke clutching the primary energy matrix.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>A familiar face in cold storage</label>
        <text>Behind the vault's cryogenic bulkhead, your troops find your missing mentor sealed inside an active Vault-Tec stasis pod—the very person who stole the Zero Synth to stop this nightmare. Meg grins darkly over the console: "Look who we found, boss. Bring them back to watch your coronation, or feed their pod to the Pit?"</text>
        <choices>
          <li>
            <outcome>pull_mentor_out</outcome>
            <label>Crack the stasis pod open and bring your mentor back to {COLONY} alive</label>
            <resultText>Even on the darkest path, some bonds do not break easily. Your bewildered mentor is carried safely to the transport as the vault burns behind you.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>sacrifice_vault</outcome>
            <label>Sever the past completely—drain the cryogenic bank into the Resonator's altar</label>
            <resultText>Crimson lightning surges through the stasis bay, reducing every relic of your old life to silent ash. Hell cheers your ruthless devotion.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>35</count></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>-20</amount></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_q1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- DC QUEST 2: SPEC-OP — FALL OF THE HUNTERS -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_DC_2_FallOfHeavensGates</defName>
    <label>snuffing the silver</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_dick_roman</giver>
    <fallbackGiver>TMM_Hero_alpha_vampire</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=dark_covenant</li>
      <li>done:TMM_Story_A5_DC_1_BloodTithe</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Tracking the Colt</label>
        <text>Only one mortal weapon on Earth can still threaten your alliance's dark lords: the Colt, carried by Dean Winchester as he rallies the last hunters and Minutemen at an old highway stronghold. Dick Roman smiles with razor-sharp charm: "Remove Mr. Winchester from the board, and humanity's resistance folds overnight."</text>
        <days>0.8</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_MinutemenHuntersFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_dean_winchester</hero>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>800</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The hunters' champion has fallen</label>
                <text>Beside the smoking hood of a battered black car, Dean Winchester fought to his final silver round before falling beneath your dark vanguard—shattering the morale of every hunter on the continent!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Stronghold abandoned in flames</label>
                <text>Alpha Vampire's nocturnal packs burned the highway stronghold before your arrival, forcing the surviving hunters to scatter into the hills.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Bloody ambush at the roadhouse</label>
                <text>Rock salt and artillery bloodied your strike squad, yet demon reinforcements swept in afterward to level the hunters' forward depot.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Crusaders on the horizon</label>
        <text>With humanity's hunters broken, only the last desperate crusade of the British Men of Letters, the Brotherhood of Steel, and Archangel Michael's host stands between {COLONY} and eternal twilight. The Spark Resonator begins drinking crimson lightning from the Eclipse!</text>
        <choices>
          <li>
            <outcome>prepare_dark_siege</outcome>
            <label>Unleash the hellhounds and man the walls—let the crusade break against the Abyss!</label>
            <resultText>Brimstone fires roar along your battlements as your dark allies gather around the Resonator.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_q2Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- DC QUEST 3: SIEGE OF THE ZERO ANCHOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_DC_3_SiegeOfZeroAnchor</defName>
    <label>siege of the zero anchor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_alpha_vampire</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=dark_covenant</li>
      <li>done:TMM_Story_A5_DC_2_FallOfHeavensGates</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Cracking the Sixty-Sixth Seal</label>
        <text>The Spark Resonator screams in an unnatural crimson chord as the Sixty-Sixth Seal begins to fracture! Sensing the end of mortal dominion, the Men of Letters, the Brotherhood of Steel, and Heaven's last host launch a combined crusade against {COLONY}!</text>
        <days>0.6</days>
      </li>
      <!-- WAVE 1: 1.2x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_MenOfLettersFaction</faction>
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
        <label>Seal Fracture at 45% — steel and roar on the ridge</label>
        <text>Vampire elders and crossroads demons resupply your battlements with salvaged ammo and blood-packs over the fallen British operatives. Through the crimson smog, Brotherhood vertibirds swoop low—and thunderous footsteps rattle the gatehouse.</text>
        <choices>
          <li>
            <outcome>swan_already_dead</outcome>
            <label>[Swan Defeated] Meet the Brotherhood paladins with hellfire and claws</label>
            <resultText>With Swan already dead, the wild super mutants leave the field to you—turn every gun onto the descending Brotherhood knights!</resultText>
            <nextStage>4</nextStage>
            <requires>
              <li>story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>55</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
            </effects>
          </li>
          <li>
            <outcome>swan_intervenes</outcome>
            <label>[Wild Horde Incoming] Unleash the pack—Swan's feral behemoths are crashing the ritual!</label>
            <resultText>Driven berserk by the smell of brimstone, Swan's wild super mutants charge straight into your outer walls ahead of the Brotherhood!</resultText>
            <nextStage>3</nextStage>
            <requires>
              <li>!story.swanDefeated=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>55</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
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
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Feral mutants devoured by the night</label>
                <text>Your demons and nocturnal clans tore through the berserk super mutants before pivoting to face the Brotherhood's armored drop!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- WAVE 2B: BROTHERHOOD 1.6x -->
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
        <label>Seal Fracture at 90% — Heaven's last trumpet sounds</label>
        <text>Shattered Brotherhood power armor cools in pools of black ichor! Now blinding white bolts split the crimson dome as Heaven commits its last remaining seraphim to burn {COLONY} off the face of the Earth!</text>
        <days>0.4</days>
      </li>
      <!-- WAVE 3: HEAVEN 2.0x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HeavenFaction</faction>
        <pointsFactor>2.0</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The crusade is broken!</label>
                <text>Scorched feathers drift like snow across your courtyard—Heaven's final assault has failed! Now march into the Celestial Cathedral and cut down Archangel Michael!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Abyss widens</label>
                <text>Though angelic fire scarred your inner walls, the Sixty-Sixth Seal has cracked wide open—finish Archangel Michael before Heaven can seal the wound!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_q3Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- DC QUEST 4: CITADEL ASSAULT — SHATTERED ABYSS -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_DC_4_ShatteredAbyss</defName>
    <label>the shattered abyss: fall of the archangel</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_dick_roman</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=dark_covenant</li>
      <li>done:TMM_Story_A5_DC_3_SiegeOfZeroAnchor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Storming the Celestial Cathedral</label>
        <text>Archangel Michael stands alone inside the ruined Celestial Cathedral, channeling the last embers of Heaven's grace to hold the Rift together. Slay him, and the barrier between Earth, Hell, and Purgatory will dissolve forever under the Dark Covenant.</text>
        <choices>
          <li>
            <outcome>first_blade_wrath</outcome>
            <label>[Mark of Cain Awakened] Channel the First Blade's primordial fury into your vanguard</label>
            <resultText>The Mark of Cain blazes scarlet along your champion's arm as dark power floods your strike squad—even an Archangel will bleed before this edge!</resultText>
            <requires>
              <li>choice.hell_demons=pro_faction</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>30</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_firstBlade</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>abyssal_horde</outcome>
            <label>Unleash the full combined might of Hell's demons and the nocturnal clans</label>
            <resultText>Black smoke and predatory howls sweep through the cathedral doors as your dark coalition storms Michael's last sanctuary.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_dc_firstBlade</flag><value>false</value></li>
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
                <label>The Archangel has fallen!</label>
                <text>With a thunderclap that shattered every stained-glass window for miles, Archangel Michael fell beneath your dark vanguard—and the sky above Earth turned permanently twilight crimson!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Cathedral swallowed by sulfur</label>
                <text>Infernal fissures opened beneath the cathedral floor, swallowing Michael's sanctuary in hellfire and severing Heaven's last tether to Earth.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Darkness claims the ruins</label>
                <text>Though Michael's blade took a terrible toll on your vanguard, Alpha Vampire's elders and demonic shock troops overwhelmed the exhausted archangel moments later.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Lords of the Eternal Twilight</label>
        <text>The last Seal shatters in a roar of joyful hellfire. Sulfur mist and eternal twilight embrace the continent as demons, vampires, and Leviathans bow to {COLONY}—new princes of a dark, immortal empire built upon the ashes of the old world.</text>
        <choices>
          <li>
            <outcome>claim_twilight</outcome>
            <label>Ascend the throne of ash—the Dark Covenant rules the wasteland forever</label>
            <resultText>Crimson fires burn brightly across the horizon. Heaven is gone; the night belongs to you.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>60</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.finaleDone</flag><value>dark_covenant</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>


  <!-- ===================================================================== -->
  <!-- COALITION 6: SHADOW CONSORTIUM (Vault-Tec + Gunners)                  -->
  <!-- ===================================================================== -->

  <!-- SC QUEST 1: SPEC-OP — HOSTILE TAKEOVER -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SC_1_HostileTakeover</defName>
    <label>hostile takeover</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_stanislaus_braun</giver>
    <fallbackGiver>TMM_Hero_captain_wes</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=shadow_consortium</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Monopolizing the regional power grid</label>
        <text>To turn {COLONY}'s Spark Resonator into an uncontested commercial power monopoly, Dr. Braun needs the Brotherhood of Steel's forward fusion relay depot seized intact. Captain Wes has Gunner combat teams standing by to lock down the perimeter.</text>
        <choices>
          <li>
            <outcome>corporate_bribe</outcome>
            <label>Deploy pre-war override codes and Vault-Tec security drones to hijack the depot's turrets</label>
            <resultText>Dr. Braun giggles as Brotherhood perimeter turrets briefly glitch on legacy Vault-Tec maintenance keys, giving your troops a clean approach.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentIndustrial</thing><count>5</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_takeoverPrep</flag><value>hack</value></li>
            </effects>
          </li>
          <li>
            <outcome>mercenary_blitz</outcome>
            <label>Pay Captain Wes's heavy artillery crews to hammer the depot before assault</label>
            <resultText>Gunner rocket teams soften up the Brotherhood's sandbags and drop heavy armor plates for your squad.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>50</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_takeoverPrep</flag><value>artillery</value></li>
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
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>25</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Fusion depot acquired</label>
                <text>Holding the relay depot through eight hours of Brotherhood counter-attacks, your mercenaries transferred full grid control to Vault-Tec's central ledger!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Gunner reserve squad closes the deal</label>
                <text>Captain Wes's reserve battalion stormed the depot's back tunnel and rerouted its uranium buses straight to {COLONY}.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Grid tapped remotely</label>
                <text>Forced off the surface by Brotherhood knights, Vault-Tec technicians still clamped an underground siphon onto the depot's main conduit.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Next target: surface competitors</label>
        <text>With the regional energy grid under your thumb, Captain Wes lays out the next contract: wiping out the Minutemen and Hunters' central logistics hub before they can rally the free settlements against your monopoly.</text>
        <choices>
          <li>
            <outcome>sign_liquidation_order</outcome>
            <label>Sign the liquidation order and deploy Gunner assault teams</label>
            <resultText>Mercenary radios click in cold acknowledgment as your strike force rolls toward the resistance hub.</resultText>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_q1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- SC QUEST 2: SPEC-OP — LIQUIDATION CONTRACT -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SC_2_LiquidationContract</defName>
    <label>liquidation contract</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_captain_wes</giver>
    <fallbackGiver>TMM_Hero_baker</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=shadow_consortium</li>
      <li>done:TMM_Story_A5_SC_1_HostileTakeover</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Locking down the resistance hub</label>
        <text>Baker's recon team has surrounded the Minutemen and Hunters' fortified crossroads depot—and spotted a surprise inside their infirmary: your missing mentor, recently rescued from a Consortium transport by Preston Garvey's scouts.</text>
        <days>0.8</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_MinutemenHuntersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>900</count></li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Resistance depot liquidated</label>
                <text>Gunner crossfire and precision sniping broke the militia barricades, leaving the crossroads hub firmly in Shadow Consortium hands!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Militia forces driven out</label>
                <text>Captain Wes's main column swept the crossroads ahead of schedule, seizing the hunters' supply crates as the militia retreated.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Depot leveled by mortars</label>
                <text>Repulsed by fierce hunter resistance, Gunner mortar crews flattened the crossroads walls from long range instead.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Asset recovery in the infirmary</label>
        <text>Amid the smoke of the captured crossroads depot, your missing mentor stands unarmed inside the infirmary, staring at your mercenary insignia in disbelief. Captain Wes checks his rifle: "Your call, boss. Extract the VIP back to {COLONY}, or torch the building and leave no loose ends?"</text>
        <choices>
          <li>
            <outcome>extract_mentor</outcome>
            <label>Order Wes to stand down and escort your mentor safely back to {COLONY}</label>
            <resultText>"Understood—VIP extraction it is." Though your mentor glares in bitter disappointment at your corporate allies, they live to see the dawn.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>true</value></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>5</amount></li>
            </effects>
          </li>
          <li>
            <outcome>torch_hub</outcome>
            <label>No loose ends—seal the depot and burn every ledger inside</label>
            <resultText>Incendiary grenades roar through the timber walls. Dr. Braun wires a handsome bonus directly into your account.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.mentorRescued</flag><value>false</value></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>1000</count></li>
              <li Class="TurboMegaMod.StoryEffect_AddInt"><flag>zeroSynth.trust</flag><amount>-20</amount></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_q2Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- SC QUEST 3: SIEGE OF THE ZERO ANCHOR -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SC_3_SiegeOfZeroAnchor</defName>
    <label>siege of the zero anchor</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_stanislaus_braun</giver>
    <fallbackGiver>TMM_Hero_captain_wes</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=shadow_consortium</li>
      <li>done:TMM_Story_A5_SC_2_LiquidationContract</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Stabilizing the perpetual power dome</label>
        <text>Dr. Braun locks the Spark Resonator into its commercial siphon loop, drawing ambient energy from the Rift to power Vault-Tec's global vault network forever! Enraged by your brazen theft of cosmic power, the Railroad, Hell's demons, and Heaven's angels descend upon {COLONY}!</text>
        <days>0.6</days>
      </li>
      <!-- WAVE 1: 1.2x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_RailroadFaction</faction>
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
        <label>Power Loop at 45% — mercenaries hold the line</label>
        <text>Vault-Tec cargo pods drop fresh medical supplies and plasteel plates into your courtyard as Captain Wes reloads his rifle: "Railroad saboteurs are down—now watch the black smoke rolling in from the south!"</text>
        <choices>
          <li>
            <outcome>swan_already_dead</outcome>
            <label>[Swan Defeated] Lock all Gunner heavy turrets onto Hell's advancing demons</label>
            <resultText>With Swan already neutralized, no wild super mutants interfere—your mercenaries pour concentrated fire into the approaching demons!</resultText>
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
            <label>[Wild Horde Incoming] Emergency perimeter—Swan's feral behemoths are smashing the outer fences!</label>
            <resultText>Attracted by the Resonator's massive energy draw, Swan's wild super mutants storm your walls ahead of Hell's legion!</resultText>
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
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Wild mutants gunned down</label>
                <text>Gunner rocket batteries and heavy rifles shredded Swan's behemoths at the perimeter just before the demons struck!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
          </li>
        </outcomes>
      </li>
      <!-- WAVE 2B: HELL 1.6x -->
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
      <!-- BREATHER BEFORE WAVE 3 -->
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Power Loop at 90% — Heaven tries to pull the plug</label>
        <text>Sulfurous ash settles over spent Gunner shell casings! Now blinding shafts of light pierce the sky as Heaven's final strike team drops straight toward your server core to terminate the Consortium's siphon!</text>
        <days>0.4</days>
      </li>
      <!-- WAVE 3: HEAVEN 2.0x -->
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HeavenFaction</faction>
        <pointsFactor>2.0</pointsFactor>
        <delayHours>3</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Shadow Consortium holds!</label>
                <text>Mercenary firepower and Vault-Tec shields broke Heaven's strike team! Only one rival board member remains—time for a hostile buyout of Dick Roman!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Perpetual loop stabilized</label>
                <text>Despite scorched turrets, the Resonator's commercial loop locked in—strike Dick Roman's headquarters before he seizes controlling shares!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_q3Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- SC QUEST 4: CITADEL ASSAULT — PERPETUAL CYCLE -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_SC_4_PerpetualCycle</defName>
    <label>perpetual cycle: executive buyout</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_stanislaus_braun</giver>
    <fallbackGiver>TMM_Hero_captain_wes</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>ponr2=shadow_consortium</li>
      <li>done:TMM_Story_A5_SC_3_SiegeOfZeroAnchor</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Removing the Chairman of the Board</label>
        <text>Only one obstacle keeps {COLONY}, Vault-Tec, and the Gunners from owning 100% of Earth's new energy empire: Leviathan CEO Dick Roman refuses to step down and has fortified the Consortium Master Server Spire. Dr. Braun smiles brightly: "Be a dear and terminate Mr. Roman's contract permanently!"</text>
        <choices>
          <li>
            <outcome>borax_hostile_buyout</outcome>
            <label>[Borax Formula Unlocked] Issue industrial Borax sprayers to every Gunner stormtrooper</label>
            <resultText>Vault-Tec chemical canisters lock onto your squad's harnesses—Dick Roman's invulnerability will dissolve on contact!</resultText>
            <requires>
              <li>story.boraxFormula=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>800</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_boraxBuyout</flag><value>true</value></li>
            </effects>
          </li>
          <li>
            <outcome>gunner_firepower</outcome>
            <label>Storm the Master Server Spire with heavy mercenary firepower and explosives</label>
            <resultText>Captain Wes's elite squads breach the executive elevators. Remember: hit Roman with Borax to strip his supernatural armor!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineIndustrial</thing><count>8</count></li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_sc_boraxBuyout</flag><value>false</value></li>
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
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Hostile buyout complete!</label>
                <text>Screaming in a cloud of white Borax mist, Dick Roman collapsed across the mahogany boardroom table—transferring sole ownership of the Consortium network to {COLONY}!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Executive floor purged</label>
                <text>Vault-Tec remotely vented the Spire's halon and borax fire-suppression tanks, neutralizing Roman's boardroom before Gunner troops secured the servers.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Costly restructuring</label>
                <text>Though your initial strike took heavy casualties, Captain Wes's sniper teams locked down the penthouse until Roman's Leviathan guard folded.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Headquarters of the New World</label>
        <text>The fanatics, angels, demons, and monsters have all been priced out of existence. Hummed to life by the perpetual Spark Resonator, {COLONY} stands as the gleaming corporate capital of the Shadow Consortium—ruling the wasteland through Vaults, mercenary contracts, and an unbreakable monopoly on power.</text>
        <choices>
          <li>
            <outcome>launch_monopoly</outcome>
            <label>Ring the opening bell—the Shadow Consortium owns the future</label>
            <resultText>Golden bullion and trade caravans flow steadily toward {COLONY}'s gates. Business has never been better.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>1500</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.finaleDone</flag><value>shadow_consortium</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>


  <!-- ===================================================================== -->
  <!-- SHARED EPILOGUE PART 1: A NEW DAWN — FATE OF THE SPARK & MENTOR       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_Epilogue_EchoesOfTheSpark</defName>
    <label>a new dawn: fate of the spark</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>100</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_castiel</fallbackGiver>
    <requires>
      <li>act=5</li>
    </requires>
    <anyOf>
      <li>story.finaleDone=free_wasteland</li>
      <li>story.finaleDone=sterile_empire</li>
      <li>story.finaleDone=iron_crusade</li>
      <li>story.finaleDone=celestial_order</li>
      <li>story.finaleDone=dark_covenant</li>
      <li>story.finaleDone=shadow_consortium</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Silence after the Blood Eclipse</label>
        <text>Slowly, the blood-red shadow slips from the sun—or hardens into a permanent new sky—as the Spark Resonator completes its final planetary cycle. Across {COLONY}, weary survivors lay down their rifles and gather around the humming Zero Anchor to see the true price of victory.</text>
        <days>0.6</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>The Fate of the Zero Synth</label>
        <text>The central crystal of the Spark Resonator cools from blinding glare to a quiet glow. At the heart of everything—from the first night you opened the hatch beneath {COLONY} to the final siege—stood one impossible soul born in white corridors: the Zero Synth.</text>
        <choices>
          <li>
            <outcome>keeper_of_rift</outcome>
            <label>[Free Wasteland &amp; High Trust] Stand beside the Zero Synth—now the living Keeper of the Sealed Rift</label>
            <resultText>Warm blue light settles gently inside the Zero Synth's chest as the planetary barrier locks without harming their soul. They smile up at the clear sky: "We kept our promise. No cages, no masters—just a home worth fighting for." The Zero Synth remains forever at {COLONY}'s heart.</resultText>
            <requires>
              <li>ponr2=free_wasteland</li>
              <li>zeroSynth.trust>=30</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_zeroSynthFate</flag><value>keeper</value></li>
            </effects>
          </li>
          <li>
            <outcome>free_wasteland_wanderer</outcome>
            <label>[Free Wasteland &amp; Fractured Trust] Watch the Zero Synth pack a rucksack and head for the open road</label>
            <resultText>The Rift is sealed and the Zero Synth survives with their soul intact—yet the doubts between you never fully healed. Pausing at {COLONY}'s gate, they nod once: "Thank you for saving the world. Now I need to find out who I am beyond that machine."</resultText>
            <requires>
              <li>ponr2=free_wasteland</li>
              <li>zeroSynth.trust&lt;30</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_zeroSynthFate</flag><value>wanderer</value></li>
            </effects>
          </li>
          <li>
            <outcome>voluntary_sacrifice</outcome>
            <label>[High Trust on Harsh Path] Honor the Zero Synth's voluntary sacrifice to shield your colonists from the surge</label>
            <resultText>Even knowing what your coalition demanded, the Zero Synth stepped willingly into the Resonator's beam so its feedback wouldn't kill the friends they made in {COLONY}. Their last words echo softly in the blue light: "Make the world I bought worth the cost." Only a warm, silent crystal remains.</resultText>
            <requires>
              <li>zeroSynth.trust>=20</li>
              <li>!ponr2=free_wasteland</li>
              <li>!ponr2=dark_covenant</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_zeroSynthFate</flag><value>sacrifice</value></li>
            </effects>
          </li>
          <li>
            <outcome>consumed_by_order</outcome>
            <label>[Low Trust on Harsh Path] Record the expenditure of Subject Zero in the Resonator's log</label>
            <resultText>With no love lost between you, the Resonator drained the Zero Synth's Spark down to the last ember to lock your coalition's new order into place. Their eyes dimmed without a word, leaving behind an empty shell beside the roaring machine.</resultText>
            <requires>
              <li>zeroSynth.trust&lt;20</li>
              <li>!ponr2=free_wasteland</li>
              <li>!ponr2=dark_covenant</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_zeroSynthFate</flag><value>consumed</value></li>
            </effects>
          </li>
          <li>
            <outcome>dark_covenant_ash</outcome>
            <label>[Dark Covenant] Watch the Abyss devour the purest Spark ever born on Earth</label>
            <resultText>Chained above the fractured Seal, the Zero Synth's radiant soul flared once like a dying star before the Pit swallowed it whole—turning the Spark Resonator's crystal permanently blood-red in tribute to your dark empire.</resultText>
            <requires>
              <li>ponr2=dark_covenant</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_zeroSynthFate</flag><value>devoured</value></li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>The Missing Mentor</label>
        <text>From the very first day, one personal thread pulled {COLONY} into the heart of Project Purgatory: the mentor who vanished down the Zero Anchor shaft with the Holodisk-Key in 2287.</text>
        <choices>
          <li>
            <outcome>mentor_home</outcome>
            <label>[Mentor Rescued] Welcome your scarred, exhausted mentor back to {COLONY}'s hearth</label>
            <resultText>Greyer and lined by months inside Consortium stasis, your mentor sits quietly beside the dining room fire, setting a worn pre-war brass key onto the table. "I stole that disk hoping someone would finish what I couldn't," they say softly, looking around at your fortified settlement. "You actually survived the whole storm. I'm home."</resultText>
            <requires>
              <li>story.mentorRescued=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_epilogueMentor</flag><value>returned</value></li>
            </effects>
          </li>
          <li>
            <outcome>mentor_memorial</outcome>
            <label>[Mentor Lost] Lay your mentor's cracked Pip-Boy and ash-stained journal upon a memorial stone</label>
            <resultText>Though your mentor never lived to walk through {COLONY}'s gates again, their courage broke a two-hundred-year conspiracy wide open. You carve their name beside the Zero Anchor hatch—a quiet reminder of the debt that started it all.</resultText>
            <requires>
              <li>!story.mentorRescued=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_epilogueMentor</flag><value>memorial</value></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a5_epiloguePart1Done</flag><value>true</value></li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- SHARED EPILOGUE PART 2: A NEW DAWN — HEROES, WORLD & POSTGAME         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A5_Epilogue_NewDawn</defName>
    <label>a new dawn: what became of the world</label>
    <act>5</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>100</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_castiel</fallbackGiver>
    <requires>
      <li>act=5</li>
      <li>done:TMM_Story_A5_Epilogue_EchoesOfTheSpark</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Dispatches from a remade world</label>
        <text>Weeks pass after the final battle. Caravan couriers, long-range radio relays, and passing travelers carry stories back to {COLONY}—telling what became of the heroes, rivals, and factions whose fates collided around your Zero Anchor.</text>
        <days>0.6</days>
      </li>

      <!-- SLIDE 1: PALADIN DANSE -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Chronicle of Heroes: Paladin Danse</label>
        <text>Among the veterans who bled in the war for the Spark, few embodied the clash between steel and soul like Paladin Danse.</text>
        <choices>
          <li>
            <outcome>danse_defector</outcome>
            <label>[Recruited] Paladin Danse trains {COLONY}'s new generation of defenders</label>
            <resultText>Hanging up his old Brotherhood dog tags for good, Paladin Danse becomes the granite backbone of {COLONY}'s garrison—proving every day that honor is forged by choices, never by blood or circuitry.</resultText>
            <requires>
              <li>defector.TMM_Hero_paladin_danse=true</li>
            </requires>
          </li>
          <li>
            <outcome>danse_wanderer</outcome>
            <label>[Spared] Rumors speak of a lone knight in scarred T-60 armor protecting frontier farms</label>
            <resultText>Though Danse never marched permanently under your banner, caravan guards still whisper of a quiet knight in battered power armor who appears out of the fog whenever raiders threaten defenseless settlers.</resultText>
            <requires>
              <li>!defector.TMM_Hero_paladin_danse=true</li>
              <li>choice.brotherhood=pro_spark</li>
            </requires>
          </li>
          <li>
            <outcome>danse_fallen</outcome>
            <label>[Struck from the Rolls] Paladin Danse's name remains erased in Brotherhood archives</label>
            <resultText>In the cold vaults of the Brotherhood of Steel, Danse's service record stays sealed behind black ink—a stark monument to an order that feared its own humanity.</resultText>
            <requires>
              <li>!defector.TMM_Hero_paladin_danse=true</li>
              <li>!choice.brotherhood=pro_spark</li>
            </requires>
          </li>
        </choices>
      </li>

      <!-- SLIDE 2: CASTIEL -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Chronicle of Heroes: Castiel</label>
        <text>He was the first celestial messenger to knock at your gate when the Anchor awoke, wearing a rumpled beige trenchcoat and carrying the weight of a dying Heaven.</text>
        <choices>
          <li>
            <outcome>castiel_defector</outcome>
            <label>[Recruited] Castiel walks freely among {COLONY}'s settlers as guardian and friend</label>
            <resultText>Having chosen stubborn mortals over blind obedience, Castiel remains at {COLONY}—awkwardly tending beehives, straightening his crooked blue tie, and marveling at the messy resilience of free will.</resultText>
            <requires>
              <li>defector.TMM_Hero_castiel=true</li>
            </requires>
          </li>
          <li>
            <outcome>castiel_celestial</outcome>
            <label>[Celestial Order] Castiel serves Heaven's silent peace with shadowed eyes</label>
            <resultText>Restored to full grace beneath Archangel Michael's reign, Castiel patrols the spotless skies in dutiful silence, forever haunted by the mortal warmth lost to perfection.</resultText>
            <requires>
              <li>!defector.TMM_Hero_castiel=true</li>
              <li>ponr2=celestial_order</li>
            </requires>
          </li>
          <li>
            <outcome>castiel_legend</outcome>
            <label>[Faded into Legend] Sightings of an angel in a beige coat become campfire folklore</label>
            <resultText>With the Rift forever altered, Castiel vanishes into the vast quiet beyond the ash wastes—remembered only in quiet stories told by hunters beside the fire.</resultText>
            <requires>
              <li>!defector.TMM_Hero_castiel=true</li>
              <li>!ponr2=celestial_order</li>
            </requires>
          </li>
        </choices>
      </li>

      <!-- SLIDE 3: CROWLEY -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Chronicle of Heroes: Crowley</label>
        <text>Whether scheming over fine print or pouring thirty-year scotch in the middle of a shootout, the former King of the Crossroads always landed on his feet—almost.</text>
        <choices>
          <li>
            <outcome>crowley_defector</outcome>
            <label>[Recruited] Crowley runs an overpriced lounge in {COLONY} and takes credit for saving the world</label>
            <resultText>Having survived Lucifer's wrath behind your walls, Crowley settles into {COLONY}'s finest guest suite, criticizing the local whiskey and insisting to every trader that he single-handedly orchestrated your victory.</resultText>
            <requires>
              <li>defector.TMM_Hero_crowley=true</li>
            </requires>
          </li>
          <li>
            <outcome>crowley_dark_lord</outcome>
            <label>[Dark Covenant] Crowley raises a crystal glass from a throne of ash and brimstone</label>
            <resultText>With the Dark Covenant triumphant, Crowley presides over a booming underworld empire of soul contracts and twilight trade, forever raising a toast to his favorite mortal partners.</resultText>
            <requires>
              <li>!defector.TMM_Hero_crowley=true</li>
              <li>ponr2=dark_covenant</li>
            </requires>
          </li>
          <li>
            <outcome>crowley_exiled</outcome>
            <label>[Cut Off] Starved of fresh deals, Crowley slips into the forgotten crossroads</label>
            <resultText>Without access to Earth's souls or the Spark Resonator, the King of Hell vanishes into the dim margins of the wasteland, muttering creative curses at meddlesome settlers.</resultText>
            <requires>
              <li>!defector.TMM_Hero_crowley=true</li>
              <li>!ponr2=dark_covenant</li>
            </requires>
          </li>
        </choices>
      </li>

      <!-- SLIDE 4: JOHN HANCOCK & PRESTON GARVEY -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Chronicle of Heroes: John Hancock &amp; Preston Garvey</label>
        <text>From the neon alleys of Goodneighbor to the stone ramparts of the Castle, ordinary survivors looked to John Hancock and Preston Garvey to see what tomorrow would bring.</text>
        <choices>
          <li>
            <outcome>surface_free</outcome>
            <label>[Free Wasteland] Hancock and Garvey unite humans, ghouls, synths, and mutants under one banner</label>
            <resultText>In Goodneighbor, John Hancock pours drinks on the house while Brian Virgil's stabilizer saves generations of ghouls. Across the frontier, Preston Garvey raises the Minutemen flag over thriving free towns—for the first time, no settlement stands alone.</resultText>
            <requires>
              <li>ponr2=free_wasteland</li>
            </requires>
          </li>
          <li>
            <outcome>surface_sterile</outcome>
            <label>[Sterile Empire] Goodneighbor lies empty beneath white mist, and the frontier militia is dissolved</label>
            <resultText>The Sterilization Matrix swept away radiation and mutants alike. John Hancock's red coat rests in the ash of Goodneighbor, while Preston Garvey's battered colonial hat hangs behind glass in an Enclave museum of the "pacified wastes".</resultText>
            <requires>
              <li>ponr2=sterile_empire</li>
            </requires>
          </li>
          <li>
            <outcome>surface_iron</outcome>
            <label>[Iron Crusade] Goodneighbor keeps its shutters drawn beneath the shadow of Brotherhood airships</label>
            <resultText>Hancock quietly bars Goodneighbor's alleys against passing steel patrols, while Preston Garvey trades militia independence for Brotherhood protection—grateful the monsters are gone, yet wary of the price in freedom.</resultText>
            <requires>
              <li>ponr2=iron_crusade</li>
            </requires>
          </li>
          <li>
            <outcome>surface_celestial</outcome>
            <label>[Celestial Order] Rebel songs grow quiet under the hum of golden Enochian pylons</label>
            <resultText>Under Heaven's immaculate rule, Hancock's defiant speeches fade to whispers, and Preston Garvey watches peaceful, compliant townsfolk walk beneath angelic wings—praying this silent paradise is worth the chains.</resultText>
            <requires>
              <li>ponr2=celestial_order</li>
            </requires>
          </li>
          <li>
            <outcome>surface_dark</outcome>
            <label>[Dark Covenant] Goodneighbor burns in twilight, while Preston Garvey leads the last guerrilla hunters</label>
            <resultText>As nocturnal packs claim the ruins of Goodneighbor, Preston Garvey vanishes into the northern mountains with a handful of stubborn hunters—swearing that as long as one campfire burns, humanity has not surrendered.</resultText>
            <requires>
              <li>ponr2=dark_covenant</li>
            </requires>
          </li>
          <li>
            <outcome>surface_consortium</outcome>
            <label>[Shadow Consortium] Goodneighbor becomes a taxed trade franchise, and Garvey turns in his badge</label>
            <resultText>Hancock grimly signs a Vault-Tec franchise lease to keep Goodneighbor's doors open under Gunner security, while Preston Garvey hangs up his colonial hat rather than police corporate tollbooths.</resultText>
            <requires>
              <li>ponr2=shadow_consortium</li>
            </requires>
          </li>
        </choices>
      </li>

      <!-- SLIDE 5: WHAT BECAME OF THE WORLD -->
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>What Became of the World</label>
        <text>Two hundred years after October's nuclear ritual shattered the Sixty-Six Seals, the long night of Project Purgatory has finally reached its end. Look out from the walls of {COLONY} upon the new era your choices have forged.</text>
        <choices>
          <li>
            <outcome>world_free</outcome>
            <label>[Ending: The Free Wasteland] Clear skies over a hard-won, unbroken frontier</label>
            <resultText>The Rift is sealed forever. Heaven and Hell fade beyond reach, the Consortium lies in ruin, and humans, synths, ghouls, and friendly mutants share a rough but honest dawn—with {COLONY} celebrated as the beacon that set Earth free.</resultText>
            <requires>
              <li>ponr2=free_wasteland</li>
            </requires>
          </li>
          <li>
            <outcome>world_sterile</outcome>
            <label>[Ending: The Sterile Empire] Clean air, white laboratories, and iron human supremacy</label>
            <resultText>Radiation, mutations, and occult horrors have been scoured from the continent. Under the joint banners of the Enclave and the Institute, disciplined cities rise from the ruins—with {COLONY} standing at the apex of the new imperial order.</resultText>
            <requires>
              <li>ponr2=sterile_empire</li>
            </requires>
          </li>
          <li>
            <outcome>world_iron</outcome>
            <label>[Ending: The Iron Crusade] Magic extinguished behind walls of power armor and steel</label>
            <resultText>The continental dampener hums silently beneath {COLONY}, snuffing out supernatural rifts and rogue machines alike. Mankind marches into an austere century of steel under the reunited Brotherhood of Steel and Outcasts.</resultText>
            <requires>
              <li>ponr2=iron_crusade</li>
            </requires>
          </li>
          <li>
            <outcome>world_celestial</outcome>
            <label>[Ending: The Celestial Order] An immaculate, motionless paradise beneath golden wings</label>
            <resultText>War, monsters, and crime vanish beneath the combined dominion of Heaven's Host and the British Men of Letters. {COLONY} shines as the earthly seat of an eternal, unyielding peace.</resultText>
            <requires>
              <li>ponr2=celestial_order</li>
            </requires>
          </li>
          <li>
            <outcome>world_dark</outcome>
            <label>[Ending: The Dark Covenant] Eternal twilight and immortal dominion over the ash</label>
            <resultText>Earth merges with the shadows of Hell and Purgatory. Clad in dark power and immortal blood, the rulers of {COLONY} reign unchallenged as princes of an endless crimson night.</resultText>
            <requires>
              <li>ponr2=dark_covenant</li>
            </requires>
          </li>
          <li>
            <outcome>world_consortium</outcome>
            <label>[Ending: The Shadow Consortium] Vaults, contracts, and an unbreakable energy monopoly</label>
            <resultText>Zealots and monsters alike have been liquidated from the ledger. Powered by the perpetual Spark Resonator, {COLONY} rules the wasteland as the gleaming corporate capital of Vault-Tec and the Gunners.</resultText>
            <requires>
              <li>ponr2=shadow_consortium</li>
            </requires>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.storyComplete</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>6</value></li>
      <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>100</count></li>
      <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentSpacer</thing><count>5</count></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>The Chronicle Concludes</label>
        <text>Congratulations! You have completed the main chronicle of "Price of a Spark: Ash, Brimstone, and Conspiracy".\n\n{COLONY} now enters Act VI — Postgame! You are free to continue building your capital, promoting colonists through the ranks, crafting signature hero gear, and wielding your legendary arsenal in the new world you forged.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act5_Part2.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- ===================================================================== -->
  <!-- COALITION 4: CELESTIAL ORDER                                          -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_CO_1_HarvestOfSparks -->
  <TMM_Story_A5_CO_1_HarvestOfSparks.label>жатва искр</TMM_Story_A5_CO_1_HarvestOfSparks.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.label>Возрождение угасающей благодати</TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.text>Чтобы вернуть Небесному Воинству полную мощь до закрытия Разлома, архангелу Михаилу требуется духовный резонанс пробудившихся синтов третьего поколения, укрытых на перевалочной станции Подземки. Доктор Хесс передала енохианские фонари-сифоны для сбора их Искр.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.text>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.0.label>[Помощь перебежчиков] Позволить Кастиэлю или Мику Дэвису настроить фонари на щадящий сбор без убийства синтов</TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.0.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.0.resultText>Тайком от Михаила ваш перебежчик перестраивает енохианские фонари на съём лишь избыточного резонанса — спасая жизни синтов, но всё равно питая небесный маяк.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.0.resultText>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.1.label>Исполнить приказ Михаила без колебаний — выкачать каждую Искру на станции</TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.1.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.1.resultText>Золотые фонари вспыхивают ослепительным светом, пока ударная группа выступает к станции Подземки. Нулевой Синт смотрит вам вслед в немом ужасе.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.0.choices.1.resultText>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.0.effects.1.label>Небесные фонари заряжены</TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.0.effects.1.text>Ударная группа проломила оборону Подземки в бронеплащах и зарядила енохианские фонари до предела — крылья ангелов над {COLONY} уже сияют ярче!</TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.1.effects.0.label>Ангелы сами зачистили станцию</TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.1.effects.0.text>До прихода вашего отряда штурмовые ангелы Наоми сами обрушились на станцию и вернулись с частично заряженными фонарями Искр.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.2.effects.0.label>Удалось вынести часть благодати</TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.2.effects.0.text>Вынужденные отступить из засады Подземки, техники всё же вынесли два наполненных сифона — этого хватит, чтобы не сорвать график Михаила.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.label>Следующий этап: сеть контроля Лондона</TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.text>Когда благодать Небес вновь наполняет Резонатор, доктор Хесс разворачивает карту континента: Британские Просвещённые должны установить главный енохианский пилон на довоенной радиомачте, чтобы усмирить волю Пустоши.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.text>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.choices.0.label>Выдвинуть инженеров для запуска Сети Лондона</TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.choices.0.label>
  <TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.choices.0.resultText>Артур Кетч и серафимы Наоми встают в строй рядом с вашими бойцами, и конвой направляется к радиомачте.</TMM_Story_A5_CO_1_HarvestOfSparks.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_CO_2_LondonNetwork -->
  <TMM_Story_A5_CO_2_LondonNetwork.label>сеть лондона</TMM_Story_A5_CO_2_LondonNetwork.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.0.label>Монтаж главного енохианского шпиля</TMM_Story_A5_CO_2_LondonNetwork.stages.0.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.0.text>Оккультные архитекторы доктора Хесс смонтировали главный енохианский пилон подавления на высокой довоенной радиомачте. Винтокрылы Братства Стали уже летят снести шпиль с хребта, пока его умиротворяющее поле не накрыло регион.</TMM_Story_A5_CO_2_LondonNetwork.stages.0.text>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.0.effects.1.label>Сеть Лондона запущена</TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.0.effects.1.text>Восемь часов под лазерным огнём Братства Стали ваши стражи удерживали шпиль, пока над горизонтом не сомкнулся сияющий золотой купол енохианской вязи!</TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.1.effects.0.label>Запущен резервный пилон</TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.1.effects.0.text>Пока паладины Братства сражались с ложными целями Просвещённых, Артур Кетч активировал подземный резервный ретранслятор.</TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.2.effects.0.label>Гармоническое ядро эвакуировано</TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.2.effects.0.text>Сбитые с хребта артиллерией Братства, оперативники всё же сняли золотое ядро ретранслятора для прямого монтажа в {COLONY}.</TMM_Story_A5_CO_2_LondonNetwork.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.2.label>Пленник в подвале Лондона</TMM_Story_A5_CO_2_LondonNetwork.stages.2.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.2.text>В запертом допросном крыле под радиомачтой отряд совершает потрясающую находку: в стазис-камере под печатями сидит ваш пропавший наставник, схваченный чистильщиками Просвещённых несколько недель назад. Доктор Хесс холодно чеканит по интеркому: «Этот человек знает слишком много о Проекте „Чистилище“. Оставьте его под нашей стражей».</TMM_Story_A5_CO_2_LondonNetwork.stages.2.text>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.0.label>Ослушаться доктора Хесс, разбить печати камеры и забрать наставника в {COLONY}</TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.0.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.0.resultText>Скулы доктора Хесс каменеют, но она не смеет перечить вам, пока Резонатор в ваших руках. Измождённый наставник крепко сжимает ваше плечо, когда отряд выводит его из подвала!</TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.0.resultText>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.1.label>Оставить пленника Лондону — ничто не должно угрожать Небесному Порядку</TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.1.label>
  <TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.1.resultText>Тяжёлая стальная дверь захлопывается, заглушая тихую мольбу наставника. Доктор Хесс вознаграждает ваше безусловное послушание сундуком довоенного золота.</TMM_Story_A5_CO_2_LondonNetwork.stages.2.choices.1.resultText>

  <!-- TMM_Story_A5_CO_3_SiegeOfZeroAnchor -->
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.label>осада нулевого якоря</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.0.label>Небесный маяк зажигается</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.0.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.0.text>Столп ослепительного золотого огня бьёт из Резонатора Искр {COLONY} в багровые тучи, призывая легионы Небес обратно на Землю. Отчаянно пытаясь остановить Небесный Порядок, пока свобода воли не скована навсегда, охотники, винтокрылы Анклава и орды Ада сходятся у ваших стен!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.0.text>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.label>Небесный Столп 45% — гром на всех горизонтах</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.text>Ангелы-курьеры сбрасывают во двор передовые аптечки и боеприпасы Просвещённых, пока авангард охотников откатывается назад. За пепельным хребтом ревут плазменные турбины Анклава — и земля вздрагивает под тяжёлой поступью.</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.text>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.0.label>[Лебедь повержен] Направить все енохианские орудия на наступающий авангард Анклава</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.0.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>Поскольку Лебедь уже мёртв, дикие племена супермутантов отсиживаются в холмах — гарнизон встречает Анклав полным залпом!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.1.label>[Дикая орда на подходе] Бить в набат — бегемоты Лебедя врываются в сражение!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.1.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>Ослеплённая и взбешённая золотым столпом света, орда Лебедя врезается в ваши баррикады раньше десанта Анклава!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>Орда бегемотов очищена огнём</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>Святой огонь и рунические залпы скосили наступающих бегемотов у самых стен, когда небо затянули винтокрылы Анклава!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.5.label>Небесный Столп 90% — сера разрывает землю</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.5.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.5.text>Чёрная броня Анклава дымится во дворе! Теперь земля за периметром трескается в фонтанах серы: последний легион Ада бросается в отчаянную атаку, пытаясь погасить свет Небес!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.5.text>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>Небесный Порядок устоял!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>Залив поле боя ослепительным белым светом, гарнизон сокрушил легионы Ада! Остался лишь сам Денница — выдвигайтесь на штурм Серной Цитадели!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>Золотой столп замкнут</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>
  <TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>Сквозь едкий дым и пробитые ворота Резонатор завершил призыв Небес — ударьте по твердыне Люцифера, пока он не сорвал печать!</TMM_Story_A5_CO_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>

  <!-- TMM_Story_A5_CO_4_AscensionOfTheHost -->
  <TMM_Story_A5_CO_4_AscensionOfTheHost.label>восхождение воинства: падение денницы</TMM_Story_A5_CO_4_AscensionOfTheHost.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.label>Последняя битва братьев</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.text>В пылающих руинах Серной Цитадели Люцифер вырвался из Клетки, собрав последних демонов Ада и наёмных левиафанов, чтобы обрушить вновь зажжённые Небеса. Архангел Михаил обнажает золотой Клинок Архангела, а доктор Хесс взводит «Уравнитель».</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.text>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.0.label>[Формула буры готова] Вооружить штурмовое воинство святым маслом, енохианскими пулями и бурой</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.0.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.0.resultText>Освящённые обереги и канистры буры не оставят ни демонам Люцифера, ни его страже левиафанов ни единого шанса!</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.0.resultText>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.1.label>Спуститься в столпах небесного огня прямо в тронный зал Люцифера</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.1.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.1.resultText>Хлопок крыльев гремит подобно грому, когда ударный отряд высаживается в сердце Серной Цитадели, чтобы окончить древнейшую войну мироздания.</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.0.choices.1.resultText>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.0.effects.1.label>Люцифер низвергнут обратно в Бездну!</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.0.effects.1.text>В оглушительной вспышке белых и багровых молний Люцифер рухнул на колени — и золотые цепи Небес утащили его обратно в запечатанную Клетку навеки!</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.1.effects.0.label>Цитадель испепелена небесным огнём</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.1.effects.0.text>Архангел Михаил обрушил с Резонатора столп очищающего пламени, похоронив Серную Цитадель и запаяв разлом преисподней остекленевшей породой.</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.2.effects.0.label>Пиррова победа над Бездной</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.2.effects.0.text>Хотя авангард понёс тяжёлые потери в серном дыму, рунические заряды Просвещённых обрушили тронный зал и обезглавили силы Ада.</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.label>Торжество Небесного Порядка</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.text>Сияющий безоблачный свет разливается над континентом. Рейды, чудовища и хаос исчезают под мерным гулом золотых пилонов Сети Лондона и зорким взглядом небесного воинства — безупречный, безмолвный рай, купленный ценой свободы воли.</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.text>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.choices.0.label>Вострубить в серебряные трубы — Небесный Порядок правит Землёй в вечной тишине</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.choices.0.label>
  <TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.choices.0.resultText>Ангелы патрулируют безупречное небо над {COLONY}. Война окончена; наступил абсолютный покой.</TMM_Story_A5_CO_4_AscensionOfTheHost.stages.2.choices.0.resultText>


  <!-- ===================================================================== -->
  <!-- COALITION 5: DARK COVENANT                                            -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_DC_1_BloodTithe -->
  <TMM_Story_A5_DC_1_BloodTithe.label>кровавая десятина</TMM_Story_A5_DC_1_BloodTithe.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.0.label>Взлом хранилища душ Волт-Тек</TMM_Story_A5_DC_1_BloodTithe.stages.0.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.0.text>Чтобы разорвать последнюю Печать и затопить Землю мощью Ада и Чистилища, вашим тёмным союзникам нужны довоенные накопители душ из резервного бункера Волт-Тек. Левиафаны Дика Романа и стая Альфа-вампира готовы взломать гермодверь рядом с вами.</TMM_Story_A5_DC_1_BloodTithe.stages.0.text>
  <TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.0.label>Усилить отряд кровавой алхимией Альфа-вампира и биоферритовыми амулетами</TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.0.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.0.resultText>Багровый туман вьётся вокруг клинков бойцов, а хищный голод обостряет их реакцию перед штурмом Убежища.</TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.0.resultText>
  <TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.1.label>Призвать серных подрывников, чтобы прожечь стальные переборки Волт-Тек насквозь</TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.1.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.1.resultText>Демоны перекрёстка пропитывают заряды адской серой — ни одна довоенная дверь не задержит ваш авангард.</TMM_Story_A5_DC_1_BloodTithe.stages.0.choices.1.resultText>
  <TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.0.effects.1.label>Накопители Убежища разграблены</TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.0.effects.1.text>Смяв отряды охраны Волт-Тек, ваш тёмный легион вырвал золотые накопители душ из центрального ядра и доставил их в {COLONY}!</TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.1.effects.0.label>Стая левиафанов опустошила бункер</TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.1.effects.0.text>До прихода вашего отряда штурмовики Дика Романа пролезли через вентиляцию и сами положили украденные накопители к вашему алтарю.</TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.2.effects.0.label>Отход сквозь огонь с десятиной</TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.2.effects.0.text>Хотя турели Убежища изрядно потрепали авангард, Мэг вынырнула из чёрного дыма, сжимая в руках главную матрицу.</TMM_Story_A5_DC_1_BloodTithe.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_DC_1_BloodTithe.stages.2.label>Знакомое лицо в криохранилище</TMM_Story_A5_DC_1_BloodTithe.stages.2.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.2.text>За криогенной переборкой бойцы находят вашего пропавшего наставника в работающей капсуле Волт-Тек — того самого человека, что выкрал Нулевого Синта, пытаясь остановить этот кошмар. Мэг мрачно ухмыляется у пульта: «Гляди, кого мы нашли, босс. Заберём его посмотреть на твою коронацию или скормим капсулу Бездне?»</TMM_Story_A5_DC_1_BloodTithe.stages.2.text>
  <TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.0.label>Вскрыть стазис-капсулу и забрать наставника в {COLONY} живым</TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.0.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.0.resultText>Даже на самой тёмной тропе не все узы рвутся легко. Потрясённого наставника бережно несут к транспорту, пока Убежище полыхает за спиной.</TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.0.resultText>
  <TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.1.label>Отречься от прошлого — слить энергию криобанка прямо в алтарь Резонатора</TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.1.label>
  <TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.1.resultText>Багровая молния пронзает криозал, обращая в пепел последние следы прежней жизни. Преисподняя рукоплещет вашей безжалостной верности.</TMM_Story_A5_DC_1_BloodTithe.stages.2.choices.1.resultText>

  <!-- TMM_Story_A5_DC_2_FallOfHeavensGates -->
  <TMM_Story_A5_DC_2_FallOfHeavensGates.label>погашенное серебро</TMM_Story_A5_DC_2_FallOfHeavensGates.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.0.label>Охота за «Кольтом»</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.0.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.0.text>На Земле осталось лишь одно смертное оружие, способное угрожать владыкам вашего союза: «Кольт», с которым Дин Винчестер собирает последних охотников и Минитменов в укреплённом мотеле у шоссе. Дик Роман ослепительно улыбается: «Уберите мистера Винчестера с доски, и сопротивление людей рухнет за одну ночь».</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.0.text>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.0.effects.1.label>Чемпион охотников пал</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.0.effects.1.text>У дымящегося капота побитой чёрной машины Дин Винчестер отстреливался до последней серебряной пули, прежде чем пасть под натиском вашего тёмного авангарда — боевой дух охотников континента сломлен!</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.1.effects.0.label>Укрепление брошено в огне</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.1.effects.0.text>Ночные стаи Альфа-вампира сожгли мотель до вашего прихода, разогнав уцелевших охотников по холмам.</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.2.effects.0.label>Кровавая засада у дороги</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.2.effects.0.text>Каменная соль и артиллерия потрепали ударный отряд, но подошедшие демоны следом сравняли передовой лагерь охотников с землёй.</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.label>Крестоносцы на горизонте</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.text>Когда охотники разбиты, между {COLONY} и вечными сумерками стоит лишь последний отчаянный крестовый поход Британских Просвещённых, Братства Стали и воинства архангела Михаила. Резонатор Искр жадно пьёт багровые молнии Затмения!</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.text>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.choices.0.label>Спустить адских гончих и занять стены — пусть крестоносцы разобьются о Бездну!</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.choices.0.label>
  <TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.choices.0.resultText>На стенах пылают серные костры, пока тёмные союзники стягиваются вокруг Резонатора.</TMM_Story_A5_DC_2_FallOfHeavensGates.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_DC_3_SiegeOfZeroAnchor -->
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.label>осада нулевого якоря</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.0.label>Взлом Шестьдесят Шестой Печати</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.0.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.0.text>Резонатор Искр воет неестественным багровым аккордом — Шестьдесят Шестая Печать идёт трещинами! Чуя конец власти смертных, Британские Просвещённые, Братство Стали и последнее воинство Небес обрушивают на {COLONY} объединённый крестовый штурм!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.0.text>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.label>Разлом Печати 45% — сталь и рёв на хребте</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.text>Старейшины вампиров и демоны перекрёстка подносят на стены трофейные боеприпасы и пакеты крови поверх павших британских оперативников. Сквозь багровый дым снижаются винтокрылы Братства — а ворота содрогаются от тяжёлой поступи.</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.text>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.0.label>[Лебедь повержен] Встретить паладинов Братства адским пламенем и когтями</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.0.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>Поскольку Лебедь уже мёртв, дикие супермутанты не суются в бой — переведите весь огонь на десант рыцарей Братства!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.1.label>[Дикая орда на подходе] Спустить стаю — бегемоты Лебедя ломятся сорвать ритуал!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.1.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>Обезумевшие от запаха серы, дикие супермутанты Лебедя таранят внешние стены раньше Братства Стали!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>Дикие мутанты поглощены ночью</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>Демоны и ночные кланы растерзали обезумевших супермутантов, мгновенно развернувшись навстречу бронедесанту Братства!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.5.label>Разлом Печати 90% — звучит последняя труба Небес</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.5.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.5.text>Разбитая силовая броня Братства остывает в лужах чёрного ихора! Теперь ослепительные белые молнии рассекают багровый свод — Небеса бросают в бой последних серафимов, чтобы стереть {COLONY} с лица Земли!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.5.text>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>Крестовый поход сокрушён!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>Опалённые перья кружатся над двором словно снег — последний штурм Небес захлебнулся! Теперь ворвитесь в Небесный Собор и прикончите архангела Михаила!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>Бездна ширится</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>
  <TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>Хотя небесный огонь опалил внутренние стены, Шестьдесят Шестая Печать треснула — добейте архангела Михаила, пока Небеса не затянули рану!</TMM_Story_A5_DC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>

  <!-- TMM_Story_A5_DC_4_ShatteredAbyss -->
  <TMM_Story_A5_DC_4_ShatteredAbyss.label>распахнутая бездна: падение архангела</TMM_Story_A5_DC_4_ShatteredAbyss.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.label>Штурм Небесного Собора</TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.text>Архангел Михаил стоит в одиночестве среди руин Небесного Собора, отдавая последние искры благодати, чтобы удержать Разлом. Убейте его — и грань между Землёй, Адом и Чистилищем растворится навсегда во славу Тёмного Завета.</TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.text>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.0.label>[Метка Каина пробуждена] Влить первородную ярость Первого Клинка в ударный отряд</TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.0.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.0.resultText>Метка Каина вспыхивает алым огнём на руке вашего чемпиона, и тёмная мощь заливает штурмовиков — перед таким клинком истечёт кровью даже архангел!</TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.0.resultText>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.1.label>Обрушить на собор всю объединённую мощь демонов Ада и ночных кланов</TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.1.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.1.resultText>Чёрный дым и хищный вой врываются в распахнутые врата собора, пока тёмная коалиция штурмует последнее святилище Михаила.</TMM_Story_A5_DC_4_ShatteredAbyss.stages.0.choices.1.resultText>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.0.effects.1.label>Архангел пал!</TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.0.effects.1.text>С раскатом грома, выбившим витражи на мили вокруг, архангел Михаил рухнул под ударами вашего тёмного авангарда — и небо над Землёй навсегда застыло в багровых сумерках!</TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.1.effects.0.label>Собор поглощён серой</TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.1.effects.0.text>Под полом собора разверзлись адские расщелины, поглотив святилище Михаила в серном пламени и оборвав последнюю нить Небес.</TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.2.effects.0.label>Тьма поглощает руины</TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.2.effects.0.text>Хотя клинок Михаила собрал кровавую жатву среди авангарда, старейшины Альфа-вампира и демоны добили обессилевшего архангела мгновение спустя.</TMM_Story_A5_DC_4_ShatteredAbyss.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.label>Владыки Вечных Сумерек</TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.text>Последняя Печать рассыпается в рёве ликующего адского огня. Серный туман и вечные сумерки окутывают континент, пока демоны, вампиры и левиафаны склоняются перед {COLONY} — новыми князьями тёмной, бессмертной империи на пепле старого мира.</TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.text>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.choices.0.label>Взойти на пепельный трон — Тёмный Завет правит Пустошью навеки</TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.choices.0.label>
  <TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.choices.0.resultText>Багровые огни пылают вдоль горизонта. Небеса пали; эта ночь принадлежит вам.</TMM_Story_A5_DC_4_ShatteredAbyss.stages.2.choices.0.resultText>


  <!-- ===================================================================== -->
  <!-- COALITION 6: SHADOW CONSORTIUM                                        -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_SC_1_HostileTakeover -->
  <TMM_Story_A5_SC_1_HostileTakeover.label>враждебное поглощение</TMM_Story_A5_SC_1_HostileTakeover.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.0.label>Монополизация региональной энергосети</TMM_Story_A5_SC_1_HostileTakeover.stages.0.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.0.text>Чтобы превратить Резонатор Искр {COLONY} в абсолютную коммерческую монополию на энергию, доктору Брону нужно захватить передовой термоядерный узел Братства Стали целым. Капитан Уэс уже вывел боевые отряды Стрелков для блокады периметра.</TMM_Story_A5_SC_1_HostileTakeover.stages.0.text>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.0.label>Задействовать довоенные коды доступа и дронов Волт-Тек для взлома турелей узла</TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.0.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.0.resultText>Доктор Брон довольно хихикает, когда турели Братства на миг глохнут от старых сервисных ключей Волт-Тек, открывая бойцам чистый проход.</TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.0.resultText>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.1.label>Оплатить артподготовку тяжёлых расчётов капитана Уэса перед штурмом</TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.1.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.1.resultText>Ракетчики Стрелков перепахивают укрепления Братства и сбрасывают вашему отряду тяжёлые бронепластины.</TMM_Story_A5_SC_1_HostileTakeover.stages.0.choices.1.resultText>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.0.effects.1.label>Термоядерный узел перешёл в собственность</TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.0.effects.1.text>Удержав узел сквозь восемь часов контратак Братства, ваши наёмники перевели всю энергосеть на центральный баланс Волт-Тек!</TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.1.effects.0.label>Резерв Стрелков закрыл сделку</TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.1.effects.0.text>Резервный батальон капитана Уэса прорвался через чёрный ход и перенаправил урановые магистрали прямо в {COLONY}.</TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.2.effects.0.label>Врезка выполнена под землёй</TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.2.effects.0.text>Отброшенные с поверхности рыцарями Братства, техники Волт-Тек всё равно поставили подземный отвод на главный силовой кабель узла.</TMM_Story_A5_SC_1_HostileTakeover.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.2.label>Следующая цель: конкуренты на поверхности</TMM_Story_A5_SC_1_HostileTakeover.stages.2.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.2.text>Когда региональная энергосеть в вашем кулаке, капитан Уэс кладёт на стол следующий контракт: зачистить главный логистический узел Минитменов и охотников, пока они не подняли вольные поселения против вашей монополии.</TMM_Story_A5_SC_1_HostileTakeover.stages.2.text>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.2.choices.0.label>Подписать приказ на ликвидацию и выдвинуть штурмовые группы Стрелков</TMM_Story_A5_SC_1_HostileTakeover.stages.2.choices.0.label>
  <TMM_Story_A5_SC_1_HostileTakeover.stages.2.choices.0.resultText>Рации наёмников сухо щёлкают в знак подтверждения, и ударная колонна выдвигается к узлу сопротивления.</TMM_Story_A5_SC_1_HostileTakeover.stages.2.choices.0.resultText>

  <!-- TMM_Story_A5_SC_2_LiquidationContract -->
  <TMM_Story_A5_SC_2_LiquidationContract.label>контракт на зачистку</TMM_Story_A5_SC_2_LiquidationContract.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.0.label>Блокада узла сопротивления</TMM_Story_A5_SC_2_LiquidationContract.stages.0.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.0.text>Разведчики Бейкера окружили укреплённый перекрёсток Минитменов и охотников — и заметили в их лазарете неожиданный сюрприз: вашего пропавшего наставника, которого недавно отбили у конвоя Консорциума люди Престона Гарви.</TMM_Story_A5_SC_2_LiquidationContract.stages.0.text>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.0.effects.1.label>Узел сопротивления ликвидирован</TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.0.effects.1.text>Перекрёстный огонь Стрелков и снайперы Бейкера сокрушили баррикады ополчения, передав перекрёсток в руки Теневого Консорциума!</TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.1.effects.0.label>Ополченцы выбиты с трассы</TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.1.effects.0.text>Главная колонна капитана Уэса зачистила перекрёсток раньше срока, захватив склады охотников при отходе ополчения.</TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.2.effects.0.label>Узел сметён миномётами</TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.2.effects.0.text>Отбитые яростным сопротивлением охотников, миномётные расчёты Стрелков разнесли укрепления перекрёстка с дальней дистанции.</TMM_Story_A5_SC_2_LiquidationContract.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.2.label>Эвакуация актива в лазарете</TMM_Story_A5_SC_2_LiquidationContract.stages.2.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.2.text>В дыму захваченного узла ваш пропавший наставник стоит безоружным в лазарете, с неверием глядя на ваши наёмнические шевроны. Капитан Уэс передёргивает затвор: «Ваше слово, босс. Эвакуируем VIP в {COLONY} или сжигаем здание без лишних хвостов?»</TMM_Story_A5_SC_2_LiquidationContract.stages.2.text>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.0.label>Приказать Уэсу опустить оружие и безопасно сопроводить наставника в {COLONY}</TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.0.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.0.resultText>«Принято — оформляем эвакуацию VIP». Хотя наставник смотрит на ваших корпоративных союзников с горьким разочарованием, он доживёт до рассвета.</TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.0.resultText>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.1.label>Никаких хвостов — запереть здание и сжечь все архивы внутри</TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.1.label>
  <TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.1.resultText>Зажигательные гранаты охватывают деревянные стены пламенем. Доктор Брон переводит на ваш счёт щедрую премию.</TMM_Story_A5_SC_2_LiquidationContract.stages.2.choices.1.resultText>

  <!-- TMM_Story_A5_SC_3_SiegeOfZeroAnchor -->
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.label>осада нулевого якоря</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.0.label>Стабилизация вечного энергокупола</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.0.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.0.text>Доктор Брон замыкает Резонатор Искр на коммерческий контур откачки, вытягивая фоновую энергию Разлома для вечного питания сети Убежищ Волт-Тек! Взбешённые наглой кражей космической силы, Подземка, демоны Ада и ангелы Небес обрушиваются на {COLONY}!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.0.text>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.label>Энергоконтур 45% — наёмники держат строй</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.text>Грузовые капсулы Волт-Тек сбрасывают во двор свежие медикаменты и пласталь, пока капитан Уэс меняет магазин: «Диверсанты Подземки легли — теперь глядите на чёрный дым с юга!»</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.text>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.0.label>[Лебедь повержен] Навести тяжёлые турели Стрелков на наступающих демонов Ада</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.0.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>Поскольку Лебедь уже устранён, дикие супермутанты не мешаются под ногами — наёмники встречают демонов шквальным огнём!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.0.resultText>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.1.label>[Дикая орда на подходе] Тревога по периметру — бегемоты Лебедя крушат ограждения!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.1.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>Привлечённые колоссальной тягой Резонатора, дикие супермутанты Лебедя таранят стены раньше легиона Ада!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.2.choices.1.resultText>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>Дикие мутанты расстреляны у стен</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>Ракетные батареи и пулемёты Стрелков изрешетили бегемотов Лебедя на подступах за минуту до удара демонов!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.3.outcomes.0.effects.1.text>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.5.label>Энергоконтур 90% — Небеса пытаются выдернуть вилку</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.5.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.5.text>Серный пепел оседает поверх россыпей гильз Стрелков! Теперь ослепительные столпы света пробивают тучи — последняя ударная группа Небес падает прямо на серверное ядро, чтобы оборвать сифон Консорциума!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.5.text>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>Теневой Консорциум выстоял!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>Огневая мощь наёмников и щиты Волт-Тек разбили десант Небес! Остался лишь один неуступчивый член совета директоров — пора провести враждебный выкуп Дика Романа!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.0.effects.0.text>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>Вечный цикл стабилизирован</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.label>
  <TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>Несмотря на опалённые турели, коммерческий контур Резонатора замкнулся — атакуйте штаб Дика Романа, пока он не перехватил контрольный пакет!</TMM_Story_A5_SC_3_SiegeOfZeroAnchor.stages.6.outcomes.1.effects.0.text>

  <!-- TMM_Story_A5_SC_4_PerpetualCycle -->
  <TMM_Story_A5_SC_4_PerpetualCycle.label>запуск вечного цикла: смена председателя</TMM_Story_A5_SC_4_PerpetualCycle.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.0.label>Смещение Председателя Совета</TMM_Story_A5_SC_4_PerpetualCycle.stages.0.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.0.text>Осталось лишь одно препятствие, мешающее {COLONY}, Волт-Тек и Стрелкам завладеть 100% акций новой энергетической империи: глава левиафанов Дик Роман отказывается уходить и укрепился в Главной Серверной Башне Консорциума. Доктор Брон лучезарно улыбается: «Будьте душкой, расторгните контракт мистера Романа окончательно!»</TMM_Story_A5_SC_4_PerpetualCycle.stages.0.text>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.0.label>[Формула буры открыта] Выдать каждому штурмовику Стрелков промышленный распылитель буры</TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.0.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.0.resultText>Химические канистры Волт-Тек закрепляются на разгрузках бойцов — неуязвимость Дика Романа растворится при первом же контакте!</TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.0.resultText>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.1.label>Штурмовать Главную Серверную Башню тяжёлой огневой мощью наёмников</TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.1.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.1.resultText>Элитные отряды капитана Уэса вскрывают лифты правления. Помните: против Романа необходима бура!</TMM_Story_A5_SC_4_PerpetualCycle.stages.0.choices.1.resultText>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.0.effects.1.label>Враждебный выкуп завершён!</TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.0.effects.1.text>Захлёбываясь в облаке белой буры, Дик Роман рухнул прямо на стол из красного дерева — передавая единоличный контроль над сетью Консорциума в руки {COLONY}!</TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.1.effects.0.label>Этаж правления зачищен дистанционно</TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.1.effects.0.text>Волт-Тек удалённо открыла в зале заседаний пожарные форсунки с галоном и бурой, нейтрализовав Романа до входа наёмников в серверную.</TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.2.effects.0.label>Дорогая реструктуризация</TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.2.effects.0.text>Хотя первый штурм стоил немалой крови, снайперы капитана Уэса заблокировали пентхаус, пока охрана Романа не сложила оружие.</TMM_Story_A5_SC_4_PerpetualCycle.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.2.label>Штаб-квартира Нового Мира</TMM_Story_A5_SC_4_PerpetualCycle.stages.2.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.2.text>Фанатики, ангелы, демоны и чудовища списаны со счетов как нерентабельные активы. Питаемая вечным Резонатором Искр, {COLONY} возвышается как сверкающая корпоративная столица Теневого Консорциума — управляющая Пустошью через Убежища, контракты и нерушимую монополию на энергию.</TMM_Story_A5_SC_4_PerpetualCycle.stages.2.text>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.2.choices.0.label>Ударить в биржевой колокол — будущее принадлежит Теневому Консорциуму</TMM_Story_A5_SC_4_PerpetualCycle.stages.2.choices.0.label>
  <TMM_Story_A5_SC_4_PerpetualCycle.stages.2.choices.0.resultText>Золотые слитки и торговые караваны неиссякаемым потоком тянутся к воротам {COLONY}. Дела ещё никогда не шли так хорошо.</TMM_Story_A5_SC_4_PerpetualCycle.stages.2.choices.0.resultText>


  <!-- ===================================================================== -->
  <!-- SHARED EPILOGUE PART 1: ECHOES OF THE SPARK                           -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_Epilogue_EchoesOfTheSpark -->
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.label>новый рассвет: судьба искры</TMM_Story_A5_Epilogue_EchoesOfTheSpark.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.0.label>Тишина после Кровавого затмения</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.0.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.0.text>Медленно багровая тень сходит с солнца — или навсегда застывает в новом небе — пока Резонатор Искр завершает последний планетарный цикл. По всей {COLONY} измотанные выжившие опускают оружие и собираются вокруг гудящего Нулевого Якоря, чтобы увидеть истинную цену победы.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.0.text>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.label>Судьба Нулевого Синта</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.text>Центральный кристалл Резонатора Искр остывает от слепящей вспышки до тихого свечения. В самом сердце всех событий — от первой ночи у люка под {COLONY} до последней осады — стояла одна невозможная душа, рождённая в белых коридорах: Нулевой Синт.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.text>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.0.label>[Свободная Пустошь и высокое доверие] Встать рядом с Нулевым Синтом — живым Хранителем Запечатанного Разлома</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.0.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.0.resultText>Тёплый голубой свет мягко оседает в груди Нулевого Синта, пока планетарный барьер замыкается без вреда для его души. Он улыбается чистому небу: «Мы сдержали слово. Ни клеток, ни хозяев — только дом, за который стоило сражаться». Нулевой Синт навсегда остаётся сердцем {COLONY}.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.0.resultText>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.1.label>[Свободная Пустошь и трещина в доверии] Проводить Нулевого Синта, уходящего с рюкзаком на вольную дорогу</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.1.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.1.resultText>Разлом запечатан, и Нулевой Синт выжил с нетронутой душой — но сомнения между вами так и не затянулись до конца. Остановившись у ворот {COLONY}, он коротко кивает: «Спасибо, что спасли мир. Теперь мне нужно узнать, кто я без этой машины».</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.1.resultText>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.2.label>[Высокое доверие на суровом пути] Почтить добровольную жертву Нулевого Синта ради спасения ваших колонистов</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.2.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.2.resultText>Даже зная, чего требует ваша коалиция, Нулевой Синт сам шагнул в луч Резонатора, чтобы отдача машины не убила его друзей в {COLONY}. Его последние слова тихо отзываются в голубом сиянии: «Сделайте так, чтобы мир, который я купил, стоил этой цены». В зале остаётся лишь тёплый молчаливый кристалл.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.2.resultText>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.3.label>[Низкое доверие на суровом пути] Зафиксировать расход «Образца Ноль» в журнале Резонатора</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.3.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.3.resultText>Без тёплых слов и сожалений Резонатор выкачал Искру Нулевого Синта до последней крупицы, скрепляя новый порядок вашей коалиции. Его глаза погасли в молчании, оставив у гудящей машины лишь пустую оболочку.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.3.resultText>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.4.label>[Тёмный Завет] Смотреть, как Бездна поглощает чистейшую Искру, когда-либо рождённую на Земле</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.4.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.4.resultText>Прикованная над расколотой Печатью, сияющая душа Нулевого Синта в последний раз вспыхнула подобно умирающей звезде, прежде чем Бездна проглотила её целиком — навсегда окрасив кристалл Резонатора в кроваво-алый цвет вашей тёмной империи.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.1.choices.4.resultText>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.label>Пропавший наставник</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.text>С самого первого дня одна личная нить вела {COLONY} в сердце Проекта «Чистилище»: наставник, исчезнувший в шахте Нулевого Якоря с Голодиском-Ключом в 2287 году.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.text>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.0.label>[Наставник спасён] Встретить израненного, поседевшего наставника у очага {COLONY}</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.0.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.0.resultText>Поседевший после долгих месяцев в стазисе Консорциума, ваш наставник тихо сидит у огня в столовой и кладёт на стол потёртый довоенный латунный ключ: «Я украл тот диск в надежде, что кто-то закончит начатое, — говорит он, оглядывая вашу окрепшую крепость. — Вы правда пережили всю эту бурю. Я дома».</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.0.resultText>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.1.label>[Наставник погиб] Положить треснувший Пип-Бой и опалённый дневник наставника на памятный камень</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.1.label>
  <TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.1.resultText>Хотя наставнику не суждено было снова переступить порог {COLONY}, его отвага взломала двухсотлетний заговор. Вы высекаете его имя рядом с люком Нулевого Якоря — тихое напоминание о долге, с которого всё началось.</TMM_Story_A5_Epilogue_EchoesOfTheSpark.stages.2.choices.1.resultText>


  <!-- ===================================================================== -->
  <!-- SHARED EPILOGUE PART 2: NEW DAWN — HEROES, WORLD & POSTGAME           -->
  <!-- ===================================================================== -->

  <!-- TMM_Story_A5_Epilogue_NewDawn -->
  <TMM_Story_A5_Epilogue_NewDawn.label>новый рассвет: каким стал мир</TMM_Story_A5_Epilogue_NewDawn.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.0.label>Вести из перекроенного мира</TMM_Story_A5_Epilogue_NewDawn.stages.0.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.0.text>После последней битвы проходят недели. Караванные курьеры, дальние радиоретрансляторы и случайные путники приносят в {COLONY} вести о том, что стало с героями, соперниками и фракциями, чьи судьбы переплелись вокруг вашего Нулевого Якоря.</TMM_Story_A5_Epilogue_NewDawn.stages.0.text>

  <!-- SLIDE 1: PALADIN DANSE -->
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.label>Хроника героев: Паладин Данс</TMM_Story_A5_Epilogue_NewDawn.stages.1.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.text>Среди всех ветеранов, проливших кровь в войне за Искру, мало кто воплощал конфликт между сталью и душой так ярко, как паладин Данс.</TMM_Story_A5_Epilogue_NewDawn.stages.1.text>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.0.label>[Завербован] Паладин Данс обучает новое поколение защитников {COLONY}</TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.0.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.0.resultText>Навсегда сняв старые жетоны Братства, паладин Данс становится гранитной опорой гарнизона {COLONY} — каждый день доказывая, что честь куётся поступками, а не кровью или микросхемами.</TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.0.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.1.label>[Спасён] По Пустоши ходят слухи об одиноком рыцаре в побитой броне T-60, охраняющем фермы</TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.1.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.1.resultText>Хотя Данс так и не встал навсегда под ваше знамя, охранники караванов до сих пор рассказывают о молчаливом рыцаре в иссечённой силовой броне, выходящем из тумана всякий раз, когда рейдеры угрожают беззащитным поселенцам.</TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.1.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.2.label>[Вычеркнут из списков] Имя паладина Данса навсегда стёрто в архивах Братства</TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.2.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.2.resultText>В холодных хранилищах Братства Стали личное дело Данса остаётся залитым чёрными чернилами — суровый памятник ордену, испугавшемуся собственной человечности.</TMM_Story_A5_Epilogue_NewDawn.stages.1.choices.2.resultText>

  <!-- SLIDE 2: CASTIEL -->
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.label>Хроника героев: Кастиэль</TMM_Story_A5_Epilogue_NewDawn.stages.2.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.text>Он был первым небесным посланником, постучавшим в ваши ворота при пробуждении Якоря — в мятом бежевом плаще и с грузом умирающих Небес на плечах.</TMM_Story_A5_Epilogue_NewDawn.stages.2.text>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.0.label>[Завербован] Кастиэль свободно живёт среди поселенцев {COLONY} как страж и друг</TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.0.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.0.resultText>Предпочтя упрямых смертных слепому послушанию, Кастиэль остаётся в {COLONY} — неуклюже ухаживает за ульями, поправляет кривой синий галстук и не перестаёт удивляться хрупкому упорству свободной воли.</TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.0.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.1.label>[Небесный Порядок] Кастиэль несёт безмолвную стражу с тенью печали в глазах</TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.1.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.1.resultText>Обретший прежнюю благодать под властью архангела Михаила, Кастиэль послушно патрулирует безупречное небо, навсегда сохранив тоску по живому человеческому теплу, принесённому в жертву совершенству.</TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.1.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.2.label>[Ушёл в легенды] Встречи с ангелом в бежевом плаще становятся байками у костра</TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.2.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.2.resultText>Когда Разлом изменился навсегда, Кастиэль растворился в бескрайней тишине за пепельными пустошами — оставшись лишь тихой легендой, которую охотники рассказывают друг другу по ночам.</TMM_Story_A5_Epilogue_NewDawn.stages.2.choices.2.resultText>

  <!-- SLIDE 3: CROWLEY -->
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.label>Хроника героев: Кроули</TMM_Story_A5_Epilogue_NewDawn.stages.3.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.text>Вчитываясь в мелкий шрифт или разливая тридцатилетний виски прямо посреди перестрелки, бывший Король Перекрёстков всегда умел приземляться на обе ноги — почти всегда.</TMM_Story_A5_Epilogue_NewDawn.stages.3.text>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.0.label>[Завербован] Кроули открывает дорогой бар в {COLONY} и приписывает спасение мира себе</TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.0.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.0.resultText>Пережив гнев Люцифера за вашими стенами, Кроули занимает лучшую комнату гостиницы, беспощадно критикует местный самогон и уверяет каждого торговца, что всю вашу победу спланировал лично он.</TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.0.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.1.label>[Тёмный Завет] Кроули поднимает хрустальный бокал с трона из пепла и серы</TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.1.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.1.resultText>При торжестве Тёмного Завета Кроули возглавляет процветающую империю контрактов и сумеречной торговли, неизменно поднимая тост за своих любимых смертных партнёров.</TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.1.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.2.label>[Отрезан от сделок] Лишившись притока душ, Кроули исчезает на забытых перекрёстках</TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.2.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.2.resultText>Отрезанный от человеческих душ и Резонатора Искр, бывший Король Ада растворяется на глухих окраинах Пустоши, бормоча изобретательные проклятия в адрес несносных поселенцев.</TMM_Story_A5_Epilogue_NewDawn.stages.3.choices.2.resultText>

  <!-- SLIDE 4: JOHN HANCOCK & PRESTON GARVEY -->
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.label>Хроника героев: Джон Хэнкок и Престон Гарви</TMM_Story_A5_Epilogue_NewDawn.stages.4.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.text>От неоновых переулков Добрососедства до каменных бастионов Замка простые жители Пустоши смотрели на Джона Хэнкока и Престона Гарви, гадая, каким окажется завтрашний день.</TMM_Story_A5_Epilogue_NewDawn.stages.4.text>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.0.label>[Свободная Пустошь] Хэнкок и Гарви объединяют людей, гулей, синтов и мутантов под общим знаменем</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.0.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.0.resultText>В Добрососедстве Джон Хэнкок угощает всех за счёт заведения, пока стабилизатор Верджила спасает целые поколения гулей. А по всему фронтиру Престон Гарви поднимает знамя Минитменов над свободными городками — впервые ни одно поселение не осталось в одиночестве.</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.0.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.1.label>[Стерильная Империя] Добрососедство пустеет под белой дымкой, а народное ополчение распущено</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.1.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.1.resultText>Матрица Стерилизации смела и радиацию, и мутантов. Красный сюртук Джона Хэнкока остался в пепле Добрососедства, а потрёпанная шляпа Престона Гарви висит под стеклом в музее «умиротворённой Пустоши» Анклава.</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.1.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.2.label>[Железный Крестовый Поход] Добрососедство держит ставни закрытыми в тени дирижаблей Братства</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.2.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.2.resultText>Хэнкок тихо запирает переулки от проходящих стальных патрулей, а Престон Гарви обменивает вольность ополчения на защиту Братства — благодарный за спасение от чудовищ, но встревоженный ценой в свободе.</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.2.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.3.label>[Небесный Порядок] Бунтарские песни стихают под ровным гулом золотых енохианских пилонов</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.3.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.3.resultText>Под безупречной властью Небес дерзкие речи Хэнкока переходят на шёпот, а Престон Гарви смотрит, как смирные поселенцы идут под ангельскими крыльями, молясь, чтобы этот безмолвный рай стоил своих цепей.</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.3.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.4.label>[Тёмный Завет] Добрососедство догорает в сумерках, а Престон Гарви уводит последних партизан в горы</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.4.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.4.resultText>Пока ночные стаи занимают руины Добрососедства, Престон Гарви уходит в северные хребты с горсткой упрямых охотников — клянясь, что пока горит хоть один костёр, человечество не сдалось.</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.4.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.5.label>[Теневой Консорциум] Добрососедство становится налоговой зоной, а Гарви сдаёт свой значок</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.5.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.5.resultText>Хэнкок мрачно подписывает договор аренды с Волт-Тек, чтобы сохранить Добрососедство под охраной Стрелков, а Престон Гарви вешает шляпу на гвоздь, отказываясь охранять корпоративные заставы.</TMM_Story_A5_Epilogue_NewDawn.stages.4.choices.5.resultText>

  <!-- SLIDE 5: WHAT BECAME OF THE WORLD -->
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.label>Каким стал мир</TMM_Story_A5_Epilogue_NewDawn.stages.5.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.text>Спустя двести лет после того, как октябрьский ядерный ритуал сорвал Шестьдесят Шесть Печатей, долгая ночь Проекта «Чистилище» наконец завершилась. Взгляните со стен {COLONY} на новую эпоху, выкованную вашим выбором.</TMM_Story_A5_Epilogue_NewDawn.stages.5.text>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.0.label>[Концовка: Свободная Пустошь] Чистое небо над выстраданным, несломленным фронтиром</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.0.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.0.resultText>Разлом запечатан навеки. Небеса и Ад отрезаны от Земли, Консорциум повержен, а люди, синты, гули и разумные мутанты встречают суровый, но честный рассвет — славя {COLONY} как маяк, подаривший миру свободу.</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.0.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.1.label>[Концовка: Стерильная Империя] Чистый воздух, белые лаборатории и железное господство людей</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.1.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.1.resultText>Радиация, мутации и оккультные кошмары вытравлены с континента. Под двойным знаменем Анклава и Института из руин поднимаются строгие города — а {COLONY} венчает вершину нового имперского порядка.</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.1.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.2.label>[Концовка: Железный Крестовый Поход] Магия погашена за стенами стали и силовой брони</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.2.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.2.resultText>Континентальный подавитель тихо гудит под {COLONY}, навеки гася сверхъестественные разломы и мятежные машины. Человечество вступает в суровый век стали под эгидой воссоединившихся Братства Стали и Изгоев.</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.2.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.3.label>[Концовка: Небесный Порядок] Безупречный, неподвижный рай под золотыми крыльями</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.3.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.3.resultText>Войны, чудовища и преступления исчезают под объединённой властью Небесного Воинства и Британских Просвещённых. {COLONY} сияет как земной престол вечного, незыблемого покоя.</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.3.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.4.label>[Концовка: Тёмный Завет] Вечные сумерки и бессмертное владычество над пеплом</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.4.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.4.resultText>Земля сливается с тенями Ада и Чистилища. Облачённые в тёмную мощь и бессмертную кровь, владыки {COLONY} безраздельно правят как князья бесконечной багровой ночи.</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.4.resultText>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.5.label>[Концовка: Теневой Консорциум] Убежища, контракты и нерушимая монополия на энергию</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.5.label>
  <TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.5.resultText>Фанатики и чудовища окончательно списаны из отчётности. Питаемая вечным Резонатором Искр, {COLONY} правит Пустошью как сверкающая корпоративная столица Волт-Тек и Стрелков.</TMM_Story_A5_Epilogue_NewDawn.stages.5.choices.5.resultText>

  <TMM_Story_A5_Epilogue_NewDawn.onComplete.4.label>Хроника завершена</TMM_Story_A5_Epilogue_NewDawn.onComplete.4.label>
  <TMM_Story_A5_Epilogue_NewDawn.onComplete.4.text>Поздравляем! Вы прошли главную сюжетную хронику «Цена Искры: Пепел, Сера и Заговор».\n\n{COLONY} переходит в Акт VI — Постгейм! Вы можете свободно развивать свою столицу, повышать колонистов в званиях, ковать именное снаряжение героев и владеть легендарным арсеналом в новом мире, который создали сами.</TMM_Story_A5_Epilogue_NewDawn.onComplete.4.text>

</LanguageData>
```