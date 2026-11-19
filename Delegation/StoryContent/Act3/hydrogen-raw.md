`Defs/StoryDefs/Story_Act3_Main.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- ACT 3, STEP 1: POINT OF NO RETURN #1 — THE WASTELAND ULTIMATUM        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_WastelandUltimatum</defName>
    <label>the wasteland ultimatum</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>98</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_preston_garvey</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A2_SecondAwakening</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Envoys on the horizon</label>
        <text>The ash storm from the Second Awakening has barely settled, yet every long-range radio in {COLONY} is overwhelmed by encrypted hails. Envoys from across the wasteland are converging on your waystation, demanding to know where your settlement stands now that the Spark Resonator blueprints are in your hands.</text>
        <days>0.8</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_MinutemenHuntersFaction</faction>
        <hero>TMM_Hero_sam_winchester</hero>
        <groupSize>4</groupSize>
        <stayDays>2</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>10</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-10</amount>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Point of No Return: The Declaration</label>
        <text>The waystation hall is packed to the walls—hunters, Brotherhood paladins, Railroad couriers, Vault-Tec clerks, and veiled figures smelling of ozone and sulfur all watch your every move.\n\n{GIVER} steps beside you, speaking quietly: "There's no hiding in the middle anymore. Whatever you declare tonight will split this entire region into armed camps—and force dissenters inside every faction to choose whether to follow their leaders or break ranks for you."</text>
        <choices>
          <li>
            <outcome>declare_protect</outcome>
            <label>Declaration of Protection: Shield the Sparks and seal the Rift forever</label>
            <resultText>Your voice rings across the hall and out over every open radio band: {COLONY} will protect the living Sparks of synths and seal the Rift against all invaders. Cheers erupt from the Railroad and Minutemen, while Enclave, Institute, and infernal envoys walk out in icy silence.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr1</flag>
                <value>protect</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_declaration</flag>
                <value>protect</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>-35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>-35</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>declare_purify</outcome>
            <label>Declaration of Purification: Cleanse the wasteland of anomalies, mutations, and occult parasites</label>
            <resultText>You declare that humanity must reclaim Earth with steel and fire, purging anomalies, mutants, and rogue machines alike. Brotherhood and Enclave officers nod in grim approval, while ghouls, mutants, and Railroad agents vanish into the night.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr1</flag>
                <value>purify</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_declaration</flag>
                <value>purify</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-55</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>-55</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>-60</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>declare_order</outcome>
            <label>Declaration of Higher Order: Entrust the Anchor to celestial and institutional architects</label>
            <resultText>You announce that chaos can only end under strict higher governance, offering the Anchor's power to Heaven's host and the pre-war institutional order. Celestial envoys and Men of Letters operatives incline their heads, while surface rebels vow resistance.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr1</flag>
                <value>order</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_declaration</flag>
                <value>order</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>15</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>declare_abyss</outcome>
            <label>Declaration of the Abyss: Shatter the Cage and open the gates of Hell</label>
            <resultText>The lights flicker crimson as you proclaim that the old world's barrier will burn—and its harvest of Sparks will feed the lords of Hell and the nocturnal clans. Demons smile darkly in the corners of the room while hunters draw weapons and retreat.</resultText>
            <requires>
              <li>choice.hell_demons=pro_faction</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr1</flag>
                <value>abyss</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_declaration</flag>
                <value>abyss</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>-20</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>declare_abyss_ambitious</outcome>
            <label>Declaration of the Abyss: Seize the power of the Pit and rewrite the wasteland</label>
            <resultText>Fulfilling your darkest ambition, you declare that {COLONY} will tear down the celestial barrier and harness the infernal tide. Smoke coils along the floorboards as demons bow in mock reverence and hunters vow vengeance.</resultText>
            <requires>
              <li>story.a1_endingStance=ambitious</li>
              <li>!choice.hell_demons=pro_faction</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr1</flag>
                <value>abyss</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_declaration</flag>
                <value>abyss</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>-20</amount>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>story.a3_ponr1Done</flag>
        <value>true</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>The Cold War Begins</label>
        <text>Your declaration has echoed across every corner of the wasteland. Factions have drawn their battle lines—expect covert spies in your waystation, sabotage at your gates, and secret transmissions from dissenters ready to defect to your banner.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- COLD WAR 1A: SPY IN THE HOTEL (CONSORTIUM / MEN OF LETTERS OPERATIVE) -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_ColdWar_SpyConsortium</defName>
    <label>shadows in the guest wing</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>88</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_mr_kendricks</giver>
    <fallbackGiver>TMM_Hero_arthur_ketch</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_WastelandUltimatum</li>
    </requires>
    <anyOf>
      <li>ponr1=protect</li>
      <li>ponr1=abyss</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Unusual guests</label>
        <text>Following your declaration, two impeccably polite "antiquities merchants" check into your waystation—carrying heavy brass luggage that emits a faint Enochian hum whenever they walk past the Zero Anchor hatch.</text>
        <days>1.2</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_MenOfLettersFaction</faction>
        <hero>TMM_Hero_mr_kendricks</hero>
        <groupSize>2</groupSize>
        <stayDays>2</stayDays>
        <objective>Investigate</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Caught red-handed</label>
        <text>Your investigators corner Mr. Kendricks in the lower corridor just as he tries to attach a Consortium triangulation beacon to the Anchor's structural beam. Calmly adjusting his cuffs, he offers a cold smile: "Clever. Now, what do you intend to do with a guest?"</text>
        <choices>
          <li>
            <outcome>feed_false_data</outcome>
            <label>Reprogram his beacon to feed false coordinates to London and the Consortium</label>
            <resultText>Your engineers quietly alter the transmitter's harmonic offset. Kendricks departs believing his mission succeeded, buying {COLONY} precious breathing room.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>2</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_spyOutcome</flag>
                <value>deceived</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>detain_and_interrogate</outcome>
            <label>Confiscate his silver wards and interrogate him for Consortium patrol routes</label>
            <resultText>Stripped of his concealed gadgets, Kendricks lets slip the location of an auxiliary Consortium chemical cache before escaping through a smoke screen.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>350</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_spyOutcome</flag>
                <value>interrogated</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>expel_publicly</outcome>
            <label>Throw him out at gunpoint as a warning to all spies</label>
            <resultText>Your guards march the operative out the front gate in full view of passing caravans. The Zero Synth nods approvingly at your vigilance.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_spyOutcome</flag>
                <value>expelled</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- COLD WAR 1B: SPY IN THE HOTEL (RESISTANCE / RAILROAD INFILTRATOR)     -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_ColdWar_SpyResistance</defName>
    <label>whispers in the bunkhouse</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>88</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_tinker_tom</giver>
    <fallbackGiver>TMM_Hero_glory</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_WastelandUltimatum</li>
    </requires>
    <anyOf>
      <li>ponr1=purify</li>
      <li>ponr1=order</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Nervous drifters</label>
        <text>Following your hardline declaration, two twitchy "rail-line prospectors" rent a room at your waystation, spending every night whispering to the Zero Synth and sketching your turret blind spots.</text>
        <days>1.2</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_RailroadFaction</faction>
        <hero>TMM_Hero_tinker_tom</hero>
        <groupSize>2</groupSize>
        <stayDays>2</stayDays>
        <objective>Investigate</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Intercepted at the bulkhead</label>
        <text>Your security team catches Tinker Tom trying to slip an encrypted extraction holotape to the Zero Synth near the Anchor bulkhead. He raises his hands hastily: "Whoa, easy! We just wanted to make sure the kid had an exit plan before you hook 'em up to that machine!"</text>
        <choices>
          <li>
            <outcome>let_slip_warning</outcome>
            <label>Let him leave quietly after taking his scrambler module</label>
            <resultText>Tinker Tom drops his jury-rigged signal scrambler and bolts into the fog, muttering under his breath. The Zero Synth watches him go with conflicted eyes.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>2</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>5</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_spyOutcome</flag>
                <value>deceived</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>seize_decoder</outcome>
            <label>Confiscate his Railroad cipher keys and interrogate him</label>
            <resultText>You strip the agent of his coded maps and silver stash before escorting him beyond your perimeter.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>350</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_spyOutcome</flag>
                <value>interrogated</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>arrest_saboteur</outcome>
            <label>Publicly denounce the Railroad infiltrator and tighten security</label>
            <resultText>Your stern crackdown impresses your authoritarian allies, though the Zero Synth grows noticeably colder toward your guards.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_spyOutcome</flag>
                <value>expelled</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- COLD WAR 2: SABOTAGE AT DUSK & RETALIATORY RAID                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_ColdWar_Sabotage</defName>
    <label>sabotage at dusk</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>86</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_captain_wes</giver>
    <fallbackGiver>TMM_Hero_baker</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_WastelandUltimatum</li>
    </requires>
    <anyOf>
      <li>done:TMM_Story_A3_ColdWar_SpyConsortium</li>
      <li>done:TMM_Story_A3_ColdWar_SpyResistance</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Blinding flash on the perimeter</label>
        <text>A deafening blast rocks the outer perimeter of {COLONY} at dusk! Hostile sappers planted a shaped charge against your auxiliary power relay and vanished, pinning a taunting note to the scorched concrete: "Your walls won't hold when the real storm hits."</text>
        <days>1.4</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Assessing the breach</label>
        <text>Smoke billows from the damaged junction box while distant motion sensors pick up a hired mercenary strike force moving in to exploit the chaos. How do you respond?</text>
        <choices>
          <li>
            <outcome>trace_detonator</outcome>
            <label>Reverse-trace the detonator signal to spot the incoming raiders early</label>
            <resultText>Your comms officer locks onto the sappers' short-range burst frequency, giving your defenders clear warning of the incoming assault vector.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_sabotageResponse</flag>
                <value>traced</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>fortify_perimeters</outcome>
            <label>Reroute emergency capacitors and brace the inner barricades</label>
            <resultText>Your mechanics bypass the fried relays in record time, restoring full power to your defenses just as gunfire erupts at the edge of the map.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Steel</thing>
                <count>80</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_sabotageResponse</flag>
                <value>fortified</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_GunnersFaction</faction>
        <pointsFactor>1.2</pointsFactor>
        <delayHours>5</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>450</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Saboteurs routed</label>
                <text>Your defenders crushed the mercenary strike team sent to exploit the sabotage. On the squad leader's body, you found an anonymous contract paid in pre-war bullion.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-5</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Costly skirmish</label>
                <text>Though the raiders eventually withdrew into the ash, the sabotage hit your stockpiles hard—proving your enemies will stop at nothing.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- COLD WAR 3: ATTEMPTED KIDNAPPING OF THE ZERO SYNTH                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_ColdWar_KidnapAttempt</defName>
    <label>snatch and grab</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>89</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_x6_88</giver>
    <fallbackGiver>TMM_Hero_justin_ayo</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_ColdWar_Sabotage</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Stasis signatures closing in</label>
        <text>The Zero Synth wakes in a cold sweat, clutching their head: "They're coming—not to kill me, to put me in a stasis cage! I can feel their quantum dampeners locking onto my Spark!"</text>
        <days>1.4</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_InstituteFaction</faction>
        <pointsFactor>1.3</pointsFactor>
        <delayHours>4</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.zeroSynthKidnapped</flag>
                <value>false</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Abduction team wiped out</label>
                <text>Your garrison intercepted the Courser extraction team before they could reach the Zero Synth, shattering their stasis shackles and securing the colony!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.zeroSynthKidnapped</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>The Zero Synth has been taken!</label>
                <text>In the chaos of the assault, X6-88's extraction squad clamped a stasis collar onto the Zero Synth and retreated to a forward relay bunker! Launch a rescue strike immediately!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
        <objective>Rescue</objective>
        <hero>TMM_Hero_x6_88</hero>
        <timeoutDays>5</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.zeroSynthKidnapped</flag>
                <value>false</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentSpacer</thing>
                <count>1</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Zero Synth rescued!</label>
                <text>Your strike team breached the forward bunker, cut the stasis collar off the Zero Synth, and brought them home safely!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.zeroSynthKidnapped</flag>
                <value>false</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>A desperate self-escape</label>
                <text>Before the captors could complete their final teleport, the Zero Synth overloaded their own stasis collar in a violent Spark surge, limping back to {COLONY} bruised and bitter that no rescue came.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.zeroSynthKidnapped</flag>
                <value>false</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Battered return</label>
                <text>Though your initial assault faltered, passing Railroad scouts ambushed the weakened transport and escorted the shaken Zero Synth back to {COLONY}.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Aftermath of the raid</label>
        <text>With the Zero Synth back inside {COLONY}'s walls and the enemy's stasis equipment lying in pieces, one truth is unmistakable: you need the Borax countermeasure and the Spark Resonator completed before the entire wasteland descends on your doorstep.</text>
        <choices>
          <li>
            <outcome>reassure_synth</outcome>
            <label>Sit with the Zero Synth and reinforce the personal wards around their quarters</label>
            <resultText>The Zero Synth nods slowly, gripping your hand. "I'm ready. Let's reach out to our allies and finish building the Resonator."</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_kidnapSurvived</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>harden_resolve</outcome>
            <label>Strip the enemy stasis collars for spare components and double the watch</label>
            <resultText>Your engineers salvage high-grade electronics from the captured shackles while guards lock down every perimeter gate.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>3</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_kidnapSurvived</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- DEFECTOR THREAD 1: SECRET LETTERS OF DISSENT — STEEL & SILICON        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_DefectorLetter_SteelAndScience</defName>
    <label>secret letters: steel and silicon</label>
    <act>3</act>
    <main>false</main>
    <priority>82</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_alan_binet</giver>
    <fallbackGiver>TMM_Hero_paladin_danse</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_WastelandUltimatum</li>
    </requires>
    <anyOf>
      <li>choice.brotherhood=pro_spark</li>
      <li>choice.institute=pro_spark</li>
      <li>ponr1=protect</li>
    </anyOf>
    <letterLabel>Encrypted signal from inside the lines</letterLabel>
    <letterText>Your declaration shattered the uneasy peace inside the Institute and the Brotherhood alike. Over a low-band emergency frequency, {GIVER} has slipped a coded message past their commanders:\n\n"Maxson and Justin Ayo are preparing a total purge—no prisoners, no mercy. Not all of us signed on for slaughter. Keep your comms open for our breakout signals; if you help us escape, we'll bring everything we know to your colony."</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Answering the dissenters</label>
        <text>You can reply on the encrypted burst frequency to coordinate extraction windows with sympathetic officers and scientists inside the Brotherhood and Institute.</text>
        <choices>
          <li>
            <outcome>open_escape_channel</outcome>
            <label>Broadcast safe-passage codes and pledge shelter to all defecting heroes</label>
            <resultText>A brief double-click confirms receipt on the other end. The defection quests for the Brotherhood and Institute are ready to trigger as soon as their operatives make their move.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>5</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_defectorNetworkSteel</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>request_schematics</outcome>
            <label>Ask them to smuggle out technical data and spare parts when they run</label>
            <resultText>"Understood," the encrypted voice whispers before cutting out. "Keep a landing pad clear."</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>2</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_defectorNetworkSteel</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- DEFECTOR THREAD 2: SECRET LETTERS OF DISSENT — WINGS & SULFUR         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_DefectorLetter_WingsAndSulfur</defName>
    <label>secret letters: wings and sulfur</label>
    <act>3</act>
    <main>false</main>
    <priority>82</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_castiel</giver>
    <fallbackGiver>TMM_Hero_crowley</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_WastelandUltimatum</li>
    </requires>
    <anyOf>
      <li>choice.heaven_angels=pro_spark</li>
      <li>choice.hell_demons=pro_spark</li>
      <li>ponr1=protect</li>
      <li>ponr1=order</li>
    </anyOf>
    <letterLabel>A warning written in ash and ozone</letterLabel>
    <letterText>Two wildly different messengers leave warnings at your perimeter within the same hour: a feather scorched by celestial fire from {GIVER}, and a sulfur-smudged note bearing Crowley's seal.\n\nHeaven's archangels are mobilizing execution squads against their own rebels, while Hell's fanatics are cracking the locks on Lucifer's Cage. Both dissenters warn that when the coups erupt, they will turn to {COLONY} for sanctuary.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Preparing for celestial and infernal exiles</label>
        <text>The war between Heaven and Hell is spilling directly onto your doorstep. How do you prepare your settlement for incoming supernatural defectors?</text>
        <choices>
          <li>
            <outcome>welcome_rebels</outcome>
            <label>Prepare Enochian wards and guest quarters for Castiel or Crowley</label>
            <resultText>Your ward-smiths carve protective sigils into the gateposts—allowing rebel angels or a fugitive King of Hell to enter without triggering your traps.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>5</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_defectorNetworkOccult</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>prepare_wards</outcome>
            <label>Stockpile rock salt and silver in case their pursuers follow them home</label>
            <resultText>Your armory crates are stacked with consecrated salt and silver—you will be ready when the hunters of Heaven and Hell arrive.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>200</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_defectorNetworkOccult</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- DEFECTOR THREAD 3: SECRET LETTERS OF DISSENT — CRACKS IN CONSORTIUM   -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_DefectorLetter_CracksInConsortium</defName>
    <label>secret letters: cracks in the consortium</label>
    <act>3</act>
    <main>false</main>
    <priority>83</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_mick_davies</giver>
    <fallbackGiver>TMM_Hero_scribe_silas</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_WastelandUltimatum</li>
    </requires>
    <anyOf>
      <li>choice.men_of_letters=pro_spark</li>
      <li>choice.outcasts=pro_spark</li>
      <li>choice.vault_tec=pro_spark</li>
      <li>ponr1=protect</li>
      <li>ponr1=purify</li>
    </anyOf>
    <letterLabel>A cipher from inside the vaults</letterLabel>
    <letterText>Your public stance has triggered a quiet mutiny inside the Consortium's outer ranks. {GIVER} transmits a frantic, half-burned cipher confirming that Dr. Hess, Dr. Braun, and Dick Roman are accelerating their harvest—and that dissenters inside Vault-Tec, the Outcasts, and the Men of Letters are ready to defect with the chemical key to Leviathan invulnerability.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Coordinating the inner-circle breakout</label>
        <text>The Consortium's own archivists and overseers want out before the Blood Eclipse locks the doors forever.</text>
        <choices>
          <li>
            <outcome>coordinate_breakout</outcome>
            <label>Assign scouts to watch the borders for escaping archivists and overseers</label>
            <resultText>Your perimeter lookouts are briefed on the dissenters' recognition signals. As soon as any of them join your colony, you will have the minds needed to crack the Borax formula.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_defectorNetworkConsortium</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>demand_chemical_logs</outcome>
            <label>Urge them to prioritize smuggling out the Leviathan containment files</label>
            <resultText>"Already stuffed in my coat pocket," the coded reply flashes back. "Just keep the back gate unlocked."</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gold</thing>
                <count>10</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_defectorNetworkConsortium</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ACT 3, STEP 4A: THE BORAX SECRET & RESONATOR CALIBRATION              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_BoraxAndBlueprints</defName>
    <label>the borax secret</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>91</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_bobby_singer</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_ColdWar_KidnapAttempt</li>
    </requires>
    <anyOf>
      <li>defector.TMM_Hero_paladin_danse=true</li>
      <li>defector.TMM_Hero_sarah_lyons=true</li>
      <li>defector.TMM_Hero_alan_binet=true</li>
      <li>defector.TMM_Hero_madison_li=true</li>
      <li>defector.TMM_Hero_castiel=true</li>
      <li>defector.TMM_Hero_crowley=true</li>
      <li>defector.TMM_Hero_mick_davies=true</li>
      <li>defector.TMM_Hero_scribe_silas=true</li>
      <li>defector.TMM_Hero_gwen_mcnamara=true</li>
      <li>defector.TMM_Hero_deacon=true</li>
      <li>defector.TMM_Hero_benny_lafitte=true</li>
      <li>defector.TMM_Hero_brian_virgil=true</li>
      <li>defector.TMM_Hero_sergeant_dornan=true</li>
      <li>defector.TMM_Hero_rufus_turner=true</li>
      <li>defector.TMM_Hero_tessa=true</li>
      <li>defector.TMM_Hero_dead_eye=true</li>
      <li>defector.TMM_Hero_charon=true</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Cross-referencing the defectors' intel</label>
        <text>Your newly recruited defectors gather around {COLONY}'s research terminal, comparing their stolen faction archives against Sector 4 of the Holodisk-Key. Within hours, a breakthrough emerges: the exact chemical catalyst—concentrated sodium borate—needed to strip Leviathans and Dick Roman of their invulnerability, stored at a nearby SucroCorp annex.</text>
        <days>1.0</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>Retrieve</objective>
        <thing>Chemfuel</thing>
        <timeoutDays>6</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Plasteel</thing>
                <count>40</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>4</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Borax catalyst drums secured</label>
                <text>Your squad fought past Leviathan guards at the SucroCorp annex and hauled back intact drums of concentrated sodium borate along with pre-war industrial alloys!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Synthesized from lab notes</label>
                <text>Though Consortium sweepers torched the annex before your team arrived, your defectors successfully reconstructed the Borax sprayer formula from the Holodisk-Key's chemical equations.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Emergency chemical synthesis</label>
                <text>Forced to retreat from the annex under heavy fire, your researchers still managed to synthesize the Borax weapon formula from salvaged sample vials.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Weaponizing the formula</label>
        <text>In your workshop, the caustic white Borax solution is stabilized and loaded into pressurized sprayers and automated turret canisters—while the final harmonic schematics for the Spark Resonator compile on your main screen.</text>
        <choices>
          <li>
            <outcome>mass_produce_borax</outcome>
            <label>Prioritize Borax sprayers and turret blueprints for immediate defense</label>
            <resultText>Your machinists lock the Borax sprayer and Borax turret schematics into your production benches. Leviathans will no longer be untouchable at your gates.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Steel</thing>
                <count>100</count>
              </li>
            </effects>
          </li>
          <li>
            <outcome>calibrate_resonator_first</outcome>
            <label>Sync the Borax formula with the Spark Resonator's harmonic blueprints</label>
            <resultText>Both the Borax defense schematics and the Spark Resonator assembly list are finalized, ready for final construction.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>story.boraxFormula</flag>
        <value>true</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>story.a3_boraxReady</flag>
        <value>true</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Borax Formula &amp; Resonator Blueprints Ready</label>
        <text>Your researchers have unlocked the Borax Sprayer and Borax Turret schematics! With the anti-Leviathan countermeasure ready, it is time to gather the final materials and assemble the Spark Resonator.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ACT 3 FINALE: ASSEMBLING THE SPARK RESONATOR                          -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A3_BuildSparkResonator</defName>
    <label>assembling the spark resonator</label>
    <act>3</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>96</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_alan_binet</giver>
    <fallbackGiver>TMM_Hero_sam_winchester</fallbackGiver>
    <requires>
      <li>act=3</li>
      <li>done:TMM_Story_A3_BoraxAndBlueprints</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <thing>Plasteel</thing>
        <count>80</count>
        <to>colony</to>
        <timeoutDays>10</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Resonator frame forged</label>
                <text>Your engineers have machined the plasteel containment housing for the Spark Resonator. Next, allocate pre-war gold for the Enochian conductive filaments.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Salvaged frame improvised</label>
                <text>Running short on time, your mechanics welded salvaged Zero Anchor plating to form the housing, moving directly to the conductive gold filaments.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <thing>Gold</thing>
        <count>20</count>
        <to>colony</to>
        <timeoutDays>10</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Enochian filaments inlaid</label>
                <text>Fine gold circuitry has been etched into the Resonator's rings, bridging pre-war quantum physics with ancient celestial wards.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Thin-plated conduits</label>
                <text>Your smiths stretched every spare gold shaving into razor-thin filaments to complete the circuit before the deadline.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Harmonic tuning</label>
        <text>Before installing the final control components, the Zero Synth must attune the Resonator's central crystal to their own spiritual frequency. How do you guide the calibration?</text>
        <choices>
          <li>
            <outcome>gentle_calibration</outcome>
            <label>Run a slow, painless resonance sync with the Zero Synth</label>
            <resultText>Soft blue light pulses smoothly through the gold filaments without straining the Zero Synth. The crystal sings with a clear, steady tone.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_resonatorTuning</flag>
                <value>harmonic</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>overclock_coils</outcome>
            <label>Push maximum power through the crystal to maximize output</label>
            <resultText>Lightning arcs violently across the frame as the crystal locks into high-output mode, leaving the Zero Synth pale and breathless.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_resonatorTuning</flag>
                <value>overclocked</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <thing>ComponentIndustrial</thing>
        <count>8</count>
        <to>colony</to>
        <timeoutDays>10</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>4</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Control console wired</label>
                <text>The final industrial components are locked into place. The Spark Resonator is ready to be anchored into its permanent position.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>4</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Jury-rigged console complete</label>
                <text>Using salvaged relay boards from the bunker, your technicians wired the final console just in time for placement.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Anchoring the Spark Resonator</label>
        <text>The Spark Resonator stands complete, humming with enough power to rewrite the fate of Earth. Where will you anchor the machine before activating its main console?</text>
        <choices>
          <li>
            <outcome>install_deep_anchor</outcome>
            <label>Lower it directly into the opened Zero Anchor shaft beneath {COLONY}</label>
            <resultText>Heavy winches lower the Resonator into the ancient subterranean chamber, locking it directly onto the continental ley-line core.</resultText>
            <requires>
              <li>story.anchorOpened=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_resonatorLocation</flag>
                <value>deep_anchor</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>install_central_citadel</outcome>
            <label>Mount it in the fortified heart of {COLONY}'s central keep</label>
            <resultText>Heavy power conduits link the Resonator through the floor to the Anchor below, placing the console right where all your defenders and turrets can cover it.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_resonatorLocation</flag>
                <value>courtyard</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>install_sanctuary_ward</outcome>
            <label>Seal it inside an Enochian-warded sanctuary chamber to mask its pulse</label>
            <resultText>Surrounded by carved silver wards, the Resonator hums quietly out of sight—shielded from stray psychic interference until the moment of reckoning.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a3_resonatorLocation</flag>
                <value>sanctuary</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>act</flag>
        <value>4</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>story.a3_resonatorBuilt</flag>
        <value>true</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>story.bloodEclipseStarted</flag>
        <value>true</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act IV: The Blood Eclipse</label>
        <text>As the Spark Resonator locks into place, a blinding column of blue light pierces the clouds—and the sun above the wasteland turns a deep, ominous crimson. The Blood Eclipse has begun! Every faction leader is marching toward your console for Point of No Return #2.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act3_Main.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_A3_WastelandUltimatum -->
  <TMM_Story_A3_WastelandUltimatum.label>ультиматум пустоши</TMM_Story_A3_WastelandUltimatum.label>
  <TMM_Story_A3_WastelandUltimatum.stages.0.label>Послы на горизонте</TMM_Story_A3_WastelandUltimatum.stages.0.label>
  <TMM_Story_A3_WastelandUltimatum.stages.0.text>Пепельный шторм после Второго пробуждения едва утих, но все дальние радиостанции в {COLONY} разрываются от зашифрованных вызовов. Послы со всей Пустоши стягиваются к вашей гостинице, требуя ответа, чью сторону займёт поселение теперь, когда чертежи Резонатора Искр у вас в руках.</TMM_Story_A3_WastelandUltimatum.stages.0.text>
  <TMM_Story_A3_WastelandUltimatum.stages.2.label>Точка невозврата №1: Декларация</TMM_Story_A3_WastelandUltimatum.stages.2.label>
  <TMM_Story_A3_WastelandUltimatum.stages.2.text>Зал гостиницы набит до отказа — охотники, паладины Братства, курьеры Подземки, клерки Волт-Тек и фигуры в тёмных плащах, от которых тянет озоном и серой, ловят каждое ваше слово.\n\n{GIVER} встаёт рядом и тихо говорит: «Отсидеться посередине больше не выйдет. То, что вы объявите сегодня ночью, расколет весь регион на враждующие лагеря — и заставит несогласных внутри каждой фракции решать, идти ли за своими лидерами или бежать к вам».</TMM_Story_A3_WastelandUltimatum.stages.2.text>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.0.label>Декларация защиты: уберечь Искры и навсегда запечатать Разлом</TMM_Story_A3_WastelandUltimatum.stages.2.choices.0.label>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.0.resultText>Ваш голос разносится по залу и уходит на все открытые частоты: {COLONY} защитит живые Искры синтов и запечатает Разлом от любых захватчиков. Подземка и Минитмены встречают решение одобрительным гулом, тогда как посланники Анклава, Института и преисподней покидают зал в ледяном молчании.</TMM_Story_A3_WastelandUltimatum.stages.2.choices.0.resultText>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.1.label>Декларация очищения: выжечь из Пустоши аномалии, мутации и оккультных паразитов</TMM_Story_A3_WastelandUltimatum.stages.2.choices.1.label>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.1.resultText>Вы объявляете, что человечество должно вернуть себе Землю огнём и сталью, очистив её от аномалий, мутантов и вышедших из-под контроля машин. Офицеры Братства и Анклава мрачно кивают в знак согласия, а гули, мутанты и агенты Подземки растворяются в ночи.</TMM_Story_A3_WastelandUltimatum.stages.2.choices.1.resultText>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.2.label>Декларация высшего порядка: передать силу Якоря Небесам и архитекторам порядка</TMM_Story_A3_WastelandUltimatum.stages.2.choices.2.label>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.2.resultText>Вы провозглашаете, что хаос прекратится лишь под строгой высшей властью, предлагая энергию Якоря небесному воинству и довоенным структурам. Посланники Небес и Просвещённых склоняют головы, а вольные жители Пустоши клянутся дать отпор.</TMM_Story_A3_WastelandUltimatum.stages.2.choices.2.resultText>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.3.label>Декларация бездны: сокрушить Клетку и распахнуть врата Ада</TMM_Story_A3_WastelandUltimatum.stages.2.choices.3.label>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.3.resultText>Лампы вспыхивают багровым светом, когда вы объявляете, что барьер старого мира сгорит, а жатва Искр напитает владык Ада и ночные кланы. Демоны в углах зала хищно ухмыляются, а охотники хватаются за оружие и отступают к дверям.</TMM_Story_A3_WastelandUltimatum.stages.2.choices.3.resultText>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.4.label>Декларация бездны: подчинить силу преисподней и перекроить Пустошь</TMM_Story_A3_WastelandUltimatum.stages.2.choices.4.label>
  <TMM_Story_A3_WastelandUltimatum.stages.2.choices.4.resultText>Воплощая свои самые тёмные замыслы, вы заявляете, что {COLONY} обрушит небесный барьер и оседлает адский прилив. По полу стелется чёрный дым, демоны насмешливо кланяются, а охотники клянутся отомстить.</TMM_Story_A3_WastelandUltimatum.stages.2.choices.4.resultText>
  <TMM_Story_A3_WastelandUltimatum.onComplete.1.label>Начинается холодная война</TMM_Story_A3_WastelandUltimatum.onComplete.1.label>
  <TMM_Story_A3_WastelandUltimatum.onComplete.1.text>Ваша декларация разнеслась по всем уголкам Пустоши. Фракции заняли боевые рубежи — ждите шпионов в гостинице, диверсий у ворот и тайных писем от перебежчиков, готовых перейти под ваше знамя.</TMM_Story_A3_WastelandUltimatum.onComplete.1.text>

  <!-- TMM_Story_A3_ColdWar_SpyConsortium -->
  <TMM_Story_A3_ColdWar_SpyConsortium.label>тени в гостевом крыле</TMM_Story_A3_ColdWar_SpyConsortium.label>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.0.label>Странные постояльцы</TMM_Story_A3_ColdWar_SpyConsortium.stages.0.label>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.0.text>Вскоре после вашей декларации в гостиницу заселяются двое безупречно вежливых «торговцев антиквариатом» — их тяжёлые латунные саквояжи издают тихий енохианский гул каждый раз, когда они проходят мимо люка Нулевого Якоря.</TMM_Story_A3_ColdWar_SpyConsortium.stages.0.text>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.label>С поличным</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.label>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.text>Ваши контрразведчики зажимают мистера Кендрикса в нижнем коридоре в тот самый миг, когда он пытается прикрепить триангуляционный маячок Консорциума к несущей балке Якоря. Спокойно поправив манжеты, он холодно усмехается: «Неглупо. И что же вы намерены делать с гостем?»</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.text>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.0.label>Перепрограммировать маячок и скормить Лондону и Консорциуму ложные координаты</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.0.label>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.0.resultText>Инженеры незаметно сбивают гармонический сдвиг передатчика. Кендрикс уезжает в полной уверенности, что задание выполнено, выигрывая для {COLONY} драгоценное время.</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.0.resultText>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.1.label>Конфисковать его серебряные обереги и допросить о маршрутах Консорциума</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.1.label>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.1.resultText>Лишённый потайных приборов, Кендрикс проговаривается о расположении вспомогательного химического склада Консорциума, прежде чем скрыться в дымовой завесе.</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.1.resultText>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.2.label>Вышвырнуть его за ворота под прицелом в назидание всем шпионам</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.2.label>
  <TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.2.resultText>Охрана выводит оперативника за главные ворота на глазах у проходящих караванов. Нулевой Синт одобрительно кивает, оценив вашу бдительность.</TMM_Story_A3_ColdWar_SpyConsortium.stages.2.choices.2.resultText>

  <!-- TMM_Story_A3_ColdWar_SpyResistance -->
  <TMM_Story_A3_ColdWar_SpyResistance.label>шёпот в бараке</TMM_Story_A3_ColdWar_SpyResistance.label>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.0.label>Нервные бродяги</TMM_Story_A3_ColdWar_SpyResistance.stages.0.label>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.0.text>После вашей жёсткой декларации в гостинице снимают комнату двое дёрганых «старателей» — каждую ночь они о чём-то шепчутся с Нулевым Синтом и зарисовывают мёртвые зоны ваших турелей.</TMM_Story_A3_ColdWar_SpyResistance.stages.0.text>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.label>Перехват у переборки</TMM_Story_A3_ColdWar_SpyResistance.stages.2.label>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.text>Охрана перехватывает Техника Тома у переборки Якоря, когда он пытается передать Нулевому Синту зашифрованную голозапись с планом побега. Он торопливо поднимает руки: «Эй, полегче! Мы просто хотели оставить парню запасной выход, пока вы не подключили его к этой адской машине!»</TMM_Story_A3_ColdWar_SpyResistance.stages.2.text>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.0.label>Забрать его шифратор и позволить уйти без лишнего шума</TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.0.label>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.0.resultText>Техник Том оставляет свой самодельный скремблер и исчезает в тумане, бормоча что-то себе под нос. Нулевой Синт провожает его противоречивым взглядом.</TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.0.resultText>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.1.label>Конфисковать шифры Подземки и допросить лазутчика</TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.1.label>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.1.resultText>Вы изымаете у агента кодовые карты и запас серебра, после чего выпроваживаете его за пределы периметра.</TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.1.resultText>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.2.label>Публично разоблачить агента Подземки и ужесточить охрану</TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.2.label>
  <TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.2.resultText>Ваша суровость впечатляет авторитарных союзников, хотя Нулевой Синт становится заметно холоднее к вашей охране.</TMM_Story_A3_ColdWar_SpyResistance.stages.2.choices.2.resultText>

  <!-- TMM_Story_A3_ColdWar_Sabotage -->
  <TMM_Story_A3_ColdWar_Sabotage.label>диверсия в сумерках</TMM_Story_A3_ColdWar_Sabotage.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.0.label>Вспышка на периметре</TMM_Story_A3_ColdWar_Sabotage.stages.0.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.0.text>В сумерках внешний периметр {COLONY} сотрясает оглушительный взрыв! Вражеские диверсанты подорвали вспомогательный силовой узел и скрылись, прибив к обугленной стене записку: «Ваши стены не устоят, когда грянет настоящая буря».</TMM_Story_A3_ColdWar_Sabotage.stages.0.text>
  <TMM_Story_A3_ColdWar_Sabotage.stages.1.label>Оценка прорыва</TMM_Story_A3_ColdWar_Sabotage.stages.1.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.1.text>Из разбитого распределительного щита валит дым, а дальние датчики движения фиксируют отряд наёмников, спешащий воспользоваться суматохой. Как вы поступите?</TMM_Story_A3_ColdWar_Sabotage.stages.1.text>
  <TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.0.label>Запеленговать сигнал детонатора, чтобы заранее вычислить направление удара</TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.0.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.0.resultText>Связист засекает частоту подрывников, давая защитникам точное предупреждение о маршруте вражеского отряда.</TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.0.resultText>
  <TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.1.label>Переключить аварийные конденсаторы и укрепить внутренние баррикады</TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.1.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.1.resultText>Механики в рекордный срок обходят сгоревшие реле, возвращая питание обороне как раз в тот миг, когда на краю карты гремят выстрелы.</TMM_Story_A3_ColdWar_Sabotage.stages.1.choices.1.resultText>
  <TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.0.effects.1.label>Диверсионный отряд разгромлен</TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.0.effects.1.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.0.effects.1.text>Защитники разбили отряд наёмников, посланный добить ослабленный периметр. При командире группы найден анонимный контракт, оплаченный довоенным золотом.</TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.0.effects.1.text>
  <TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.1.effects.1.label>Тяжёлая стычка у пролома</TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.1.effects.1.label>
  <TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.1.effects.1.text>Хотя налётчики в итоге отступили в пепел, диверсия больно ударила по запасам — враги не остановятся ни перед чем.</TMM_Story_A3_ColdWar_Sabotage.stages.2.outcomes.1.effects.1.text>

  <!-- TMM_Story_A3_ColdWar_KidnapAttempt -->
  <TMM_Story_A3_ColdWar_KidnapAttempt.label>охота на нулевого синта</TMM_Story_A3_ColdWar_KidnapAttempt.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.0.label>Стазис-кандалы на подходе</TMM_Story_A3_ColdWar_KidnapAttempt.stages.0.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.0.text>Нулевой Синт просыпается в холодном поту, схватившись за голову: «Они идут — не убивать, а заковать меня в стазис-клетку! Я чувствую, как их квантовые глушители наводятся на мою Искру!»</TMM_Story_A3_ColdWar_KidnapAttempt.stages.0.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.0.effects.2.label>Группа захвата уничтожена</TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.0.effects.2.text>Гарнизон перехватил ударную группу охотников на подступах к Нулевому Синту, разбив их стазис-ошейники и отстояв поселение!</TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.1.effects.2.label>Нулевой Синт похищен!</TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.1.effects.2.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.1.effects.2.text>В суматохе штурма отряд X6-88 набросил на Нулевого Синта стазис-ошейник и отступил в передовой бункер-ретранслятор! Немедленно высылайте спасательный отряд!</TMM_Story_A3_ColdWar_KidnapAttempt.stages.1.outcomes.1.effects.2.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.0.effects.3.label>Нулевой Синт спасён!</TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.0.effects.3.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.0.effects.3.text>Ваш штурмовой отряд ворвался в передовой бункер, срезал стазис-ошейник с Нулевого Синта и благополучно вернул его домой!</TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.0.effects.3.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.1.effects.2.label>Отчаянный побег в одиночку</TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.1.effects.2.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.1.effects.2.text>Прежде чем похитители завершили телепортацию, Нулевой Синт перегрузил собственный стазис-ошейник вспышкой Искры и вернулся в {COLONY} израненным и озлобленным тем, что помощь не пришла.</TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.1.effects.2.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.2.effects.2.label>Тяжёлое возвращение</TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.2.effects.2.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.2.effects.2.text>Хотя первая атака захлебнулась, разведчики Подземки устроили засаду на ослабленный конвой и помогли избитому Нулевому Синту добраться обратно до {COLONY}.</TMM_Story_A3_ColdWar_KidnapAttempt.stages.2.outcomes.2.effects.2.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.label>После налёта</TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.text>Когда Нулевой Синт снова в безопасности за стенами {COLONY}, а вражеские стазис-устройства превращены в груду лома, ясно одно: формулу буры и Резонатор Искр нужно завершить раньше, чем на порог явятся главные армии.</TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.text>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.0.label>Поговорить с Нулевым Синтом и усилить личные обереги в его комнате</TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.0.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.0.resultText>Нулевой Синт медленно кивает, сжимая вашу руку: «Я готов. Свяжемся с нашими союзниками и закончим сборку Резонатора».</TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.0.resultText>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.1.label>Разобрать захваченные стазис-ошейники на детали и удвоить караулы</TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.1.label>
  <TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.1.resultText>Инженеры извлекают из ошейников ценную электронику, а часовые берут под прицел каждые ворота.</TMM_Story_A3_ColdWar_KidnapAttempt.stages.3.choices.1.resultText>

  <!-- TMM_Story_A3_DefectorLetter_SteelAndScience -->
  <TMM_Story_A3_DefectorLetter_SteelAndScience.label>тайные письма: сталь и кремний</TMM_Story_A3_DefectorLetter_SteelAndScience.label>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.letterLabel>Шифровка из-за линии фронта</TMM_Story_A3_DefectorLetter_SteelAndScience.letterLabel>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.letterText>Ваша декларация расколола хрупкое единство внутри Института и Братства Стали. По резервной аварийной частоте {GIVER} пробил зашифрованное послание мимо своих командиров:\n\n«Мэксон и Джастин Айо готовят тотальную зачистку — без пленных и пощады. Не все из нас подписывались на бойню. Держите эфир открытым для наших сигналов прорыва: если поможете уйти, мы принесём в вашу колонию всё, что знаем».</TMM_Story_A3_DefectorLetter_SteelAndScience.letterText>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.label>Ответ несогласным</TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.label>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.text>Вы можете ответить по закрытому каналу, чтобы согласовать окна эвакуации с сочувствующими офицерами и учёными из Братства и Института.</TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.text>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.0.label>Передать коды безопасного прохода и пообещать убежище всем перебежчикам</TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.0.label>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.0.resultText>Короткий двойной щелчок подтверждает приём. Квесты побега героев Братства и Института сработают, как только они решатся на прорыв.</TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.0.resultText>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.1.label>Попросить при побеге прихватить технические архивы и запасные детали</TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.1.label>
  <TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.1.resultText>«Принято, — шепчет зашифрованный голос перед тем, как связь обрывается. — Держите посадочную площадку свободной».</TMM_Story_A3_DefectorLetter_SteelAndScience.stages.0.choices.1.resultText>

  <!-- TMM_Story_A3_DefectorLetter_WingsAndSulfur -->
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.label>тайные письма: крылья и сера</TMM_Story_A3_DefectorLetter_WingsAndSulfur.label>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.letterLabel>Предупреждение из пепла и озона</TMM_Story_A3_DefectorLetter_WingsAndSulfur.letterLabel>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.letterText>В течение одного часа у вашего периметра появляются два совершенно разных послания: опалённое небесным огнём перо от {GIVER} и пропитанная серой записка с печатью Кроули.\n\nАрхангелы Небес поднимают карательные отряды против собственных мятежников, а фанатики Ада взламывают замки Клетки Люцифера. Оба отступника предупреждают: когда грянут перевороты, они попросят убежища в {COLONY}.</TMM_Story_A3_DefectorLetter_WingsAndSulfur.letterText>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.label>Подготовка к приёму изгнанников</TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.label>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.text>Война Небес и Ада выплёскивается прямо на ваш порог. Как поселение подготовится к появлению сверхъестественных перебежчиков?</TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.text>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.0.label>Подготовить енохианские обереги и комнаты для Кастиэля или Кроули</TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.0.label>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.0.resultText>Мастера настраивают защитные печати на воротах так, чтобы мятежные ангелы или беглый Король Ада могли войти, не попав в ваши ловушки.</TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.0.resultText>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.1.label>Запасти каменную соль и серебро на случай, если за ними явятся палачи</TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.1.label>
  <TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.1.resultText>Ящики в оружейной забиты освящённой солью и серебром — вы встретите карателей Небес и Ада во всеоружии.</TMM_Story_A3_DefectorLetter_WingsAndSulfur.stages.0.choices.1.resultText>

  <!-- TMM_Story_A3_DefectorLetter_CracksInConsortium -->
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.label>тайные письма: трещины в консорциуме</TMM_Story_A3_DefectorLetter_CracksInConsortium.label>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.letterLabel>Шифр из глубины Убежищ</TMM_Story_A3_DefectorLetter_CracksInConsortium.letterLabel>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.letterText>Ваша открытая позиция вызвала тихий мятеж в рядах исполнителей Консорциума. {GIVER} передаёт спешную, полуобгоревшую шифровку: доктор Хесс, доктор Брон и Дик Роман форсируют жатву — и несогласные внутри Волт-Тек, Изгоев и Просвещённых готовы бежать к вам, прихватив химический ключ против неуязвимости левиафанов.</TMM_Story_A3_DefectorLetter_CracksInConsortium.letterText>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.label>Координация прорыва изнутри</TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.label>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.text>Собственные архивариусы и смотрители Консорциума хотят вырваться наружу до того, как Кровавое затмение навсегда запрёт двери.</TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.text>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.0.label>Выставить дозорных на границах для встречи беглых архивариусов и смотрителей</TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.0.label>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.0.resultText>Наблюдатели на периметре получили условные знаки перебежчиков. Как только кто-то из них вступит в колонию, у вас появятся специалисты для расшифровки формулы буры.</TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.0.resultText>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.1.label>Попросить в первую очередь вынести файлы по сдерживанию левиафанов</TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.1.label>
  <TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.1.resultText>«Уже зашиты в подкладку плаща, — мигает ответная шифровка. — Только держите задние ворота открытыми».</TMM_Story_A3_DefectorLetter_CracksInConsortium.stages.0.choices.1.resultText>

  <!-- TMM_Story_A3_BoraxAndBlueprints -->
  <TMM_Story_A3_BoraxAndBlueprints.label>секрет буры</TMM_Story_A3_BoraxAndBlueprints.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.0.label>Сверка данных перебежчиков</TMM_Story_A3_BoraxAndBlueprints.stages.0.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.0.text>Завербованные вами перебежчики собираются у исследовательского терминала {COLONY}, сопоставляя вынесенные архивы своих фракций с Сектором 4 Голодиска-Ключа. Спустя несколько часов находится разгадка: точная химическая формула концентрированного тетрабората натрия (буры), лишающего левиафанов и Дика Романа неуязвимости, хранится на соседнем складе «Сукрокорп».</TMM_Story_A3_BoraxAndBlueprints.stages.0.text>
  <TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.0.effects.2.label>Бочки с катализатором буры захвачены</TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.0.effects.2.text>Ваш отряд прорвался мимо охраны левиафанов на складе «Сукрокорп» и вывез целые бочки концентрированного тетрабората натрия вместе с довоенными сплавами!</TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.1.effects.0.label>Синтез по лабораторным записям</TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.1.effects.0.text>Хотя чистильщики Консорциума успели сжечь склад до прибытия отряда, ваши перебежчики восстановили формулу распылителя буры по химическим уравнениям с Голодиска-Ключа.</TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.2.effects.0.label>Экстренный химический синтез</TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.2.effects.0.text>Отступив со склада под плотным огнём, исследователи всё же сумели воссоздать боевую формулу буры по спасённым пробникам.</TMM_Story_A3_BoraxAndBlueprints.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A3_BoraxAndBlueprints.stages.2.label>Оружие против бездны</TMM_Story_A3_BoraxAndBlueprints.stages.2.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.2.text>В мастерской едкий белый раствор буры стабилизирован и заправлен в баллоны под давлением — одновременно на главном экране завершается компиляция финальных чертежей Резонатора Искр.</TMM_Story_A3_BoraxAndBlueprints.stages.2.text>
  <TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.0.label>Загрузить чертежи распылителей и турелей с бурой в производство</TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.0.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.0.resultText>Механики заносят схемы распылителя и турели с бурой в рабочие столы. Левиафаны больше не будут неуязвимыми у ваших ворот.</TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.0.resultText>
  <TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.1.label>Синхронизировать формулу буры с гармоническими схемами Резонатора</TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.1.label>
  <TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.1.resultText>И чертежи защиты с бурой, и спецификация сборки Резонатора Искр полностью готовы к монтажу.</TMM_Story_A3_BoraxAndBlueprints.stages.2.choices.1.resultText>
  <TMM_Story_A3_BoraxAndBlueprints.onComplete.2.label>Формула буры и чертежи Резонатора готовы</TMM_Story_A3_BoraxAndBlueprints.onComplete.2.label>
  <TMM_Story_A3_BoraxAndBlueprints.onComplete.2.text>Ваши исследователи открыли производство распылителей и турелей с бурой! Теперь, когда оружие против левиафанов готово, пора собрать финальные материалы и построить Резонатор Искр.</TMM_Story_A3_BoraxAndBlueprints.onComplete.2.text>

  <!-- TMM_Story_A3_BuildSparkResonator -->
  <TMM_Story_A3_BuildSparkResonator.label>сборка резонатора искр</TMM_Story_A3_BuildSparkResonator.label>
  <TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.0.effects.0.label>Каркас Резонатора отлит</TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.0.effects.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.0.effects.0.text>Инженеры выточили пласталевый корпус сдерживания для Резонатора Искр. Теперь нужно выделить довоенное золото для енохианских токопроводящих нитей.</TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.0.effects.0.text>
  <TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.1.effects.0.label>Каркас собран из резервных плит</TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.1.effects.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.1.effects.0.text>Не теряя времени, механики сварили корпус из запасных плит Нулевого Якоря и перешли к золотым тоководам.</TMM_Story_A3_BuildSparkResonator.stages.0.outcomes.1.effects.0.text>
  <TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.0.effects.0.label>Енохианские нити вплетены</TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.0.effects.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.0.effects.0.text>Тончайшая золотая вязка вплавлена в кольца Резонатора, соединяя довоенную квантовую физику с древними небесными печатями.</TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.0.effects.0.text>
  <TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.1.effects.0.label>Тонкослойная проводка готова</TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.1.effects.0.text>Кузнецы вытянули каждую крупицу золота в тончайшие нити, замкнув контур до истечения срока.</TMM_Story_A3_BuildSparkResonator.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A3_BuildSparkResonator.stages.2.label>Гармоническая настройка</TMM_Story_A3_BuildSparkResonator.stages.2.label>
  <TMM_Story_A3_BuildSparkResonator.stages.2.text>Перед установкой управляющих компонентов Нулевой Синт должен сонастроить центральный кристалл Резонатора со своей духовной частотой. Как вы проведёте калибровку?</TMM_Story_A3_BuildSparkResonator.stages.2.text>
  <TMM_Story_A3_BuildSparkResonator.stages.2.choices.0.label>Провести плавную, безболезненную синхронизацию с Нулевым Синтом</TMM_Story_A3_BuildSparkResonator.stages.2.choices.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.2.choices.0.resultText>Мягкий голубой свет ровно струится по золотым нитям, не истощая Нулевого Синта. Кристалл отзывается чистым, устойчивым гулом.</TMM_Story_A3_BuildSparkResonator.stages.2.choices.0.resultText>
  <TMM_Story_A3_BuildSparkResonator.stages.2.choices.1.label>Подать максимальную мощность на кристалл ради предельной отдачи</TMM_Story_A3_BuildSparkResonator.stages.2.choices.1.label>
  <TMM_Story_A3_BuildSparkResonator.stages.2.choices.1.resultText>Молнии с треском пляшут по каркасу, фиксируя кристалл на пике мощности и оставляя Нулевого Синта бледным и обессиленным.</TMM_Story_A3_BuildSparkResonator.stages.2.choices.1.resultText>
  <TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.0.effects.0.label>Пульт управления смонтирован</TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.0.effects.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.0.effects.0.text>Последние компоненты установлены в разъёмы. Резонатор Искр готов к установке на постоянное место.</TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.0.effects.0.text>
  <TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.1.effects.0.label>Пульт собран на резервных платах</TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.1.effects.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.1.effects.0.text>Использовав резервные реле из бункера, техники завершили монтаж пульта как раз к моменту выбора площадки.</TMM_Story_A3_BuildSparkResonator.stages.3.outcomes.1.effects.0.text>
  <TMM_Story_A3_BuildSparkResonator.stages.4.label>Размещение Резонатора Искр</TMM_Story_A3_BuildSparkResonator.stages.4.label>
  <TMM_Story_A3_BuildSparkResonator.stages.4.text>Резонатор Искр полностью собран и гудит мощью, способной переписать судьбу Земли. Где вы установите машину перед запуском главного пульта?</TMM_Story_A3_BuildSparkResonator.stages.4.text>
  <TMM_Story_A3_BuildSparkResonator.stages.4.choices.0.label>Опустить прямо в открытую шахту Нулевого Якоря под {COLONY}</TMM_Story_A3_BuildSparkResonator.stages.4.choices.0.label>
  <TMM_Story_A3_BuildSparkResonator.stages.4.choices.0.resultText>Тяжёлые лебёдки опускают Резонатор в древний подземный зал, стыкуя его напрямую с ядром континентальных линий силы.</TMM_Story_A3_BuildSparkResonator.stages.4.choices.0.resultText>
  <TMM_Story_A3_BuildSparkResonator.stages.4.choices.1.label>Установить в укреплённом сердце центральной цитадели {COLONY}</TMM_Story_A3_BuildSparkResonator.stages.4.choices.1.label>
  <TMM_Story_A3_BuildSparkResonator.stages.4.choices.1.resultText>Толстые силовые кабели связывают Резонатор сквозь пол с Якорем внизу, ставя пульт прямо под прикрытием всех ваших турелей и бойцов.</TMM_Story_A3_BuildSparkResonator.stages.4.choices.1.resultText>
  <TMM_Story_A3_BuildSparkResonator.stages.4.choices.2.label>Запереть в экранированном святилище с енохианскими печатями</TMM_Story_A3_BuildSparkResonator.stages.4.choices.2.label>
  <TMM_Story_A3_BuildSparkResonator.stages.4.choices.2.resultText>Окружённый серебряными оберегами, Резонатор тихо гудит в укрытии — защищённый от случайных психических помех до самого часа расплаты.</TMM_Story_A3_BuildSparkResonator.stages.4.choices.2.resultText>
  <TMM_Story_A3_BuildSparkResonator.onComplete.3.label>Акт IV: Кровавое затмение</TMM_Story_A3_BuildSparkResonator.onComplete.3.label>
  <TMM_Story_A3_BuildSparkResonator.onComplete.3.text>Едва Резонатор Искр встаёт на место, ослепительный столб синего света пронзает облака — и солнце над Пустошью наливается густым, зловещим багрянцем. Началось Кровавое затмение! Лидеры всех фракций движутся к вашему пульту ко Второй точке невозврата.</TMM_Story_A3_BuildSparkResonator.onComplete.3.text>

</LanguageData>
```