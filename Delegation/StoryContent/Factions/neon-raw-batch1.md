`Defs/StoryDefs/Story_institute.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- INSTITUTE Q1: COLD SCANNER                                            -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_institute_Q1</defName>
    <label>cold scanner</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_justin_ayo</giver>
    <fallbackGiver>TMM_Hero_x6_88</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Cold Scanner</letterLabel>
    <letterText>Surface dweller. Our telemetry indicates an unregistered synthetic unit has taken shelter among the refugees at your settlement's waystation. The Synth Retention Bureau expects your full cooperation in identifying and securing Institute property. Comply, and you will be compensated; interfere, and we will consider your outpost hostile.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>hotel_scan</key>
        <label>Investigate the waystation refugees</label>
        <text>Three refugees have checked into {COLONY}'s waystation—one of them is an unregistered third-generation synth. Question the guests before the Synth Retention Bureau arrives.</text>
        <faction>TMM_RailroadFaction</faction>
        <groupSize>3</groupSize>
        <stayDays>2</stayDays>
        <objective>Investigate</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>noHotel</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>courser_ultimatum</key>
        <label>Courser at the perimeter</label>
        <text>Blue lightning flashes at the edge of {COLONY}: Institute Courser X6-88 and a squad of synths have teleported in, demanding you hand over the runaway unit immediately.</text>
        <choices>
          <li>
            <outcome>handed_over</outcome>
            <label>Hand the runaway synth over to Courser X6-88</label>
            <resultText>A rational decision. The Synth Retention Bureau values discipline and punctuality.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.inst_q1</flag>
                <value>handed_over</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>600</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.institute_lab_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>saved</outcome>
            <label>Hide the runaway synth and feed X6-88 a decoy transponder</label>
            <resultText>So the signal went cold. Very well—but the Bureau will keep watching your sector.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.inst_q1</flag>
                <value>saved</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>15</amount>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- INSTITUTE Q2: FIRMWARE GLITCH                                         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_institute_Q2</defName>
    <label>firmware glitch</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_alan_binet</giver>
    <fallbackGiver>TMM_Hero_madison_li</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_institute_Q1</li>
    </requires>
    <letterLabel>Firmware Glitch</letterLabel>
    <letterText>Please, keep this transmission off the main Bureau frequencies. One of my experimental third-generation units suffered a quantum cascade during a surface relay test—she is hurt, frightened, and displaying true emotional resonance. Patch her neural link before Ayo's trackers triangulate her signal.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>crash_site</key>
        <label>Secure the crashed relay pod</label>
        <text>Clear the scavengers converging on the broken Institute relay pod so your team can evacuate the wounded synth assistant Eve back to {COLONY}.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>medbay_stabilize</key>
        <label>Stabilizing the neural cascade</label>
        <text>In {COLONY}'s infirmary, your medics work furiously to calm Eve's overheating neural interface before Bureau drones triangulate her. Half-conscious, she quietly hums an old pre-war lullaby—the exact same tune only the Zero Synth knows.</text>
        <days>0.5</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>neural_decision</key>
        <label>Eve's neural matrix</label>
        <text>The Bureau tracking pulse is seconds away from locking onto your infirmary. You can wipe the Bureau tracker and preserve Eve's emerging personality as Alan Binet begs, or factory-reset her firmware and return her to Father.</text>
        <choices>
          <li>
            <outcome>preserved</outcome>
            <label>Restore her personality and erase the Bureau tracker</label>
            <resultText>You gave her the right to feel. I will not forget this when the reckoning comes.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.inst_q2</flag>
                <value>preserved</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>5</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentSpacer</thing>
                <count>2</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.synth_ceramic_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>wiped</outcome>
            <label>Reset her firmware to factory defaults and return her to Father</label>
            <resultText>A pity... another spark snuffed out. Still, Father will reward your loyalty.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.inst_q2</flag>
                <value>wiped</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentSpacer</thing>
                <count>4</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.synth_ceramic_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- INSTITUTE Q3: REACTOR ON THE BRINK                                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_institute_Q3</defName>
    <label>reactor on the brink</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_madison_li</giver>
    <fallbackGiver>TMM_Hero_father_shaun</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_institute_Q2</li>
    </requires>
    <letterLabel>Reactor on the Brink</letterLabel>
    <letterText>I do not care for surface politics, and I certainly do not care for Father's philosophical games, but our main fusion agitator is losing containment. Only one pre-war beryllium amplifier survived in the flooded Mass Fusion sub-levels. Retrieve it before the entire sector experiences a thermal meltdown.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>mass_fusion</key>
        <label>Retrieve the beryllium amplifier at Mass Fusion</label>
        <text>Fight through Brotherhood of Steel knights contesting the flooded Mass Fusion sub-levels and extract the pre-war beryllium amplifier.</text>
        <enemyFaction>TMM_BrotherhoodFaction</enemyFaction>
        <objective>Retrieve</objective>
        <thing>ComponentSpacer</thing>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>agitator_fate</key>
        <label>Fate of the beryllium amplifier</label>
        <text>Beside the reactor core, your team also found a pre-war engineer's skeleton clutching a note about secret uranium shipments to Vault-Tec. Now you hold the priceless beryllium amplifier—deliver it to Dr. Madison Li, or install it in {COLONY}'s own reactor?</text>
        <choices>
          <li>
            <outcome>institute</outcome>
            <label>Deliver the beryllium amplifier to Dr. Madison Li</label>
            <resultText>Finally, someone on the surface who knows how to get real work done. The reactor is stabilized.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.inst_q3</flag>
                <value>institute</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>1200</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.synth_implants</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>kept</outcome>
            <label>Keep the amplifier to supercharge {COLONY}'s own nuclear reactor</label>
            <resultText>You stole the future of science for your miserable little generator! I won't let this slide.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.inst_q3</flag>
                <value>kept</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Uranium</thing>
                <count>20</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.beryllium_reactor_mod</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- INSTITUTE Q4 (FACTION POINT): GHOST IN THE MACHINE                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_institute_Q4</defName>
    <label>ghost in the machine</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_father_shaun</giver>
    <fallbackGiver>TMM_Hero_justin_ayo</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_institute_Q3</li>
    </requires>
    <letterLabel>Ghost in the Machine</letterLabel>
    <letterText>You have proven yourself remarkably capable, my friend. Now we face a critical anomaly: a prototype synth has developed a self-sustaining spiritual resonance—a 'Spark'—that is attracting both angels and demons to our relay frequencies. Help X6-88 bring this specimen home so we may study and contain this flaw.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>chapel_clash</key>
        <label>Intercept the prototype at the ruined chapel</label>
        <text>An angel watcher and Institute Courser X6-88 have converged on the prototype synth inside a ruined chapel. Fight through the angelic strike squad to reach the relay beacon.</text>
        <enemyFaction>TMM_HeavenFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>beacon_choice</key>
        <label>The Spark at the relay beacon</label>
        <text>Before the chapel altar, the terrified prototype synth speaks an angel's true Enochian name—stopping the last watcher cold. Over your radio, Alan Binet begs you to spare the synth's soul, while Father calmly orders you to lock the teleporter beacon and extract its core.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Surrender the prototype to Father for Spark extraction (Side with the Institute)</label>
            <resultText>Hard choices define the future of humanity. The Spark telemetry is now in our hands.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.institute</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_experiment_18a</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.father_white_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Smash the Institute beacon and smuggle the synth to freedom (Protect the Spark)</label>
            <resultText>You betrayed the Institute's trust for a defective machine. From this hour on, the Retention Bureau shows no mercy.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.institute</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.courser_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.institute</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- INSTITUTE Q5 (DEFECTOR QUEST): ESCAPE FROM BELOW                      -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_institute_Q5</defName>
    <label>escape from below</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_alan_binet</giver>
    <fallbackGiver>TMM_Hero_madison_li</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_institute_Q4</li>
    </requires>
    <anyOf>
      <li>choice.institute=pro_spark</li>
      <li>ponr1=protect</li>
    </anyOf>
    <letterLabel>Escape from Below</letterLabel>
    <letterText>Father has handed full security control to Justin Ayo, and they are preparing to purge every third-generation synth in Robotics! Madison Li and I have rigged an emergency relay to your coordinates, but Coursers are already breaching our blast doors. Hold the perimeter!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>courser_pursuit</key>
        <label>Hold the relay against Institute Coursers</label>
        <text>Justin Ayo's Courser hit squads are teleporting into {COLONY} to stop Alan Binet and Dr. Madison Li from escaping! Defeat the hunters!</text>
        <faction>TMM_InstituteFaction</faction>
        <pointsFactor>1.25</pointsFactor>
        <delayHours>3</delayHours>
        <timeoutDays>3</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>welcome_scientists</key>
        <label>Refugees from Robotics</label>
        <text>In a blinding column of blue static, Alan Binet and Dr. Madison Li stumble onto your floor—followed by Alan's startled pet cat, which somehow survived quantum relay transit. Your techs jam the Institute's frequency overhead.</text>
        <choices>
          <li>
            <outcome>accept_defectors</outcome>
            <label>Welcome Alan Binet and Dr. Madison Li into {COLONY}</label>
            <resultText>We are free... and we will help you build the Spark Resonator to protect every last one of them.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_alan_binet</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_madison_li</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_alan_binet</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_madison_li</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-100</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.molecular_relay</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_institute.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_institute_Q1 -->
  <TMM_Story_institute_Q1.label>холодный сканер</TMM_Story_institute_Q1.label>
  <TMM_Story_institute_Q1.letterLabel>Холодный сканер</TMM_Story_institute_Q1.letterLabel>
  <TMM_Story_institute_Q1.letterText>Житель поверхности. Наша телеметрия указывает, что незарегистрированный синтетический образец укрылся среди беженцев в гостинице вашего поселения. Бюро возврата синтов ожидает вашего полного содействия в опознании и передаче собственности Института. Подчинитесь — и получите награду; помешаете — мы сочтём ваш аванпост враждебным.</TMM_Story_institute_Q1.letterText>
  <TMM_Story_institute_Q1.stages.0.label>Опросить беженцев в гостинице</TMM_Story_institute_Q1.stages.0.label>
  <TMM_Story_institute_Q1.stages.0.text>В гостиницу {COLONY} заселились трое беженцев, один из которых — скрытый синт третьего поколения. Проведите опрос постояльцев до прихода Бюро возврата.</TMM_Story_institute_Q1.stages.0.text>
  <TMM_Story_institute_Q1.stages.1.label>Охотник на периметре</TMM_Story_institute_Q1.stages.1.label>
  <TMM_Story_institute_Q1.stages.1.text>На границе {COLONY} вспыхивают синие молнии: охотник Института X6-88 с отрядом синтов телепортировался к воротам и требует немедленно выдать беглеца.</TMM_Story_institute_Q1.stages.1.text>
  <TMM_Story_institute_Q1.stages.1.choices.0.label>Выдать синта охотнику X6-88</TMM_Story_institute_Q1.stages.1.choices.0.label>
  <TMM_Story_institute_Q1.stages.1.choices.0.resultText>Разумное решение. Бюро возврата ценит дисциплину и пунктуальность.</TMM_Story_institute_Q1.stages.1.choices.0.resultText>
  <TMM_Story_institute_Q1.stages.1.choices.1.label>Скрыть беглеца и откупиться фальшивым маячком</TMM_Story_institute_Q1.stages.1.choices.1.label>
  <TMM_Story_institute_Q1.stages.1.choices.1.resultText>Что ж, сигнал оборвался. Но мы продолжим наблюдать за вашим сектором.</TMM_Story_institute_Q1.stages.1.choices.1.resultText>

  <!-- TMM_Story_institute_Q2 -->
  <TMM_Story_institute_Q2.label>сбой в прошивке</TMM_Story_institute_Q2.label>
  <TMM_Story_institute_Q2.letterLabel>Сбой в прошивке</TMM_Story_institute_Q2.letterLabel>
  <TMM_Story_institute_Q2.letterText>Прошу, держите этот канал в тайне от частот Бюро возврата. Один из моих экспериментальных образцов третьего поколения пережил квантовый сбой при выбросе на поверхность — она ранена, напугана и проявляет настоящий эмоциональный резонанс. Стабилизируйте её нейросеть, пока ищейки Айо не запеленговали сигнал.</TMM_Story_institute_Q2.letterText>
  <TMM_Story_institute_Q2.stages.0.label>Зачистить место падения капсулы</TMM_Story_institute_Q2.stages.0.label>
  <TMM_Story_institute_Q2.stages.0.text>Отбейте разбитую капсулу телепортации Института у наёмников, чтобы эвакуировать раненую синт-ассистентку Ив в лазарет {COLONY}.</TMM_Story_institute_Q2.stages.0.text>
  <TMM_Story_institute_Q2.stages.1.label>Стабилизация нейросети</TMM_Story_institute_Q2.stages.1.label>
  <TMM_Story_institute_Q2.stages.1.text>В лазарете {COLONY} медики спешно стабилизируют перегревшийся нейроинтерфейс Ив до прилёта поискового дрона. В полубреду она тихо напевает старую колыбельную, которую знает только Нулевой Синт.</TMM_Story_institute_Q2.stages.1.text>
  <TMM_Story_institute_Q2.stages.2.label>Нейроматрица Ив</TMM_Story_institute_Q2.stages.2.label>
  <TMM_Story_institute_Q2.stages.2.text>Поисковый импульс Бюро вот-вот запеленгует лазарет. Восстановите личность Ив и сотрите маячок, как умоляет Алан Бине, или сбросьте прошивку до заводской и верните её Отцу.</TMM_Story_institute_Q2.stages.2.text>
  <TMM_Story_institute_Q2.stages.2.choices.0.label>Восстановить личность и стереть маячок Бюро</TMM_Story_institute_Q2.stages.2.choices.0.label>
  <TMM_Story_institute_Q2.stages.2.choices.0.resultText>Вы дали ей право чувствовать. Я этого не забуду, когда придёт время платить по счетам.</TMM_Story_institute_Q2.stages.2.choices.0.resultText>
  <TMM_Story_institute_Q2.stages.2.choices.1.label>Сбросить прошивку до заводской и вернуть Отцу</TMM_Story_institute_Q2.stages.2.choices.1.label>
  <TMM_Story_institute_Q2.stages.2.choices.1.resultText>Жаль... ещё одна искра погасла. Но Отец оценит вашу лояльность.</TMM_Story_institute_Q2.stages.2.choices.1.resultText>

  <!-- TMM_Story_institute_Q3 -->
  <TMM_Story_institute_Q3.label>реактор на грани</TMM_Story_institute_Q3.label>
  <TMM_Story_institute_Q3.letterLabel>Реактор на грани</TMM_Story_institute_Q3.letterLabel>
  <TMM_Story_institute_Q3.letterText>Меня не интересует политика поверхности и философские игры Отца, но наш главный термоядерный реактор теряет стабильность. Единственный уцелевший бериллиевый усилитель находится на затопленных уровнях «Масс Фьюжн». Достаньте его, пока весь сектор не накрыло тепловым выбросом.</TMM_Story_institute_Q3.letterText>
  <TMM_Story_institute_Q3.stages.0.label>Добыть бериллиевый усилитель в «Масс Фьюжн»</TMM_Story_institute_Q3.stages.0.label>
  <TMM_Story_institute_Q3.stages.0.text>Пробейтесь сквозь десант Братства Стали на затопленных уровнях комплекса «Масс Фьюжн» и вынесите довоенный бериллиевый усилитель.</TMM_Story_institute_Q3.stages.0.text>
  <TMM_Story_institute_Q3.stages.1.label>Судьба бериллиевого усилителя</TMM_Story_institute_Q3.stages.1.label>
  <TMM_Story_institute_Q3.stages.1.text>В зале реактора отряд нашёл скелет довоенного инженера с запиской о тайных поставках урана для Волт-Тек. Теперь бесценный бериллиевый усилитель у вас в руках — отдать его доктору Мэдисон Ли или забрать для реактора {COLONY}?</TMM_Story_institute_Q3.stages.1.text>
  <TMM_Story_institute_Q3.stages.1.choices.0.label>Передать бериллиевый усилитель доктору Мэдисон Ли</TMM_Story_institute_Q3.stages.1.choices.0.label>
  <TMM_Story_institute_Q3.stages.1.choices.0.resultText>Наконец-то хоть кто-то на поверхности умеет работать руками. Реактор стабилизирован.</TMM_Story_institute_Q3.stages.1.choices.0.resultText>
  <TMM_Story_institute_Q3.stages.1.choices.1.label>Оставить усилитель для ядерного реактора своей колонии</TMM_Story_institute_Q3.stages.1.choices.1.label>
  <TMM_Story_institute_Q3.stages.1.choices.1.resultText>Вы украли будущее науки ради своего жалкого генератора! Я этого так не оставлю.</TMM_Story_institute_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_institute_Q4 -->
  <TMM_Story_institute_Q4.label>призрак в машине</TMM_Story_institute_Q4.label>
  <TMM_Story_institute_Q4.letterLabel>Призрак в машине</TMM_Story_institute_Q4.letterLabel>
  <TMM_Story_institute_Q4.letterText>Вы показали себя удивительно способным союзником, мой друг. Теперь перед нами критическая аномалия: в прототипе синта возник устойчивый духовный резонанс — «Искра», притягивающая к нашим частотам и ангелов, и демонов. Помогите X6-88 вернуть образец домой, чтобы мы могли изучить и устранить этот сбой.</TMM_Story_institute_Q4.letterText>
  <TMM_Story_institute_Q4.stages.0.label>Перехватить прототип в заброшенной часовне</TMM_Story_institute_Q4.stages.0.label>
  <TMM_Story_institute_Q4.stages.0.text>В заброшенную часовню к беглому прототипу одновременно ворвались ангел-наблюдатель и охотник X6-88. Пробейтесь сквозь небесный отряд к маяку телепортации.</TMM_Story_institute_Q4.stages.0.text>
  <TMM_Story_institute_Q4.stages.1.label>Искра у маяка телепортации</TMM_Story_institute_Q4.stages.1.label>
  <TMM_Story_institute_Q4.stages.1.text>У алтаря часовни синт смотрит в глаза ангелу и произносит его истинное енохианское имя, заставляя того отступить. По рации Алан Бине умоляет пощадить душу синта, а Отец спокойно приказывает активировать маяк для извлечения ядра.</TMM_Story_institute_Q4.stages.1.text>
  <TMM_Story_institute_Q4.stages.1.choices.0.label>Выдать синта Отцу для извлечения Искры (за Институт)</TMM_Story_institute_Q4.stages.1.choices.0.label>
  <TMM_Story_institute_Q4.stages.1.choices.0.resultText>Тяжёлые решения определяют будущее человечества. Данные Искры теперь в наших руках.</TMM_Story_institute_Q4.stages.1.choices.0.resultText>
  <TMM_Story_institute_Q4.stages.1.choices.1.label>Уничтожить маяк Института и спасти синта (за Искру)</TMM_Story_institute_Q4.stages.1.choices.1.label>
  <TMM_Story_institute_Q4.stages.1.choices.1.resultText>Вы предали доверие Института ради бракованной машины. Отныне Бюро возврата не знает пощады.</TMM_Story_institute_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_institute_Q5 -->
  <TMM_Story_institute_Q5.label>побег из-под земли</TMM_Story_institute_Q5.label>
  <TMM_Story_institute_Q5.letterLabel>Побег из-под земли</TMM_Story_institute_Q5.letterLabel>
  <TMM_Story_institute_Q5.letterText>Отец передал всю власть Джастину Айо, и они готовят полную зачистку отдела робототехники! Мы с Мэдисон Ли настроили аварийный телепорт на ваши координаты, но охотники уже режут гермодвери. Удержите периметр!</TMM_Story_institute_Q5.letterText>
  <TMM_Story_institute_Q5.stages.0.label>Отразить погоню охотников Института</TMM_Story_institute_Q5.stages.0.label>
  <TMM_Story_institute_Q5.stages.0.text>Карательные отряды охотников Джастина Айо телепортируются в {COLONY}, чтобы не дать Алану Бине и Мэдисон Ли сбежать! Удержите периметр!</TMM_Story_institute_Q5.stages.0.text>
  <TMM_Story_institute_Q5.stages.1.label>Беглецы из робототехники</TMM_Story_institute_Q5.stages.1.label>
  <TMM_Story_institute_Q5.stages.1.text>В ослепительном столбе синей статики на пол вываливаются Алан Бине и доктор Мэдисон Ли — а следом за ними домашний кот Алана, переживший квантовый перенос. Техники глушат частоты Института над базой.</TMM_Story_institute_Q5.stages.1.text>
  <TMM_Story_institute_Q5.stages.1.choices.0.label>Принять Алана Бине и Мэдисон Ли в колонию</TMM_Story_institute_Q5.stages.1.choices.0.label>
  <TMM_Story_institute_Q5.stages.1.choices.0.resultText>Мы свободны... и мы поможем вам построить Резонатор Искр, чтобы защитить каждого из них.</TMM_Story_institute_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_brotherhood.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- BROTHERHOOD Q1: THE LOST BEACON                                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_brotherhood_Q1</defName>
    <label>the lost beacon</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_paladin_danse</giver>
    <fallbackGiver>TMM_Hero_captain_kells</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>The Lost Beacon</letterLabel>
    <letterText>Civilian. My recon squad 'Gladius' went dark near an old satellite array three clicks from your position. High Command demands their holotags and field logs recovered immediately. Assist the Brotherhood in securing the perimeter, and you will earn the respect of the steel.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>satellite_array</key>
        <label>Clear the satellite array</label>
        <text>Secure the old satellite array surrounded by feral horrors and radiation craters so Paladin Danse can evacuate the wounded Brotherhood scribe.</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>holotag_handover</key>
        <label>The Gladius holotags</label>
        <text>Inside the bunker—whose wall bears a scratched Enochian sigil keeping feral ghouls at bay—you recover the fallen squad's holotags and encrypted field logs before the vertibird arrives.</text>
        <choices>
          <li>
            <outcome>honorable</outcome>
            <label>Return all holotags and sealed logs untouched to Paladin Danse</label>
            <resultText>You fought like a true soldier. The Brotherhood of Steel never forgets those who guard its flank.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.bos_q1</flag>
                <value>honorable</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>450</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.bos_scribe_uniform</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.danse_hood</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>copied</outcome>
            <label>Secretly copy the encrypted field logs before handing them over</label>
            <resultText>The holotags are intact, yet these logs show signs of tampering... I'll be watching you closely.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.bos_q1</flag>
                <value>copied</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>450</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.bos_scribe_uniform</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- BROTHERHOOD Q2: ARMOR FOR THE ASSAULT                                 -->
  <!-- Note: Proctor Ingram not in base HeroDefs; giver omitted per Rule 3.  -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_brotherhood_Q2</defName>
    <label>armor for the assault</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <fallbackGiver>TMM_Hero_paladin_danse</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_brotherhood_Q1</li>
    </requires>
    <letterLabel>Armor for the Assault</letterLabel>
    <letterText>Listen up, wastelander. Proctor Ingram here. My field repair gantry took a mortar hit, and half our T-60 hydraulic frames are locked up tighter than a vault door. Bring your best mechanics and spare components, and keep the Gunners off my back while I weld these coolant lines.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <key>deliver_parts</key>
        <label>Supply repair components for Ingram's gantry</label>
        <text>Allocate 6 industrial components from {COLONY}'s stockpiles so Proctor Ingram can replace the blown T-60 hydraulic valves.</text>
        <thing>ComponentIndustrial</thing>
        <count>6</count>
        <to>colony</to>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>hold_gantry</key>
        <label>Defend the Brotherhood repair gantry</label>
        <text>Hold the Brotherhood field repair dock against waves of Gunner mercenaries for 6 hours while Proctor Ingram—welding inside her legless power-armor frame—cracks sarcastic jokes under fire.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>6</holdHours>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>gantry_reward</key>
        <label>Hydraulics back online</label>
        <text>The last Gunner squad retreats as all three T-60 frames hiss back to life on the gantry.</text>
        <choices>
          <li>
            <outcome>complete_repairs</outcome>
            <label>Collect Ingram's uranium cells and recon suit blueprints</label>
            <resultText>Damn, your crew actually knows which end of a wrench to hold! Take a couple of fusion cores for the save.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.bos_q2</flag>
                <value>success</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Uranium</thing>
                <count>20</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.bos_recon_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- BROTHERHOOD Q3: LESSONS OF THE OLD ELDER                              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_brotherhood_Q3</defName>
    <label>lessons of the old elder</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_owen_lyons</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_sarah_lyons</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_brotherhood_Q2</li>
    </requires>
    <letterLabel>Lessons of the Old Elder</letterLabel>
    <letterText>Arthur Maxson speaks of steel and fire, and he is not wrong about the darkness gathering over this land. Yet if we crush the very people we swore to protect, what remains worth saving? Meet me at the crossroads settlement and help me broker peace before Kells orders an artillery strike.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>crossroads_approach</key>
        <label>Meeting Owen Lyons at the crossroads</label>
        <text>Old Owen Lyons waits outside the crossroads settlement, offering a worn canteen of pure Project Purity water while a tense Brotherhood patrol aims rifles at locals sheltering a wounded sentient ghoul.</text>
        <days>0.5</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>lyons_or_maxson</key>
        <label>Shield or sword</label>
        <text>The patrol commander demands the villagers step aside so the ghoul can be purged under Maxson's code. Owen Lyons looks to you to tip the scales.</text>
        <choices>
          <li>
            <outcome>mercy</outcome>
            <label>Convince the patrol to stand down and protect the settlers (Lyons' Way)</label>
            <resultText>You reminded them what it truly means to be humanity's shield. Thank you, my friend.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.bos_q3</flag>
                <value>mercy</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>400</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.sarah_lyons_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>purge</outcome>
            <label>Back Maxson's codex and clear the settlement of ghouls</label>
            <resultText>You chose harsh necessity... I pray our souls can endure the weight of what we do.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.bos_q3</flag>
                <value>purge</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>700</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gun_ChargeRifle</thing>
                <count>1</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- BROTHERHOOD Q4 (FACTION POINT): BLIND BETRAYAL                        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_brotherhood_Q4</defName>
    <label>blind betrayal</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_arthur_maxson</giver>
    <fallbackGiver>TMM_Hero_captain_kells</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_brotherhood_Q3</li>
    </requires>
    <letterLabel>Blind Betrayal</letterLabel>
    <letterText>Decrypted Institute records have revealed a vile truth: Paladin Danse is not a man, but a synthetic infiltrator carrying an anomalous 'Spark' in his neural core. He has fled to Listening Post Bravo. Find him and execute the sentence—humanity cannot allow machines to wear our skin.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>listening_post_bravo</key>
        <label>Reach Listening Post Bravo</label>
        <text>Fight past Institute trackers scouting the perimeter of Listening Post Bravo to reach the lower bunker where Paladin Danse awaits his fate.</text>
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>danse_verdict</key>
        <label>The verdict on Paladin Danse</label>
        <text>Kneeling unarmed inside the bunker, Paladin Danse pulls off his Brotherhood dog tags and holds them out to you: "Ad Victoriam." Outside the blast door, Elder Arthur Maxson stands waiting with a squad of paladins.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Execute Paladin Danse by Maxson's order (Side with the Brotherhood)</label>
            <resultText>You did what few would have the stomach to do. The steel is cleansed of taint.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.brotherhood</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.danseDead</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_final_judgment</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.bos_t60_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Spare Danse and stand between him and Arthur Maxson (Protect the Spark)</label>
            <resultText>From this day forward, he is dead to the Brotherhood! If he ever crosses our path again, I will personally grind him to dust!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.brotherhood</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.danseSaved</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.danse_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.brotherhood</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- BROTHERHOOD Q5 (DEFECTOR QUEST): MUTINY OF THE SENTINEL               -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_brotherhood_Q5</defName>
    <label>mutiny of the sentinel</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_sarah_lyons</giver>
    <fallbackGiver>TMM_Hero_paladin_danse</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_brotherhood_Q4</li>
      <li>choice.brotherhood=pro_spark</li>
    </requires>
    <letterLabel>Mutiny of the Sentinel</letterLabel>
    <letterText>Maxson has lost his mind—he's preparing an orbital and vertibird strike on every settlement sheltering synths or ghouls. My father and I won't be part of a slaughter. Danse and I are breaking through the cordon to your colony; cover our landing zone!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>kells_pursuit</key>
        <label>Repel Captain Kells's punitive strike</label>
        <text>Sarah Lyons and Paladin Danse have crash-landed a stolen vertibird inside {COLONY} while old Owen Lyons stayed aboard the flagship to buy them time. Repel Captain Kells's pursuing paladins!</text>
        <faction>TMM_BrotherhoodFaction</faction>
        <pointsFactor>1.3</pointsFactor>
        <delayHours>3</delayHours>
        <timeoutDays>3</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>welcome_lyons_danse</key>
        <label>Knights without an Elder</label>
        <text>With the pursuing Brotherhood shock team driven back, Sarah Lyons and Paladin Danse remove their helmets and stand before your command desk.</text>
        <choices>
          <li>
            <outcome>accept_defectors</outcome>
            <label>Welcome Sarah Lyons and Paladin Danse into {COLONY}'s ranks</label>
            <resultText>Our armor still serves the people—only now we choose for ourselves whom we protect.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_paladin_danse</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_sarah_lyons</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_paladin_danse</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_sarah_lyons</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Plasteel</thing>
                <count>40</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_brotherhood.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_brotherhood_Q1 -->
  <TMM_Story_brotherhood_Q1.label>потерянный маяк</TMM_Story_brotherhood_Q1.label>
  <TMM_Story_brotherhood_Q1.letterLabel>Потерянный маяк</TMM_Story_brotherhood_Q1.letterLabel>
  <TMM_Story_brotherhood_Q1.letterText>Гражданский. Мой разведвзвод «Гладиус» перестал выходить на связь у старого спутникового узла в трёх километрах от вашей позиции. Командование требует немедленно вернуть их жетоны и полевые журналы. Помогите Братству зачистить периметр, и вы заслужите уважение стали.</TMM_Story_brotherhood_Q1.letterText>
  <TMM_Story_brotherhood_Q1.stages.0.label>Зачистить спутниковый узел</TMM_Story_brotherhood_Q1.stages.0.label>
  <TMM_Story_brotherhood_Q1.stages.0.text>Зачистите старую станцию слежения, окружённую дикими гулями и фонящими воронками, чтобы паладин Данс смог эвакуировать раненого писца Братства.</TMM_Story_brotherhood_Q1.stages.0.text>
  <TMM_Story_brotherhood_Q1.stages.1.label>Жетоны разведвзвода «Гладиус»</TMM_Story_brotherhood_Q1.stages.1.label>
  <TMM_Story_brotherhood_Q1.stages.1.text>Внутри бункера, на стене которого выцарапана енохианская печать, отпугивающая диких гулей, вы находите жетоны павших бойцов и зашифрованные полевые журналы.</TMM_Story_brotherhood_Q1.stages.1.text>
  <TMM_Story_brotherhood_Q1.stages.1.choices.0.label>Вернуть все жетоны и секретные голодиски Дансу</TMM_Story_brotherhood_Q1.stages.1.choices.0.label>
  <TMM_Story_brotherhood_Q1.stages.1.choices.0.resultText>Вы действовали как настоящий солдат. Братство Стали не забывает тех, кто прикрывает спину.</TMM_Story_brotherhood_Q1.stages.1.choices.0.resultText>
  <TMM_Story_brotherhood_Q1.stages.1.choices.1.label>Скопировать данные с голодисков перед передачей</TMM_Story_brotherhood_Q1.stages.1.choices.1.label>
  <TMM_Story_brotherhood_Q1.stages.1.choices.1.resultText>Жетоны на месте, но в логах видны следы взлома... Я буду следить за вами.</TMM_Story_brotherhood_Q1.stages.1.choices.1.resultText>

  <!-- TMM_Story_brotherhood_Q2 -->
  <TMM_Story_brotherhood_Q2.label>броня для штурма</TMM_Story_brotherhood_Q2.label>
  <TMM_Story_brotherhood_Q2.letterLabel>Броня для штурма</TMM_Story_brotherhood_Q2.letterLabel>
  <TMM_Story_brotherhood_Q2.letterText>Слушай сюда, житель Пустоши. Говорит проктор Ингрэм. Мой полевой ремонтный док накрыло миномётом, и половину наших каркасов T-60 заклинило намертво. Присылай лучших механиков и запчасти и прикрой мне спину от Стрелков, пока я завариваю магистрали охлаждения.</TMM_Story_brotherhood_Q2.letterText>
  <TMM_Story_brotherhood_Q2.stages.0.label>Выделить запчасти для ремонтного дока</TMM_Story_brotherhood_Q2.stages.0.label>
  <TMM_Story_brotherhood_Q2.stages.0.text>Передайте 6 компонентов со складов {COLONY}, чтобы проктор Ингрэм смогла заменить пробитые клапаны силовой брони T-60.</TMM_Story_brotherhood_Q2.stages.0.text>
  <TMM_Story_brotherhood_Q2.stages.1.label>Удержать ремонтный док Братства</TMM_Story_brotherhood_Q2.stages.1.label>
  <TMM_Story_brotherhood_Q2.stages.1.text>Удерживайте полевой аванпост Братства от налётов Стрелков в течение 6 часов, пока Ингрэм в модифицированном каркасе силовой брони без ног отпускает саркастичные шутки под обстрелом.</TMM_Story_brotherhood_Q2.stages.1.text>
  <TMM_Story_brotherhood_Q2.stages.2.label>Гидравлика снова в строю</TMM_Story_brotherhood_Q2.stages.2.label>
  <TMM_Story_brotherhood_Q2.stages.2.text>Последняя волна Стрелков отступает, когда все три комплекта T-60 с шипением оживают в ремонтном доке.</TMM_Story_brotherhood_Q2.stages.2.text>
  <TMM_Story_brotherhood_Q2.stages.2.choices.0.label>Завершить ремонт всех трёх комплектов без потерь</TMM_Story_brotherhood_Q2.stages.2.choices.0.label>
  <TMM_Story_brotherhood_Q2.stages.2.choices.0.resultText>Чёрт возьми, у твоих ребят руки растут откуда надо! Держи пару энергоячеек за выручку.</TMM_Story_brotherhood_Q2.stages.2.choices.0.resultText>

  <!-- TMM_Story_brotherhood_Q3 -->
  <TMM_Story_brotherhood_Q3.label>уроки старого старейшины</TMM_Story_brotherhood_Q3.label>
  <TMM_Story_brotherhood_Q3.letterLabel>Уроки старого старейшины</TMM_Story_brotherhood_Q3.letterLabel>
  <TMM_Story_brotherhood_Q3.letterText>Артур Мэксон говорит о стали и огне, и он прав насчёт тьмы, сгущающейся над этой землёй. Но если мы раздавим тех, кого поклялись защищать, что останется спасать? Встреть меня в поселении на перекрёстке и помоги договориться миром, пока Келлс не отдал приказ об артобстреле.</TMM_Story_brotherhood_Q3.letterText>
  <TMM_Story_brotherhood_Q3.stages.0.label>Встреча с Оуэном Лайонсом на перекрёстке</TMM_Story_brotherhood_Q3.stages.0.label>
  <TMM_Story_brotherhood_Q3.stages.0.text>Старый Оуэн Лайонс ждёт у ворот посёлка, доставая потрёпанную флягу с чистой водой из проекта «Чистота», пока патруль Братства держит на прицеле местных жителей, укрывающих раненого разумного гуля.</TMM_Story_brotherhood_Q3.stages.0.text>
  <TMM_Story_brotherhood_Q3.stages.1.label>Щит или меч</TMM_Story_brotherhood_Q3.stages.1.label>
  <TMM_Story_brotherhood_Q3.stages.1.text>Командир патруля требует отступить в сторону, чтобы зачистить гуля по уставу Мэксона. Оуэн Лайонс смотрит на вас, ожидая вашего слова.</TMM_Story_brotherhood_Q3.stages.1.text>
  <TMM_Story_brotherhood_Q3.stages.1.choices.0.label>Убедить командира патруля отступить и защитить поселенцев (путь Лайонса)</TMM_Story_brotherhood_Q3.stages.1.choices.0.label>
  <TMM_Story_brotherhood_Q3.stages.1.choices.0.resultText>Ты напомнил им, что значит быть щитом человечества. Спасибо, друг.</TMM_Story_brotherhood_Q3.stages.1.choices.0.resultText>
  <TMM_Story_brotherhood_Q3.stages.1.choices.1.label>Поддержать устав Мэксона и зачистить поселение от гулей</TMM_Story_brotherhood_Q3.stages.1.choices.1.label>
  <TMM_Story_brotherhood_Q3.stages.1.choices.1.resultText>Ты выбрал суровую необходимость... Молюсь, чтобы наши души выдержали этот груз.</TMM_Story_brotherhood_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_brotherhood_Q4 -->
  <TMM_Story_brotherhood_Q4.label>слепой предатель</TMM_Story_brotherhood_Q4.label>
  <TMM_Story_brotherhood_Q4.letterLabel>Слепой предатель</TMM_Story_brotherhood_Q4.letterLabel>
  <TMM_Story_brotherhood_Q4.letterText>Расшифрованные архивы Института раскрыли мерзкую правду: паладин Данс не человек, а синтетический лазутчик с аномальной «Искрой» в нейроядре. Он бежал на пост прослушивания «Браво». Найдите его и приведите приговор в исполнение — человечество не позволит машинам носить нашу кожу.</TMM_Story_brotherhood_Q4.letterText>
  <TMM_Story_brotherhood_Q4.stages.0.label>Пробиться к посту прослушивания «Браво»</TMM_Story_brotherhood_Q4.stages.0.label>
  <TMM_Story_brotherhood_Q4.stages.0.text>Отогнав разведчиков Института на подступах к бункеру «Браво», вы спускаетесь внутрь, где безоружный Данс ждёт своей участи.</TMM_Story_brotherhood_Q4.stages.0.text>
  <TMM_Story_brotherhood_Q4.stages.1.label>Приговор паладину Дансу</TMM_Story_brotherhood_Q4.stages.1.label>
  <TMM_Story_brotherhood_Q4.stages.1.text>Стоя на коленях в глубине бункера, паладин Данс сам снимает свои жетоны Братства и протягивает их вам со словами «Ad Victoriam». За гермодверью уже ждёт Артур Мэксон в боевом плаще с отрядом паладинов.</TMM_Story_brotherhood_Q4.stages.1.text>
  <TMM_Story_brotherhood_Q4.stages.1.choices.0.label>Казнить паладина Данса по приказу Мэксона (за Братство)</TMM_Story_brotherhood_Q4.stages.1.choices.0.label>
  <TMM_Story_brotherhood_Q4.stages.1.choices.0.resultText>Ты сделал то, на что у многих не хватило бы духа. Сталь очищена от скверны.</TMM_Story_brotherhood_Q4.stages.1.choices.0.resultText>
  <TMM_Story_brotherhood_Q4.stages.1.choices.1.label>Укрыть Данса и убедить Мэксона или отбить его боем (за Искру)</TMM_Story_brotherhood_Q4.stages.1.choices.1.label>
  <TMM_Story_brotherhood_Q4.stages.1.choices.1.resultText>Отныне он мёртв для Братства! Если он снова попадётся нам на глаза — я лично сотру его в пыль!</TMM_Story_brotherhood_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_brotherhood_Q5 -->
  <TMM_Story_brotherhood_Q5.label>мятеж стража</TMM_Story_brotherhood_Q5.label>
  <TMM_Story_brotherhood_Q5.letterLabel>Мятеж Стража</TMM_Story_brotherhood_Q5.letterLabel>
  <TMM_Story_brotherhood_Q5.letterText>Мэксон окончательно потерял рассудок — он готовит удар винтокрылов по всем поселениям, укрывающим синтов и гулей. Мы с отцом не станем участвовать в бойне. Мы с Дансом прорываемся к вашей колонии — прикройте зону посадки!</TMM_Story_brotherhood_Q5.letterText>
  <TMM_Story_brotherhood_Q5.stages.0.label>Отразить карательный десант капитана Келлса</TMM_Story_brotherhood_Q5.stages.0.label>
  <TMM_Story_brotherhood_Q5.stages.0.text>Сара Лайонс и паладин Данс совершили аварийную посадку на угнанном винтокрыле, пока старый Оуэн Лайонс остался на флагмане задержать преследователей. Отразите штурм паладинов капитана Келлса!</TMM_Story_brotherhood_Q5.stages.0.text>
  <TMM_Story_brotherhood_Q5.stages.1.label>Рыцари без Старейшины</TMM_Story_brotherhood_Q5.stages.1.label>
  <TMM_Story_brotherhood_Q5.stages.1.text>Когда карательный отряд Братства отбит, Сара Лайонс и паладин Данс снимают шлемы перед вашим штабным столом.</TMM_Story_brotherhood_Q5.stages.1.text>
  <TMM_Story_brotherhood_Q5.stages.1.choices.0.label>Принять Сару Лайонс и паладина Данса в ряды колонии</TMM_Story_brotherhood_Q5.stages.1.choices.0.label>
  <TMM_Story_brotherhood_Q5.stages.1.choices.0.resultText>Наши доспехи всё ещё служат людям — просто теперь мы сами выбираем, кого защищать.</TMM_Story_brotherhood_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_outcasts.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- OUTCASTS Q1: SALVAGE PROTOCOL                                         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_outcasts_Q1</defName>
    <label>salvage protocol</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_OutcastsFaction</faction>
    <giver>TMM_Hero_specialist_olin</giver>
    <fallbackGiver>TMM_Hero_henry_casdin</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Salvage Protocol</letterLabel>
    <letterText>Wastelander. Unlike Lyons' soft-hearted fools, the Outcasts remember our true mission: keeping dangerous pre-war tech out of ignorant hands. Clear the cave-in at the Fort Independence sub-archive and bring us three intact power armor servo-modules.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>sub_archive</key>
        <label>Recover servo-modules from the collapsed sub-archive</label>
        <text>Clear the Fort Independence sub-archive and retrieve intact power-armor servo components from the pre-war vaults.</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>Retrieve</objective>
        <thing>ComponentIndustrial</thing>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>olin_handover</key>
        <label>Handover to Specialist Olin</label>
        <text>Inside the archive, your team also spotted pre-war Vault-Tec inspection logs checking the armor's radiation shielding. Specialist Olin meets your scouts at the perimeter to claim the three servo-modules.</text>
        <choices>
          <li>
            <outcome>delivered</outcome>
            <label>Hand all three servo-modules over to Specialist Olin</label>
            <resultText>Clean work and no foolish questions. Here is your share of supplies.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.out_q1</flag>
                <value>delivered</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>4</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.outcasts_recon_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- OUTCASTS Q2: COLD CALCULATION                                         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_outcasts_Q2</defName>
    <label>cold calculation</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_OutcastsFaction</faction>
    <giver>TMM_Hero_scribe_silas</giver>
    <fallbackGiver>TMM_Hero_henry_casdin</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_outcasts_Q1</li>
    </requires>
    <letterLabel>Cold Calculation</letterLabel>
    <letterText>Gunner mercenaries ambushed our supply convoy and stole three high-capacity fusion cells meant for our containment grid. Intercept their courier before he reaches their stronghold—and please, try to recover the cells intact.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>gunner_ambush</key>
        <label>Intercept the Gunner courier convoy</label>
        <text>Ambush the Gunner mercenaries hauling the stolen Outcast fusion cells before they reach their stronghold.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>silas_return</key>
        <label>Intact fusion cells</label>
        <text>Apart from a scorched crowbar dent where a Gunner courier tried prying a casing open, all three high-capacity fusion cells are intact. Young Scribe Silas awaits delivery.</text>
        <choices>
          <li>
            <outcome>returned</outcome>
            <label>Return the recovered fusion cells to Scribe Silas</label>
            <resultText>Not a single ruptured seal! Defender Casdin will be impressed.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.out_q2</flag>
                <value>returned</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>650</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentSpacer</thing>
                <count>3</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- OUTCASTS Q3: CAGE FOR A SHADOW                                        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_outcasts_Q3</defName>
    <label>cage for a shadow</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_OutcastsFaction</faction>
    <giver>TMM_Hero_henry_casdin</giver>
    <fallbackGiver>TMM_Hero_specialist_olin</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_outcasts_Q2</li>
    </requires>
    <letterLabel>Cage for a Shadow</letterLabel>
    <letterText>These 'anomalies' and demons bleeding into our world are nothing more than uncontained energy phenomena. We have built a stasis resonator, and we need a live subject. Lure a Shadow Entity into our trap and hold the line until the containment field locks.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>stasis_lure</key>
        <label>Hold the stasis resonator against lured entities</label>
        <text>The Outcast stasis trap on the edge of {COLONY} emits a shrill pulse, drawing nocturnal horrors straight into your killzone! Hold until the containment cylinder locks!</text>
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>1.1</pointsFactor>
        <delayHours>3</delayHours>
        <timeoutDays>3</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>deliver_specimen</key>
        <label>Frost on the containment glass</label>
        <text>Trapped inside the humming Outcast cylinder, the shadow entity traces frost across the glass—spelling out the exact coordinates of an underground ghoul hospital before going still.</text>
        <choices>
          <li>
            <outcome>captured</outcome>
            <label>Transfer the sealed containment pod to Defender Henry Casdin</label>
            <resultText>Magnificent. Now we possess raw telemetry that will overturn everything we thought we knew about this Rift.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.out_q3</flag>
                <value>captured</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>800</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.casdin_helmet</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- OUTCASTS Q4 (FACTION POINT): CRYPT UNDER THE HOSPITAL                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_outcasts_Q4</defName>
    <label>crypt under the hospital</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_OutcastsFaction</faction>
    <giver>TMM_Hero_henry_casdin</giver>
    <fallbackGiver>TMM_Hero_specialist_olin</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_outcasts_Q3</li>
    </requires>
    <letterLabel>Crypt Under the Hospital</letterLabel>
    <letterText>Our sensors located a pre-war micro-containment core beneath an old clinic run by wasteland ghouls. Without that core, our bunker's anomaly shields will fail within days. Extract the core—do not let sentiment for mutated corpses stand in the way of real humans.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>ghoul_hospital</key>
        <label>Standoff at the underground ghoul hospital</label>
        <text>Beneath the clinic, a pre-war reactor stamped "Project Purgatory — Sector 4" keeps dozens of sick ghoul patients alive. Hold the basement chamber as Henry Casdin's armored breach squad arrives to rip the core out.</text>
        <enemyFaction>TMM_OutcastsFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>4</holdHours>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>hospital_choice</key>
        <label>The reactor core of Sector 4</label>
        <text>Casdin's laser rifle hums as frightened ghoul doctors shield their patients. Behind Casdin, young Scribe Silas looks sickened by his commander's ruthlessness. Decide who gets the reactor core.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Rip out the containment core and hand it to Casdin (Side with the Outcasts)</label>
            <resultText>Brutal, yet effective. With allies like you, we will outlast any apocalypse.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.outcasts</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_protector_armor</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.outcasts_t45d_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Drive off Casdin's squad and save the hospital's patients (Protect the Living)</label>
            <resultText>You chose a ward full of rotting ghouls over securing vital technology?! You will regret this!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.outcasts</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>1000</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineUltratech</thing>
                <count>3</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.outcasts</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- OUTCASTS Q5 (DEFECTOR QUEST): STOLEN BLUEPRINTS                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_outcasts_Q5</defName>
    <label>stolen blueprints</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_OutcastsFaction</faction>
    <giver>TMM_Hero_scribe_silas</giver>
    <fallbackGiver>TMM_Hero_specialist_olin</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_outcasts_Q4</li>
      <li>choice.outcasts=pro_spark</li>
    </requires>
    <letterLabel>Stolen Blueprints</letterLabel>
    <letterText>Casdin has gone completely off the rails—he's planning to raid neutral settlements for every last scrap of tech! I copied his master armor schematics onto a holotape and ran. I'm heading for your gates, and two Outcast hit squads are right behind me!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>outcast_pursuit</key>
        <label>Repel the Outcast hit squads chasing Scribe Silas</label>
        <text>Exhausted and clutching a salvaged hospital scanner, Scribe Silas sprints through your gates with black-and-red power-armored Outcast squads right on his heels!</text>
        <faction>TMM_OutcastsFaction</faction>
        <pointsFactor>1.25</pointsFactor>
        <delayHours>2</delayHours>
        <timeoutDays>3</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>accept_silas</key>
        <label>A scribe with a conscience</label>
        <text>With the pursuing Outcast strike force defeated at your walls, Scribe Silas sets Casdin's master armor holotape on your workbench.</text>
        <choices>
          <li>
            <outcome>recruit_silas</outcome>
            <label>Shelter Scribe Silas and welcome him into {COLONY}</label>
            <resultText>Thank you for not turning me over. With these schematics, we'll forge armor every bit as tough as theirs.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_scribe_silas</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_scribe_silas</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_OutcastsFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.outcasts_t45d_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_outcasts.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_outcasts_Q1 -->
  <TMM_Story_outcasts_Q1.label>сбор металлолома</TMM_Story_outcasts_Q1.label>
  <TMM_Story_outcasts_Q1.letterLabel>Сбор металлолома</TMM_Story_outcasts_Q1.letterLabel>
  <TMM_Story_outcasts_Q1.letterText>Житель Пустоши. В отличие от мягкотелых глупцов Лайонса, Изгои помнят нашу истинную миссию: не давать опасные довоенные технологии в руки невежд. Расчистите завал в нижнем архиве форта Индепенденс и принесите три уцелевших сервомодуля силовой брони.</TMM_Story_outcasts_Q1.letterText>
  <TMM_Story_outcasts_Q1.stages.0.label>Добыть сервомодули в обрушенном архиве</TMM_Story_outcasts_Q1.stages.0.label>
  <TMM_Story_outcasts_Q1.stages.0.text>Расчистите нижний архив форта Индепенденс и вынесите уцелевшие сервомодули силовой брони из довоенных залов.</TMM_Story_outcasts_Q1.stages.0.text>
  <TMM_Story_outcasts_Q1.stages.1.label>Передача модулей специалисту Олин</TMM_Story_outcasts_Q1.stages.1.label>
  <TMM_Story_outcasts_Q1.stages.1.text>Внутри бункера обнаружились следы довоенной инспекции Волт-Тек, проверявшей радиационную защиту брони. Специалист Олин встречает разведчиков на границе карты.</TMM_Story_outcasts_Q1.stages.1.text>
  <TMM_Story_outcasts_Q1.stages.1.choices.0.label>Передать все три модуля специалисту Олин</TMM_Story_outcasts_Q1.stages.1.choices.0.label>
  <TMM_Story_outcasts_Q1.stages.1.choices.0.resultText>Чётко и без лишних вопросов. Вот твоя доля припасов.</TMM_Story_outcasts_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_outcasts_Q2 -->
  <TMM_Story_outcasts_Q2.label>холодный расчёт</TMM_Story_outcasts_Q2.label>
  <TMM_Story_outcasts_Q2.letterLabel>Холодный расчёт</TMM_Story_outcasts_Q2.letterLabel>
  <TMM_Story_outcasts_Q2.letterText>Наёмники Стрелков устроили засаду на наш конвой и украли три энергоячейки высокой ёмкости для нашей сети сдерживания. Перехватите их курьера, пока он не добрался до штаба — и постарайтесь вернуть ячейки целыми.</TMM_Story_outcasts_Q2.letterText>
  <TMM_Story_outcasts_Q2.stages.0.label>Перехватить курьера Стрелков</TMM_Story_outcasts_Q2.stages.0.label>
  <TMM_Story_outcasts_Q2.stages.0.text>Устройте засаду на караван Стрелков, везущий украденные энергоячейки Изгоев, прежде чем они доберутся до своего штаба.</TMM_Story_outcasts_Q2.stages.0.text>
  <TMM_Story_outcasts_Q2.stages.1.label>Уцелевшие энергоячейки</TMM_Story_outcasts_Q2.stages.1.label>
  <TMM_Story_outcasts_Q2.stages.1.text>Если не считать оплавленной вмятины от лома, которым курьер Стрелков пытался вскрыть корпус, все три энергоячейки целы. Молодой писец Сайлас ждёт груз.</TMM_Story_outcasts_Q2.stages.1.text>
  <TMM_Story_outcasts_Q2.stages.1.choices.0.label>Вернуть энергоячейки писцу Сайласу</TMM_Story_outcasts_Q2.stages.1.choices.0.label>
  <TMM_Story_outcasts_Q2.stages.1.choices.0.resultText>Ни единой царапины на корпусах! Защитник Касдин будет впечатлён.</TMM_Story_outcasts_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_outcasts_Q3 -->
  <TMM_Story_outcasts_Q3.label>клетка для тени</TMM_Story_outcasts_Q3.label>
  <TMM_Story_outcasts_Q3.letterLabel>Клетка для тени</TMM_Story_outcasts_Q3.letterLabel>
  <TMM_Story_outcasts_Q3.letterText>Все эти «аномалии» и демоны, просачивающиеся в наш мир, — не более чем неуправляемые энергетические феномены. Мы собрали стазис-резонатор и нуждаемся в живом образце. Заманите теневую сущность в нашу ловушку и удерживайте её, пока поле не замкнётся.</TMM_Story_outcasts_Q3.letterText>
  <TMM_Story_outcasts_Q3.stages.0.label>Удержать стазис-ловушку от приманенных тварей</TMM_Story_outcasts_Q3.stages.0.label>
  <TMM_Story_outcasts_Q3.stages.0.text>Стазис-резонатор Изгоев на окраине {COLONY} испускает пронзительный зов, притягивая ночных тварей прямо на ваши укрепления! Продержитесь, пока капсула не захлопнется!</TMM_Story_outcasts_Q3.stages.0.text>
  <TMM_Story_outcasts_Q3.stages.1.label>Изморозь на стекле капсулы</TMM_Story_outcasts_Q3.stages.1.label>
  <TMM_Story_outcasts_Q3.stages.1.text>Запертая в гудящей капсуле тень выводит изморозью на стекле точные координаты подземного госпиталя гулей, прежде чем затихнуть.</TMM_Story_outcasts_Q3.stages.1.text>
  <TMM_Story_outcasts_Q3.stages.1.choices.0.label>Передать захваченную сущность в капсуле Изгоям</TMM_Story_outcasts_Q3.stages.1.choices.0.label>
  <TMM_Story_outcasts_Q3.stages.1.choices.0.resultText>Великолепно. Теперь у нас есть источник данных, который перевернёт понимание этого Разлома.</TMM_Story_outcasts_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_outcasts_Q4 -->
  <TMM_Story_outcasts_Q4.label>склеп под госпиталем</TMM_Story_outcasts_Q4.label>
  <TMM_Story_outcasts_Q4.letterLabel>Склеп под госпиталем</TMM_Story_outcasts_Q4.letterLabel>
  <TMM_Story_outcasts_Q4.letterText>Наши сенсоры обнаружили довоенное ядро сдерживания под старой клиникой, которую заняли гули Пустоши. Без этого ядра щиты нашего бункера откажут через пару дней. Извлеките ядро — не позволяйте жалости к мутировавшим трупам встать на пути настоящих людей.</TMM_Story_outcasts_Q4.letterText>
  <TMM_Story_outcasts_Q4.stages.0.label>Противостояние в подземном госпитале гулей</TMM_Story_outcasts_Q4.stages.0.label>
  <TMM_Story_outcasts_Q4.stages.0.text>Под клиникой старый реактор с клеймом «Проект Чистилище — Сектор 4» поддерживает жизнь десятков больных гулей. Удержите зал, когда в него ворвётся отряд Касдина.</TMM_Story_outcasts_Q4.stages.0.text>
  <TMM_Story_outcasts_Q4.stages.1.label>Ядро реактора Сектора 4</TMM_Story_outcasts_Q4.stages.1.label>
  <TMM_Story_outcasts_Q4.stages.1.text>Лазерная винтовка Касдина гудит, пока испуганные врачи-гули заслоняют пациентов. За спиной Касдина молодой писец Сайлас с ужасом смотрит на жестокость командира. Решите судьбу ядра.</TMM_Story_outcasts_Q4.stages.1.text>
  <TMM_Story_outcasts_Q4.stages.1.choices.0.label>Вырвать ядро и отдать Касдину (за Изгоев)</TMM_Story_outcasts_Q4.stages.1.choices.0.label>
  <TMM_Story_outcasts_Q4.stages.1.choices.0.resultText>Жестоко, но эффективно. С такими союзниками мы переживём любой конец света.</TMM_Story_outcasts_Q4.stages.1.choices.0.resultText>
  <TMM_Story_outcasts_Q4.stages.1.choices.1.label>Защитить госпиталь от Изгоев и сохранить реактор (за людей и Искру)</TMM_Story_outcasts_Q4.stages.1.choices.1.label>
  <TMM_Story_outcasts_Q4.stages.1.choices.1.resultText>Вы выбрали кучку гниющих гулей вместо спасения технологии?! Вы пожалеете об этом!</TMM_Story_outcasts_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_outcasts_Q5 -->
  <TMM_Story_outcasts_Q5.label>украденные чертежи</TMM_Story_outcasts_Q5.label>
  <TMM_Story_outcasts_Q5.letterLabel>Украденные чертежи</TMM_Story_outcasts_Q5.letterLabel>
  <TMM_Story_outcasts_Q5.letterText>Касдин окончательно слетел с катушек — он готовит налёты на нейтральные поселения ради каждой крохи технологий! Я скопировал его главные чертежи брони на голодиск и сбежал. Я бегу к вашим воротам, а за мной по пятам идут два карательных отряда Изгоев!</TMM_Story_outcasts_Q5.letterText>
  <TMM_Story_outcasts_Q5.stages.0.label>Отбить погоню Изгоев за писцом Сайласом</TMM_Story_outcasts_Q5.stages.0.label>
  <TMM_Story_outcasts_Q5.stages.0.text>Измотанный писец Сайлас со спасённым медицинским сканером из госпиталя вбегает в ворота {COLONY}, а следом рвутся каратели Изгоев в чёрно-красной силовой броне!</TMM_Story_outcasts_Q5.stages.0.text>
  <TMM_Story_outcasts_Q5.stages.1.label>Писец с совестью</TMM_Story_outcasts_Q5.stages.1.label>
  <TMM_Story_outcasts_Q5.stages.1.text>Когда карательный отряд Изгоев разбит у ваших стен, писец Сайлас кладёт на верстак голодиск с главными чертежами силовой брони Касдина.</TMM_Story_outcasts_Q5.stages.1.text>
  <TMM_Story_outcasts_Q5.stages.1.choices.0.label>Укрыть Сайласа и принять его в колонию</TMM_Story_outcasts_Q5.stages.1.choices.0.label>
  <TMM_Story_outcasts_Q5.stages.1.choices.0.resultText>Спасибо, что не выдали. С этими чертежами мы соберём броню не хуже их хвалёных комплектов.</TMM_Story_outcasts_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_vault_tec.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- VAULT-TEC Q1: A GENEROUS OFFER                                        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_vault_tec_Q1</defName>
    <label>a generous offer</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_vaulttec_rep</giver>
    <fallbackGiver>TMM_Hero_gwen_mcnamara</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>A Generous Offer</letterLabel>
    <letterText>Good morning! Vault-Tec calling! You won't believe the luck—even after two hundred years, your settlement qualifies for our Platinum Resilience Package! Let me stop by your hospitality lounge to sign the forms—just watch out for some very rude raiders who want my briefcase.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>rep_visit</key>
        <label>Host the Vault-Tec Representative</label>
        <text>The Vault-Tec Rep has checked into {COLONY}'s waystation, passing out glossy pre-war registration forms and telling stories from 2077.</text>
        <faction>TMM_VaultTecFaction</faction>
        <hero>TMM_Hero_vaulttec_rep</hero>
        <groupSize>1</groupSize>
        <stayDays>1</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>noHotel</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>briefcase_raiders</key>
        <label>Repel the raiders hunting the briefcase</label>
        <text>Armed mercenaries have arrived at {COLONY} trying to steal the Vault-Tec Rep's pre-war cipher briefcase!</text>
        <faction>TMM_GunnersFaction</faction>
        <pointsFactor>0.9</pointsFactor>
        <delayHours>6</delayHours>
        <timeoutDays>3</timeoutDays>
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
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>sign_package</key>
        <label>Signature on the shipping receipt</label>
        <text>Handing back the recovered briefcase, you notice the pre-war shipping slip tucked into its latch bears an eerie signature: "R. Roman, 2077."</text>
        <choices>
          <li>
            <outcome>signed</outcome>
            <label>Sign the Platinum Resilience forms and return the briefcase</label>
            <resultText>Fantastic! You're officially enrolled! Here are genuine Vault jumpsuits—wear them with pride!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.vt_q1</flag>
                <value>signed</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>350</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.vault_jumpsuit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- VAULT-TEC Q2: SECTOR THREE LEAK                                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_vault_tec_Q2</defName>
    <label>sector three leak</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_gwen_mcnamara</giver>
    <fallbackGiver>TMM_Hero_vaulttec_rep</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_vault_tec_Q1</li>
    </requires>
    <letterLabel>Sector Three Leak</letterLabel>
    <letterText>This is Overseer McNamara. A seismic tremor ruptured the filtration seals in our lower hydroponics wing, flooding the sector with toxic coolant gas. Our engineers are trapped behind emergency bulkheads. Send a team with repair gear immediately!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>hydroponics_leak</key>
        <label>Seal the hydroponics filtration valves</label>
        <text>Hold the gas-choked lower hydroponics wing for 5 hours while your team seals the ruptured filtration pipes and evacuates Overseer McNamara's trapped engineers.</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>5</holdHours>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>sabotage_marks</key>
        <label>Saw marks on the coolant pipes</label>
        <text>As the air clears and the rescued engineers catch their breath, your mechanics spot clean saw-marks along the ruptured filtration pipes—sabotaged from the inside on Dr. Braun's quiet orders.</text>
        <choices>
          <li>
            <outcome>saved</outcome>
            <label>Report the sealed valves to Overseer Gwen McNamara</label>
            <resultText>You saved dozens of lives. My Vault owes your colony a debt we can never fully repay.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.vt_q2</flag>
                <value>saved</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>600</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>5</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.vault_security_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- VAULT-TEC Q3: IRON ORDER                                              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_vault_tec_Q3</defName>
    <label>iron order</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_alphonse_almodovar</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_gwen_mcnamara</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_vault_tec_Q2</li>
    </requires>
    <letterLabel>Iron Order</letterLabel>
    <letterText>Order is the sole wall separating Vault-Tec from the savagery of the surface. Subversive elements in my mining wing are threatening to breach the main vault door. Assist Chief Kendall in restoring absolute discipline.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>mining_wing_standoff</key>
        <label>Standoff at the Vault airlock</label>
        <text>Arriving at Overseer Almodovar's Vault airlock, you learn why the miners went on strike: their drills broke through bedrock into an ancient Enochian monolith humming directly beneath the Vault.</text>
        <days>0.6</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>strike_resolution</key>
        <label>Restoring discipline</label>
        <text>Security Chief Kendall waits for your signal to storm the barricade with shock batons—unless you step forward and broker a quiet settlement with the frightened miners.</text>
        <choices>
          <li>
            <outcome>crackdown</outcome>
            <label>Back Almodovar and crush the miners' strike by force</label>
            <resultText>Splendid. Everyone knows their place, and the air filters remain unpolluted.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.vt_q3</flag>
                <value>crackdown</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Steel</thing>
                <count>100</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>3</count>
              </li>
            </effects>
          </li>
          <li>
            <outcome>compromise</outcome>
            <label>Persuade Chief Kendall to grant concessions without bloodshed</label>
            <resultText>Unorthodox... yet the main door stayed shut and ore output has resumed.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.vt_q3</flag>
                <value>compromise</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Steel</thing>
                <count>100</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>3</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- VAULT-TEC Q4 (FACTION POINT): A TWO-HUNDRED-YEAR SLEEP                -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_vault_tec_Q4</defName>
    <label>a two-hundred-year sleep</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_stanislaus_braun</giver>
    <fallbackGiver>TMM_Hero_gwen_mcnamara</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_vault_tec_Q3</li>
    </requires>
    <letterLabel>A Two-Hundred-Year Sleep</letterLabel>
    <letterText>Ah, my clever little surface anomaly! You have found the key to my grandest theater—Vault 112! Step into my Tranquility Loungers and experience perfection, or perhaps we can discuss a mutually beneficial exchange regarding your little 'Spark' problem.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>vault_112</key>
        <label>Breach Vault 112's Tranquility Hall</label>
        <text>Fight past hired Gunner security protecting Vault 112 to reach the central Tranquility Lounger chamber where dozens of pre-war captives are trapped in endless virtual torment.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>braun_console</key>
        <label>The master terminal of Vault 112</label>
        <text>On Dr. Braun's master terminal, archived correspondence with "R. Roman" calculates soul quotas per Vault. Braun's smiling hologram beckons from the screen—cut a deal for G.E.C.K. tech and his Golden Pip-Boy, or pull the failsafe switch to grant the captives merciful peace.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Strike a bargain with Dr. Braun and leave Vault 112 running (Side with Vault-Tec)</label>
            <resultText>Wonderful! A true visionary! Here is your Golden Pip-Boy—enjoy it in good health, ha-ha!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.vault_tec</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_golden_pipboy</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.braun_sim_suit</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.geck_tech</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Trigger the emergency failsafe and free the captives in death (Side with Humanity)</label>
            <resultText>Barbarians! You ruined my masterpiece! I will wipe your miserable little settlement off the map!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.vault_tec</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.braunRomanLinkFound</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>1200</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.vault_tec</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- VAULT-TEC Q5 (DEFECTOR QUEST): OPEN BLAST DOORS                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_vault_tec_Q5</defName>
    <label>open blast doors</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_gwen_mcnamara</giver>
    <fallbackGiver>TMM_Hero_vaulttec_rep</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_vault_tec_Q4</li>
    </requires>
    <anyOf>
      <li>choice.vault_tec=pro_spark</li>
      <li>ponr1=protect</li>
    </anyOf>
    <letterLabel>Open Blast Doors</letterLabel>
    <letterText>The records you uncovered in Vault 112 sickened us all. Braun and Almodovar have locked down the central council and hired Gunner mercenaries to silence anyone who speaks out. Help me and the Vault-Tec Rep break out of our lockdown and bring our engineers to your colony!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>vault_blockade</key>
        <label>Break the Gunner blockade at McNamara's Vault</label>
        <text>Assault the Gunner mercenary cordon pinning Overseer Gwen McNamara and the Vault-Tec Rep inside the outer blast-door perimeter.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>welcome_vault_defectors</key>
        <label>Out from behind the steel doors</label>
        <text>With the Gunner blockade shattered, Overseer Gwen McNamara and the Vault-Tec Rep arrive safely at {COLONY}—where the Rep finally gets a clean room of his own after two centuries.</text>
        <choices>
          <li>
            <outcome>accept_defectors</outcome>
            <label>Welcome Gwen McNamara and the Vault-Tec Rep into {COLONY}</label>
            <resultText>We are done hiding behind steel doors. Our knowledge and technology now serve your colony.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_gwen_mcnamara</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_vaulttec_rep</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_gwen_mcnamara</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_vaulttec_rep</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>-75</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.vault_cryopods</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_vault_tec.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_vault_tec_Q1 -->
  <TMM_Story_vault_tec_Q1.label>выгодное предложение</TMM_Story_vault_tec_Q1.label>
  <TMM_Story_vault_tec_Q1.letterLabel>Выгодное предложение</TMM_Story_vault_tec_Q1.letterLabel>
  <TMM_Story_vault_tec_Q1.letterText>Доброе утро! Звонит Волт-Тек! Вы не поверите своей удаче — даже спустя двести лет ваше поселение подходит под наш Платиновый пакет выживания! Позвольте заглянуть в вашу гостиницу и оформить бумаги — только приглядите за парой грубых рейдеров, которым приглянулся мой чемоданчик.</TMM_Story_vault_tec_Q1.letterText>
  <TMM_Story_vault_tec_Q1.stages.0.label>Принять Представителя Волт-Тек</TMM_Story_vault_tec_Q1.stages.0.label>
  <TMM_Story_vault_tec_Q1.stages.0.text>Представитель Волт-Тек заселился в гостиницу {COLONY}, раздаёт бланки регистрации и травит довоенные байки из 2077 года.</TMM_Story_vault_tec_Q1.stages.0.text>
  <TMM_Story_vault_tec_Q1.stages.1.label>Отразить налёт за кодовым чемоданом</TMM_Story_vault_tec_Q1.stages.1.label>
  <TMM_Story_vault_tec_Q1.stages.1.text>Вооружённые налётчики нагрянули в {COLONY}, пытаясь выкрасть довоенный кодовый чемодан агента Волт-Тек!</TMM_Story_vault_tec_Q1.stages.1.text>
  <TMM_Story_vault_tec_Q1.stages.2.label>Подпись на квитанции</TMM_Story_vault_tec_Q1.stages.2.label>
  <TMM_Story_vault_tec_Q1.stages.2.text>Возвращая отбитый чемодан, вы замечаете на прикреплённой квитанции странную подпись: «Р. Роман, 2077 г.».</TMM_Story_vault_tec_Q1.stages.2.text>
  <TMM_Story_vault_tec_Q1.stages.2.choices.0.label>Подписать контракт и вернуть чемодан Представителю</TMM_Story_vault_tec_Q1.stages.2.choices.0.label>
  <TMM_Story_vault_tec_Q1.stages.2.choices.0.resultText>Фантастика! Вы официально в программе! Вот вам фирменные комбинезоны — носите с гордостью!</TMM_Story_vault_tec_Q1.stages.2.choices.0.resultText>

  <!-- TMM_Story_vault_tec_Q2 -->
  <TMM_Story_vault_tec_Q2.label>утечка в третьем секторе</TMM_Story_vault_tec_Q2.label>
  <TMM_Story_vault_tec_Q2.letterLabel>Утечка в третьем секторе</TMM_Story_vault_tec_Q2.letterLabel>
  <TMM_Story_vault_tec_Q2.letterText>Говорит смотритель Макнамара. Подземный толчок разорвал уплотнители фильтрации в нижнем крыле гидропоники, затопив сектор токсичным газом. Наши инженеры заперты за аварийными переборками. Немедленно пришлите команду с ремонтным снаряжением!</TMM_Story_vault_tec_Q2.letterText>
  <TMM_Story_vault_tec_Q2.stages.0.label>Запечатать клапаны фильтрации в гидропонике</TMM_Story_vault_tec_Q2.stages.0.label>
  <TMM_Story_vault_tec_Q2.stages.0.text>Удерживайте задымлённое нижнее крыло Убежища 5 часов, пока техники чинят клапаны фильтрации и выводят запертых инженеров Макнамары.</TMM_Story_vault_tec_Q2.stages.0.text>
  <TMM_Story_vault_tec_Q2.stages.1.label>Следы спила на трубах</TMM_Story_vault_tec_Q2.stages.1.label>
  <TMM_Story_vault_tec_Q2.stages.1.text>Когда газ рассеивается и спасённые инженеры переводят дух, механики замечают на трубах фильтрации ровные следы намеренного спила — диверсию изнутри по приказу доктора Брона.</TMM_Story_vault_tec_Q2.stages.1.text>
  <TMM_Story_vault_tec_Q2.stages.1.choices.0.label>Спасти всех инженеров и доложить смотрителю Гвен Макнамаре</TMM_Story_vault_tec_Q2.stages.1.choices.0.label>
  <TMM_Story_vault_tec_Q2.stages.1.choices.0.resultText>Вы спасли десятки жизней. Моё Убежище в неоплатном долгу перед вашей колонией.</TMM_Story_vault_tec_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_vault_tec_Q3 -->
  <TMM_Story_vault_tec_Q3.label>железный порядок</TMM_Story_vault_tec_Q3.label>
  <TMM_Story_vault_tec_Q3.letterLabel>Железный порядок</TMM_Story_vault_tec_Q3.letterLabel>
  <TMM_Story_vault_tec_Q3.letterText>Порядок — единственная стена, отделяющая Волт-Тек от дикости поверхности. Подрывные элементы в моём шахтёрском крыле угрожают вскрыть главную гермодверь. Помогите начальнику охраны Кендаллу восстановить абсолютную дисциплину.</TMM_Story_vault_tec_Q3.letterText>
  <TMM_Story_vault_tec_Q3.stages.0.label>Противостояние у шлюза Убежища</TMM_Story_vault_tec_Q3.stages.0.label>
  <TMM_Story_vault_tec_Q3.stages.0.text>Прибыв к шлюзу Убежища Альмодовара, вы узнаёте истинную причину забастовки: шахтёры прокопали стену и наткнулись на древний енохианский камень, гудящий прямо под Убежищем.</TMM_Story_vault_tec_Q3.stages.0.text>
  <TMM_Story_vault_tec_Q3.stages.1.label>Восстановление дисциплины</TMM_Story_vault_tec_Q3.stages.1.label>
  <TMM_Story_vault_tec_Q3.stages.1.text>Начальник охраны Кендалл ждёт вашего сигнала для силового разгона с шоковыми дубинками — если только вы не выйдете вперёд и не договоритесь с перепуганными шахтёрами миром.</TMM_Story_vault_tec_Q3.stages.1.text>
  <TMM_Story_vault_tec_Q3.stages.1.choices.0.label>Поддержать Альмодовара и подавить бунт</TMM_Story_vault_tec_Q3.stages.1.choices.0.label>
  <TMM_Story_vault_tec_Q3.stages.1.choices.0.resultText>Прекрасно. Каждый знает своё место, и воздух в фильтрах остаётся чистым.</TMM_Story_vault_tec_Q3.stages.1.choices.0.resultText>
  <TMM_Story_vault_tec_Q3.stages.1.choices.1.label>Убедить Кендалла пойти на уступки без кровопролития</TMM_Story_vault_tec_Q3.stages.1.choices.1.label>
  <TMM_Story_vault_tec_Q3.stages.1.choices.1.resultText>Неортодоксальный метод, но... двери остались закрыты, а добыча возобновилась.</TMM_Story_vault_tec_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_vault_tec_Q4 -->
  <TMM_Story_vault_tec_Q4.label>сон длиной в двести лет</TMM_Story_vault_tec_Q4.label>
  <TMM_Story_vault_tec_Q4.letterLabel>Сон длиной в двести лет</TMM_Story_vault_tec_Q4.letterLabel>
  <TMM_Story_vault_tec_Q4.letterText>Ах, моя смышлёная поверхностная аномалия! Вы нашли ключ к моему величайшему театру — Убежищу 112! Лягте в мои шезлонги «Транквилити» и познайте совершенство, или же мы обсудим взаимовыгодный обмен насчёт вашей маленькой проблемы с «Искрой».</TMM_Story_vault_tec_Q4.letterText>
  <TMM_Story_vault_tec_Q4.stages.0.label>Прорваться в зал капсул Убежища 112</TMM_Story_vault_tec_Q4.stages.0.label>
  <TMM_Story_vault_tec_Q4.stages.0.text>Пробейтесь сквозь наёмную охрану Убежища 112 в центральный зал шезлонгов «Транквилити», где десятки довоенных жителей заперты в вечной виртуальной пытке.</TMM_Story_vault_tec_Q4.stages.0.text>
  <TMM_Story_vault_tec_Q4.stages.1.label>Главный пульт Убежища 112</TMM_Story_vault_tec_Q4.stages.1.label>
  <TMM_Story_vault_tec_Q4.stages.1.text>В терминале Брона сохранена переписка с «Р. Романом» о расчёте квоты душ с каждого Убежища. Улыбающаяся голограмма Брона предлагает сделку за технологии ГЭКК и Золотой Пип-Бой — либо вы можете дёрнуть рубильник аварийного сброса.</TMM_Story_vault_tec_Q4.stages.1.text>
  <TMM_Story_vault_tec_Q4.stages.1.choices.0.label>Заключить сделку с Броном и оставить эксперимент в силе (за Волт-Тек)</TMM_Story_vault_tec_Q4.stages.1.choices.0.label>
  <TMM_Story_vault_tec_Q4.stages.1.choices.0.resultText>Чудесно! Настоящий визионер! Вот ваш Золотой Пип-Бой — пользуйтесь на здоровье, ха-ха!</TMM_Story_vault_tec_Q4.stages.1.choices.0.resultText>
  <TMM_Story_vault_tec_Q4.stages.1.choices.1.label>Запустить аварийный сброс и освободить пленников смертью (за человечность)</TMM_Story_vault_tec_Q4.stages.1.choices.1.label>
  <TMM_Story_vault_tec_Q4.stages.1.choices.1.resultText>Варвары! Вы разрушили мой шедевр! Я сотру ваше жалкое поселение с карты!</TMM_Story_vault_tec_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_vault_tec_Q5 -->
  <TMM_Story_vault_tec_Q5.label>открытые гермодвери</TMM_Story_vault_tec_Q5.label>
  <TMM_Story_vault_tec_Q5.letterLabel>Открытые гермодвери</TMM_Story_vault_tec_Q5.letterLabel>
  <TMM_Story_vault_tec_Q5.letterText>Записи, которые вы вскрыли в Убежище 112, ужаснули нас всех. Брон и Альмодовар заблокировали центральный совет и наняли Стрелков, чтобы заткнуть рот каждому несогласному. Помогите мне и Представителю Волт-Тек прорвать блокаду и вывести инженеров в вашу колонию!</TMM_Story_vault_tec_Q5.letterText>
  <TMM_Story_vault_tec_Q5.stages.0.label>Прорвать блокаду Стрелков у Убежища Макнамары</TMM_Story_vault_tec_Q5.stages.0.label>
  <TMM_Story_vault_tec_Q5.stages.0.text>Разгромите оцепление Стрелков, прижавших сторонников смотрителя Гвен Макнамары и Представителя Волт-Тек у внешнего шлюза Убежища.</TMM_Story_vault_tec_Q5.stages.0.text>
  <TMM_Story_vault_tec_Q5.stages.1.label>Прочь из-за стальных дверей</TMM_Story_vault_tec_Q5.stages.1.label>
  <TMM_Story_vault_tec_Q5.stages.1.text>Когда блокада Стрелков прорвана, Гвен Макнамара и Представитель Волт-Тек благополучно прибывают в {COLONY}, где Представитель наконец получает чистую комнату спустя два столетия.</TMM_Story_vault_tec_Q5.stages.1.text>
  <TMM_Story_vault_tec_Q5.stages.1.choices.0.label>Принять Гвен Макнамару и Представителя Волт-Тек в колонию</TMM_Story_vault_tec_Q5.stages.1.choices.0.label>
  <TMM_Story_vault_tec_Q5.stages.1.choices.0.resultText>Мы больше не прячемся за стальными дверями. Наши знания и технологии теперь служат вашей колонии.</TMM_Story_vault_tec_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_minutemen_hunters.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- MINUTEMEN & HUNTERS Q1: SALT AND SHOTGUN                              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_minutemen_hunters_Q1</defName>
    <label>salt and shotgun</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_dean_winchester</giver>
    <fallbackGiver>TMM_Hero_sam_winchester</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Salt and Shotgun</letterLabel>
    <letterText>Hey there, neighbor. Name's Dean. Me and my brother Sam are tracking a nasty son of a bitch—a wendigo that's been picking off caravans near your sector and wearing the face of a wounded trader. If a guy limps into your waystation asking for raw meat and a dark room, don't offer him pie—call us.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>wendigo_guests</key>
        <label>Question the wounded caravan guests</label>
        <text>A trio of battered caravan traders has limped into {COLONY}'s waystation—one of them is the wendigo in disguise. Investigate them closely before nightfall.</text>
        <faction>TMM_MinutemenHuntersFaction</faction>
        <groupSize>3</groupSize>
        <stayDays>1</stayDays>
        <objective>Investigate</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>noHotel</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>wendigo_hunt</key>
        <label>Put down the exposed wendigo</label>
        <text>Cornered at midnight, the wendigo sheds its human disguise and calls nocturnal horrors out of the dark! Fight alongside the Winchesters!</text>
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>0.9</pointsFactor>
        <delayHours>3</delayHours>
        <timeoutDays>3</timeoutDays>
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
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>pie_and_salt</key>
        <label>After the hunt</label>
        <text>With the wendigo burned to ash, Dean Winchester immediately raids your pantry looking for cherry pie—and leaves a handful of pre-war silver on the counter for his trouble.</text>
        <choices>
          <li>
            <outcome>clean</outcome>
            <label>Accept Dean's jacket pattern and rock salt bandolier</label>
            <resultText>Not bad at all! Real hunters. Here's a decent jacket pattern and a couple packs of rock salt.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mh_q1</flag>
                <value>clean</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>350</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.dean_leather_jacket</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.salt_bandolier</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MINUTEMEN & HUNTERS Q2: THE SALVAGE YARD GRIMOIRE                     -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_minutemen_hunters_Q2</defName>
    <label>the salvage yard grimoire</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_bobby_singer</giver>
    <fallbackGiver>TMM_Hero_sam_winchester</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_minutemen_hunters_Q1</li>
    </requires>
    <letterLabel>The Salvage Yard Grimoire</letterLabel>
    <letterText>Listen up, idjits! Sam found a pre-war occult ledger buried under three tons of rusted Buicks in my yard, and it mentions your 'Zero Anchor'. Problem is, half of Hell's crossroads lackeys smelled the ink and are swarming my perimeter. Get your boots over here and hold the line while Sam finishes the translation!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>bobbys_yard</key>
        <label>Hold Bobby Singer's salvage yard</label>
        <text>Defend Bobby Singer's fortified salvage yard against waves of crossroads demons for 6 hours while Sam translates the Sixty-Six Seals ledger and Bobby lures the demon commander into his iron panic room.</text>
        <enemyFaction>TMM_HellFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>6</holdHours>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>translation_done</key>
        <label>The 66 Seals decoded</label>
        <text>Sam closes the ancient ledger as the last demon burns out at the fence, confirming the direct link between the Sixty-Six Seals and the October 2077 nuclear blasts.</text>
        <choices>
          <li>
            <outcome>translated</outcome>
            <label>Take Sam's translation copy and Bobby's spare cap</label>
            <resultText>Well, at least somebody left in this world ain't completely hopeless. Take a copy of the translation and my spare cap.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mh_q2</flag>
                <value>translated</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.bobby_cap</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.sam_jacket</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MINUTEMEN & HUNTERS Q3: GUNS OF THE CASTLE                            -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_minutemen_hunters_Q3</defName>
    <label>guns of the castle</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_ronnie_shaw</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_preston_garvey</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_minutemen_hunters_Q2</li>
    </requires>
    <letterLabel>Guns of the Castle</letterLabel>
    <letterText>General's got faith in you, so I'll skip the speeches. A feral super mutant warband is massing near the southern ridge, ready to flatten three farming settlements. Help us haul shells, repair the old Castle mortars, and blow their war-beacon to kingdom come.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <key>deliver_shells</key>
        <label>Supply mortar shells for the Castle</label>
        <text>Deliver 6 high-explosive mortar shells from {COLONY}'s stockpiles so Ronnie Shaw can load the old Castle artillery battery—alongside a barrel of consecrated salt for good measure.</text>
        <thing>Shell_HighExplosive</thing>
        <count>6</count>
        <to>colony</to>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>hold_castle</key>
        <label>Hold the Castle battery against wild super mutants</label>
        <text>The first barrage shattered the mutant war-beacon, sending a furious super mutant counterattack straight toward the artillery outpost! Hold the line for 5 hours!</text>
        <enemyFaction>TMM_WildMutantsFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>5</holdHours>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>castle_secured</key>
        <label>Southern ridge cleared</label>
        <text>Smoke rises from the shattered super mutant camp on the southern ridge as the farming settlements signal all-clear.</text>
        <choices>
          <li>
            <outcome>victory</outcome>
            <label>Accept Minutemen artillery flares and Preston Garvey's hat pattern</label>
            <resultText>Fine work, soldier. With fire support like this, the wasteland can finally breathe free again.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mh_q3</flag>
                <value>victory</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>600</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.preston_hat</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.castle_artillery</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MINUTEMEN & HUNTERS Q4 (FACTION POINT): NIGHT AT THE ROADHOUSE        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_minutemen_hunters_Q4</defName>
    <label>night at the roadhouse</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_ellen_harvelle</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_dean_winchester</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_minutemen_hunters_Q3</li>
    </requires>
    <letterLabel>Night at the Roadhouse</letterLabel>
    <letterText>Drop everything and get to Harvelle's Roadhouse! A black storm rolled in out of nowhere, demonic raiders have surrounded the bar, and worse—one of the travelers we let inside is possessed and sabotaging our salt lines from the inside. We can't hold out till dawn alone!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>roadhouse_siege</key>
        <label>Defend Harvelle's Roadhouse</label>
        <text>Hold Harvelle's Roadhouse through the black storm for 6 hours against waves of possessed raiders and hellhounds while the corner jukebox blares 1970s classic rock.</text>
        <enemyFaction>TMM_HellFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>6</holdHours>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>roadhouse_exorcism</key>
        <label>The demon inside the bar</label>
        <text>Between waves, the front salt line smears away from the inside. Five frightened travelers huddle by the jukebox—one of them wears black eyes behind a panicked smile.</text>
        <choices>
          <li>
            <outcome>pro_spark</outcome>
            <label>Test each guest with holy water, expose the demon, and save every innocent (Protect the Living)</label>
            <resultText>You saved my home and my people. The Winchesters agreed you've earned the hunters' greatest trump card—take The Colt.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.minutemen_hunters</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_the_colt</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.minutemen_general_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_faction</outcome>
            <label>Sweep the room with gunfire indiscriminately to guarantee the demon dies (Ruthless)</label>
            <resultText>The demon is dead... and you gunned down innocent travelers right in front of my bar. Get out of my roadhouse!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.minutemen_hunters</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>900</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.enochian_rounds</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.minutemen_hunters</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MINUTEMEN & HUNTERS Q5 (DEFECTOR QUEST): COLD PRAGMATISM              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_minutemen_hunters_Q5</defName>
    <label>cold pragmatism</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_rufus_turner</giver>
    <fallbackGiver>TMM_Hero_preston_garvey</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_minutemen_hunters_Q4</li>
    </requires>
    <letterLabel>Cold Pragmatism</letterLabel>
    <letterText>Winchester and Garvey want to play nice with synths and friendly monsters, and maybe they're right—or maybe we're all gonna get burned. Either way, an old trapper like me knows which way the wind blows. Let's settle our business once and for all.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>bounty_ambush</key>
        <label>Clear the bounty hunters' ambush with Rufus Turner</label>
        <text>Meet veteran trapper Rufus Turner in the hills to spring an ambush on Gunner bounty hunters tracking local hunters—and settle where his loyalty lies.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>rufus_verdict</key>
        <label>An old trapper's loyalty</label>
        <text>With the smoke clearing over the ridge, Rufus Turner checks his rifle and sizes up {COLONY}'s stance on the Rift.</text>
        <choices>
          <li>
            <outcome>recruit_rufus</outcome>
            <label>Welcome Rufus Turner into {COLONY}'s garrison</label>
            <resultText>Fine—just don't go disarming the holy-water tripwires I string around my bunk. I'll wire the approaches myself.</resultText>
            <requires>
              <li>!ponr1=abyss</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_rufus_turner</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_rufus_turner</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.hunter_traps</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>veteran_schism</outcome>
            <label>[Abyss Path] Watch Rufus Turner and Ronnie Shaw break ranks and depart for the Brotherhood</label>
            <resultText>You cut deals with demons and expect old hunters to salute? Count us out—we're riding with steel from here on.</resultText>
            <requires>
              <li>ponr1=abyss</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.rufus_departed</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>15</amount>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_minutemen_hunters.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_minutemen_hunters_Q1 -->
  <TMM_Story_minutemen_hunters_Q1.label>соль и дробовик</TMM_Story_minutemen_hunters_Q1.label>
  <TMM_Story_minutemen_hunters_Q1.letterLabel>Соль и дробовик</TMM_Story_minutemen_hunters_Q1.letterLabel>
  <TMM_Story_minutemen_hunters_Q1.letterText>Эй, сосед. Меня зовут Дин. Мы с братом Сэмом идём по следу одной мерзкой твари — вендиго, который вырезает караваны в вашем секторе и прикидывается раненым торговцем. Если к вам в гостиницу прихромает парень и попросит сырого мяса и тёмную комнату — не угощай его пирогом, зови нас.</TMM_Story_minutemen_hunters_Q1.letterText>
  <TMM_Story_minutemen_hunters_Q1.stages.0.label>Опросить раненых караванщиков в гостинице</TMM_Story_minutemen_hunters_Q1.stages.0.label>
  <TMM_Story_minutemen_hunters_Q1.stages.0.text>В гостиницу {COLONY} прихромала группа израненных торговцев — один из них замаскированный вендиго. Присмотритесь к постояльцам до наступления ночи.</TMM_Story_minutemen_hunters_Q1.stages.0.text>
  <TMM_Story_minutemen_hunters_Q1.stages.1.label>Добить разоблачённого вендиго</TMM_Story_minutemen_hunters_Q1.stages.1.label>
  <TMM_Story_minutemen_hunters_Q1.stages.1.text>Загнанный в угол в полночь вендиго сбрасывает человеческую личину и скликает ночных тварей из темноты! Сражайтесь плечом к плечу с Винчестерами!</TMM_Story_minutemen_hunters_Q1.stages.1.text>
  <TMM_Story_minutemen_hunters_Q1.stages.2.label>После охоты</TMM_Story_minutemen_hunters_Q1.stages.2.label>
  <TMM_Story_minutemen_hunters_Q1.stages.2.text>Едва вендиго догорает дотла, Дин Винчестер тут же проверяет кладовку в поисках вишнёвого пирога и оставляет за него горсть серебра на стойке.</TMM_Story_minutemen_hunters_Q1.stages.2.text>
  <TMM_Story_minutemen_hunters_Q1.stages.2.choices.0.label>Вычислить вендиго без жертв среди мирных и добить его с Винчестерами</TMM_Story_minutemen_hunters_Q1.stages.2.choices.0.label>
  <TMM_Story_minutemen_hunters_Q1.stages.2.choices.0.resultText>Неплохо сработано! Настоящие охотники. Держи выкройку нормальной куртки и пару пачек соли.</TMM_Story_minutemen_hunters_Q1.stages.2.choices.0.resultText>

  <!-- TMM_Story_minutemen_hunters_Q2 -->
  <TMM_Story_minutemen_hunters_Q2.label>гримуар с автосвалки</TMM_Story_minutemen_hunters_Q2.label>
  <TMM_Story_minutemen_hunters_Q2.letterLabel>Гримуар с автосвалки</TMM_Story_minutemen_hunters_Q2.letterLabel>
  <TMM_Story_minutemen_hunters_Q2.letterText>Слушайте сюда, балбесы! Сэм откопал довоенный оккультный гроссбух под тремя тоннами ржавых «Бьюиков» на моей свалке, и там упоминается ваш «Нулевой Якорь». Проблема в том, что половина чертей с перекрёстка учуяла чернила и лезет через забор. Тащите сюда свои задницы и держите периметр, пока Сэм не закончит перевод!</TMM_Story_minutemen_hunters_Q2.letterText>
  <TMM_Story_minutemen_hunters_Q2.stages.0.label>Удержать автосвалку Бобби Сингера</TMM_Story_minutemen_hunters_Q2.stages.0.label>
  <TMM_Story_minutemen_hunters_Q2.stages.0.text>Обороняйте укреплённую автосвалку Бобби Сингера от волн демонов в течение 6 часов, пока Сэм переводит книгу Шестидесяти Шести Печатей, а Бобби заманивает командира демонов в железную комнату-ловушку.</TMM_Story_minutemen_hunters_Q2.stages.0.text>
  <TMM_Story_minutemen_hunters_Q2.stages.1.label>Связь с 66 Печатями расшифрована</TMM_Story_minutemen_hunters_Q2.stages.1.label>
  <TMM_Story_minutemen_hunters_Q2.stages.1.text>Сэм захлопывает древний гроссбух, когда последний демон сгорает у забора, подтверждая прямую связь между Шестьюдесятью Шестью Печатями и ядерным ударом 2077 года.</TMM_Story_minutemen_hunters_Q2.stages.1.text>
  <TMM_Story_minutemen_hunters_Q2.stages.1.choices.0.label>Удержать дом Бобби до завершения перевода Сэма</TMM_Story_minutemen_hunters_Q2.stages.1.choices.0.label>
  <TMM_Story_minutemen_hunters_Q2.stages.1.choices.0.resultText>Ну, хоть кто-то в этом мире не совсем безнадёжен. Забирайте копию перевода и мою запасную кепку.</TMM_Story_minutemen_hunters_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_minutemen_hunters_Q3 -->
  <TMM_Story_minutemen_hunters_Q3.label>орудия замка</TMM_Story_minutemen_hunters_Q3.label>
  <TMM_Story_minutemen_hunters_Q3.letterLabel>Орудия Замка</TMM_Story_minutemen_hunters_Q3.letterLabel>
  <TMM_Story_minutemen_hunters_Q3.letterText>Генерал в тебя верит, так что обойдёмся без речей. Дикая банда супермутантов собирается у южного хребта и готова раскатать три фермерских поселения. Помоги подтащить снаряды, починить старые мортиры Замка и разнести их боевой маяк к чертям.</TMM_Story_minutemen_hunters_Q3.letterText>
  <TMM_Story_minutemen_hunters_Q3.stages.0.label>Поставить миномётные снаряды для Замка</TMM_Story_minutemen_hunters_Q3.stages.0.label>
  <TMM_Story_minutemen_hunters_Q3.stages.0.text>Передайте 6 фугасных снарядов со складов {COLONY}, чтобы Ронни Шоу зарядила старую батарею Замка — вместе с бочкой освящённой соли против нечисти.</TMM_Story_minutemen_hunters_Q3.stages.0.text>
  <TMM_Story_minutemen_hunters_Q3.stages.1.label>Удержать батарею Замка от прорыва мутантов</TMM_Story_minutemen_hunters_Q3.stages.1.label>
  <TMM_Story_minutemen_hunters_Q3.stages.1.text>Первый залп разнёс боевой маяк мутантов, вызвав яростный ответный прорыв на артиллерийский аванпост! Удерживайте позицию 5 часов!</TMM_Story_minutemen_hunters_Q3.stages.1.text>
  <TMM_Story_minutemen_hunters_Q3.stages.2.label>Южный хребет зачищен</TMM_Story_minutemen_hunters_Q3.stages.2.label>
  <TMM_Story_minutemen_hunters_Q3.stages.2.text>Над разгромленным лагерем супермутантов поднимается дым, а фермерские поселения дают сигнал отбоя.</TMM_Story_minutemen_hunters_Q3.stages.2.text>
  <TMM_Story_minutemen_hunters_Q3.stages.2.choices.0.label>Уничтожить лагерь мутантов и защитить фермеров</TMM_Story_minutemen_hunters_Q3.stages.2.choices.0.label>
  <TMM_Story_minutemen_hunters_Q3.stages.2.choices.0.resultText>Отличная работа, солдат. С такой огневой поддержкой Пустошь снова может дышать свободно.</TMM_Story_minutemen_hunters_Q3.stages.2.choices.0.resultText>

  <!-- TMM_Story_minutemen_hunters_Q4 -->
  <TMM_Story_minutemen_hunters_Q4.label>ночь в дорожном доме</TMM_Story_minutemen_hunters_Q4.label>
  <TMM_Story_minutemen_hunters_Q4.letterLabel>Ночь в Дорожном доме</TMM_Story_minutemen_hunters_Q4.letterLabel>
  <TMM_Story_minutemen_hunters_Q4.letterText>Бросай всё и дуй в «Дорожный дом» Харвелл! Чёрная буря налетела из ниоткуда, одержимые рейдеры окружили бар, а хуже того — один из путников, которых мы впустили, одержим и стирает линии соли изнутри. До рассвета нам в одиночку не продержаться!</TMM_Story_minutemen_hunters_Q4.letterText>
  <TMM_Story_minutemen_hunters_Q4.stages.0.label>Оборонять «Дорожный дом» Харвелл</TMM_Story_minutemen_hunters_Q4.stages.0.label>
  <TMM_Story_minutemen_hunters_Q4.stages.0.text>Обороняйте придорожный бар Эллен Харвелл 6 часов от волн одержимых рейдеров и адских гончих, пока старый музыкальный автомат в углу крутит рок-н-ролл 70-х.</TMM_Story_minutemen_hunters_Q4.stages.0.text>
  <TMM_Story_minutemen_hunters_Q4.stages.1.label>Демон внутри бара</TMM_Story_minutemen_hunters_Q4.stages.1.label>
  <TMM_Story_minutemen_hunters_Q4.stages.1.text>В перерыве между атаками полоса соли у порога оказывается стёрта изнутри. У автомата жмутся пятеро напуганных путников — у одного из них под маской страха скрываются чёрные глаза.</TMM_Story_minutemen_hunters_Q4.stages.1.text>
  <TMM_Story_minutemen_hunters_Q4.stages.1.choices.0.label>Вычислить одержимого, изгнать демона и спасти всех постояльцев (за людей и Искру)</TMM_Story_minutemen_hunters_Q4.stages.1.choices.0.label>
  <TMM_Story_minutemen_hunters_Q4.stages.1.choices.0.resultText>Вы спасли мой дом и моих людей. Винчестеры решили, что вы достойны владеть главным козырем охотников — держите «Кольт».</TMM_Story_minutemen_hunters_Q4.stages.1.choices.0.resultText>
  <TMM_Story_minutemen_hunters_Q4.stages.1.choices.1.label>Зачистить зал огнём без разбора, чтобы гарантированно убить демона (жёсткий путь)</TMM_Story_minutemen_hunters_Q4.stages.1.choices.1.label>
  <TMM_Story_minutemen_hunters_Q4.stages.1.choices.1.resultText>Демон мёртв... но ты перестрелял невинных людей прямо у моей стойки. Убирайся из моего бара!</TMM_Story_minutemen_hunters_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_minutemen_hunters_Q5 -->
  <TMM_Story_minutemen_hunters_Q5.label>холодный прагматизм</TMM_Story_minutemen_hunters_Q5.label>
  <TMM_Story_minutemen_hunters_Q5.letterLabel>Холодный прагматизм</TMM_Story_minutemen_hunters_Q5.letterLabel>
  <TMM_Story_minutemen_hunters_Q5.letterText>Винчестер и Гарви хотят играть в благородство с синтами и «добрыми» монстрами — может, они и правы, а может, мы все сгорим. Так или иначе, старый капканщик вроде меня чует, откуда дует ветер. Давай окончательно решим, по пути ли нам.</TMM_Story_minutemen_hunters_Q5.letterText>
  <TMM_Story_minutemen_hunters_Q5.stages.0.label>Зачистить засаду охотников за головами с Руфусом Тёрнером</TMM_Story_minutemen_hunters_Q5.stages.0.label>
  <TMM_Story_minutemen_hunters_Q5.stages.0.text>Встретьте ветерана Руфуса Тёрнера в холмах, чтобы накрыть засаду наёмников Стрелков и окончательно решить, по пути ли вам.</TMM_Story_minutemen_hunters_Q5.stages.0.text>
  <TMM_Story_minutemen_hunters_Q5.stages.1.label>Верность старого капканщика</TMM_Story_minutemen_hunters_Q5.stages.1.label>
  <TMM_Story_minutemen_hunters_Q5.stages.1.text>Когда дым над хребтом рассеивается, Руфус Тёрнер проверяет винтовку и оценивает курс, который выбрала {COLONY}.</TMM_Story_minutemen_hunters_Q5.stages.1.text>
  <TMM_Story_minutemen_hunters_Q5.stages.1.choices.0.label>Принять Руфуса Тёрнера в гарнизон колонии</TMM_Story_minutemen_hunters_Q5.stages.1.choices.0.label>
  <TMM_Story_minutemen_hunters_Q5.stages.1.choices.0.resultText>Ладно, только чур мою комнату без спроса не минировать. Я сам заминирую подходы.</TMM_Story_minutemen_hunters_Q5.stages.1.choices.0.resultText>
  <TMM_Story_minutemen_hunters_Q5.stages.1.choices.1.label>[Путь Бездны] Проводить Руфуса Тёрнера и Ронни Шоу, уходящих к Братству Стали</TMM_Story_minutemen_hunters_Q5.stages.1.choices.1.label>
  <TMM_Story_minutemen_hunters_Q5.stages.1.choices.1.resultText>Крутишь сделки с демонами и ждёшь, что старые охотники возьмут под козырёк? Без нас — дальше мы идём со сталью.</TMM_Story_minutemen_hunters_Q5.stages.1.choices.1.resultText>

</LanguageData>
```