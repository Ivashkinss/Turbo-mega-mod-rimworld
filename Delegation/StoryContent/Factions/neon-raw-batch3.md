`Defs/StoryDefs/Story_wild_mutants.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- WILD MUTANTS Q1: GORE BAGS                                            -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_wild_mutants_Q1</defName>
    <label>gore bags</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_WildMutantsFaction</faction>
    <giver>TMM_Hero_master_butcher</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_fist</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Gore Bags</letterLabel>
    <letterText>Little meat-bags send signals! Master Butcher got three soft human traders hanging in cages at old slaughterhouse. Come bring shiny scrap or sharp knives—or come become dinner! Haha!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>slaughterhouse</key>
        <label>Raid the wild super mutant slaughterhouse</label>
        <text>Strike the old slaughterhouse under a rusted sign warning "Beware of Swan" and clear the mutant guards before the caged traders become dinner.</text>
        <enemyFaction>TMM_WildMutantsFaction</enemyFaction>
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
        <key>slaughterhouse_aftermath</key>
        <label>Beneath the "Beware of Swan" sign</label>
        <text>The cages hang open inside the reeking slaughterhouse. Decide how your scouts close the operation before sunrise.</text>
        <choices>
          <li>
            <outcome>rescued</outcome>
            <label>Quietly slip the freed traders out before the main pack returns</label>
            <resultText>Grrr! Sneaky little rats stole Butcher's dinner! Next time we smash you flat!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.wm_q1</flag>
                <value>rescued</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MealSurvivalPack</thing>
                <count>15</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.mutant_cleaver</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>slaughtered</outcome>
            <label>Cut down every last guard head-on and claim the slaughterhouse spoils</label>
            <resultText>Strong arms on little humans! Fist will remember your scent!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.wm_q1</flag>
                <value>slaughtered</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>10</amount>
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
                <thing>MealSurvivalPack</thing>
                <count>15</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.mutant_cleaver</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- WILD MUTANTS Q2: MAD CONVOY                                           -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_wild_mutants_Q2</defName>
    <label>mad convoy</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_WildMutantsFaction</faction>
    <giver>TMM_Hero_mad_racer</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_fist</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_wild_mutants_Q1</li>
    </requires>
    <letterLabel>Mad Convoy</letterLabel>
    <letterText>VROOM! BOOM! Mad Racer found shiny ticking footballs! Fast brothers strap mini-nukes to chests and run straight at your walls! Catch us if you can, little ants!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>suicider_rush</key>
        <label>Survive Mad Racer's explosive mutant rush</label>
        <text>The unmistakable rising beep of armed mini-nukes echoes across the valley—shoot down Mad Racer's charging wild super mutants before they reach {COLONY}'s walls!</text>
        <faction>TMM_WildMutantsFaction</faction>
        <pointsFactor>1.15</pointsFactor>
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
        <key>salvage_warheads</key>
        <label>Unexploded ordnance at the gates</label>
        <text>Smoking craters scar the approaches to {COLONY}, yet your sharpshooters dropped two runners cleanly before detonation—leaving intact high-explosive shells and catching Dead Eye's quiet notice.</text>
        <choices>
          <li>
            <outcome>survived</outcome>
            <label>Disarm the intact shells and salvage the scrap steel</label>
            <resultText>Haha! Loud fireworks! Your shooters are fast as bullets!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.wm_q2</flag>
                <value>survived</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Shell_HighExplosive</thing>
                <count>2</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>400</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Steel</thing>
                <count>100</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- WILD MUTANTS Q3: BEACON OF RAGE                                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_wild_mutants_Q3</defName>
    <label>beacon of rage</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_WildMutantsFaction</faction>
    <giver>TMM_Hero_dead_eye</giver>
    <fallbackGiver>TMM_Hero_fist</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_wild_mutants_Q2</li>
    </requires>
    <letterLabel>Beacon of Rage</letterLabel>
    <letterText>Human leader. I am Dead Eye. Unlike loud brothers, I watch and think. An old pre-war tower on the hill screams a high sound that burns our heads and makes brothers blind with rage. Break the tower—or make it scream at our enemies.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>ultrasonic_tower</key>
        <label>Reach the pre-war ultrasonic emitter tower</label>
        <text>Fight past frenzied super mutants surrounding the screaming hilltop tower to reach the control panel.</text>
        <enemyFaction>TMM_WildMutantsFaction</enemyFaction>
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
        <key>tower_panel</key>
        <label>Vault-Tec Aggression Control Experiment</label>
        <text>The steel control box carries an unmistakable pre-war crest: "Vault-Tec — Aggression Control Experiment." Smash the ultrasonic emitter to end the mutants' torment, or retune its frequency into a weapon for {COLONY}?</text>
        <choices>
          <li>
            <outcome>destroyed</outcome>
            <label>Smash the ultrasonic tower and end the mutants' agony</label>
            <resultText>Silence... For the first time in long years, my head is quiet. I will remember this debt, human leader.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.wm_q3</flag>
                <value>destroyed</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>700</count>
              </li>
            </effects>
          </li>
          <li>
            <outcome>weaponized</outcome>
            <label>Retune the tower's frequency as a berserk pulse against {COLONY}'s enemies</label>
            <resultText>You are sly as a deathclaw. Let your enemies go mad from that howl now.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.wm_q3</flag>
                <value>weaponized</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_VaultTecFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.ultrasonic_suppressor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- WILD MUTANTS Q4 (FACTION POINT): ROAR FROM THE DEEP (BOSS: SWAN)      -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_wild_mutants_Q4</defName>
    <label>roar from the deep</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_WildMutantsFaction</faction>
    <giver>TMM_Hero_fist</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_dead_eye</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_wild_mutants_Q3</li>
    </requires>
    <letterLabel>Roar from the Deep</letterLabel>
    <letterText>Little leader! Swan wakes from deep dark water! Ground shakes, trees snap! Fist says: if you are strong, come to sunken park and fight the King of Behemoths! Beat Swan, and wild horde bows to your strength—or gets crushed beneath his anchor!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>swan_pond</key>
        <label>Slay the Behemoth King Swan in the sunken park</label>
        <text>Enter the flooded pre-war park and bring down the colossal behemoth Swan—wielding a ship's anchor and a twin-barrel nuclear catapult—to stop the wild horde from crashing the final siege.</text>
        <enemyFaction>TMM_WildMutantsFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_swan</hero> <!-- check id -->
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.swanKilledAtPond</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.swanKilledAtPond</flag>
                <value>false</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.swanKilledAtPond</flag>
                <value>false</value>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>swans_legacy</key>
        <label>Edgar Swann's faded badge</label>
        <text>Around the sunken park gazebo lie torn pages of an old janitor's diary belonging to "Edgar Swann", the handwriting growing larger and more frantic day by day until only rage remained.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>[Swan Slain] Claim the Right of Strength, The Big Boy, and Swan's anchor</label>
            <resultText>Oooooh... The mountain fell... Fist and brothers see: little leader is stronger than Behemoth! Take big boom-gun and anchor—we pull back into the deep wilds!</resultText>
            <requires>
              <li>story.swanKilledAtPond=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.wild_mutants</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.swanDefeated</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_big_boy</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.swan_anchor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>swan_lives</outcome>
            <label>[Swan Still Loose] Fall back—the King of Behemoths still roams the wasteland</label>
            <resultText>Fist laughs wildly from the ruins as Swan's roar shakes the ground. When the final siege begins, the wild horde will come crashing down on everyone!</resultText>
            <requires>
              <li>!story.swanKilledAtPond=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.wild_mutants</flag>
                <value>compromise</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.swanDefeated</flag>
                <value>false</value>
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
        <flag>arc.wild_mutants</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- WILD MUTANTS Q5 (DEFECTOR QUEST): THE MARKSMAN'S RESPECT              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_wild_mutants_Q5</defName>
    <label>the marksman's respect</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_WildMutantsFaction</faction>
    <giver>TMM_Hero_dead_eye</giver>
    <fallbackGiver>TMM_Hero_fist</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_wild_mutants_Q4</li>
    </requires>
    <anyOf>
      <li>story.swanDefeated=true</li>
      <li>story.wm_q3=destroyed</li>
    </anyOf>
    <letterLabel>The Marksman's Respect</letterLabel>
    <letterText>Swan is gone, and Fist wants to throw the tribe into a mindless slaughter against men in power armor. I am tired of dying for loud fools. Let me test my rifle beside your sharpshooters—if you hit the mark, I stand on your walls.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>hunting_trial</key>
        <label>Pass Dead Eye's hunting trial</label>
        <text>Clear a den of apex wasteland beasts alongside Dead Eye to prove your colony's marksmanship.</text>
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
        <key>recruit_dead_eye</key>
        <label>A polished sniper scope</label>
        <text>Dead Eye—the only mutant in the wild pack who keeps his rifle scope spotless inside a leather pouch—nods once in calm approval.</text>
        <choices>
          <li>
            <outcome>accept_dead_eye</outcome>
            <label>Welcome Dead Eye onto {COLONY}'s watchtowers</label>
            <resultText>Good shot, leader. Give me a high tower and clean ammo—no enemy will reach your gate alive.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_dead_eye</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_dead_eye</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gun_SniperRifle</thing>
                <count>1</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_wild_mutants.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_wild_mutants_Q1 -->
  <TMM_Story_wild_mutants_Q1.label>мясные мешки</TMM_Story_wild_mutants_Q1.label>
  <TMM_Story_wild_mutants_Q1.letterLabel>Мясные мешки</TMM_Story_wild_mutants_Q1.letterLabel>
  <TMM_Story_wild_mutants_Q1.letterText>Маленькие мясные мешки шлют сигналы! У Мастера-мясника три мягких торговца людей висят в клетках на старой скотобойне. Приходите нести блестящий лом или острые ножи — или сами станете ужином! Ха-ха!</TMM_Story_wild_mutants_Q1.letterText>
  <TMM_Story_wild_mutants_Q1.stages.0.label>Совершить вылазку на скотобойню диких мутантов</TMM_Story_wild_mutants_Q1.stages.0.label>
  <TMM_Story_wild_mutants_Q1.stages.0.text>Проникните на старую скотобойню под ржавой вывеской «Берегитесь Лебедя» и перебейте охрану мутантов до того, как пленные торговцы станут ужином.</TMM_Story_wild_mutants_Q1.stages.0.text>
  <TMM_Story_wild_mutants_Q1.stages.1.label>Под вывеской «Берегитесь Лебедя»</TMM_Story_wild_mutants_Q1.stages.1.label>
  <TMM_Story_wild_mutants_Q1.stages.1.text>Клетки распахнуты посреди смрадной скотобойни. Решите, как ваши разведчики завершат вылазку до рассвета.</TMM_Story_wild_mutants_Q1.stages.1.text>
  <TMM_Story_wild_mutants_Q1.stages.1.choices.0.label>Тихо взломать клетки и увести всех пленников без потерь</TMM_Story_wild_mutants_Q1.stages.1.choices.0.label>
  <TMM_Story_wild_mutants_Q1.stages.1.choices.0.resultText>Гррр! Хитрые маленькие крысы украли ужин Мясника! В следующий раз раздавим!</TMM_Story_wild_mutants_Q1.stages.1.choices.0.resultText>
  <TMM_Story_wild_mutants_Q1.stages.1.choices.1.label>Перебить охрану скотобойни в лобовом бою</TMM_Story_wild_mutants_Q1.stages.1.choices.1.label>
  <TMM_Story_wild_mutants_Q1.stages.1.choices.1.resultText>Сильные руки у мелких людей! Кувалда запомнит ваш запах!</TMM_Story_wild_mutants_Q1.stages.1.choices.1.resultText>

  <!-- TMM_Story_wild_mutants_Q2 -->
  <TMM_Story_wild_mutants_Q2.label>безумный конвой</TMM_Story_wild_mutants_Q2.label>
  <TMM_Story_wild_mutants_Q2.letterLabel>Безумный конвой</TMM_Story_wild_mutants_Q2.letterLabel>
  <TMM_Story_wild_mutants_Q2.letterText>ВРУМ! БУМ! Безумный Гонщик нашёл блестящие тикающие мячики! Быстрые братья привязали мини-заряды к груди и бегут прямо к вашим стенам! Ловите нас, мелкие муравьи!</TMM_Story_wild_mutants_Q2.letterText>
  <TMM_Story_wild_mutants_Q2.stages.0.label>Пережить атаку камикадзе Безумного Гонщика</TMM_Story_wild_mutants_Q2.stages.0.label>
  <TMM_Story_wild_mutants_Q2.stages.0.text>Над долиной нарастает зловещий писк взведённых мини-зарядов — отстреляйте бегущих диких супермутантов на подступах к стенам {COLONY}!</TMM_Story_wild_mutants_Q2.stages.0.text>
  <TMM_Story_wild_mutants_Q2.stages.1.label>Неразорвавшиеся заряды у ворот</TMM_Story_wild_mutants_Q2.stages.1.label>
  <TMM_Story_wild_mutants_Q2.stages.1.text>Подступы к {COLONY} покрыты дымящимися воронками, но ваши стрелки сняли двоих бегунов до детонации, добыв целые снаряды и привлекая тихое внимание Мёртвого Глаза.</TMM_Story_wild_mutants_Q2.stages.1.text>
  <TMM_Story_wild_mutants_Q2.stages.1.choices.0.label>Отстрелять камикадзе на дальних подступах и забрать трофейные заряды</TMM_Story_wild_mutants_Q2.stages.1.choices.0.label>
  <TMM_Story_wild_mutants_Q2.stages.1.choices.0.resultText>Ха-ха! Громкий фейерверк! Твои стрелки быстрые, как пули!</TMM_Story_wild_mutants_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_wild_mutants_Q3 -->
  <TMM_Story_wild_mutants_Q3.label>маяк ярости</TMM_Story_wild_mutants_Q3.label>
  <TMM_Story_wild_mutants_Q3.letterLabel>Маяк ярости</TMM_Story_wild_mutants_Q3.letterLabel>
  <TMM_Story_wild_mutants_Q3.letterText>Вожак людей. Я — Мёртвый Глаз. В отличие от шумных братьев, я смотрю и думаю. Старая довоенная вышка на холме кричит высоким звуком, который жжёт головы и сводит братьев с ума от ярости. Сломай вышку — или заставь её кричать на наших врагов.</TMM_Story_wild_mutants_Q3.letterText>
  <TMM_Story_wild_mutants_Q3.stages.0.label>Прорваться к довоенной ультразвуковой вышке</TMM_Story_wild_mutants_Q3.stages.0.label>
  <TMM_Story_wild_mutants_Q3.stages.0.text>Пробейтесь мимо обезумевших мутантов к пульту управления кричащей вышки на холме.</TMM_Story_wild_mutants_Q3.stages.0.text>
  <TMM_Story_wild_mutants_Q3.stages.1.label>«Волт-Тек — Эксперимент контроля агрессии»</TMM_Story_wild_mutants_Q3.stages.1.label>
  <TMM_Story_wild_mutants_Q3.stages.1.text>На стальном щитке вышки выбит довоенный логотип: «Волт-Тек — Эксперимент контроля агрессии». Разбить излучатель и облегчить боль мутантов или перенастроить его против врагов {COLONY}?</TMM_Story_wild_mutants_Q3.stages.1.text>
  <TMM_Story_wild_mutants_Q3.stages.1.choices.0.label>Уничтожить ультразвуковую вышку и облегчить боль мутантов</TMM_Story_wild_mutants_Q3.stages.1.choices.0.label>
  <TMM_Story_wild_mutants_Q3.stages.1.choices.0.resultText>Тишина... Впервые за долгие годы в голове тихо. Я запомню этот долг, вожак людей.</TMM_Story_wild_mutants_Q3.stages.1.choices.0.resultText>
  <TMM_Story_wild_mutants_Q3.stages.1.choices.1.label>Перенастроить частоту вышки как оружие против врагов колонии</TMM_Story_wild_mutants_Q3.stages.1.choices.1.label>
  <TMM_Story_wild_mutants_Q3.stages.1.choices.1.resultText>Ты хитрый, как когтистый зверь. Пусть теперь враги сходят с ума от этого воя.</TMM_Story_wild_mutants_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_wild_mutants_Q4 -->
  <TMM_Story_wild_mutants_Q4.label>рёв из глубины</TMM_Story_wild_mutants_Q4.label>
  <TMM_Story_wild_mutants_Q4.letterLabel>Рёв из глубины</TMM_Story_wild_mutants_Q4.letterLabel>
  <TMM_Story_wild_mutants_Q4.letterText>Мелкий вожак! Лебедь проснулся в глубокой тёмной воде! Земля дрожит, деревья ломаются! Кувалда говорит: если ты сильный, приходи в затопленный парк и сразись с Королём Бегемотов! Победишь Лебедя — дикая орда признает твою силу, или сгинет под его якорем!</TMM_Story_wild_mutants_Q4.letterText>
  <TMM_Story_wild_mutants_Q4.stages.0.label>Сразиться с Королём Бегемотов Лебедем в затопленном парке</TMM_Story_wild_mutants_Q4.stages.0.label>
  <TMM_Story_wild_mutants_Q4.stages.0.text>Высадитесь в затопленный довоенный парк и одолейте исполинского Лебедя с корабельным якорем и сдвоенным ядерным гранатомётом, чтобы орда не ударила в спину в финальной осаде.</TMM_Story_wild_mutants_Q4.stages.0.text>
  <TMM_Story_wild_mutants_Q4.stages.1.label>Выцветший бейдж Эдгара Свонна</TMM_Story_wild_mutants_Q4.stages.1.label>
  <TMM_Story_wild_mutants_Q4.stages.1.text>У беседки затопленного парка лежат разорванные страницы дневника старого уборщика по имени «Эдгар Свонн» — с каждым днём буквы в нём становились всё крупнее и бессвязнее, пока не осталась одна ярость.</TMM_Story_wild_mutants_Q4.stages.1.text>
  <TMM_Story_wild_mutants_Q4.stages.1.choices.0.label>[Лебедь повержен] Заявить право сильного и забрать «Большой мальчик» и якорь Лебедя</TMM_Story_wild_mutants_Q4.stages.1.choices.0.label>
  <TMM_Story_wild_mutants_Q4.stages.1.choices.0.resultText>Оооо... Гора упала... Кувалда и братья видят: мелкий вожак сильнее бегемота! Забирай большую пушку и якорь — мы уходим в глушь!</TMM_Story_wild_mutants_Q4.stages.1.choices.0.resultText>
  <TMM_Story_wild_mutants_Q4.stages.1.choices.1.label>[Лебедь на свободе] Отступить — Король Бегемотов всё ещё бродит по Пустоши</TMM_Story_wild_mutants_Q4.stages.1.choices.1.label>
  <TMM_Story_wild_mutants_Q4.stages.1.choices.1.resultText>Кувалда дико хохочет из руин под сотрясающий землю рёв Лебедя. Когда начнётся финальная осада, дикая орда обрушится на всех подряд!</TMM_Story_wild_mutants_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_wild_mutants_Q5 -->
  <TMM_Story_wild_mutants_Q5.label>уважение снайпера</TMM_Story_wild_mutants_Q5.label>
  <TMM_Story_wild_mutants_Q5.letterLabel>Уважение снайпера</TMM_Story_wild_mutants_Q5.letterLabel>
  <TMM_Story_wild_mutants_Q5.letterText>Лебедя больше нет, и Кувалда хочет бросить племя на бессмысленную бойню против людей в силовой броне. Я устал умирать за крикливых дураков. Позволь мне проверить винтовку рядом с твоими стрелками — если попадёшь в цель, я встану на твои стены.</TMM_Story_wild_mutants_Q5.letterText>
  <TMM_Story_wild_mutants_Q5.stages.0.label>Пройти испытание меткости с Мёртвым Глазом</TMM_Story_wild_mutants_Q5.stages.0.label>
  <TMM_Story_wild_mutants_Q5.stages.0.text>Зачистите логово опасных тварей Пустоши плечом к плечу с Мёртвым Глазом, доказав меткость ваших стрелков.</TMM_Story_wild_mutants_Q5.stages.0.text>
  <TMM_Story_wild_mutants_Q5.stages.1.label>Чистая оптика в кожаном чехле</TMM_Story_wild_mutants_Q5.stages.1.label>
  <TMM_Story_wild_mutants_Q5.stages.1.text>Мёртвый Глаз — единственный из дикой стаи, кто держит свой оптический прицел протёртым до блеска в кожаном чехле — спокойно кивает в знак уважения.</TMM_Story_wild_mutants_Q5.stages.1.text>
  <TMM_Story_wild_mutants_Q5.stages.1.choices.0.label>Принять Мёртвого Глаза в гарнизон колонии</TMM_Story_wild_mutants_Q5.stages.1.choices.0.label>
  <TMM_Story_wild_mutants_Q5.stages.1.choices.0.resultText>Хороший выстрел, вожак. Дай мне высокую башню и патроны — ни один враг не подойдёт к воротам живым.</TMM_Story_wild_mutants_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_men_of_letters.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- MEN OF LETTERS Q1: A POLITE PROPOSITION                               -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_men_of_letters_Q1</defName>
    <label>a polite proposition</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_mick_davies</giver>
    <fallbackGiver>TMM_Hero_antonia_bevell</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>A Polite Proposition</letterLabel>
    <letterText>Good day to you. My name is Mick Davies, representing the British Men of Letters. We've observed your settlement's rather impressive resilience against supernatural incursions. Allow me to visit your waystation—I've brought an Enochian suppression beacon that could make your perimeter entirely invisible to angels and demons alike.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>mick_tea_visit</key>
        <label>Host Mick Davies at the waystation</label>
        <text>Mick Davies checks into {COLONY}'s waystation—bringing real imported British tea and politely grimacing at local wasteland brew—while setting up his prototype Enochian beacon.</text>
        <faction>TMM_MenOfLettersFaction</faction>
        <hero>TMM_Hero_mick_davies</hero>
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
        <key>beacon_field_test</key>
        <label>Field-test the Enochian beacon against lured anomalies</label>
        <text>The trial pulse from Mick's beacon draws nocturnal horrors toward {COLONY}! Neutralize the test subjects!</text>
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>0.85</pointsFactor>
        <delayHours>4</delayHours>
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
        <key>sign_london_accord</key>
        <label>Data-sharing accord</label>
        <text>Satisfied with the field telemetry, Mick Davies dusts off his tweed jacket and offers an official London exchange agreement.</text>
        <choices>
          <li>
            <outcome>tested</outcome>
            <label>Complete the beacon test and sign the data-sharing accord</label>
            <resultText>Splendid! London will be thrilled. Here are our runic suit patterns—wear them in good health.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mol_q1</flag>
                <value>tested</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.mol_runic_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MEN OF LETTERS Q2: CLEAN SWEEP                                        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_men_of_letters_Q2</defName>
    <label>clean sweep</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_kendricks</giver>
    <fallbackGiver>TMM_Hero_arthur_ketch</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_men_of_letters_Q1</li>
    </requires>
    <letterLabel>Clean Sweep</letterLabel>
    <letterText>Attention. A nest of feral vampires has established itself in an abandoned railway depot six miles from your border. London requires absolute containment—no survivors, no witnesses, no messy local hunting methods. Accompany my tactical unit and execute a textbook sweep.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>vampire_depot</key>
        <label>Execute a clean sweep of the vampire railway depot</label>
        <text>Assault the abandoned railway depot by night and wipe out the feral vampire nest down to the last monster.</text>
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
        <key>depot_crate</key>
        <label>Crates from Richard Roman Enterprises</label>
        <text>Inside the cleared railway depot sits a wooden shipping crate stamped "Richard Roman Enterprises", packed tight with pre-war preserved blood pouches. Mr. Kendricks calmly inspects your squad's work.</text>
        <choices>
          <li>
            <outcome>swept</outcome>
            <label>Report a textbook sweep to Mr. Kendricks</label>
            <resultText>Disciplined and devoid of needless emotion. A rarity on this continent. You have earned your compensation.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mol_q2</flag>
                <value>swept</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>750</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.enochian_grenades</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.runic_ammo</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MEN OF LETTERS Q3: THE INTERROGATION ROOM                             -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_men_of_letters_Q3</defName>
    <label>the interrogation room</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_antonia_bevell</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_dr_hess</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_men_of_letters_Q2</li>
    </requires>
    <letterLabel>The Interrogation Room</letterLabel>
    <letterText>We have captured a high-ranking crossroads demon who knows the exact frequency of the Zero Anchor's containment seals. However, he is stubborn, and my colleagues in London require results tonight. Assist me at our field black-site—whether by arcane extraction or psychological pressure.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>blacksite_cell</key>
        <label>Inside the London field black-site</label>
        <text>Beneath a painted ceiling Devil's Trap, Lady Antonia Bevell lays out ritual silver blades before a bound crossroads demon—who laughs in her face, sneering that her own London bosses work for Leviathans.</text>
        <days>0.6</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>demon_interrogation</key>
        <label>Extracting the seal frequencies</label>
        <text>Antonia Bevell raises a glowing Enochian iron. Allow her to proceed with arcane torture, or step in and pry the truth out through cunning and a deal?</text>
        <choices>
          <li>
            <outcome>torture</outcome>
            <label>Let Lady Antonia Bevell conduct a ruthless arcane interrogation</label>
            <resultText>Pain opens every lock. The coordinates are ours, and you proved you are not afraid to soil your hands.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mol_q3</flag>
                <value>torture</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>800</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.seal_intel</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>persuaded</outcome>
            <label>Halt the torture and extract the truth by wit and bargain</label>
            <resultText>Hmm... Unexpectedly effective, though far too soft for my taste. The intelligence checks out.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mol_q3</flag>
                <value>persuaded</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>800</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.seal_intel</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MEN OF LETTERS Q4 (FACTION POINT): THE KENDRICKS CODE                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_men_of_letters_Q4</defName>
    <label>the kendricks code</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_dr_hess</giver>
    <fallbackGiver>TMM_Hero_arthur_ketch</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_men_of_letters_Q3</li>
    </requires>
    <letterLabel>The Kendricks Code</letterLabel>
    <letterText>It has come to my attention that a settlement of mutated ghouls—including an archivist named Kent Connolly—possesses pre-war shipping ledgers linking our order to certain... unfortunate architectural projects. London demands total operational security. Liquidate the settlement and retrieve those ledgers, and I shall entrust you with our ultimate deterrent: The Equalizer.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>ghoul_camp_order</key>
        <label>Orders outside the ghoul camp</label>
        <text>Standing on the ridge overlooking the peaceful ghoul archive beside Arthur Ketch's liquidation squad, you catch sight of the boxed 2052 Zero Anchor contracts—signed jointly by Dr. Hess and Dick Roman. Execute Dr. Hess's purge order, or turn your guns on Arthur Ketch to save the ghouls and the evidence?</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Carry out Dr. Hess's order and liquidate the ghoul archive (Side with Men of Letters)</label>
            <resultText>Immaculate discipline. The continent is in capable hands. Accept The Equalizer—use it only as a last resort.</resultText>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.men_of_letters</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_the_equalizer</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.hess_monocle</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Defend the ghouls and crush Arthur Ketch's purge squad (Spark &amp; Truth)</label>
            <resultText>You dared raise weapons against British operatives?! From this hour, every hunter in the region is a target!</resultText>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.men_of_letters</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.consortium2052Contract</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-70</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.ketch_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>purge_ghouls</key>
        <label>Liquidate the ghoul archive camp</label>
        <text>Sweep the ghoul archive camp alongside Arthur Ketch and seize the pre-war 2052 shipping ledgers for London.</text>
        <enemyFaction>TMM_GhoulsFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>ambush_ketch</key>
        <label>Destroy Arthur Ketch's liquidation squad</label>
        <text>Engage Arthur Ketch's elite British Men of Letters strike team to protect the ghoul settlement and secure the 2052 Consortium contract.</text>
        <enemyFaction>TMM_MenOfLettersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>kendricks_aftermath</key>
        <label>The 2052 Zero Anchor ledgers</label>
        <text>Smoke drifts across the ridge as the battle concludes. The truth of London's 2052 pact with Dick Roman has forever changed how the British Men of Letters view {COLONY}.</text>
        <days>0.2</days>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.men_of_letters</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MEN OF LETTERS Q5 (DEFECTOR QUEST): ESCAPE FROM LONDON COMPOUND       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_men_of_letters_Q5</defName>
    <label>escape from the london compound</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_MenOfLettersFaction</faction>
    <giver>TMM_Hero_mick_davies</giver>
    <fallbackGiver>TMM_Hero_dr_hess</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_men_of_letters_Q4</li>
    </requires>
    <anyOf>
      <li>choice.men_of_letters=pro_spark</li>
      <li>ponr1=protect</li>
    </anyOf>
    <letterLabel>Escape from the London Compound</letterLabel>
    <letterText>Bloody hell, they've completely lost it! Dr. Hess ordered Arthur Ketch to liquidate me for questioning the Consortium files! I've nicked the master schematics for our Enochian suppression pylons and bolted for your border—please tell me your gates are open, because Ketch is right on my heels!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>ketch_hit_squad</key>
        <label>Repel Arthur Ketch's liquidation team</label>
        <text>Mick Davies sprints inside {COLONY}'s walls as Arthur Ketch—politely tipping his hat before opening fire—and Lady Antonia Bevell assault your perimeter!</text>
        <faction>TMM_MenOfLettersFaction</faction>
        <pointsFactor>1.35</pointsFactor>
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
        <key>recruit_mick</key>
        <label>Schematics from London</label>
        <text>With the liquidation squad broken, Mick Davies catches his breath and hands over the master Enochian suppression pylon blueprints.</text>
        <choices>
          <li>
            <outcome>accept_mick</outcome>
            <label>Shelter Mick Davies and welcome him into {COLONY}</label>
            <resultText>I owe you my life, truly. Let's build these suppressors and show London what real hunting looks like!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_mick_davies</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_mick_davies</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-90</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.enochian_suppressor</flag>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_men_of_letters.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_men_of_letters_Q1 -->
  <TMM_Story_men_of_letters_Q1.label>вежливое предложение</TMM_Story_men_of_letters_Q1.label>
  <TMM_Story_men_of_letters_Q1.letterLabel>Вежливое предложение</TMM_Story_men_of_letters_Q1.letterLabel>
  <TMM_Story_men_of_letters_Q1.letterText>Добрый день. Меня зовут Мик Дэвис, я представляю Британских Просвещённых. Мы наблюдали за впечатляющей стойкостью вашего поселения перед лицом сверхъестественных вторжений. Позвольте нанести визит в вашу гостиницу — я привёз енохианский подавитель, способный скрыть ваш периметр от ангелов и демонов.</TMM_Story_men_of_letters_Q1.letterText>
  <TMM_Story_men_of_letters_Q1.stages.0.label>Принять Мика Дэвиса в гостинице</TMM_Story_men_of_letters_Q1.stages.0.label>
  <TMM_Story_men_of_letters_Q1.stages.0.text>Мик Дэвис заселяется в гостиницу {COLONY} — привозит настоящий британский чай и вежливо морщится от пойла Пустоши — пока монтирует пробный енохианский маяк.</TMM_Story_men_of_letters_Q1.stages.0.text>
  <TMM_Story_men_of_letters_Q1.stages.1.label>Испытать енохианский маяк на привлечённых тварях</TMM_Story_men_of_letters_Q1.stages.1.label>
  <TMM_Story_men_of_letters_Q1.stages.1.text>Пробный импульс маяка притянул к {COLONY} ночных тварей! Нейтрализуйте цели испытания!</TMM_Story_men_of_letters_Q1.stages.1.text>
  <TMM_Story_men_of_letters_Q1.stages.2.label>Соглашение об обмене данными</TMM_Story_men_of_letters_Q1.stages.2.label>
  <TMM_Story_men_of_letters_Q1.stages.2.text>Удовлетворённый показаниями датчиков, Мик Дэвис отряхивает твидовый пиджак и предлагает подписать соглашение с Лондоном.</TMM_Story_men_of_letters_Q1.stages.2.text>
  <TMM_Story_men_of_letters_Q1.stages.2.choices.0.label>Провести испытания маяка и подписать соглашение об обмене данными</TMM_Story_men_of_letters_Q1.stages.2.choices.0.label>
  <TMM_Story_men_of_letters_Q1.stages.2.choices.0.resultText>Splendid! Потрясающе! Лондон будет в восторге. Вот наши выкройки рунических костюмов — носите на здоровье.</TMM_Story_men_of_letters_Q1.stages.2.choices.0.resultText>

  <!-- TMM_Story_men_of_letters_Q2 -->
  <TMM_Story_men_of_letters_Q2.label>чистая зачистка</TMM_Story_men_of_letters_Q2.label>
  <TMM_Story_men_of_letters_Q2.letterLabel>Чистая зачистка</TMM_Story_men_of_letters_Q2.letterLabel>
  <TMM_Story_men_of_letters_Q2.letterText>Внимание. Гнездо одичавших вампиров обосновалось в заброшенном железнодорожном депо в шести милях от вашей границы. Лондон требует абсолютной зачистки — никаких выживших, никаких свидетелей, никакой кустарщины. Присоединяйтесь к моему тактическому отряду и проведите образцовую операцию.</TMM_Story_men_of_letters_Q2.letterText>
  <TMM_Story_men_of_letters_Q2.stages.0.label>Зачистить логово вампиров в железнодорожном депо</TMM_Story_men_of_letters_Q2.stages.0.label>
  <TMM_Story_men_of_letters_Q2.stages.0.text>Проведите ночной тактический штурм заброшенного депо и уничтожьте гнездо одичавших вампиров до последней твари.</TMM_Story_men_of_letters_Q2.stages.0.text>
  <TMM_Story_men_of_letters_Q2.stages.1.label>Ящик «Ричард Роман Энтерпрайзис»</TMM_Story_men_of_letters_Q2.stages.1.label>
  <TMM_Story_men_of_letters_Q2.stages.1.text>Внутри зачищенного депо обнаруживается деревянный ящик со штампом «Ричард Роман Энтерпрайзис», доверху набитый пакетами консервированной крови. Мистер Кендрикс спокойно оценивает вашу работу.</TMM_Story_men_of_letters_Q2.stages.1.text>
  <TMM_Story_men_of_letters_Q2.stages.1.choices.0.label>Зачистить депо точно по протоколу Кендрикса</TMM_Story_men_of_letters_Q2.stages.1.choices.0.label>
  <TMM_Story_men_of_letters_Q2.stages.1.choices.0.resultText>Дисциплинированно и без лишних эмоций. Редкость для этого континента. Вы заслужили награду.</TMM_Story_men_of_letters_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_men_of_letters_Q3 -->
  <TMM_Story_men_of_letters_Q3.label>комната допросов</TMM_Story_men_of_letters_Q3.label>
  <TMM_Story_men_of_letters_Q3.letterLabel>Комната допросов</TMM_Story_men_of_letters_Q3.letterLabel>
  <TMM_Story_men_of_letters_Q3.letterText>Мы захватили высокопоставленного демона перекрёстка, знающего точную частоту печатей Нулевого Якоря. Однако он упрям, а моим коллегам в Лондоне нужен результат уже сегодня ночью. Помогите мне на нашем полевом объекте — оккультным извлечением или психологическим давлением.</TMM_Story_men_of_letters_Q3.letterText>
  <TMM_Story_men_of_letters_Q3.stages.0.label>В тайной камере допросов Лондона</TMM_Story_men_of_letters_Q3.stages.0.label>
  <TMM_Story_men_of_letters_Q3.stages.0.text>Под нарисованной на потолке дьявольской ловушкой Антония Бевелл раскладывает ритуальные серебряные ножи перед пленным демоном, а тот хохочет ей в лицо, бросая, что её начальники в Лондоне работают на левиафанов.</TMM_Story_men_of_letters_Q3.stages.0.text>
  <TMM_Story_men_of_letters_Q3.stages.1.label>Извлечение частот печатей</TMM_Story_men_of_letters_Q3.stages.1.label>
  <TMM_Story_men_of_letters_Q3.stages.1.text>Антония Бевелл поднимает раскалённое енохианское клеймо. Позволить ей провести жёсткий оккультный допрос или остановить пытки и вытянуть правду хитростью?</TMM_Story_men_of_letters_Q3.stages.1.text>
  <TMM_Story_men_of_letters_Q3.stages.1.choices.0.label>Позволить Антонии Бевелл провести жёсткий оккультный допрос</TMM_Story_men_of_letters_Q3.stages.1.choices.0.label>
  <TMM_Story_men_of_letters_Q3.stages.1.choices.0.resultText>Боль открывает любые замки. Координаты у нас, а вы доказали, что не боитесь испачкать руки.</TMM_Story_men_of_letters_Q3.stages.1.choices.0.resultText>
  <TMM_Story_men_of_letters_Q3.stages.1.choices.1.label>Остановить пытки и вытянуть правду хитростью и сделкой</TMM_Story_men_of_letters_Q3.stages.1.choices.1.label>
  <TMM_Story_men_of_letters_Q3.stages.1.choices.1.resultText>Хм... Неожиданно эффективно, пусть и слишком мягко для моего вкуса. Информация верна.</TMM_Story_men_of_letters_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_men_of_letters_Q4 -->
  <TMM_Story_men_of_letters_Q4.label>кодекс кендрикса</TMM_Story_men_of_letters_Q4.label>
  <TMM_Story_men_of_letters_Q4.letterLabel>Кодекс Кендрикса</TMM_Story_men_of_letters_Q4.letterLabel>
  <TMM_Story_men_of_letters_Q4.letterText>До моего сведения дошло, что поселение мутировавших гулей — включая некоего архивариуса Кента Коннолли — владеет довоенными накладными, связывающими наш орден с кое-какими... нежелательными проектами. Лондон требует абсолютной секретности. Ликвидируйте поселение и верните бумаги, и я доверю вам наше высшее оружие: «Уравнитель».</TMM_Story_men_of_letters_Q4.letterText>
  <TMM_Story_men_of_letters_Q4.stages.0.label>Приказ на подступах к лагерю гулей</TMM_Story_men_of_letters_Q4.stages.0.label>
  <TMM_Story_men_of_letters_Q4.stages.0.text>На холме перед мирным архивом гулей рядом с отрядом ликвидаторов Артура Кетча вы замечаете ящики с контрактами 2052 года на строительство Нулевых Якорей за подписями доктора Хесс и Дика Романа. Исполнить приказ Хесс или развернуть оружие против Кетча?</TMM_Story_men_of_letters_Q4.stages.0.text>
  <TMM_Story_men_of_letters_Q4.stages.0.choices.0.label>Выполнить приказ доктора Хесс и уничтожить свидетелей (за Просвещённых)</TMM_Story_men_of_letters_Q4.stages.0.choices.0.label>
  <TMM_Story_men_of_letters_Q4.stages.0.choices.0.resultText>Безупречная дисциплина. Континент в надёжных руках. Примите «Уравнитель» — используйте его только в крайнем случае.</TMM_Story_men_of_letters_Q4.stages.0.choices.0.resultText>
  <TMM_Story_men_of_letters_Q4.stages.0.choices.1.label>Защитить гулей и разгромить карательный отряд Просвещённых (за Искру и правду)</TMM_Story_men_of_letters_Q4.stages.0.choices.1.label>
  <TMM_Story_men_of_letters_Q4.stages.0.choices.1.resultText>Вы осмелились поднять руку на британских оперативников?! Отныне каждый охотник в регионе станет нашей мишенью!</TMM_Story_men_of_letters_Q4.stages.0.choices.1.resultText>
  <TMM_Story_men_of_letters_Q4.stages.1.label>Зачистить лагерь архивариусов-гулей</TMM_Story_men_of_letters_Q4.stages.1.label>
  <TMM_Story_men_of_letters_Q4.stages.1.text>Зачистите поселение гулей вместе с Артуром Кетчем и изымите довоенные накладные 2052 года для Лондона.</TMM_Story_men_of_letters_Q4.stages.1.text>
  <TMM_Story_men_of_letters_Q4.stages.2.label>Уничтожить карательный отряд Артура Кетча</TMM_Story_men_of_letters_Q4.stages.2.label>
  <TMM_Story_men_of_letters_Q4.stages.2.text>Разгромите элитную группу зачистки Артура Кетча, чтобы защитить гулей и сохранить контракт Консорциума 2052 года.</TMM_Story_men_of_letters_Q4.stages.2.text>
  <TMM_Story_men_of_letters_Q4.stages.3.label>Накладные Нулевого Якоря 2052 года</TMM_Story_men_of_letters_Q4.stages.3.label>
  <TMM_Story_men_of_letters_Q4.stages.3.text>Дым рассеивается над холмом. Правда о сговоре Лондона с Диком Романом в 2052 году навсегда изменила отношения Британских Просвещённых и {COLONY}.</TMM_Story_men_of_letters_Q4.stages.3.text>

  <!-- TMM_Story_men_of_letters_Q5 -->
  <TMM_Story_men_of_letters_Q5.label>побег из лондонского штаба</TMM_Story_men_of_letters_Q5.label>
  <TMM_Story_men_of_letters_Q5.letterLabel>Побег из лондонского штаба</TMM_Story_men_of_letters_Q5.letterLabel>
  <TMM_Story_men_of_letters_Q5.letterText>Чёрт побери, они окончательно спятили! Доктор Хесс приказала Артуру Кетчу ликвидировать меня за вопросы о файлах Консорциума! Я стащил главные чертежи наших енохианских глушителей и рванул к вашей границе — скажите, что ваши ворота открыты, потому что Кетч висит у меня на хвосте!</TMM_Story_men_of_letters_Q5.letterText>
  <TMM_Story_men_of_letters_Q5.stages.0.label>Отбить группу ликвидации Артура Кетча</TMM_Story_men_of_letters_Q5.stages.0.label>
  <TMM_Story_men_of_letters_Q5.stages.0.text>Мик Дэвис врывается за стены {COLONY}, а следом идут Антония Бевелл и Артур Кетч — вежливо приподнимающий шляпу перед открытием огня!</TMM_Story_men_of_letters_Q5.stages.0.text>
  <TMM_Story_men_of_letters_Q5.stages.1.label>Чертежи из Лондона</TMM_Story_men_of_letters_Q5.stages.1.label>
  <TMM_Story_men_of_letters_Q5.stages.1.text>Когда группа ликвидации разбита, Мик Дэвис переводит дух и выкладывает на стол главные чертежи енохианских подавителей.</TMM_Story_men_of_letters_Q5.stages.1.text>
  <TMM_Story_men_of_letters_Q5.stages.1.choices.0.label>Укрыть Мика Дэвиса и принять его в колонию</TMM_Story_men_of_letters_Q5.stages.1.choices.0.label>
  <TMM_Story_men_of_letters_Q5.stages.1.choices.0.resultText>Я в долгу перед вами по гроб жизни. Давайте соберём эти глушители и покажем Лондону, что такое настоящая охота!</TMM_Story_men_of_letters_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_hell_demons.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- HELL Q1: THE FINE PRINT                                               -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_hell_demons_Q1</defName>
    <label>the fine print</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_lucifer</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>The Fine Print</letterLabel>
    <letterText>Hello, darling. Don't reach for the rock salt just yet—I'm here on strictly professional business. One of my overzealous crossroads middlemen tricked a poor settler into signing a soul contract using a forged clause, and that sort of sloppy paperwork ruins Hell's reputation. Come settle the dispute, and I'll make it worth your while.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>crossroads_audit</key>
        <label>Auditing the soul contract at the crossroads</label>
        <text>Standing calmly in the middle of a radioactive crossroads sipping thirty-year scotch from a crystal tumbler, Crowley hands you the glowing parchment of the forged soul contract.</text>
        <days>0.5</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>contract_loophole</key>
        <label>A forged Latin footnote</label>
        <text>Spotting the illegal sub-clause buried in microscopic sulfur ink, you lay out the legal breach in front of the sweating crossroads demon while Crowley smiles coldly.</text>
        <choices>
          <li>
            <outcome>settled</outcome>
            <label>Expose the forged clause and let Crowley discipline his sloppy broker</label>
            <resultText>Brilliant! You'd make a meteoric rise in my legal department. Here is the pattern for a proper suit—stop wandering around in rags.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.hell_q1</flag>
                <value>settled</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.crossroads_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HELL Q2: BLOOD AND FLAME                                              -->
  <!-- Note: Meg is a newHero (not yet in base HeroDefs); giver omitted.     -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_hell_demons_Q2</defName>
    <label>blood and flame</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_HellFaction</faction>
    <fallbackGiver>TMM_Hero_crowley</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_hell_demons_Q1</li>
    </requires>
    <letterLabel>Blood and Flame</letterLabel>
    <letterText>Listen up, Clarence's little friends. Meg here. Abaddon's Knights of Hell fanatics are tearing through the region, burning crossroads and slaughtering anyone who backs Crowley's truce. I'm pinned down in an old church basement with half a dozen hellhounds on my tail—pull my fat out of the fire, and I'll owe you one.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>church_basement</key>
        <label>Hold the ruined church basement with Meg</label>
        <text>Fight through Abaddon's fanatic Knights of Hell and hold the church cellar—where a huge painted Devil's Trap pins the alpha hellhound—for 5 hours.</text>
        <enemyFaction>TMM_HellFaction</enemyFaction>
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
        <key>meg_saved</key>
        <label>A rebel demon's debt</label>
        <text>Wiping demon ichor off her battered leather jacket, Meg grins sharply over the broken Devil's Trap.</text>
        <choices>
          <li>
            <outcome>saved</outcome>
            <label>Accept Meg's devilstrand and hell-operative coat schematics</label>
            <resultText>Not bad for a bunch of meat-suits. I'll tell Castiel you're still breathing—if I ever see the feathered idiot.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.hell_q2</flag>
                <value>saved</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>700</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>DevilstrandCloth</thing>
                <count>40</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.hell_operative_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HELL Q3: TRIAL OF THE FATHER OF MURDER                                -->
  <!-- Note: Cain is a newHero (not yet in base HeroDefs); giver omitted.    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_hell_demons_Q3</defName>
    <label>trial of the father of murder</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_HellFaction</faction>
    <fallbackGiver>TMM_Hero_crowley</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_hell_demons_Q2</li>
    </requires>
    <letterLabel>Trial of the Father of Murder</letterLabel>
    <letterText>I am Cain. I have lived in quiet isolation tending my bees for centuries, away from the noise of Hell and the folly of men. Yet the tremors of your Zero Anchor have awakened the Mark upon my arm. Come to my chapel—if your warriors can withstand my trial without succumbing to blind bloodlust, I will share the secret of the First Blade.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>cains_chapel</key>
        <label>Endure Cain's trial at the secluded chapel</label>
        <text>Survive waves of demonic challengers for 6 hours beside Cain's quiet beehives while the Father of Murder calmly harvests honey and watches whether your warriors can master their rage.</text>
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
        <key>cain_verdict</key>
        <label>Master of the beast within</label>
        <text>The scarlet mark on Cain's forearm dims as the last challenger turns to ash. He sets down his wooden bee smoker and studies your champions with quiet sorrow.</text>
        <choices>
          <li>
            <outcome>worthy</outcome>
            <label>Accept Cain's coordinates to the shrine of the First Blade</label>
            <resultText>You restrained the beast inside. Few ever manage that. When the hour strikes, you will know where the jawbone waits.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.hell_q3</flag>
                <value>worthy</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>850</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.first_blade_shrine</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HELL Q4 (FACTION POINT): THE CROSSROADS BARGAIN (FIRST BLADE)         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_hell_demons_Q4</defName>
    <label>the crossroads bargain</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_lucifer</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_hell_demons_Q3</li>
    </requires>
    <letterLabel>The Crossroads Bargain</letterLabel>
    <letterText>Let's drop the pleasantries, shall we? My kingdom is starving for souls, and Heaven's winged thugs are hoarding every Spark they can get their halos on. I'm offering you the ultimate prize—the First Blade itself, capable of dropping an archangel in one swing. All I want in return is the life of a senior angel watcher—and a tiny bit of your soul's future.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>obelisk_bargain</key>
        <label>The jawbone upon the crossroads obelisk</label>
        <text>Beneath a bruised twilight sky, the primordial First Blade pulses with crimson light on the ancient obelisk while elite angelic watchers close in. Seal Crowley's bloody pact and cut down Heaven's elite for the First Blade, or shatter Crowley's ritual and fight Hell's vanguard?</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Accept Crowley's bargain, slay the elite angels, and claim The First Blade (Side with Hell)</label>
            <resultText>Ah, I love the smell of signed contracts in the morning! The First Blade is yours—mind the edge, the Mark of Cain craves fresh blood.</resultText>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.hell_demons</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>65</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_first_blade</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.crowley_king_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Reject the bargain, break Crowley's ritual, and defend the angels (Light &amp; Spark)</label>
            <resultText>Idiots! You could have been gods of this wasteland, and you chose petty morality! Let's see how you sing when the Cage cracks open!</resultText>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.hell_demons</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>1500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.angel_blade</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>slay_angel_watchers</key>
        <label>Cut down the elite angelic ambush at the obelisk</label>
        <text>Survive a brutal battle against heavily armed angelic watchers descending upon the obelisk to claim the First Blade!</text>
        <enemyFaction>TMM_HeavenFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>shatter_hell_ritual</key>
        <label>Crush Crowley's ritual guard at the obelisk</label>
        <text>Break through Hell's elite crossroads demons to dismantle the blood-obelisk before the angelic watchers are slaughtered!</text>
        <enemyFaction>TMM_HellFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>crimson_aftermath</key>
        <label>Crimson flash over the crossroads</label>
        <text>The blood-lit sky clears over the cracked obelisk. Whether bound to the Mark of Cain or marked as an enemy of Hell's throne, {COLONY} has drawn the gaze of the Pit.</text>
        <days>0.2</days>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.hell_demons</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HELL Q5 (DEFECTOR QUEST): THE RUNAWAY KING OF HELL (LUCIFER FREED)    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_hell_demons_Q5</defName>
    <label>the runaway king of hell</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_lucifer</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_hell_demons_Q4</li>
    </requires>
    <anyOf>
      <li>ponr1=protect</li>
      <li>ponr1=order</li>
      <li>choice.hell_demons=pro_spark</li>
    </anyOf>
    <letterLabel>The Runaway King of Hell</letterLabel>
    <letterText>Emergency, darling! Those brain-dead zealots in Hell actually did it—they shattered the final lock on the Cage! Lucifer is out, he's furious, and he's currently redecorating my throne room with my former subordinates! Open your doors—I know every dirty secret Hell has, and I'd rather not be turned into sulfur dust!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>lucifer_vanguard</key>
        <label>Repel Lucifer's punitive demon legion</label>
        <text>Crowley teleports straight into {COLONY}'s courtyard with Lucifer's furious hellfire legion storming your perimeter right behind him!</text>
        <faction>TMM_HellFaction</faction>
        <pointsFactor>1.5</pointsFactor>
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
        <key>welcome_crowley</key>
        <label>An exiled King at the bar</label>
        <text>Already demanding his own liquor cabinet in your waystation and insulting your local whiskey, Crowley offers his unmatched knowledge of Hell's secrets as Lucifer claims the infernal throne.</text>
        <choices>
          <li>
            <outcome>recruit_crowley</outcome>
            <label>Shelter Crowley in {COLONY} and accept him as advisor</label>
            <resultText>Clever darling. I promise to behave... mostly. Now let's figure out how to stop Lucifer before he burns down my favorite tailor.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_crowley</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_crowley</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.luciferReleased</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-100</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.crowley_king_suit</flag>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_hell_demons.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_hell_demons_Q1 -->
  <TMM_Story_hell_demons_Q1.label>мелкий шрифт</TMM_Story_hell_demons_Q1.label>
  <TMM_Story_hell_demons_Q1.letterLabel>Мелкий шрифт</TMM_Story_hell_demons_Q1.letterLabel>
  <TMM_Story_hell_demons_Q1.letterText>Привет, солнышко. Не хватайся сразу за каменную соль — я здесь исключительно по делу. Один из моих чересчур ретивых чертей на перекрёстке обманом заставил бедного поселенца подписать контракт на душу с поддельным пунктом, а такая халтура портит репутацию Ада. Помоги уладить спор, и я в долгу не останусь.</TMM_Story_hell_demons_Q1.letterText>
  <TMM_Story_hell_demons_Q1.stages.0.label>Разбор контракта на перекрёстке</TMM_Story_hell_demons_Q1.stages.0.label>
  <TMM_Story_hell_demons_Q1.stages.0.text>Спокойно потягивая тридцатилетний шотландский виски из хрустального стакана прямо посреди радиоактивного перекрёстка, Кроули протягивает вам тлеющий пергамент контракта.</TMM_Story_hell_demons_Q1.stages.0.text>
  <TMM_Story_hell_demons_Q1.stages.1.label>Поддельная сноска на латыни</TMM_Story_hell_demons_Q1.stages.1.label>
  <TMM_Story_hell_demons_Q1.stages.1.text>Заметив незаконный подпункт, вписанный микроскопическими серными чернилами, вы вскрываете подлог прямо перед вспотевшим чертом под ледяную ухмылку Кроули.</TMM_Story_hell_demons_Q1.stages.1.text>
  <TMM_Story_hell_demons_Q1.stages.1.choices.0.label>Найти подделку в договоре и помочь Кроули наказать нерадивого демона</TMM_Story_hell_demons_Q1.stages.1.choices.0.label>
  <TMM_Story_hell_demons_Q1.stages.1.choices.0.resultText>Блестяще! Вы бы сделали головокружительную карьеру в моём юридическом отделе. Вот вам выкройка приличного костюма — хватит ходить в лохмотьях.</TMM_Story_hell_demons_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_hell_demons_Q2 -->
  <TMM_Story_hell_demons_Q2.label>кровь и пламя</TMM_Story_hell_demons_Q2.label>
  <TMM_Story_hell_demons_Q2.letterLabel>Кровь и пламя</TMM_Story_hell_demons_Q2.letterLabel>
  <TMM_Story_hell_demons_Q2.letterText>Слушайте сюда, дружки Кларенса. Это Мэг. Фанатики Рыцарей Ада Абаддон рвут регион на куски, сжигая перекрёстки и вырезая всех, кто поддерживает перемирие Кроули. Меня зажали в подвале старой церкви с полдюжиной адских гончих на хвосте — вытащите меня из огня, и я буду перед вами в долгу.</TMM_Story_hell_demons_Q2.letterText>
  <TMM_Story_hell_demons_Q2.stages.0.label>Удержать подвал церкви вместе с Мэг</TMM_Story_hell_demons_Q2.stages.0.label>
  <TMM_Story_hell_demons_Q2.stages.0.text>Обороняйте подвал старой церкви — на стене которого огромная дьявольская ловушка сдерживает вожака адских гончих — от фанатиков Абаддон в течение 5 часов.</TMM_Story_hell_demons_Q2.stages.0.text>
  <TMM_Story_hell_demons_Q2.stages.1.label>Долг мятежного демона</TMM_Story_hell_demons_Q2.stages.1.label>
  <TMM_Story_hell_demons_Q2.stages.1.text>Стирая демонический ихор с потёртой кожаной куртки, Мэг язвительно усмехается над прожжённой пентаграммой.</TMM_Story_hell_demons_Q2.stages.1.text>
  <TMM_Story_hell_demons_Q2.stages.1.choices.0.label>Отбить штурм и спасти Мэг</TMM_Story_hell_demons_Q2.stages.1.choices.0.label>
  <TMM_Story_hell_demons_Q2.stages.1.choices.0.resultText>Неплохо для кучки смертных. Передам Кастиэлю, что вы ещё дышите — если увижу этого пернатого.</TMM_Story_hell_demons_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_hell_demons_Q3 -->
  <TMM_Story_hell_demons_Q3.label>охота за костью</TMM_Story_hell_demons_Q3.label>
  <TMM_Story_hell_demons_Q3.letterLabel>Охота за костью</TMM_Story_hell_demons_Q3.letterLabel>
  <TMM_Story_hell_demons_Q3.letterText>Я — Каин. Я веками жил в тихом уединении, ухаживая за пчёлами вдали от шума Ада и глупости людей. Но дрожь вашего Нулевого Якоря пробудила Метку на моей руке. Приходите в мою часовню — если ваши бойцы выдержат моё испытание и не поддадутся слепой жажде крови, я открою тайну Первого Клинка.</TMM_Story_hell_demons_Q3.letterText>
  <TMM_Story_hell_demons_Q3.stages.0.label>Выдержать испытание Каина у часовни</TMM_Story_hell_demons_Q3.stages.0.label>
  <TMM_Story_hell_demons_Q3.stages.0.text>Выстойте 6 часов против волн демонов у тихих ульев Каина, пока Отец Убийства спокойно собирает мёд и проверяет, сумеют ли ваши бойцы удержать разум от ярости.</TMM_Story_hell_demons_Q3.stages.0.text>
  <TMM_Story_hell_demons_Q3.stages.1.label>Власть над зверем внутри</TMM_Story_hell_demons_Q3.stages.1.label>
  <TMM_Story_hell_demons_Q3.stages.1.text>Алая метка на предплечье Каина тускнеет, когда последний противник обращается в пепел. Он опускает деревянный дымарь и смотрит на ваших бойцов со спокойной печалью.</TMM_Story_hell_demons_Q3.stages.1.text>
  <TMM_Story_hell_demons_Q3.stages.1.choices.0.label>Выдержать испытание Каина и получить координаты святилища Первого Клинка</TMM_Story_hell_demons_Q3.stages.1.choices.0.label>
  <TMM_Story_hell_demons_Q3.stages.1.choices.0.resultText>Ты сдержал зверя внутри. Немногие способны на это. Когда пробьёт час — ты будешь знать, где искать челюстную кость.</TMM_Story_hell_demons_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_hell_demons_Q4 -->
  <TMM_Story_hell_demons_Q4.label>сделка на перекрёстке</TMM_Story_hell_demons_Q4.label>
  <TMM_Story_hell_demons_Q4.letterLabel>Сделка на перекрёстке</TMM_Story_hell_demons_Q4.letterLabel>
  <TMM_Story_hell_demons_Q4.letterText>Отбросим любезности, ладно? Моё королевство умирает с голоду без душ, а крылатые головорезы Небес сгребают каждую Искру. Я предлагаю тебе главный приз — сам Первый Клинок, способный свалить архангела с одного удара. Взамен я прошу лишь жизнь старшего ангела-наблюдателя... и крошечный залог на будущее.</TMM_Story_hell_demons_Q4.letterText>
  <TMM_Story_hell_demons_Q4.stages.0.label>Челюстная кость на обелиске перекрёстка</TMM_Story_hell_demons_Q4.stages.0.label>
  <TMM_Story_hell_demons_Q4.stages.0.text>Под багровым сумеречным небом Первый Клинок пульсирует алым светом на древнем обелиске, пока к нему стягиваются элитные ангелы-наблюдатели. Скрепить кровавую сделку Кроули ради Первого Клинка или сорвать ритуал и защитить ангелов?</TMM_Story_hell_demons_Q4.stages.0.text>
  <TMM_Story_hell_demons_Q4.stages.0.choices.0.label>Принять сделку Кроули, перебить ангелов и забрать Первый Клинок (за Ад)</TMM_Story_hell_demons_Q4.stages.0.choices.0.label>
  <TMM_Story_hell_demons_Q4.stages.0.choices.0.resultText>Ах, люблю запах заключённых сделок по утрам! Первый Клинок твой — только не порежься, Метка Каина любит свежую кровь.</TMM_Story_hell_demons_Q4.stages.0.choices.0.resultText>
  <TMM_Story_hell_demons_Q4.stages.0.choices.1.label>Отвергнуть сделку, спасти ангелов и сорвать ритуал Кроули (за свет и Искру)</TMM_Story_hell_demons_Q4.stages.0.choices.1.label>
  <TMM_Story_hell_demons_Q4.stages.0.choices.1.resultText>Идиоты! Вы могли стать богами этой Пустоши, а выбрали жалкую мораль! Что ж, посмотрим, как вы запоёте, когда Клетка треснет!</TMM_Story_hell_demons_Q4.stages.0.choices.1.resultText>
  <TMM_Story_hell_demons_Q4.stages.1.label>Перебить элитную засаду ангелов у обелиска</TMM_Story_hell_demons_Q4.stages.1.label>
  <TMM_Story_hell_demons_Q4.stages.1.text>Выстойте в смертельно опасной схватке против элитных ангелов-наблюдателей, чтобы забрать Первый Клинок!</TMM_Story_hell_demons_Q4.stages.1.text>
  <TMM_Story_hell_demons_Q4.stages.2.label>Разбить ритуальную стражу Кроули у обелиска</TMM_Story_hell_demons_Q4.stages.2.label>
  <TMM_Story_hell_demons_Q4.stages.2.text>Пробейтесь сквозь элитных демонов перекрёстка и сокрушите кровавый обелиск, пока ангелы живы!</TMM_Story_hell_demons_Q4.stages.2.text>
  <TMM_Story_hell_demons_Q4.stages.3.label>Багровый отблеск над перекрёстком</TMM_Story_hell_demons_Q4.stages.3.label>
  <TMM_Story_hell_demons_Q4.stages.3.text>Кровавые тучи рассеиваются над расколотым обелиском. Независимо от того, вспыхнула ли на руке Метка Каина или преисподняя объявила вам вендетту, взгляд Бездны теперь прикован к {COLONY}.</TMM_Story_hell_demons_Q4.stages.3.text>

  <!-- TMM_Story_hell_demons_Q5 -->
  <TMM_Story_hell_demons_Q5.label>беглый король ада</TMM_Story_hell_demons_Q5.label>
  <TMM_Story_hell_demons_Q5.letterLabel>Беглый Король Ада</TMM_Story_hell_demons_Q5.letterLabel>
  <TMM_Story_hell_demons_Q5.letterText>Тревога, солнышко! Эти безмозглые фанатики в Аду всё-таки сделали это — они взломали последний замок на Клетке! Люцифер на свободе, он в ярости и прямо сейчас украшает мой тронный зал моими бывшими подчинёнными! Открывайте ворота — я знаю каждый грязный секрет Ада и предпочту не превратиться в серную пыль!</TMM_Story_hell_demons_Q5.letterText>
  <TMM_Story_hell_demons_Q5.stages.0.label>Отразить карательный легион Люцифера</TMM_Story_hell_demons_Q5.stages.0.label>
  <TMM_Story_hell_demons_Q5.stages.0.text>Кроули телепортируется прямо во двор {COLONY}, а следом на ваш периметр обрушивается яростный легион демонов освобождённого Люцифера!</TMM_Story_hell_demons_Q5.stages.0.text>
  <TMM_Story_hell_demons_Q5.stages.1.label>Король в изгнании у барной стойки</TMM_Story_hell_demons_Q5.stages.1.label>
  <TMM_Story_hell_demons_Q5.stages.1.text>Уже требуя отдельный барный шкаф в гостинице и ругая качество местного виски, Кроули предлагает все тайны Ада в обмен на убежище, пока на преисподний трон восходит Люцифер.</TMM_Story_hell_demons_Q5.stages.1.text>
  <TMM_Story_hell_demons_Q5.stages.1.choices.0.label>Укрыть Кроули в колонии и признать его советником</TMM_Story_hell_demons_Q5.stages.1.choices.0.label>
  <TMM_Story_hell_demons_Q5.stages.1.choices.0.resultText>Умничка. Обещаю вести себя прилично... ну, почти. А теперь давай подумаем, как нам остановить Люцифера, пока он не сжёг мой любимый портной цех.</TMM_Story_hell_demons_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_heaven_angels.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- HEAVEN Q1: FALLEN STAR                                                -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_heaven_angels_Q1</defName>
    <label>fallen star</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_castiel</giver>
    <fallbackGiver>TMM_Hero_naomi</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Fallen Star</letterLabel>
    <letterText>Friends of the settlement. A brother of my garrison has fallen from the upper tiers, his grace nearly depleted by the barrier sealing Earth. Demons are already converging on his crash crater to harvest his essence. Help me reach him before they tear his vessel apart.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>winged_crater</key>
        <label>Defend the fallen angel at the scorched crater</label>
        <text>Hold the smoking crater—where two giant scorched wing-prints burn into the earth—for 5 hours alongside Castiel against demons trying to devour the weakened angel.</text>
        <enemyFaction>TMM_HellFaction</enemyFaction>
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
        <key>castiel_gratitude</key>
        <label>Wings etched in ash</label>
        <text>The last demon vanishes into sulfur smoke as the fallen angel rises steadily beside Castiel.</text>
        <choices>
          <li>
            <outcome>saved</outcome>
            <label>Accept Castiel's blessing and beige vessel trenchcoat pattern</label>
            <resultText>You have shown mercy rare in this broken world. I will remember your kindness.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.heav_q1</flag>
                <value>saved</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>450</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.vessel_trenchcoat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HEAVEN Q2: CELESTIAL WATCH                                            -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_heaven_angels_Q2</defName>
    <label>celestial watch</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_naomi</giver>
    <fallbackGiver>TMM_Hero_zachariah</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_heaven_angels_Q1</li>
    </requires>
    <letterLabel>Celestial Watch</letterLabel>
    <letterText>Your settlement has drawn the attention of Heaven's intelligence division. A shape-shifting demon operative has infiltrated a trade caravan heading toward your gates, carrying a cursed relic meant to destabilize your Zero Anchor. Identify and neutralize the infiltrator before the caravan arrives.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>inspect_caravan</key>
        <label>Screen the arriving caravan guests</label>
        <text>Question the trade caravan guests stopping at {COLONY}'s waystation using Naomi's Enochian scanner and holy water to flush out the hidden demon operative.</text>
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
        <key>exposed_infiltrator</key>
        <label>Banish the exposed demon infiltrator</label>
        <text>Flushed out by holy water, the demon drops its disguise and summons crossroads backup right at {COLONY}'s gate!</text>
        <faction>TMM_HellFaction</faction>
        <pointsFactor>1.0</pointsFactor>
        <delayHours>2</delayHours>
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
        <key>naomi_handover</key>
        <label>Flutter of unseen wings</label>
        <text>Watching quietly from the corner with folded arms, Naomi steps forward the moment the demon falls to claim the cursed relic—ready to vanish with a sharp flutter of wings.</text>
        <choices>
          <li>
            <outcome>purged</outcome>
            <label>Surrender the cursed relic to Naomi and Heaven's intelligence</label>
            <resultText>Efficient and quiet. Perhaps mortals in this sector are capable of order after all.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.heav_q2</flag>
                <value>purged</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>650</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gold</thing>
                <count>15</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HEAVEN Q3: BUREAUCRACY OF PARADISE                                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_heaven_angels_Q3</defName>
    <label>bureaucracy of paradise</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_zachariah</giver>
    <fallbackGiver>TMM_Hero_naomi</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_heaven_angels_Q2</li>
    </requires>
    <letterLabel>Bureaucracy of Paradise</letterLabel>
    <letterText>Let's cut to the chase, champ. Heaven is running on fumes, and we've got a schedule to keep. My angels tracked down a third-generation synth carrying a fresh 'Spark'—a battery, basically—hiding in an old schoolhouse. Hand him over for 'ascension', and you'll get a first-class ticket when the big day comes.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>schoolhouse_meeting</key>
        <label>Meeting Zachariah at the ruined schoolhouse</label>
        <text>Strolling through the ash-filled schoolhouse in an immaculate corporate suit without a single speck of dust on his shoes, Zachariah taps his wristwatch beside a trembling third-generation synth.</text>
        <days>0.6</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>zachariah_demand</key>
        <label>A battery or a soul</label>
        <text>Zachariah smiles like a middle manager closing a merger: surrender the runaway synth for "ascension", or throw the angel out the door.</text>
        <choices>
          <li>
            <outcome>handed_over</outcome>
            <label>Hand the synth over to Zachariah for Heaven's favor</label>
            <resultText>Now THAT'S a team player! See how smooth things go when nobody asks silly questions?</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.heav_q3</flag>
                <value>handed_over</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>900</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineUltratech</thing>
                <count>2</count>
              </li>
            </effects>
          </li>
          <li>
            <outcome>refused</outcome>
            <label>Protect the synth and order Zachariah off the premises</label>
            <resultText>You'll regret that stubbornness, bug. When Michael comes down here, there won't be anywhere left to hide.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.heav_q3</flag>
                <value>refused</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>15</amount>
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
  <!-- HEAVEN Q4 (FACTION POINT): THE FADING LIGHT (ARCHANGEL BLADE)         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_heaven_angels_Q4</defName>
    <label>the fading light</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_michael</giver>
    <fallbackGiver>TMM_Hero_zachariah</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_heaven_angels_Q3</li>
    </requires>
    <letterLabel>The Fading Light</letterLabel>
    <letterText>Mortal. I am Michael, Commander of the Heavenly Host. Without the flow of souls, my brothers are fading into dust—even one of Castiel's most loyal lieutenants is dying at the ruined cathedral. To save him, we must channel a Spark into his vessel—or sever his grace forever and leave him human. Come witness Heaven's judgment, and choose where you stand.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>cathedral_judgment</key>
        <label>Judgment beneath the broken stained glass</label>
        <text>Inside the ruined cathedral, Archangel Michael and his elite seraphim stand over Castiel's dying lieutenant and a bound synth whose Spark shines like blue fire. Back Michael's ritual for the Archangel Blade, or stand with Castiel to spare the synth and let the dying angel become mortal?</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Support Michael's decree and siphon the synth's Spark to save the angel (Side with Heaven)</label>
            <resultText>You understand higher duty, mortal. Take this Archangel Blade—let celestial fire burn our common enemies.</resultText>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.heaven_angels</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>65</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>-70</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_archangel_blade</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.celestial_vestments</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Halt the ritual, save the synth, and help Castiel make his brother human (Spark &amp; Free Will)</label>
            <resultText>Castiel! You betray Heaven again for these clay puppets! You are an outcast, and this colony will be erased from the earth!</resultText>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.heaven_angels</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.angelMadeHuman</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-70</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.castiel_trenchcoat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>guard_transfer</key>
        <label>Guard Michael's Spark ritual against demon attackers</label>
        <text>Hold the cathedral nave for 6 hours against furious demonic raiders trying to disrupt Archangel Michael's Spark-channeling ritual!</text>
        <enemyFaction>TMM_HellFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>6</holdHours>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>fight_seraphim</key>
        <label>Fight Michael's seraphim guard in the cathedral</label>
        <text>Survive a deadly clash against Archangel Michael's elite seraphim guard while Castiel severs his dying brother's grace and escorts the synth clear!</text>
        <enemyFaction>TMM_HeavenFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>3</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>cathedral_sunlight</key>
        <label>Sunlight through shattered glass</label>
        <text>Golden dust motes drift through the cathedral's broken stained glass as the battle ends—leaving either a reborn warrior of Heaven or a newly mortal man taking his first astonished breath of air.</text>
        <days>0.2</days>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.heaven_angels</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- HEAVEN Q5 (DEFECTOR QUEST): FALL OF A SERAPH (CASTIEL JOINS)          -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_heaven_angels_Q5</defName>
    <label>fall of a seraph</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_castiel</giver>
    <fallbackGiver>TMM_Hero_gabriel</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_heaven_angels_Q4</li>
      <li>!ponr1=abyss</li>
    </requires>
    <letterLabel>Fall of a Seraph</letterLabel>
    <letterText>My brothers have branded me a traitor for refusing Michael's scorched-earth decree. Zachariah has dispatched an execution squad to strip my grace and erase my mind. I am coming to your colony—not as a soldier of Heaven, but as a friend of humanity. Stand with me one last time.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>zachariah_executioners</key>
        <label>Repel Zachariah's angelic execution squad</label>
        <text>Blinding shafts of white light strike {COLONY}'s perimeter as Zachariah's execution squad descends to strip Castiel's grace! Stand beside your friend!</text>
        <faction>TMM_HeavenFaction</faction>
        <pointsFactor>1.45</pointsFactor>
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
        <key>welcome_castiel</key>
        <label>A crooked blue tie</label>
        <text>With the execution squad driven back to the clouds, Castiel walks over to the Zero Synth, awkwardly straightens his crooked blue tie, and looks to you.</text>
        <choices>
          <li>
            <outcome>recruit_castiel</outcome>
            <label>Welcome Castiel into {COLONY}'s ranks</label>
            <resultText>I no longer know what Father's plan is... but I know the people living here are worth fighting for.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_castiel</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_castiel</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-90</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.castiel_trenchcoat</flag>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_heaven_angels.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_heaven_angels_Q1 -->
  <TMM_Story_heaven_angels_Q1.label>упавшая звезда</TMM_Story_heaven_angels_Q1.label>
  <TMM_Story_heaven_angels_Q1.letterLabel>Упавшая звезда</TMM_Story_heaven_angels_Q1.letterLabel>
  <TMM_Story_heaven_angels_Q1.letterText>Друзья поселения. Брат из моего гарнизона упал с верхних небес, его благодать почти иссякла из-за барьера, запечатавшего Землю. Демоны уже стягиваются к воронке его падения, чтобы пожрать его сущность. Помогите мне добраться до него, пока они не разорвали его сосуд.</TMM_Story_heaven_angels_Q1.letterText>
  <TMM_Story_heaven_angels_Q1.stages.0.label>Защитить падшего ангела у выжженного кратера</TMM_Story_heaven_angels_Q1.stages.0.label>
  <TMM_Story_heaven_angels_Q1.stages.0.text>Удерживайте дымящуюся воронку с чётким выжженным силуэтом огромных крыльев на земле 5 часов вместе с Кастиэлем от волн демонов.</TMM_Story_heaven_angels_Q1.stages.0.text>
  <TMM_Story_heaven_angels_Q1.stages.1.label>Крылья на пепле</TMM_Story_heaven_angels_Q1.stages.1.label>
  <TMM_Story_heaven_angels_Q1.stages.1.text>Последний демон растворяется в серном дыму, и спасённый ангел уверенно поднимается на ноги рядом с Кастиэлем.</TMM_Story_heaven_angels_Q1.stages.1.text>
  <TMM_Story_heaven_angels_Q1.stages.1.choices.0.label>Удержать периметр до восстановления сил ангела</TMM_Story_heaven_angels_Q1.stages.1.choices.0.label>
  <TMM_Story_heaven_angels_Q1.stages.1.choices.0.resultText>Вы проявили милосердие, редкое для этого сломанного мира. Я запомню вашу доброту.</TMM_Story_heaven_angels_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_heaven_angels_Q2 -->
  <TMM_Story_heaven_angels_Q2.label>небесный дозор</TMM_Story_heaven_angels_Q2.label>
  <TMM_Story_heaven_angels_Q2.letterLabel>Небесный дозор</TMM_Story_heaven_angels_Q2.letterLabel>
  <TMM_Story_heaven_angels_Q2.letterText>Ваше поселение привлекло внимание разведки Небес. Демон-перевёртыш внедрился в торговый караван, направляющийся к вашим воротам, и несёт проклятую реликвию для дестабилизации вашего Нулевого Якоря. Вычислите и нейтрализуйте лазутчика до того, как караван войдёт внутрь.</TMM_Story_heaven_angels_Q2.letterText>
  <TMM_Story_heaven_angels_Q2.stages.0.label>Досмотреть прибывших постояльцев каравана</TMM_Story_heaven_angels_Q2.stages.0.label>
  <TMM_Story_heaven_angels_Q2.stages.0.text>Опросите гостей торгового каравана в гостинице {COLONY} с помощью енохианского сканера Наоми и святой воды, чтобы выявить скрытого демона.</TMM_Story_heaven_angels_Q2.stages.0.text>
  <TMM_Story_heaven_angels_Q2.stages.1.label>Изгнать разоблачённого лазутчика</TMM_Story_heaven_angels_Q2.stages.1.label>
  <TMM_Story_heaven_angels_Q2.stages.1.text>Обожжённый святой водой демон сбрасывает маску и призывает подкрепление прямо к воротам {COLONY}!</TMM_Story_heaven_angels_Q2.stages.1.text>
  <TMM_Story_heaven_angels_Q2.stages.2.label>Хлопок невидимых крыльев</TMM_Story_heaven_angels_Q2.stages.2.label>
  <TMM_Story_heaven_angels_Q2.stages.2.text>Наблюдавшая со стороны со скрещёнными руками Наоми шагает вперёд в ту же секунду, как демон падает, готовая забрать проклятую реликвию и исчезнуть с хлопком крыльев.</TMM_Story_heaven_angels_Q2.stages.2.text>
  <TMM_Story_heaven_angels_Q2.stages.2.choices.0.label>Вычислить лазутчика, изгнать демона и передать реликвию разведке Небес</TMM_Story_heaven_angels_Q2.stages.2.choices.0.label>
  <TMM_Story_heaven_angels_Q2.stages.2.choices.0.resultText>Эффективно и без лишнего шума. Возможно, смертные в этом секторе действительно способны к порядку.</TMM_Story_heaven_angels_Q2.stages.2.choices.0.resultText>

  <!-- TMM_Story_heaven_angels_Q3 -->
  <TMM_Story_heaven_angels_Q3.label>бюрократия рая</TMM_Story_heaven_angels_Q3.label>
  <TMM_Story_heaven_angels_Q3.letterLabel>Бюрократия рая</TMM_Story_heaven_angels_Q3.letterLabel>
  <TMM_Story_heaven_angels_Q3.letterText>Перейдём сразу к делу, чемпион. Небеса работают на последнем издыхании, а у нас горит график. Мои ангелы выследили синта третьего поколения со свежей «Искрой» — по сути, батарейку — в старой школе. Выдайте его для «вознесения», и получите билет первого класса, когда настанет судный день.</TMM_Story_heaven_angels_Q3.letterText>
  <TMM_Story_heaven_angels_Q3.stages.0.label>Встреча с Захарией в разрушенной школе</TMM_Story_heaven_angels_Q3.stages.0.label>
  <TMM_Story_heaven_angels_Q3.stages.0.text>Разгуливая по засыпанным пеплом руинам школы в дорогом корпоративном костюме, ни разу не испачкав туфли, Захария нетерпеливо постукивает по часам рядом с испуганным синтом.</TMM_Story_heaven_angels_Q3.stages.0.text>
  <TMM_Story_heaven_angels_Q3.stages.1.label>Батарейка или душа</TMM_Story_heaven_angels_Q3.stages.1.label>
  <TMM_Story_heaven_angels_Q3.stages.1.text>Захария улыбается словно менеджер на выгодной сделке: выдать беглого синта для «вознесения» или выставить ангела за порог?</TMM_Story_heaven_angels_Q3.stages.1.text>
  <TMM_Story_heaven_angels_Q3.stages.1.choices.0.label>Выдать синта Захарии ради благосклонности Небес</TMM_Story_heaven_angels_Q3.stages.1.choices.0.label>
  <TMM_Story_heaven_angels_Q3.stages.1.choices.0.resultText>Вот это командный игрок! Видишь, как легко всё решается, когда не задаёшь глупых вопросов?</TMM_Story_heaven_angels_Q3.stages.1.choices.0.resultText>
  <TMM_Story_heaven_angels_Q3.stages.1.choices.1.label>Защитить синта и выставить Захарию за порог</TMM_Story_heaven_angels_Q3.stages.1.choices.1.label>
  <TMM_Story_heaven_angels_Q3.stages.1.choices.1.resultText>Ты пожалеешь об этом упрямстве, букашка. Когда Михаил спустится вниз, прятаться будет негде.</TMM_Story_heaven_angels_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_heaven_angels_Q4 -->
  <TMM_Story_heaven_angels_Q4.label>угасающий свет</TMM_Story_heaven_angels_Q4.label>
  <TMM_Story_heaven_angels_Q4.letterLabel>Угасающий свет</TMM_Story_heaven_angels_Q4.letterLabel>
  <TMM_Story_heaven_angels_Q4.letterText>Смертный. Я — Михаил, полководец Небесного Воинства. Без притока душ мои братья рассыпаются в прах — прямо сейчас один из вернейших лейтенантов Кастиэля умирает в разрушенном соборе. Чтобы спасти его, мы должны влить в его сосуд Искру синта — или навсегда отсечь его благодать, оставив простым смертным. Явись на суд Небес и выбери свою сторону.</TMM_Story_heaven_angels_Q4.letterText>
  <TMM_Story_heaven_angels_Q4.stages.0.label>Суд под разбитым витражом</TMM_Story_heaven_angels_Q4.stages.0.label>
  <TMM_Story_heaven_angels_Q4.stages.0.text>В разрушенном соборе архангел Михаил и элитные серафимы стоят над умирающим лейтенантом Кастиэля и пленным синтом, чья Искра горит синим огнём. Поддержать ритуал Михаила ради Клинка Архангела или встать с Кастиэлем, спасая синта и делая ангела смертным?</TMM_Story_heaven_angels_Q4.stages.0.text>
  <TMM_Story_heaven_angels_Q4.stages.0.choices.0.label>Поддержать приказ Михаила и пожертвовать Искрой синта ради спасения ангела (за Небеса)</TMM_Story_heaven_angels_Q4.stages.0.choices.0.label>
  <TMM_Story_heaven_angels_Q4.stages.0.choices.0.resultText>Ты понял высший долг, смертный. Возьми этот Клинок Архангела — пусть небесный огонь сожжёт наших общих врагов.</TMM_Story_heaven_angels_Q4.stages.0.choices.0.resultText>
  <TMM_Story_heaven_angels_Q4.stages.0.choices.1.label>Остановить ритуал, спасти синта и помочь Кастиэлю сделать брата смертным (за Искру и свободу воли)</TMM_Story_heaven_angels_Q4.stages.0.choices.1.label>
  <TMM_Story_heaven_angels_Q4.stages.0.choices.1.resultText>Кастиэль! Ты снова предал Небеса ради этих глиняных кукол! Отныне ты изгнанник, а эта колония будет стёрта с лица земли!</TMM_Story_heaven_angels_Q4.stages.0.choices.1.resultText>
  <TMM_Story_heaven_angels_Q4.stages.1.label>Защищать ритуал Михаила от штурма демонов</TMM_Story_heaven_angels_Q4.stages.1.label>
  <TMM_Story_heaven_angels_Q4.stages.1.text>Удерживайте неф собора 6 часов от яростных демонов, пытающихся сорвать ритуал перекачки Искры архангела Михаила!</TMM_Story_heaven_angels_Q4.stages.1.text>
  <TMM_Story_heaven_angels_Q4.stages.2.label>Сразиться со стражей серафимов Михаила в соборе</TMM_Story_heaven_angels_Q4.stages.2.label>
  <TMM_Story_heaven_angels_Q4.stages.2.text>Выстойте в смертельно опасном бою против элитных серафимов Михаила, пока Кастиэль отсекает благодать брата и выводит синта из-под удара!</TMM_Story_heaven_angels_Q4.stages.2.text>
  <TMM_Story_heaven_angels_Q4.stages.3.label>Солнечный луч сквозь разбитый витраж</TMM_Story_heaven_angels_Q4.stages.3.label>
  <TMM_Story_heaven_angels_Q4.stages.3.text>Золотые пылинки кружатся в луче сквозь разбитый витраж, когда бой затихает — оставляя либо возрождённого воина Небес, либо бывшего ангела, впервые с изумлением делающего глубокий человеческий вдох.</TMM_Story_heaven_angels_Q4.stages.3.text>

  <!-- TMM_Story_heaven_angels_Q5 -->
  <TMM_Story_heaven_angels_Q5.label>падение серафима</TMM_Story_heaven_angels_Q5.label>
  <TMM_Story_heaven_angels_Q5.letterLabel>Падение серафима</TMM_Story_heaven_angels_Q5.letterLabel>
  <TMM_Story_heaven_angels_Q5.letterText>Мои братья заклеймили меня предателем за отказ исполнить приказ Михаила о выжженной земле. Захария послал отряд палачей, чтобы вырвать мою благодать и стереть мне разум. Я иду в вашу колонию — не как солдат Небес, а как друг человечества. Встаньте со мной в последний раз.</TMM_Story_heaven_angels_Q5.letterText>
  <TMM_Story_heaven_angels_Q5.stages.0.label>Отразить карательный десант ангелов Захарии</TMM_Story_heaven_angels_Q5.stages.0.label>
  <TMM_Story_heaven_angels_Q5.stages.0.text>Ослепительные столпы света бьют по периметру {COLONY} — отряд палачей Захарии спустился за благодатью Кастиэля! Встаньте рядом с другом!</TMM_Story_heaven_angels_Q5.stages.0.text>
  <TMM_Story_heaven_angels_Q5.stages.1.label>Кривой синий галстук</TMM_Story_heaven_angels_Q5.stages.1.label>
  <TMM_Story_heaven_angels_Q5.stages.1.text>Когда каратели Небес отброшены в облака, Кастиэль подходит к Нулевому Синту, неуклюже поправляет свой синий галстук и смотрит на вас.</TMM_Story_heaven_angels_Q5.stages.1.text>
  <TMM_Story_heaven_angels_Q5.stages.1.choices.0.label>Принять Кастиэля в ряды колонии</TMM_Story_heaven_angels_Q5.stages.1.choices.0.label>
  <TMM_Story_heaven_angels_Q5.stages.1.choices.0.resultText>Я больше не знаю, каков план Отца... но я знаю, что здесь живут люди, за которых стоит сражаться.</TMM_Story_heaven_angels_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_monster_clans.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- MONSTER CLANS Q1: A GUEST IN THE NIGHT                                -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_monster_clans_Q1</defName>
    <label>a guest in the night</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_benny_lafitte</giver>
    <fallbackGiver>TMM_Hero_alpha_vampire</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>A Guest in the Night</letterLabel>
    <letterText>Evening, cher. Name's Benny. Before you load up the dead-man's blood, hear me out—I kicked the human-hunting habit down in Purgatory, and I'm trying to lay low in your waystation without hurting a soul. Trouble is, my old nest-mates tracked me down. Spot me a few supplies and help me keep 'em off your folks?</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>benny_visit</key>
        <label>Shelter Benny Lafitte in the waystation</label>
        <text>Benny Lafitte quietly checks into {COLONY}'s waystation for the night—even cooking a surprisingly great Cajun stew in your kitchen for your colonists though he cannot touch it himself.</text>
        <faction>TMM_MonsterClansFaction</faction>
        <hero>TMM_Hero_benny_lafitte</hero>
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
        <key>feral_vampire_pack</key>
        <label>Repel Benny's feral former nest-mates</label>
        <text>Feral vampires from Benny's old nest emerge out of the dark to tear him apart—help him hold the porch!</text>
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>0.95</pointsFactor>
        <delayHours>4</delayHours>
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
        <key>benny_thanks</key>
        <label>A vampire's gratitude</label>
        <text>With his feral former nest-mates turned to ash before dawn, Benny cleans his blade and tips his chin in sincere thanks.</text>
        <choices>
          <li>
            <outcome>helped</outcome>
            <label>Accept Benny's friendship and velvet cloak schematics</label>
            <resultText>Merci, brother. Not many humans reach out a hand to a vamp. If you ever need fangs on your side—just whistle.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mc_q1</flag>
                <value>helped</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>450</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.night_clan_velvet_cloak</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MONSTER CLANS Q2: NEW CHILDREN OF EVE                                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_monster_clans_Q2</defName>
    <label>new children of eve</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_eve</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_alpha_vampire</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_monster_clans_Q1</li>
    </requires>
    <letterLabel>New Children of Eve</letterLabel>
    <letterText>Mortal. I am Eve, Mother of All. From the ash and radiation of your ruined world, I have shaped a new brood of apex hunters—faster, stronger, immune to your silver. Yet even a mother must cull her children when they grow too wild and threaten the balance. Help me contain my rogue brood at the old cannery, and the clans will respect your borders.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>old_cannery</key>
        <label>Cull Eve's rogue hybrid brood at the old cannery</label>
        <text>Sweep the old cannery complex and wipe out the feral mutated hybrids and anomalous cocoons threatening the balance.</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
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
        <key>cannery_office</key>
        <label>SucroCorp supply contracts</label>
        <text>In the cannery's bloodstained manager office, your team uncovers a fresh meat-supply contract signed with Dick Roman's corporation, "SucroCorp". Eve watches calmly from the catwalk as the last cocoon burns.</text>
        <choices>
          <li>
            <outcome>culled</outcome>
            <label>Burn the remaining cocoons and accept Eve's bioferrite trade</label>
            <resultText>You fight with the ferocity of my finest children. Perhaps humanity is not as hopeless as Dick claims.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mc_q2</flag>
                <value>culled</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.sucrocorpMeatContractFound</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>750</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineUltratech</thing>
                <count>3</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.bioferrite_trade</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MONSTER CLANS Q3: COUNCIL OF THE NIGHT (UNLOCKS BORAX FORMULA)        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_monster_clans_Q3</defName>
    <label>council of the night</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_alpha_vampire</giver>
    <fallbackGiver>TMM_Hero_benny_lafitte</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_monster_clans_Q2</li>
    </requires>
    <letterLabel>Council of the Night</letterLabel>
    <letterText>Walk carefully, mortal. For millennia my children ruled the dark, until Dick Roman and his Leviathans crawled out of the abyss to treat both humans and monsters as cattle. Meet me at the abandoned opera house—let us discuss an ancient truce before Roman's enforcers swallow us all.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>opera_ambush</key>
        <label>Survive Edgar's Leviathan ambush at the opera house</label>
        <text>The truce talks are shattered as Edgar—unhinging his human disguise into a cavernous Leviathan maw—storms the abandoned opera house! Hold the stage for 5 hours beside the Alpha Vampire until a splash of sodium borate drives Edgar back!</text>
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
        <key>borax_revelation</key>
        <label>The secret of sodium borate</label>
        <text>As Edgar flees howling through the opera balcony, the Alpha Vampire dips a gloved finger into the spilled white cleaning powder that burned the Leviathan like acid: sodium borate—borax.</text>
        <choices>
          <li>
            <outcome>truce</outcome>
            <label>Seal the truce with the Alpha Vampire and claim the Borax formula</label>
            <resultText>You shed blood beside the Firstborn. From tonight on, the clans of night spare your settlement—while the Leviathans owe us a blood debt.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.mc_q3</flag>
                <value>truce</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.boraxFormula</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>800</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.alpha_vampire_cloak</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MONSTER CLANS Q4 (FACTION POINT): BLOOD AND BORAX (DICK ROMAN)        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_monster_clans_Q4</defName>
    <label>blood and borax</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_dick_roman</giver>
    <fallbackGiver>TMM_Hero_edgar</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_monster_clans_Q3</li>
    </requires>
    <letterLabel>Blood and Borax</letterLabel>
    <letterText>Hello there! Dick Roman here. I have to say, I'm wildly impressed by your little operation—great synergy, tremendous growth potential! Word is you've gotten your hands on a nasty chemical formula called 'borax' that's bad for business. Hand over the formula and the lab samples at our processing plant, and I'll make you a partner in the new world economy.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>sucrocorp_deal</key>
        <label>Inside the SucroCorp bottling plant</label>
        <text>Behind gleaming soda-bottling conveyors, rows of sedated humans and synths await biomass harvesting. Dick Roman extends a manicured hand with a dazzling smile—surrender the Borax formula to join the Leviathan economy, or spray him with Borax to strip his 99% invulnerability and fight him to the death!</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Accept Dick Roman's partnership and destroy the Borax formula (Side with Leviathans)</label>
            <resultText>Fantastic! Love people who understand profit margins! Here is the living blade Alpha Predator and a generous check—welcome to the big leagues!</resultText>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.monster_clans</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.boraxFormula</flag>
                <value>false</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>65</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>1500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_alpha_predator</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.dick_roman_suit</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.living_bioferrite_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Hit Dick Roman with Borax, strip his invulnerability, and fight him to the death! (Freedom)</label>
            <resultText>You... you tiny insect! How dare you burn me with that filth?! Edgar, tear them to pieces!</resultText>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.monster_clans</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>45</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_alpha_predator</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.dick_roman_suit</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.living_bioferrite_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>kill_dick_roman</key>
        <label>Slay Dick Roman at the SucroCorp plant (Bring Borax!)</label>
        <text>Dick Roman's tailored suit hisses with caustic white foam as his human face distorts into a tooth-lined maw! Hit him with Borax sprayers or Borax weapon mods to strip his 99% invulnerability and bring him down!</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_dick_roman</hero>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.dickRomanDead</flag>
                <value>true</value>
              </li>
            </effects>
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
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>sucrocorp_aftermath</key>
        <label>Aftermath at SucroCorp</label>
        <text>The bottling lines grind to a halt. Whether partnered with the Leviathan board or standing over the smoking Vault-Tec conspiracy files, {COLONY} has seized the living blade Alpha Predator.</text>
        <days>0.2</days>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.monster_clans</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- MONSTER CLANS Q5 (DEFECTOR QUEST): BLOOD PACT AGAINST THE ABYSS       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_monster_clans_Q5</defName>
    <label>blood pact against the abyss</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_benny_lafitte</giver>
    <fallbackGiver>TMM_Hero_alpha_vampire</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_monster_clans_Q4</li>
      <li>!choice.monster_clans=pro_faction</li>
    </requires>
    <letterLabel>Blood Pact Against the Abyss</letterLabel>
    <letterText>Brother, Edgar and the remaining Leviathan pack are tearing through the old Vampire nests, turning whole clans into mindless thralls. The Alpha Vampire agreed to send his best elders to hit Edgar's stronghold—and after the dust settles, I'm hanging up my fangs for good and joining your camp, if you'll have me.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>edgar_stronghold</key>
        <label>Storm Edgar's Leviathan stronghold</label>
        <text>Lead a joint assault with Benny Lafitte and the Alpha Vampire's elders—armed with Borax and silver—to eliminate Leviathan enforcer Edgar once and for all!</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_edgar</hero>
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
        <key>welcome_benny</key>
        <label>A bone machete from Purgatory</label>
        <text>Carrying a carved Purgatory bone machete as a keepsake for his wall, Benny Lafitte steps through {COLONY}'s gate with the Alpha Vampire's non-aggression pact sealed.</text>
        <choices>
          <li>
            <outcome>recruit_benny</outcome>
            <label>Welcome Benny Lafitte into {COLONY}'s ranks</label>
            <resultText>That's it, cher. Old debts are settled. Find me a quiet corner out of the sun—and I've got the back of anyone under your roof.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_benny_lafitte</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_benny_lafitte</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.living_bioferrite_armor</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.benny_bone_machete</flag>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_monster_clans.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_monster_clans_Q1 -->
  <TMM_Story_monster_clans_Q1.label>ночной гость</TMM_Story_monster_clans_Q1.label>
  <TMM_Story_monster_clans_Q1.letterLabel>Ночной гость</TMM_Story_monster_clans_Q1.letterLabel>
  <TMM_Story_monster_clans_Q1.letterText>Добрый вечер, дружище. Зовут меня Бенни. Прежде чем схватишься за кровь мертвеца, выслушай — я завязал с охотой на людей ещё в Чистилище и хочу тихо переждать бурю в вашей гостинице, никого не трогая. Беда в том, что меня выследила моя старая стая. Подкинешь пару припасов и поможешь отбиться от них, чтобы не задели ваших?</TMM_Story_monster_clans_Q1.letterText>
  <TMM_Story_monster_clans_Q1.stages.0.label>Укрыть Бенни Лафита в гостинице</TMM_Story_monster_clans_Q1.stages.0.label>
  <TMM_Story_monster_clans_Q1.stages.0.text>Бенни Лафит тихо снимает комнату в гостинице {COLONY} на ночь — и даже варит на кухне отличный каджунский суп для колонистов, хотя сам его не ест.</TMM_Story_monster_clans_Q1.stages.0.text>
  <TMM_Story_monster_clans_Q1.stages.1.label>Отбить ночной налёт бывшей стаи Бенни</TMM_Story_monster_clans_Q1.stages.1.label>
  <TMM_Story_monster_clans_Q1.stages.1.text>Из темноты выходят дикие вампиры из бывшей стаи Бенни, чтобы разорвать отступника — помогите ему удержать крыльцо!</TMM_Story_monster_clans_Q1.stages.1.text>
  <TMM_Story_monster_clans_Q1.stages.2.label>Благодарность вампира</TMM_Story_monster_clans_Q1.stages.2.label>
  <TMM_Story_monster_clans_Q1.stages.2.text>Когда бывшая стая обращается в пепел до рассвета, Бенни вытирает клинок и искренне кивает в знак признательности.</TMM_Story_monster_clans_Q1.stages.2.text>
  <TMM_Story_monster_clans_Q1.stages.2.choices.0.label>Помочь Бенни и отбить атаку его бывшей стаи</TMM_Story_monster_clans_Q1.stages.2.choices.0.label>
  <TMM_Story_monster_clans_Q1.stages.2.choices.0.resultText>Merci, брат. Не каждый человек протянет руку упырю. Если понадобятся клыки на вашей стороне — только свистни.</TMM_Story_monster_clans_Q1.stages.2.choices.0.resultText>

  <!-- TMM_Story_monster_clans_Q2 -->
  <TMM_Story_monster_clans_Q2.label>новые дети евы</TMM_Story_monster_clans_Q2.label>
  <TMM_Story_monster_clans_Q2.letterLabel>Новые дети Евы</TMM_Story_monster_clans_Q2.letterLabel>
  <TMM_Story_monster_clans_Q2.letterText>Смертный. Я — Ева, Мать всех тварей. Из пепла и радиации вашего разрушенного мира я вывела новый выводок высших хищников — быстрее, сильнее и невосприимчивых к вашему серебру. Но даже мать обязана прореживать детей, когда они дичают и нарушают равновесие. Помоги усмирить мой сорвавшийся выводок на старом консервном заводе, и кланы будут уважать ваши границы.</TMM_Story_monster_clans_Q2.letterText>
  <TMM_Story_monster_clans_Q2.stages.0.label>Уничтожить сорвавшийся выводок Евы на консервном заводе</TMM_Story_monster_clans_Q2.stages.0.label>
  <TMM_Story_monster_clans_Q2.stages.0.text>Зачистите старый консервный завод и уничтожьте мутировавших гибридов и аномальные коконы, нарушающие равновесие.</TMM_Story_monster_clans_Q2.stages.0.text>
  <TMM_Story_monster_clans_Q2.stages.1.label>Договоры поставок в «Сукрокорп»</TMM_Story_monster_clans_Q2.stages.1.label>
  <TMM_Story_monster_clans_Q2.stages.1.text>В забрызганном кровью кабинете директора завода находится свежий договор о поставках мяса в корпорацию Дика Романа «Сукрокорп». Ева спокойно наблюдает с галереи, как догорает последний кокон.</TMM_Story_monster_clans_Q2.stages.1.text>
  <TMM_Story_monster_clans_Q2.stages.1.choices.0.label>Уничтожить выводок и сжечь коконы на заводе</TMM_Story_monster_clans_Q2.stages.1.choices.0.label>
  <TMM_Story_monster_clans_Q2.stages.1.choices.0.resultText>Вы сражаетесь с яростью моих лучших детей. Возможно, человечество не так безнадёжно, как говорит Дик.</TMM_Story_monster_clans_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_monster_clans_Q3 -->
  <TMM_Story_monster_clans_Q3.label>совет старейшин ночи</TMM_Story_monster_clans_Q3.label>
  <TMM_Story_monster_clans_Q3.letterLabel>Совет старейшин ночи</TMM_Story_monster_clans_Q3.letterLabel>
  <TMM_Story_monster_clans_Q3.letterText>Ступай осторожно, смертный. Тысячелетиями мои дети правили тьмой, пока Дик Роман и его левиафаны не выползли из бездны, чтобы превратить в скот и людей, и монстров. Встреть меня в заброшенном оперном театре — обсудим древнее перемирие, пока псы Романа не сожрали нас всех.</TMM_Story_monster_clans_Q3.letterText>
  <TMM_Story_monster_clans_Q3.stages.0.label>Отбить засаду левиафанов Эдгара в оперном театре</TMM_Story_monster_clans_Q3.stages.0.label>
  <TMM_Story_monster_clans_Q3.stages.0.text>Переговоры срывает штурм Эдгара, в разгар боя сбрасывающего человеческую личину в зубастую пасть бездны! Удержите сцену 5 часов рядом с Альфа-вампиром, пока залп тетрабората натрия не обратит Эдгара в бегство!</TMM_Story_monster_clans_Q3.stages.0.text>
  <TMM_Story_monster_clans_Q3.stages.1.label>Тайна тетрабората натрия</TMM_Story_monster_clans_Q3.stages.1.label>
  <TMM_Story_monster_clans_Q3.stages.1.text>Когда Эдгар с воем скрывается за балконом, Альфа-вампир касается перчаткой рассыпанного белого чистящего порошка, обжёгшего левиафана словно кислота: тетраборат натрия — бура.</TMM_Story_monster_clans_Q3.stages.1.text>
  <TMM_Story_monster_clans_Q3.stages.1.choices.0.label>Отбить атаку левиафанов плечом к плечу с Альфа-вампиром и получить формулу буры</TMM_Story_monster_clans_Q3.stages.1.choices.0.label>
  <TMM_Story_monster_clans_Q3.stages.1.choices.0.resultText>Ты пролил кровь рядом с Первородным. Отныне кланы ночи не тронут твоё поселение — а вот с левиафанами у нас личный счёт.</TMM_Story_monster_clans_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_monster_clans_Q4 -->
  <TMM_Story_monster_clans_Q4.label>кровь и бура</TMM_Story_monster_clans_Q4.label>
  <TMM_Story_monster_clans_Q4.letterLabel>Кровь и бура</TMM_Story_monster_clans_Q4.letterLabel>
  <TMM_Story_monster_clans_Q4.letterText>Приветствую! На связи Дик Роман. Должен признать, меня дико впечатляет ваше маленькое предприятие — отличная синергия, колоссальный потенциал роста! Говорят, к вам в руки попала неприятная химическая формула под названием «бура», которая вредит бизнесу. Передайте формулу и образцы на нашем перерабатывающем заводе, и я сделаю вас партнёром в новой мировой экономике.</TMM_Story_monster_clans_Q4.letterText>
  <TMM_Story_monster_clans_Q4.stages.0.label>На пищевом комбинате «Сукрокорп»</TMM_Story_monster_clans_Q4.stages.0.label>
  <TMM_Story_monster_clans_Q4.stages.0.text>За сияющими конвейерами розлива газировки тянутся ряды усыплённых людей и синтов, готовых к жатве биомассы. Дик Роман с ослепительной улыбкой протягивает руку: сдать формулу буры ради партнёрства или облить его бурой, снять 99% неуязвимость и вступить в смертельный бой?</TMM_Story_monster_clans_Q4.stages.0.text>
  <TMM_Story_monster_clans_Q4.stages.0.choices.0.label>Принять предложение Дика Романа и уничтожить формулу буры (за левиафанов и Консорциум)</TMM_Story_monster_clans_Q4.stages.0.choices.0.label>
  <TMM_Story_monster_clans_Q4.stages.0.choices.0.resultText>Фантастика! Обожаю людей, которые умеют считать прибыль! Вот вам живой клинок «Альфа-хищник» и чек на круглую сумму — добро пожаловать в высшую лигу!</TMM_Story_monster_clans_Q4.stages.0.choices.0.resultText>
  <TMM_Story_monster_clans_Q4.stages.0.choices.1.label>Облить Дика Романа бурой, снять неуязвимость и уничтожить его в бою (за свободу от фермы)</TMM_Story_monster_clans_Q4.stages.0.choices.1.label>
  <TMM_Story_monster_clans_Q4.stages.0.choices.1.resultText>Ты... ты мелкая букашка! Как ты смеешь жечь меня этой дрянью?! Эдгар, разорви их на куски!</TMM_Story_monster_clans_Q4.stages.0.choices.1.resultText>
  <TMM_Story_monster_clans_Q4.stages.1.label>Уничтожить Дика Романа на заводе «Сукрокорп» (возьмите буру!)</TMM_Story_monster_clans_Q4.stages.1.label>
  <TMM_Story_monster_clans_Q4.stages.1.text>Идеальный деловой костюм Дика Романа дымится белой кислотной пеной, а его лицо искажается в зубастую пасть! Используйте распылители или баллоны буры, чтобы снять его 99% неуязвимость и добить босса!</TMM_Story_monster_clans_Q4.stages.1.text>
  <TMM_Story_monster_clans_Q4.stages.2.label>После событий в «Сукрокорп»</TMM_Story_monster_clans_Q4.stages.2.label>
  <TMM_Story_monster_clans_Q4.stages.2.text>Линии розлива со скрежетом останавливаются. Став партнёром совета левиафанов или забрав дымящиеся архивы сговора с Волт-Тек, {COLONY} получает живой клинок «Альфа-хищник».</TMM_Story_monster_clans_Q4.stages.2.text>

  <!-- TMM_Story_monster_clans_Q5 -->
  <TMM_Story_monster_clans_Q5.label>кровавый пакт против бездны</TMM_Story_monster_clans_Q5.label>
  <TMM_Story_monster_clans_Q5.letterLabel>Кровавый пакт против бездны</TMM_Story_monster_clans_Q5.letterLabel>
  <TMM_Story_monster_clans_Q5.letterText>Брат, Эдгар и уцелевшая стая левиафанов рвут старые вампирские гнёзда, превращая целые кланы в безмозглых рабов. Альфа-вампир согласился послать своих лучших старейшин на штурм логова Эдгара — а когда пыль уляжется, я навсегда завяжу со старой жизнью и встану в ваш строй, если примете.</TMM_Story_monster_clans_Q5.letterText>
  <TMM_Story_monster_clans_Q5.stages.0.label>Штурмовать логово левиафанов Эдгара</TMM_Story_monster_clans_Q5.stages.0.label>
  <TMM_Story_monster_clans_Q5.stages.0.text>Возглавьте совместный штурм логова Эдгара с Бенни Лафитом и старейшинами Альфа-вампира — с бурой и серебром — чтобы навсегда покончить с палачом левиафанов!</TMM_Story_monster_clans_Q5.stages.0.text>
  <TMM_Story_monster_clans_Q5.stages.1.label>Костяной мачете из Чистилища</TMM_Story_monster_clans_Q5.stages.1.label>
  <TMM_Story_monster_clans_Q5.stages.1.text>Сжимая вырезанный из кости чистилищный мачете как сувенир на стену, Бенни Лафит переступает порог {COLONY} с заключённым пактом о ненападении от Альфа-вампира.</TMM_Story_monster_clans_Q5.stages.1.text>
  <TMM_Story_monster_clans_Q5.stages.1.choices.0.label>Уничтожить Эдгара и принять Бенни Лафита в колонию</TMM_Story_monster_clans_Q5.stages.1.choices.0.label>
  <TMM_Story_monster_clans_Q5.stages.1.choices.0.resultText>Вот и всё, cher. Старые долги закрыты. Выделите мне тихий угол подальше от солнца — и я прикрою спину любому из ваших.</TMM_Story_monster_clans_Q5.stages.1.choices.0.resultText>

</LanguageData>
```