`Defs/StoryDefs/Story_ghouls.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- GHOULS Q1: THE SILVER SHROUD RETURNS                                  -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_ghouls_Q1</defName>
    <label>the silver shroud returns</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_GhoulsFaction</faction>
    <giver>TMM_Hero_kent_connolly</giver>
    <fallbackGiver>TMM_Hero_daisy</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>The Silver Shroud Returns</letterLabel>
    <letterText>Citizens of the wasteland! Evil lurks in the shadows, and Goodneighbor needs a hero! Actually, uh, it's just me, Kent Connolly. Raiders took over the old Hubris Comics archive where my authentic Silver Shroud radio scripts and costume patterns are locked. Please help me get them back—people need hope right now!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>hubris_comics</key>
        <label>Clear Hubris Comics and recover the radio reels</label>
        <text>Assault the raider-occupied Hubris Comics archive and retrieve Kent Connolly's pre-war transmitter reels and Silver Shroud costume patterns.</text>
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
        <key>shroud_broadcast</key>
        <label>Voice of justice on the airwaves</label>
        <text>In the Hubris Comics basement, your squad also cracked a hidden safe filled with pre-war invoices signed by "R. Roman", who bought up local radio stations right before the bombs fell. Up on the ridge tower, Kent Connolly waits breathlessly for the reels.</text>
        <choices>
          <li>
            <outcome>broadcasting</outcome>
            <label>Hand the radio reels and costume patterns to Kent Connolly</label>
            <resultText>Yes! Do you hear that signal?! Justice rides the airwaves once more! Here—take a copy of the Shroud's coat pattern!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.ghoul_q1</flag>
                <value>broadcasting</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>350</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.silver_shroud_coat</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.wasteland_poncho</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GHOULS Q2: CONTRACT OF CONSCIENCE                                     -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_ghouls_Q2</defName>
    <label>contract of conscience</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_GhoulsFaction</faction>
    <giver>TMM_Hero_edward_deegan</giver>
    <fallbackGiver>TMM_Hero_john_hancock</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_ghouls_Q1</li>
    </requires>
    <letterLabel>Contract of Conscience</letterLabel>
    <letterText>Listen up, smoothskin. A crew of Gunner-backed slavers has been snatching peaceful ghouls off the trade roads and selling them to fight in feral pit arenas. Hancock wants their compound burned to the ground, and I need a reliable gun at my side. Are you in?</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>slaver_pits</key>
        <label>Storm the Gunner slaver pit compound</label>
        <text>Launch a night assault on the fortified slaver arena backed by the Gunners and eliminate the guards before the caged ghouls are fed to ferals.</text>
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
        <key>cages_opened</key>
        <label>Chains broken in the dust</label>
        <text>As Edward Deegan kicks open the last iron cage, one of the freed ghoul doctors steps forward in relief—recognizing {COLONY}'s colors from the stories spreading through Goodneighbor's clinics.</text>
        <choices>
          <li>
            <outcome>liberated</outcome>
            <label>Free all captive ghouls and finish the slaver boss</label>
            <resultText>Lived two hundred years, and it's rare I meet smoothskins whose word holds weight. Goodneighbor won't forget this.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.ghoul_q2</flag>
                <value>liberated</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>650</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>4</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GHOULS Q3: RECIPE AGAINST DECAY                                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_ghouls_Q3</defName>
    <label>recipe against decay</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_GhoulsFaction</faction>
    <giver>TMM_Hero_gob</giver>
    <fallbackGiver>TMM_Hero_daisy</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_ghouls_Q2</li>
    </requires>
    <letterLabel>Recipe Against Decay</letterLabel>
    <letterText>H-hello... I'm Gob. Daisy told me you're actually decent to folks like us. Some of our oldest pre-war residents are starting to hear whispers and lose their minds—the feral rot is setting in. I found an old medical formula for a neural stabilizer, but the glowing fungus only grows inside a hot radiation crater. Could you gather it for me?</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>glowing_crater</key>
        <label>Gather glowing fungus from the radiation crater</label>
        <text>Fight through the irradiated crater's guardians and harvest the glowing medicinal fungus needed for Gob's neural stabilizer.</text>
        <enemyFaction>TMM_MonsterClansFaction</enemyFaction>
        <objective>Retrieve</objective>
        <thing>MedicineHerbal</thing>
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
        <key>stabilizer_brew</key>
        <label>The unexploded warhead</label>
        <text>At the center of the glowing crater, your team noticed an unexploded 2077 warhead etched with an Enochian containment sigil along its steel casing. Back at the lab, Gob eagerly awaits the glowing fungal samples.</text>
        <choices>
          <li>
            <outcome>stabilized</outcome>
            <label>Deliver the glowing fungus to Gob to brew the neural stabilizer</label>
            <resultText>It worked! The haze in their eyes is clearing! Thank you... for the first time in two centuries, someone helped us just because it was right.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.ghoul_q3</flag>
                <value>stabilized</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineHerbal</thing>
                <count>10</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.ghoul_stabilizer</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GHOULS Q4 (FACTION POINT): THE LAST DAWN                              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_ghouls_Q4</defName>
    <label>the last dawn</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_GhoulsFaction</faction>
    <giver>TMM_Hero_john_hancock</giver>
    <fallbackGiver>TMM_Hero_daisy</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_ghouls_Q3</li>
    </requires>
    <letterLabel>The Last Dawn</letterLabel>
    <letterText>Look, friend, I don't ask for favors lightly. My oldest pal—the guy who pulled me out of the gutter back when I first turned—has gone past what Gob's brew can fix. He's locked himself in the old observatory with an ancient alien crown relic we found, and his mind is slipping into feral rage. Help me end this right, one way or another.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>observatory_climb</key>
        <label>Break through to the ruined observatory dome</label>
        <text>Fight past nocturnal anomalies converging on the telekinetic waves pouring out of the old observatory dome.</text>
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
        <key>observatory_mercy</key>
        <label>Beneath the cracked dome</label>
        <text>Inside the dome, John Hancock slowly removes his tricorne hat as his oldest friend trembles over the humming alien artifact, fighting off the last tide of feral madness. You can attempt an experimental Spark-resonance stabilization to bring his mind back, or help Hancock grant his friend a dignified farewell.</text>
        <choices>
          <li>
            <outcome>pro_spark</outcome>
            <label>Channel the Zero Spark resonance / Virgil's serum to restore his mind (Hope &amp; the Spark)</label>
            <resultText>He... he remembered my name. Damn it all, miracles really do happen out in this cursed wasteland! Goodneighbor rides with you to the very end, brother!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.ghouls</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_lorenzos_artifact</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.hancock_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_faction</outcome>
            <label>Stand with Hancock through his final duty and claim the artifact (Harsh Mercy)</label>
            <resultText>He went out a man, not a beast. Thanks for keeping my hand steady. Take that cursed thing—it only brings trouble.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.ghouls</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_lorenzos_artifact</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.hancock_coat</flag>
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
        <flag>arc.ghouls</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GHOULS Q5 (DEFECTOR QUEST): HOLDER OF THE CONTRACT                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_ghouls_Q5</defName>
    <label>holder of the contract</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_GhoulsFaction</faction>
    <giver>TMM_Hero_charon</giver>
    <fallbackGiver>TMM_Hero_edward_deegan</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_ghouls_Q4</li>
    </requires>
    <letterLabel>Holder of the Contract</letterLabel>
    <letterText>I am Charon. My previous contract holder in the underworld has perished in the crossfire. The parchment of my binding has fallen into the hands of a Gunner lieutenant at the highway tollbooth. Reclaim my contract, and my shotgun belongs to your settlement—no matter who you fight.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>contract_approach</key>
        <label>Reclaiming Charon's parchment</label>
        <text>The Gunner lieutenant holding Charon's binding parchment is willing to sell it for 1500 silver—or you can storm the highway tollbooth and take it off his corpse.</text>
        <choices>
          <li>
            <outcome>buy_contract</outcome>
            <label>Pay the 1500 silver buyout from {COLONY}'s stockpiles</label>
            <resultText>The Gunner lieutenant smirks over the radio and sends a courier to collect the silver.</resultText>
            <nextStage>1</nextStage>
          </li>
          <li>
            <outcome>storm_tollbooth</outcome>
            <label>Storm the Gunner highway tollbooth and take the contract by force</label>
            <resultText>Charon checks the shells in his combat shotgun and nods once. Move out.</resultText>
            <nextStage>2</nextStage>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <key>pay_silver</key>
        <label>Deliver 1500 silver for Charon's contract</label>
        <text>Transfer 1500 silver from {COLONY}'s stockpiles to buy out Charon's binding contract.</text>
        <thing>Silver</thing>
        <count>1500</count>
        <to>colony</to>
        <timeoutDays>8</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>3</nextStage>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>tollbooth_raid</key>
        <label>Assault the Gunner highway tollbooth</label>
        <text>Clear the Gunner mercenaries at the highway tollbooth and seize Charon's binding parchment.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
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
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>free_charon</key>
        <label>A contract torn in two</label>
        <text>The weathered parchment rests in your hands. Standing tall and silent before {COLONY}'s gate, Charon awaits your command—expecting a new master.</text>
        <choices>
          <li>
            <outcome>recruit_charon</outcome>
            <label>Hand the parchment directly to Charon and welcome him as a free defender of {COLONY}</label>
            <resultText>You did not keep me on a chain. I will stand beside you by my own will. Point the target.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_charon</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_charon</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>25</amount>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_ghouls.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_ghouls_Q1 -->
  <TMM_Story_ghouls_Q1.label>серебряный плащ выходит в эфир</TMM_Story_ghouls_Q1.label>
  <TMM_Story_ghouls_Q1.letterLabel>Серебряный Плащ выходит в эфир</TMM_Story_ghouls_Q1.letterLabel>
  <TMM_Story_ghouls_Q1.letterText>Граждане Пустоши! Зло таится во тьме, и Добрососедству нужен герой! То есть, э-э, это просто я, Кент Коннолли. Рейдеры захватили старый архив «Хьюбрис Комикс», где заперты мои подлинные радиосценарии Серебряного Плаща и выкройки костюма. Пожалуйста, помогите их вернуть — людям сейчас нужна надежда!</TMM_Story_ghouls_Q1.letterText>
  <TMM_Story_ghouls_Q1.stages.0.label>Зачистить «Хьюбрис Комикс» и вернуть катушки</TMM_Story_ghouls_Q1.stages.0.label>
  <TMM_Story_ghouls_Q1.stages.0.text>Выбейте рейдеров из архива «Хьюбрис Комикс» и вынесите довоенные радиокатушки и выкройки костюма для Кента Коннолли.</TMM_Story_ghouls_Q1.stages.0.text>
  <TMM_Story_ghouls_Q1.stages.1.label>Голос справедливости в эфире</TMM_Story_ghouls_Q1.stages.1.label>
  <TMM_Story_ghouls_Q1.stages.1.text>В подвале издательства обнаружился потайной сейф с довоенными счетами на имя «Р. Романа», скупавшего радиостанции перед самой войной. На радиовышке Кент Коннолли с нетерпением ждёт катушки.</TMM_Story_ghouls_Q1.stages.1.text>
  <TMM_Story_ghouls_Q1.stages.1.choices.0.label>Передать катушки и выкройки Кенту Коннолли</TMM_Story_ghouls_Q1.stages.1.choices.0.label>
  <TMM_Story_ghouls_Q1.stages.1.choices.0.resultText>Да! Вы слышите этот сигнал?! Справедливость снова звучит в эфире! Вот, возьмите копию выкройки плаща!</TMM_Story_ghouls_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_ghouls_Q2 -->
  <TMM_Story_ghouls_Q2.label>контракт на совесть</TMM_Story_ghouls_Q2.label>
  <TMM_Story_ghouls_Q2.letterLabel>Контракт на совесть</TMM_Story_ghouls_Q2.letterLabel>
  <TMM_Story_ghouls_Q2.letterText>Слушай сюда, гладкокожий. Банда работорговцев под крышей Стрелков вылавливает мирных гулей на торговых трактах и продаёт их в гладиаторские ямы к одичавшим. Хэнкок хочет сжечь их базу дотла, и мне нужен надёжный ствол рядом. Ты в деле?</TMM_Story_ghouls_Q2.letterText>
  <TMM_Story_ghouls_Q2.stages.0.label>Штурмовать гладиаторский лагерь работорговцев</TMM_Story_ghouls_Q2.stages.0.label>
  <TMM_Story_ghouls_Q2.stages.0.text>Совершите ночной штурм укреплённой арены под крышей Стрелков и перебейте охрану до того, как пленных гулей бросят на растерзание одичавшим.</TMM_Story_ghouls_Q2.stages.0.text>
  <TMM_Story_ghouls_Q2.stages.1.label>Сорванные цепи в пыли</TMM_Story_ghouls_Q2.stages.1.label>
  <TMM_Story_ghouls_Q2.stages.1.text>Когда Эдвард Диган взламывает последнюю клетку, из неё выходит врач-гуль из подпольного госпиталя и с благодарностью узнаёт знаки {COLONY}.</TMM_Story_ghouls_Q2.stages.1.text>
  <TMM_Story_ghouls_Q2.stages.1.choices.0.label>Освободить всех пленных гулей и казнить главаря работорговцев</TMM_Story_ghouls_Q2.stages.1.choices.0.label>
  <TMM_Story_ghouls_Q2.stages.1.choices.0.resultText>Двести лет живу, а редко встречаю людей, у которых слово не расходится с делом. Добрососедство этого не забудет.</TMM_Story_ghouls_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_ghouls_Q3 -->
  <TMM_Story_ghouls_Q3.label>рецепт против распада</TMM_Story_ghouls_Q3.label>
  <TMM_Story_ghouls_Q3.letterLabel>Рецепт против распада</TMM_Story_ghouls_Q3.letterLabel>
  <TMM_Story_ghouls_Q3.letterText>П-привет... Я Гоб. Дейзи сказала, что вы по-настоящему порядочно относитесь к таким, как мы. Некоторые из наших старейших довоенных жителей начинают слышать шёпот и терять рассудок — подступает одичание. Я нашёл рецепт нейростабилизатора, но светящиеся грибы растут только в самом пекле радиационной воронки. Поможете собрать их?</TMM_Story_ghouls_Q3.letterText>
  <TMM_Story_ghouls_Q3.stages.0.label>Собрать светящиеся грибы в радиационной воронке</TMM_Story_ghouls_Q3.stages.0.label>
  <TMM_Story_ghouls_Q3.stages.0.text>Пробейтесь сквозь тварей в заражённой воронке и соберите светящиеся грибы для нейростабилизатора Гоба.</TMM_Story_ghouls_Q3.stages.0.text>
  <TMM_Story_ghouls_Q3.stages.1.label>Неразорвавшаяся боеголовка</TMM_Story_ghouls_Q3.stages.1.label>
  <TMM_Story_ghouls_Q3.stages.1.text>На самом дне воронки торчит неразорвавшаяся боеголовка 2077 года с нацарапанной на стальном корпусе енохианской печатью сдерживания. В лаборатории Гоб уже ждёт собранные грибы.</TMM_Story_ghouls_Q3.stages.1.text>
  <TMM_Story_ghouls_Q3.stages.1.choices.0.label>Доставить грибы Гобу для приготовления сыворотки</TMM_Story_ghouls_Q3.stages.1.choices.0.label>
  <TMM_Story_ghouls_Q3.stages.1.choices.0.resultText>Получилось! Дымка в их глазах отступает! Спасибо... впервые за двести лет кто-то помог нам просто так.</TMM_Story_ghouls_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_ghouls_Q4 -->
  <TMM_Story_ghouls_Q4.label>последний рассвет</TMM_Story_ghouls_Q4.label>
  <TMM_Story_ghouls_Q4.letterLabel>Последний рассвет</TMM_Story_ghouls_Q4.letterLabel>
  <TMM_Story_ghouls_Q4.letterText>Слушай, друг, я редко прошу об одолжениях. Мой старейший товарищ — парень, вытащивший меня из канавы, когда я только обратился — перешёл черту, где помогает отвар Гоба. Он заперся в старой обсерватории с древней инопланетной короной-артефактом, которую мы нашли, и его разум тонет в звериной ярости. Помоги мне закончить это достойно, так или иначе.</TMM_Story_ghouls_Q4.letterText>
  <TMM_Story_ghouls_Q4.stages.0.label>Прорваться к куполу разрушенной обсерватории</TMM_Story_ghouls_Q4.stages.0.label>
  <TMM_Story_ghouls_Q4.stages.0.text>Пробейтесь сквозь ночных аномалий, стянувшихся на телекинетические волны из купола старой обсерватории.</TMM_Story_ghouls_Q4.stages.0.text>
  <TMM_Story_ghouls_Q4.stages.1.label>Под треснувшим куполом</TMM_Story_ghouls_Q4.stages.1.label>
  <TMM_Story_ghouls_Q4.stages.1.text>Внутри купола Джон Хэнкок медленно снимает треуголку, пока его старейший друг дрожит над гудящим артефактом, сдерживая последний приступ безумия. Вы можете рискнуть применить резонанс Искры и сыворотку, чтобы вернуть ему разум, или помочь Хэнкоку исполнить прощальный долг.</TMM_Story_ghouls_Q4.stages.1.text>
  <TMM_Story_ghouls_Q4.stages.1.choices.0.label>Использовать резонанс Искры и сыворотку, чтобы вернуть ему разум (за Искру и надежду)</TMM_Story_ghouls_Q4.stages.1.choices.0.label>
  <TMM_Story_ghouls_Q4.stages.1.choices.0.resultText>Он... он вспомнил моё имя. Чёрт побери, в этой проклятой Пустоши ещё случаются чудеса! Добрососедство с тобой до конца, брат!</TMM_Story_ghouls_Q4.stages.1.choices.0.resultText>
  <TMM_Story_ghouls_Q4.stages.1.choices.1.label>Помочь Хэнкоку исполнить последний долг и забрать артефакт силой (суровый путь)</TMM_Story_ghouls_Q4.stages.1.choices.1.label>
  <TMM_Story_ghouls_Q4.stages.1.choices.1.resultText>Он ушёл человеком, а не зверем. Спасибо, что не дал мне дрогнуть. Забирай эту штуку — от неё одни беды.</TMM_Story_ghouls_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_ghouls_Q5 -->
  <TMM_Story_ghouls_Q5.label>новый хозяин контракта</TMM_Story_ghouls_Q5.label>
  <TMM_Story_ghouls_Q5.letterLabel>Новый хозяин контракта</TMM_Story_ghouls_Q5.letterLabel>
  <TMM_Story_ghouls_Q5.letterText>Я — Харон. Мой прежний держатель контракта погиб в перестрелке. Пергамент моей клятвы попал в руки лейтенанта Стрелков на блокпосту трассы. Верните мой контракт, и мой дробовик будет принадлежать вашему поселению — с кем бы вы ни воевали.</TMM_Story_ghouls_Q5.letterText>
  <TMM_Story_ghouls_Q5.stages.0.label>Возвращение пергамента Харона</TMM_Story_ghouls_Q5.stages.0.label>
  <TMM_Story_ghouls_Q5.stages.0.text>Лейтенант Стрелков готов продать контракт Харона за 1500 серебра — либо вы можете взять блокпост на трассе штурмом и забрать пергамент с боем.</TMM_Story_ghouls_Q5.stages.0.text>
  <TMM_Story_ghouls_Q5.stages.0.choices.0.label>Выкупить контракт за 1500 серебра со складов {COLONY}</TMM_Story_ghouls_Q5.stages.0.choices.0.label>
  <TMM_Story_ghouls_Q5.stages.0.choices.0.resultText>Лейтенант Стрелков усмехается в рацию и высылает курьера за серебром.</TMM_Story_ghouls_Q5.stages.0.choices.0.resultText>
  <TMM_Story_ghouls_Q5.stages.0.choices.1.label>Взять блокпост Стрелков штурмом и отбить контракт силой</TMM_Story_ghouls_Q5.stages.0.choices.1.label>
  <TMM_Story_ghouls_Q5.stages.0.choices.1.resultText>Харон проверяет патроны в своём боевом дробовике и молча кивает. Выступаем.</TMM_Story_ghouls_Q5.stages.0.choices.1.resultText>
  <TMM_Story_ghouls_Q5.stages.1.label>Передать 1500 серебра за контракт Харона</TMM_Story_ghouls_Q5.stages.1.label>
  <TMM_Story_ghouls_Q5.stages.1.text>Спишите 1500 серебра со складов {COLONY}, чтобы выкупить пергамент Харона у Стрелков.</TMM_Story_ghouls_Q5.stages.1.text>
  <TMM_Story_ghouls_Q5.stages.2.label>Штурмовать блокпост Стрелков на трассе</TMM_Story_ghouls_Q5.stages.2.label>
  <TMM_Story_ghouls_Q5.stages.2.text>Зачистите блокпост наёмников на трассе и заберите древний пергамент контракта Харона.</TMM_Story_ghouls_Q5.stages.2.text>
  <TMM_Story_ghouls_Q5.stages.3.label>Разорванная цепь</TMM_Story_ghouls_Q5.stages.3.label>
  <TMM_Story_ghouls_Q5.stages.3.text>Потрёпанный пергамент у вас в руках. Молчаливый гигант Харон стоит у ворот {COLONY}, ожидая приказов нового хозяина.</TMM_Story_ghouls_Q5.stages.3.text>
  <TMM_Story_ghouls_Q5.stages.3.choices.0.label>Передать контракт самому Харону и сделать его свободным бойцом колонии</TMM_Story_ghouls_Q5.stages.3.choices.0.label>
  <TMM_Story_ghouls_Q5.stages.3.choices.0.resultText>Ты не стал держать меня на цепи. Я останусь рядом по своей воле. Укажи цель.</TMM_Story_ghouls_Q5.stages.3.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_friendly_mutants.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- FRIENDLY MUTANTS Q1: THE WANDERING STORYTELLER                        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_friendly_mutants_Q1</defName>
    <label>the wandering storyteller</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_FriendlyMutantsFaction</faction>
    <giver>TMM_Hero_uncle_leo</giver>
    <fallbackGiver>TMM_Hero_fawkes</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>The Wandering Storyteller</letterLabel>
    <letterText>Hello, little human friends! My name is Uncle Leo! I was walking down the old highway with a bag of clean water and pre-war books for a struggling farmstead, but angry raiders with loud guns pinned me behind a rusted bus. I do not like hurting people—could you help me get through?</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>highway_bus</key>
        <label>Clear the raiders pinning down Uncle Leo</label>
        <text>Eliminate the raiders ambushing the old highway bus so the unarmed super mutant Uncle Leo can safely deliver supplies to the nearby farmstead.</text>
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
        <key>leos_bag</key>
        <label>A gentle giant on the highway</label>
        <text>The raiders scatter as Uncle Leo steps out from behind the rusted bus, clutching a canvas sack filled with clean water, survival rations, and a battered volume of Shakespeare he plans to gift Strong.</text>
        <choices>
          <li>
            <outcome>escorted</outcome>
            <label>Escort Uncle Leo safely to the farmstead</label>
            <resultText>See? Not all big green guys are bad! Thank you, friend—here are some supplies from my bag.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.fm_q1</flag>
                <value>escorted</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>400</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MealSurvivalPack</thing>
                <count>20</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- FRIENDLY MUTANTS Q2: THE FOGGY KENNEL                                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_friendly_mutants_Q2</defName>
    <label>the foggy kennel</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_FriendlyMutantsFaction</faction>
    <giver>TMM_Hero_erickson</giver>
    <fallbackGiver>TMM_Hero_fawkes</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_friendly_mutants_Q1</li>
    </requires>
    <letterLabel>The Foggy Kennel</letterLabel>
    <letterText>You... you helped Leo. Good. My name is Erickson. I live in the fog near the crashed plane with my hounds. A group of trophy hunters from the Gunners set poisoned bear traps around my camp and wounded my best mutant hound. Bring medicine and help me clear their ambush.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <key>hound_medicine</key>
        <label>Prepare medicine for Erickson's wounded hounds</label>
        <text>Allocate 4 industrial medicine from {COLONY}'s stockpiles to treat Erickson's poisoned mutant hounds.</text>
        <thing>MedicineIndustrial</thing>
        <count>4</count>
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
        <key>crashed_plane</key>
        <label>Clear Gunner trophy hunters at the crashed plane</label>
        <text>Drive the Gunner snipers out of the fog around Erickson's crashed-plane kennel.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
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
        <key>fog_journal</key>
        <label>Quiet inside the fuselage</label>
        <text>Inside the crashed fuselage—where Erickson keeps a remarkably neat laboratory journal tracking fog anomalies—the mutant hounds breathe calmly again.</text>
        <choices>
          <li>
            <outcome>healed</outcome>
            <label>Accept Erickson's mutant armor schematics and trade offer</label>
            <resultText>The hounds breathe steady. You do not fear us, and we value that. Take these armor blueprints—cut for broad shoulders.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.fm_q2</flag>
                <value>healed</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.mutant_scrap_armor</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.mutant_hounds</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- FRIENDLY MUTANTS Q3: MILK OF HUMAN KINDNESS                           -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_friendly_mutants_Q3</defName>
    <label>milk of human kindness</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_FriendlyMutantsFaction</faction>
    <giver>TMM_Hero_strong</giver>
    <fallbackGiver>TMM_Hero_fawkes</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_friendly_mutants_Q2</li>
    </requires>
    <letterLabel>Milk of Human Kindness</letterLabel>
    <letterText>Strong need human friend! Wild brothers attack human traders on bridge. Strong smash wild brothers to show humans Strong is good! Come help Strong fight—maybe humans finally give Strong milk of human kindness!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>bridge_clash</key>
        <label>Defend the bridge caravan with Strong</label>
        <text>Fight alongside Strong on the shattered bridge to crush the wild super mutant warband attacking a human trade caravan.</text>
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
        <key>bridge_standoff</key>
        <label>Rifles aimed at a savior</label>
        <text>As the last wild mutant falls, the terrified caravan guards panic and swing their rifles straight onto Strong! Strong's shoulders heave as rage builds—step between them before blood spills.</text>
        <choices>
          <li>
            <outcome>peace</outcome>
            <label>Calm both sides and persuade the traders to thank Strong</label>
            <resultText>Ha! Humans not shoot! Humans give Strong warm bread, bottle of brahmin milk, and say thank you! Strong respect you now!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.fm_q3</flag>
                <value>peace</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_WildMutantsFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>600</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.mutant_cage_helmet</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- FRIENDLY MUTANTS Q4 (FACTION POINT): A CURE FOR VIRGIL                -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_friendly_mutants_Q4</defName>
    <label>a cure for virgil</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_FriendlyMutantsFaction</faction>
    <giver>TMM_Hero_virgil</giver>
    <fallbackGiver>TMM_Hero_fawkes</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_friendly_mutants_Q3</li>
    </requires>
    <letterLabel>A Cure for Virgil</letterLabel>
    <letterText>I did it... by God, I actually synthesized it! Using the spiritual resonance readings from your Zero Spark, I stabilized a single dose of the FEV retro-virus! Come to my cave lab immediately—my mind is slipping every day, and this is my only chance to become human again!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>virgil_cave</key>
        <label>Defend Brian Virgil's cave laboratory</label>
        <text>Hold the mouth of Brian Virgil's rocky cave alongside Fawkes for 5 hours while elite Institute Coursers attempt to destroy the centrifuge.</text>
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>5</holdHours>
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
        <key>single_vial</key>
        <label>One surviving vial</label>
        <text>Fawkes lowers his minigun at the cave mouth—refusing the cure for himself because his strength is needed to shield the weak. Inside, only a single intact vial of FEV retro-virus survived the battle. You can hand it to Virgil to restore his humanity, or take it to save a settler child dying of acute radiation mutation.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Give the only vial to Brian Virgil (Science &amp; Alliance with Fawkes)</label>
            <resultText>It's working... I can feel my fingers, my own thoughts again! I will never forget this debt. Fawkes and I stand with you to the end!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.friendly_mutants</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.virgilCured</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_atoms_judgement</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.fawkes_vault87_suit</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Take the serum to save the dying settler child (Sacrificial Choice)</label>
            <resultText>I... I understand. I built that monster myself; it's my cross to bear. Take the vial... and go, while I still remember my name.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.friendly_mutants</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.virgilCured</flag>
                <value>false</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_atoms_judgement</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.fawkes_vault87_suit</flag>
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
        <flag>arc.friendly_mutants</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- FRIENDLY MUTANTS Q5 (DEFECTOR QUEST): LABORATORY OF HOPE              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_friendly_mutants_Q5</defName>
    <label>laboratory of hope</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_FriendlyMutantsFaction</faction>
    <giver>TMM_Hero_virgil</giver>
    <fallbackGiver>TMM_Hero_fawkes</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_friendly_mutants_Q4</li>
    </requires>
    <letterLabel>Laboratory of Hope</letterLabel>
    <letterText>With the Consortium and the Enclave mobilizing, my cave is no longer safe—and your Zero Anchor has the exact energy signature I need to synthesize a mass-produced cure for both FEV and ghoul degeneration. Help me relocate my equipment to your colony!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>enclave_ambush</key>
        <label>Break the Enclave ambush on Virgil's evacuation</label>
        <text>Clear the Enclave ambush pinning Brian Virgil and his lab crates on the ridge so he can safely relocate to {COLONY}.</text>
        <enemyFaction>TMM_EnclaveFaction</enemyFaction>
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
        <key>welcome_virgil</key>
        <label>A proper lab at last</label>
        <text>With the Enclave troopers defeated, Brian Virgil arrives safely at {COLONY}'s gates with three crates of biological equipment—ready to synthesize a cure for the entire wasteland.</text>
        <choices>
          <li>
            <outcome>recruit_virgil</outcome>
            <label>Welcome Brian Virgil into {COLONY}'s scientific staff</label>
            <resultText>A real laboratory at last. Give me time—and we will heal this cursed wasteland.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_virgil</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_virgil</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.fev_research_bench</flag>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_friendly_mutants.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_friendly_mutants_Q1 -->
  <TMM_Story_friendly_mutants_Q1.label>бродячий сказочник</TMM_Story_friendly_mutants_Q1.label>
  <TMM_Story_friendly_mutants_Q1.letterLabel>Бродячий сказочник</TMM_Story_friendly_mutants_Q1.letterLabel>
  <TMM_Story_friendly_mutants_Q1.letterText>Привет, маленькие друзья-люди! Меня зовут Дядя Лео! Я шёл по старому шоссе с мешком чистой воды и довоенных книг для бедной фермы, но злые рейдеры с громкими пушками прижали меня за ржавым автобусом. Я не люблю делать людям больно — поможете мне пройти?</TMM_Story_friendly_mutants_Q1.letterText>
  <TMM_Story_friendly_mutants_Q1.stages.0.label>Разогнать рейдеров, прижавших Дядю Лео</TMM_Story_friendly_mutants_Q1.stages.0.label>
  <TMM_Story_friendly_mutants_Q1.stages.0.text>Разгромите рейдеров у старого автобуса на шоссе, чтобы безоружный супермутант Дядя Лео смог донести припасы до фермы.</TMM_Story_friendly_mutants_Q1.stages.0.text>
  <TMM_Story_friendly_mutants_Q1.stages.1.label>Добрый великан на шоссе</TMM_Story_friendly_mutants_Q1.stages.1.label>
  <TMM_Story_friendly_mutants_Q1.stages.1.text>Рейдеры разбегаются, и Дядя Лео выходит из-за ржавого автобуса с мешком чистой воды, пайков и потрёпанным томиком Шекспира, который он хочет подарить Силачу.</TMM_Story_friendly_mutants_Q1.stages.1.text>
  <TMM_Story_friendly_mutants_Q1.stages.1.choices.0.label>Довести Дядю Лео до фермы без гибели поселенцев</TMM_Story_friendly_mutants_Q1.stages.1.choices.0.label>
  <TMM_Story_friendly_mutants_Q1.stages.1.choices.0.resultText>Видишь? Не все большие зелёные ребята плохие! Спасибо, друг, вот тебе немного припасов из моего мешка.</TMM_Story_friendly_mutants_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_friendly_mutants_Q2 -->
  <TMM_Story_friendly_mutants_Q2.label>туманная псарня</TMM_Story_friendly_mutants_Q2.label>
  <TMM_Story_friendly_mutants_Q2.letterLabel>Туманная псарня</TMM_Story_friendly_mutants_Q2.letterLabel>
  <TMM_Story_friendly_mutants_Q2.letterText>Ты... ты помог Лео. Хорошо. Меня зовут Эриксон. Я живу в тумане у разбитого самолёта со своими псами. Охотники за трофеями из Стрелков наставили вокруг лагеря отравленных капканов и ранили моего лучшего пса-мутанта. Принеси лекарства и помоги зачистить их засаду.</TMM_Story_friendly_mutants_Q2.letterText>
  <TMM_Story_friendly_mutants_Q2.stages.0.label>Выделить медикаменты для раненых псов Эриксона</TMM_Story_friendly_mutants_Q2.stages.0.label>
  <TMM_Story_friendly_mutants_Q2.stages.0.text>Передайте 4 аптечки со складов {COLONY}, чтобы вылечить отравленных гончих-мутантов Эриксона.</TMM_Story_friendly_mutants_Q2.stages.0.text>
  <TMM_Story_friendly_mutants_Q2.stages.1.label>Зачистить охотников Стрелков у разбитого самолёта</TMM_Story_friendly_mutants_Q2.stages.1.label>
  <TMM_Story_friendly_mutants_Q2.stages.1.text>Выбейте снайперов Стрелков из тумана вокруг псарни Эриксона у разбитого самолёта.</TMM_Story_friendly_mutants_Q2.stages.1.text>
  <TMM_Story_friendly_mutants_Q2.stages.2.label>Тишина в фюзеляже</TMM_Story_friendly_mutants_Q2.stages.2.label>
  <TMM_Story_friendly_mutants_Q2.stages.2.text>Внутри разбитого фюзеляжа, где Эриксон ведёт аккуратный лабораторный журнал наблюдений за туманными аномалиями, гончие-мутанты снова дышат ровно.</TMM_Story_friendly_mutants_Q2.stages.2.text>
  <TMM_Story_friendly_mutants_Q2.stages.2.choices.0.label>Вылечить всех псов Эриксона и прогнать Стрелков</TMM_Story_friendly_mutants_Q2.stages.2.choices.0.label>
  <TMM_Story_friendly_mutants_Q2.stages.2.choices.0.resultText>Псы дышат ровно. Ты не боишься нас, и мы это ценим. Возьми чертежи брони — она сшита под крупные плечи.</TMM_Story_friendly_mutants_Q2.stages.2.choices.0.resultText>

  <!-- TMM_Story_friendly_mutants_Q3 -->
  <TMM_Story_friendly_mutants_Q3.label>молоко человеческой доброты</TMM_Story_friendly_mutants_Q3.label>
  <TMM_Story_friendly_mutants_Q3.letterLabel>Молоко человеческой доброты</TMM_Story_friendly_mutants_Q3.letterLabel>
  <TMM_Story_friendly_mutants_Q3.letterText>Силачу нужен друг-человек! Дикие братья напали на торговцев людей на мосту. Силач раздавит диких братьев, чтобы люди увидели — Силач хороший! Приходи помочь Силачу драться — может, люди наконец дадут Силачу молоко человеческой доброты!</TMM_Story_friendly_mutants_Q3.letterText>
  <TMM_Story_friendly_mutants_Q3.stages.0.label>Отбить караван на мосту вместе с Силачом</TMM_Story_friendly_mutants_Q3.stages.0.label>
  <TMM_Story_friendly_mutants_Q3.stages.0.text>Разбейте на разрушенном мосту банду диких супермутантов, напавших на человеческий караван.</TMM_Story_friendly_mutants_Q3.stages.0.text>
  <TMM_Story_friendly_mutants_Q3.stages.1.label>Стволы наведены на спасителя</TMM_Story_friendly_mutants_Q3.stages.1.label>
  <TMM_Story_friendly_mutants_Q3.stages.1.text>Едва падает последний дикий мутант, перепуганные охранники каравана вскидывают стволы прямо на Силача! Вмешайтесь, пока Силач не сорвался в слепую ярость.</TMM_Story_friendly_mutants_Q3.stages.1.text>
  <TMM_Story_friendly_mutants_Q3.stages.1.choices.0.label>Успокоить обе стороны и убедить торговцев поблагодарить Силача</TMM_Story_friendly_mutants_Q3.stages.1.choices.0.label>
  <TMM_Story_friendly_mutants_Q3.stages.1.choices.0.resultText>Ха! Люди не стрелять! Люди дать Силачу тёплый хлеб, бутылку браминьего молока и сказать спасибо! Силач теперь уважать тебя!</TMM_Story_friendly_mutants_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_friendly_mutants_Q4 -->
  <TMM_Story_friendly_mutants_Q4.label>лекарство для верджила</TMM_Story_friendly_mutants_Q4.label>
  <TMM_Story_friendly_mutants_Q4.letterLabel>Лекарство для Верджила</TMM_Story_friendly_mutants_Q4.letterLabel>
  <TMM_Story_friendly_mutants_Q4.letterText>Я сделал это... боже, я правда её синтезировал! Используя данные резонанса вашей Нулевой Искры, я стабилизировал одну-единственную дозу ретровируса ВРЭ! Немедленно приходите в мою пещерную лабораторию — мой рассудок тает с каждым днём, и это мой единственный шанс снова стать человеком!</TMM_Story_friendly_mutants_Q4.letterText>
  <TMM_Story_friendly_mutants_Q4.stages.0.label>Защитить пещерную лабораторию Брайана Верджила</TMM_Story_friendly_mutants_Q4.stages.0.label>
  <TMM_Story_friendly_mutants_Q4.stages.0.text>Удерживайте вход в пещеру Верджила плечом к плечу с Фоксом в течение 5 часов, пока охотники Института рвутся уничтожить центрифугу.</TMM_Story_friendly_mutants_Q4.stages.0.text>
  <TMM_Story_friendly_mutants_Q4.stages.1.label>Одна уцелевшая ампула</TMM_Story_friendly_mutants_Q4.stages.1.label>
  <TMM_Story_friendly_mutants_Q4.stages.1.text>Фокс опускает миниган у входа в пещеру — сам он отказывается от лекарства, потому что его сила нужна для защиты слабых. Внутри уцелела единственная ампула ретровируса ВРЭ: вернуть Верджилу человеческий облик или спасти умирающего от острой лучевой мутации ребёнка поселенцев?</TMM_Story_friendly_mutants_Q4.stages.1.text>
  <TMM_Story_friendly_mutants_Q4.stages.1.choices.0.label>Отдать единственную дозу Брайану Верджилу (за науку и союз с Фоксом)</TMM_Story_friendly_mutants_Q4.stages.1.choices.0.label>
  <TMM_Story_friendly_mutants_Q4.stages.1.choices.0.resultText>Оно работает... я снова чувствую свои пальцы, свои мысли! Я никогда не забуду этого долга. Фокс и я будем с вами до конца!</TMM_Story_friendly_mutants_Q4.stages.1.choices.0.resultText>
  <TMM_Story_friendly_mutants_Q4.stages.1.choices.1.label>Забрать сыворотку для спасения умирающего ребёнка (жертвенный выбор)</TMM_Story_friendly_mutants_Q4.stages.1.choices.1.label>
  <TMM_Story_friendly_mutants_Q4.stages.1.choices.1.resultText>Я... я понимаю. Я сам создал это чудовище, мне и нести этот крест. Возьмите ампулу... и уходите, пока я ещё помню своё имя.</TMM_Story_friendly_mutants_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_friendly_mutants_Q5 -->
  <TMM_Story_friendly_mutants_Q5.label>лаборатория надежды</TMM_Story_friendly_mutants_Q5.label>
  <TMM_Story_friendly_mutants_Q5.letterLabel>Лаборатория надежды</TMM_Story_friendly_mutants_Q5.letterLabel>
  <TMM_Story_friendly_mutants_Q5.letterText>Теперь, когда Консорциум и Анклав подняли армии, моя пещера больше не безопасна — а у вашего Нулевого Якоря есть именно та энергетическая сигнатура, которая нужна для массового лекарства от ВРЭ и одичания гулей. Помогите перевезти оборудование в вашу колонию!</TMM_Story_friendly_mutants_Q5.letterText>
  <TMM_Story_friendly_mutants_Q5.stages.0.label>Прорвать засаду Анклава при эвакуации Верджила</TMM_Story_friendly_mutants_Q5.stages.0.label>
  <TMM_Story_friendly_mutants_Q5.stages.0.text>Разбейте засаду Анклава на хребте, прижавшую Брайана Верджила и ящики с его лабораторным оборудованием.</TMM_Story_friendly_mutants_Q5.stages.0.text>
  <TMM_Story_friendly_mutants_Q5.stages.1.label>Настоящая лаборатория</TMM_Story_friendly_mutants_Q5.stages.1.label>
  <TMM_Story_friendly_mutants_Q5.stages.1.text>Когда солдаты Анклава разбиты, Брайан Верджил благополучно прибывает к воротам {COLONY} с тремя ящиками научного оборудования — готовый создать лекарство для всей Пустоши.</TMM_Story_friendly_mutants_Q5.stages.1.text>
  <TMM_Story_friendly_mutants_Q5.stages.1.choices.0.label>Принять Брайана Верджила в научный штаб колонии</TMM_Story_friendly_mutants_Q5.stages.1.choices.0.label>
  <TMM_Story_friendly_mutants_Q5.stages.1.choices.0.resultText>Наконец-то настоящая лаборатория. Дайте мне время — и мы исцелим эту проклятую Пустошь.</TMM_Story_friendly_mutants_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_enclave.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- ENCLAVE Q1: RESTORING THE GRID                                        -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_enclave_Q1</defName>
    <label>restoring the grid</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_captain_richards</giver>
    <fallbackGiver>TMM_Hero_colonel_autumn</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Restoring the Grid</letterLabel>
    <letterText>Attention, local settlement. This is Captain Richards of the Enclave Expeditionary Force. Pre-war automated defense platforms have locked onto our orbital relay frequencies. Assist our Hellfire squad in securing Relay Tower 09, and the rightful government of America will reward your loyalty.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>relay_tower_09</key>
        <label>Secure Relay Tower 09 with Captain Richards</label>
        <text>Assault the pre-war radar station defending Relay Tower 09 and clear the hostile forces blocking Enclave orbital frequencies.</text>
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
        <key>relay_handover</key>
        <label>Ghosts on the monitor</label>
        <text>As power returns to Relay Tower 09, an ancient static-warped broadcast screen flickers briefly with President Eden's pre-war seal before Captain Richards wipes the buffer.</text>
        <choices>
          <li>
            <outcome>secured</outcome>
            <label>Transfer control of Relay Tower 09 to Captain Richards</label>
            <resultText>Sharp discipline for civilians. The Enclave remembers those who serve order.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.enc_q1</flag>
                <value>secured</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>550</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>3</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.enclave_officer_uniform</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ENCLAVE Q2: REGULATION AND FIST                                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_enclave_Q2</defName>
    <label>regulation and fist</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_sergeant_dornan</giver>
    <fallbackGiver>TMM_Hero_captain_richards</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_enclave_Q1</li>
    </requires>
    <letterLabel>Regulation and Fist</letterLabel>
    <letterText>Listen up, maggots! Colonel Autumn thinks your little dirt-farm has potential, which means you are now under my personal inspection! My field training camp is about to be hit by a pack of wasteland raiders—get over here, stand at attention, and show me you don't fold like wet paper!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>dornan_camp</key>
        <label>Hold Sergeant Dornan's training camp</label>
        <text>Hold the Enclave field camp for 5 hours against raiding waves while Sergeant Dornan furiously chews out a charging raider for attacking without a regulation helmet.</text>
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
        <key>dornan_inspection</key>
        <label>Inspection passed</label>
        <text>The smoke clears over the drill yard. Sergeant Dornan marches down your firing line, glaring at every rifle barrel before letting out a grudging bark of approval.</text>
        <choices>
          <li>
            <outcome>passed</outcome>
            <label>Stand at attention and collect Dornan's commendation</label>
            <resultText>Not half bad, soldier! Dammit, I might actually hammer real troopers out of you yet! Take your pay and don't dare lose your gear!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.enc_q2</flag>
                <value>passed</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>700</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.dornan_drill_buff</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ENCLAVE Q3: SAMPLES OF PURITY                                         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_enclave_Q3</defName>
    <label>samples of purity</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_charles_curling</giver> <!-- check id -->
    <fallbackGiver>TMM_Hero_colonel_autumn</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_enclave_Q2</li>
    </requires>
    <letterLabel>Samples of Purity</letterLabel>
    <letterText>Greetings. I am Dr. Charles Curling, head of the Enclave Chemical Corps. To ensure the survival of uncorrupted humanity, I require tissue samples from both a mutated super mutant behemoth and a supernatural anomaly. Collect them quietly, and you will be compensated handsomely.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>swamp_hunt</key>
        <label>Collect mutant and anomaly tissue samples in the mire</label>
        <text>Clear the wild super mutant hunting ground in the toxic mire and fill Dr. Curling's bio-vials.</text>
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
        <key>curling_vials</key>
        <label>Stamp of Project Purity</label>
        <text>Sealing the last specimen tube, you notice faded stenciling on Dr. Curling's cases: "Project Purity / Strain FEV-2." Deliver the full batch to Dr. Curling, or secretly forward a copy to Brian Virgil?</text>
        <choices>
          <li>
            <outcome>delivered</outcome>
            <label>Deliver all tissue samples straight to Dr. Charles Curling</label>
            <resultText>Immaculate preservation. Soon we will hold a remedy capable of correcting every error of evolution.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.enc_q3</flag>
                <value>delivered</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>850</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>4</count>
              </li>
            </effects>
          </li>
          <li>
            <outcome>leaked</outcome>
            <label>Leak a duplicate sample and Curling's notes to Brian Virgil</label>
            <resultText>The samples arrived, though the lab manifest shows curious discrepancies... Tread carefully, civilian.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.enc_q3</flag>
                <value>leaked</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>850</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>4</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ENCLAVE Q4 (FACTION POINT): CLEAN SLATE                               -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_enclave_Q4</defName>
    <label>clean slate</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_colonel_autumn</giver>
    <fallbackGiver>TMM_Hero_captain_richards</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_enclave_Q3</li>
    </requires>
    <letterLabel>Clean Slate</letterLabel>
    <letterText>The time for half-measures is over. Dr. Curling's modified aerosol is loaded into our mobile crawler's dispersal tanks—once released, it will cleanse the region of every mutant, ghoul, synth, and occult parasite in one stroke. Join me at the crawler to authorize the launch, and secure your place in the new America.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>mobile_crawler</key>
        <label>Reach the Enclave Mobile Base Crawler console</label>
        <text>Fight through nocturnal creatures assaulting the colossal Enclave Mobile Base Crawler to reach the central aerosol launch console.</text>
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
        <key>crawler_console</key>
        <label>The dispersal console</label>
        <text>Beside the launch terminal lies an unread Consortium directive ordering the silent liquidation of Colonel Autumn himself once the purge concludes. Colonel Autumn waits for your authorization on the aerosol tanks.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Authorize full dispersal of Dr. Curling's aerosol (Side with the Enclave)</label>
            <resultText>History will mark this day as the dawn of rebirth. Take this Tesla Cannon—a weapon worthy of a loyal ally.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.enclave</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_tesla_cannon</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.enclave_x01_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>compromise</outcome>
            <label>Reprogram the aerosol to target ONLY demons and Leviathans (Scientist's Compromise)</label>
            <resultText>Audacious... you altered a direct order! Yet if this scours the abominations from our soil, I can accept the result.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.enclave</flag>
                <value>compromise</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MonsterClansFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_tesla_cannon</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.enclave_x01_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Detonate the aerosol tanks and fight your way off the crawler (Protect the Wasteland)</label>
            <resultText>Traitor! You threw away mankind's future for a horde of mutants! Cut them down!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.enclave</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-70</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GhoulsFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_FriendlyMutantsFaction</faction>
                <amount>40</amount>
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
                <flag>unlock.autumn_coat</flag>
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
        <flag>arc.enclave</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ENCLAVE Q5 (DEFECTOR QUEST): TREASON AT HEADQUARTERS                  -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_enclave_Q5</defName>
    <label>treason at headquarters</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_sergeant_dornan</giver>
    <fallbackGiver>TMM_Hero_captain_richards</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_enclave_Q4</li>
    </requires>
    <anyOf>
      <li>choice.enclave=pro_spark</li>
      <li>choice.enclave=compromise</li>
      <li>ponr1=protect</li>
    </anyOf>
    <letterLabel>Treason at Headquarters</letterLabel>
    <letterText>Soldier! High Command has lost its damn mind! Autumn is cutting backroom deals with Institute lab-rats and occult freaks, and he just ordered Richards' squad to execute my platoon for 'insubordination'! We are breaking out—open your gates and give a real soldier a place to stand!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>hellfire_pursuit</key>
        <label>Repel Captain Richards's Hellfire pursuit squad</label>
        <text>Sergeant Dornan charges through {COLONY}'s perimeter with Enclave Hellfire troopers hot on his heels! Hold the line!</text>
        <faction>TMM_EnclaveFaction</faction>
        <pointsFactor>1.3</pointsFactor>
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
        <key>accept_dornan</key>
        <label>A drill sergeant in your ranks</label>
        <text>The last Hellfire pursuer hits the dirt. Sergeant Dornan wipes soot off his visor and immediately starts glaring at your colonists for not wearing regulation power armor.</text>
        <choices>
          <li>
            <outcome>recruit_dornan</outcome>
            <label>Welcome Sergeant Dornan into {COLONY}'s garrison</label>
            <resultText>Finally a commander with a spine! Now fall in, maggots—I'm gonna teach you how to hold a perimeter!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_sergeant_dornan</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_sergeant_dornan</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_EnclaveFaction</faction>
                <amount>-80</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gun_Minigun</thing>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_enclave.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_enclave_Q1 -->
  <TMM_Story_enclave_Q1.label>восстановление связи</TMM_Story_enclave_Q1.label>
  <TMM_Story_enclave_Q1.letterLabel>Восстановление связи</TMM_Story_enclave_Q1.letterLabel>
  <TMM_Story_enclave_Q1.letterText>Внимание, местное поселение. Говорит капитан Ричардс из экспедиционного корпуса Анклава. Довоенные автоматические системы обороны заблокировали наши орбитальные частоты. Помогите нашему отряду «Адское пламя» зачистить ретранслятор 09, и законное правительство Америки вознаградит вашу лояльность.</TMM_Story_enclave_Q1.letterText>
  <TMM_Story_enclave_Q1.stages.0.label>Зачистить ретранслятор 09 вместе с капитаном Ричардсом</TMM_Story_enclave_Q1.stages.0.label>
  <TMM_Story_enclave_Q1.stages.0.text>Штурмуйте довоенную радиолокационную станцию и устраните противников, блокирующих орбитальные частоты Анклава.</TMM_Story_enclave_Q1.stages.0.text>
  <TMM_Story_enclave_Q1.stages.1.label>Призраки на мониторе</TMM_Story_enclave_Q1.stages.1.label>
  <TMM_Story_enclave_Q1.stages.1.text>Когда питание возвращается на станцию 09, на главном мониторе на миг всплывает старая заставка президента Эдема с помехами, прежде чем капитан Ричардс очищает буфер.</TMM_Story_enclave_Q1.stages.1.text>
  <TMM_Story_enclave_Q1.stages.1.choices.0.label>Передать управление ретранслятором капитану Ричардсу</TMM_Story_enclave_Q1.stages.1.choices.0.label>
  <TMM_Story_enclave_Q1.stages.1.choices.0.resultText>Отличная выучка для гражданских. Анклав запомнит ваше содействие.</TMM_Story_enclave_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_enclave_Q2 -->
  <TMM_Story_enclave_Q2.label>устав и кулак</TMM_Story_enclave_Q2.label>
  <TMM_Story_enclave_Q2.letterLabel>Устав и кулак</TMM_Story_enclave_Q2.letterLabel>
  <TMM_Story_enclave_Q2.letterText>Слушать сюда, салаги! Полковник Отем считает, что у вашей грязной фермы есть потенциал, а значит, теперь вы под моей личной инспекцией! На мой учебный лагерь вот-вот накатят рейдеры Пустоши — тащите сюда свои задницы, встать смирно и покажите, что вы не раскиснете как мокрая бумага!</TMM_Story_enclave_Q2.letterText>
  <TMM_Story_enclave_Q2.stages.0.label>Удержать учебный лагерь сержанта Дорнана</TMM_Story_enclave_Q2.stages.0.label>
  <TMM_Story_enclave_Q2.stages.0.text>Обороняйте полевой лагерь Анклава 5 часов от волн рейдеров, пока сержант Дорнан яростно орёт на нападающих за атаку без уставного шлема.</TMM_Story_enclave_Q2.stages.0.text>
  <TMM_Story_enclave_Q2.stages.1.label>Инспекция пройдена</TMM_Story_enclave_Q2.stages.1.label>
  <TMM_Story_enclave_Q2.stages.1.text>Дым над плацем рассеивается. Сержант Дорнан проходит вдоль строя, сурово оглядывая стволы винтовок, прежде чем выдать скупое одобрение.</TMM_Story_enclave_Q2.stages.1.text>
  <TMM_Story_enclave_Q2.stages.1.choices.0.label>Выдержать натиск и не потерять ни одного бойца Дорнана</TMM_Story_enclave_Q2.stages.1.choices.0.label>
  <TMM_Story_enclave_Q2.stages.1.choices.0.resultText>Неплохо, солдат! Чёрт побери, из тебя ещё можно выбить толк! Держи пару ящиков патронов и не смей терять форму!</TMM_Story_enclave_Q2.stages.1.choices.0.resultText>

  <!-- TMM_Story_enclave_Q3 -->
  <TMM_Story_enclave_Q3.label>образцы чистоты</TMM_Story_enclave_Q3.label>
  <TMM_Story_enclave_Q3.letterLabel>Образцы чистоты</TMM_Story_enclave_Q3.letterLabel>
  <TMM_Story_enclave_Q3.letterText>Приветствую. Я доктор Чарльз Керлинг, глава химического корпуса Анклава. Чтобы обеспечить выживание чистокровного человечества, мне требуются образцы тканей супермутанта и сверхъестественной аномалии. Соберите их без лишнего шума, и вы будете щедро вознаграждены.</TMM_Story_enclave_Q3.letterText>
  <TMM_Story_enclave_Q3.stages.0.label>Собрать биопробы мутантов и аномалий на болотах</TMM_Story_enclave_Q3.stages.0.label>
  <TMM_Story_enclave_Q3.stages.0.text>Зачистите охотничьи угодья диких супермутантов на токсичном болоте и заполните контейнеры доктора Керлинга.</TMM_Story_enclave_Q3.stages.0.text>
  <TMM_Story_enclave_Q3.stages.1.label>Маркировка «Проекта Чистота»</TMM_Story_enclave_Q3.stages.1.label>
  <TMM_Story_enclave_Q3.stages.1.text>Запечатывая последнюю колбу, вы замечаете старую маркировку на кейсах Керлинга: «Проект Чистота / Штамм ФЭВ-2». Передать образцы Керлингу или тайно слить копию Брайану Верджилу?</TMM_Story_enclave_Q3.stages.1.text>
  <TMM_Story_enclave_Q3.stages.1.choices.0.label>Передать образцы доктору Керлингу</TMM_Story_enclave_Q3.stages.1.choices.0.label>
  <TMM_Story_enclave_Q3.stages.1.choices.0.resultText>Идеальная сохранность. Скоро у нас будет средство, способное исправить все ошибки эволюции.</TMM_Story_enclave_Q3.stages.1.choices.0.resultText>
  <TMM_Story_enclave_Q3.stages.1.choices.1.label>Передать копию образцов Брайану Верджилу</TMM_Story_enclave_Q3.stages.1.choices.1.label>
  <TMM_Story_enclave_Q3.stages.1.choices.1.resultText>Образцы доставлены, но в отчётах видны нестыковки... Будьте осторожны, гражданский.</TMM_Story_enclave_Q3.stages.1.choices.1.resultText>

  <!-- TMM_Story_enclave_Q4 -->
  <TMM_Story_enclave_Q4.label>чистый лист</TMM_Story_enclave_Q4.label>
  <TMM_Story_enclave_Q4.letterLabel>Чистый лист</TMM_Story_enclave_Q4.letterLabel>
  <TMM_Story_enclave_Q4.letterText>Время полумер прошло. Модифицированный аэрозоль доктора Керлинга загружен в баки нашего мобильного краулера — стоит его распылить, и регион одним ударом очистится от всех мутантов, гулей, синтов и оккультных паразитов. Присоединяйтесь ко мне на краулере для запуска и займите своё место в новой Америке.</TMM_Story_enclave_Q4.letterText>
  <TMM_Story_enclave_Q4.stages.0.label>Прорваться к пульту мобильного краулера Анклава</TMM_Story_enclave_Q4.stages.0.label>
  <TMM_Story_enclave_Q4.stages.0.text>Очистите палубу гигантского мобильного краулера Анклава от ночных тварей и выйдите к главному пусковому терминалу.</TMM_Story_enclave_Q4.stages.0.text>
  <TMM_Story_enclave_Q4.stages.1.label>Пульт распыления</TMM_Story_enclave_Q4.stages.1.label>
  <TMM_Story_enclave_Q4.stages.1.text>Рядом с терминалом лежит секретная директива Консорциума об устранении самого полковника Отема сразу после зачистки. Полковник Отем ждёт вашей авторизации на запуск баков с аэрозолем.</TMM_Story_enclave_Q4.stages.1.text>
  <TMM_Story_enclave_Q4.stages.1.choices.0.label>Авторизовать полный запуск токсина (за Анклав)</TMM_Story_enclave_Q4.stages.1.choices.0.label>
  <TMM_Story_enclave_Q4.stages.1.choices.0.resultText>История запомнит этот день как начало возрождения. Вот Пушка Теслы — достойное оружие для верного союзника.</TMM_Story_enclave_Q4.stages.1.choices.0.resultText>
  <TMM_Story_enclave_Q4.stages.1.choices.1.label>Перенастроить формулу только против демонов и левиафанов (компромисс учёного)</TMM_Story_enclave_Q4.stages.1.choices.1.label>
  <TMM_Story_enclave_Q4.stages.1.choices.1.resultText>Дерзко... Вы самовольно изменили приказ! Но если это выжжет нечисть — я приму такой результат.</TMM_Story_enclave_Q4.stages.1.choices.1.resultText>
  <TMM_Story_enclave_Q4.stages.1.choices.2.label>Подорвать баки с токсином и прорваться с боем (за Искру и Пустошь)</TMM_Story_enclave_Q4.stages.1.choices.2.label>
  <TMM_Story_enclave_Q4.stages.1.choices.2.resultText>Предатель! Вы пожертвовали будущим человечества ради кучки мутантов! Уничтожить их!</TMM_Story_enclave_Q4.stages.1.choices.2.resultText>

  <!-- TMM_Story_enclave_Q5 -->
  <TMM_Story_enclave_Q5.label>измена штаба</TMM_Story_enclave_Q5.label>
  <TMM_Story_enclave_Q5.letterLabel>Измена штаба</TMM_Story_enclave_Q5.letterLabel>
  <TMM_Story_enclave_Q5.letterText>Солдат! Высшее командование окончательно рехнулось! Отем крутит тайные сделки с крысами из Института и оккультными уродами, а только что приказал отряду Ричардса расстрелять мой взвод за «неподчинение»! Мы идём на прорыв — открывайте ворота и дайте настоящему солдату место в строю!</TMM_Story_enclave_Q5.letterText>
  <TMM_Story_enclave_Q5.stages.0.label>Отбить погоню штурмовиков «Адского пламени»</TMM_Story_enclave_Q5.stages.0.label>
  <TMM_Story_enclave_Q5.stages.0.text>Сержант Дорнан прорывается к периметру {COLONY}, а следом за ним идут штурмовики Анклава капитана Ричардса! Держите строй!</TMM_Story_enclave_Q5.stages.0.text>
  <TMM_Story_enclave_Q5.stages.1.label>Инструктор в ваших рядах</TMM_Story_enclave_Q5.stages.1.label>
  <TMM_Story_enclave_Q5.stages.1.text>Последний преследователь падает в пыль. Сержант Дорнан вытирает копоть с визора и тут же начинает проверять у колонистов наличие уставных комплектов силовой брони.</TMM_Story_enclave_Q5.stages.1.text>
  <TMM_Story_enclave_Q5.stages.1.choices.0.label>Принять сержанта Дорнана в гарнизон колонии</TMM_Story_enclave_Q5.stages.1.choices.0.label>
  <TMM_Story_enclave_Q5.stages.1.choices.0.resultText>Наконец-то командир с хребтом! А теперь построились, салаги — я научу вас, как нужно держать оборону!</TMM_Story_enclave_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_gunners.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- GUNNERS Q1: CAPS UP FRONT                                             -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_gunners_Q1</defName>
    <label>caps up front</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_baker</giver>
    <fallbackGiver>TMM_Hero_captain_bridget</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Caps Up Front</letterLabel>
    <letterText>Nothing personal, settler—just business. A pack of feral raiders set up roadblocks on our supply route, and Captain Wes wants the lane cleared before sunset. Spot for me from the ridge while I pick off their heavies, and we'll split the loot fifty-fifty.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>highway_roadblock</key>
        <label>Clear the raider highway roadblock with Baker</label>
        <text>Flush the raiders out of the overpass barricades while Gunner sniper Baker picks off their heavy gunners from above.</text>
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
        <key>split_loot</key>
        <label>Notches on the rifle stock</label>
        <text>Baker calmly carves another set of notches onto his sniper stock and nods once to your squad leader as the supply route opens.</text>
        <choices>
          <li>
            <outcome>honest</outcome>
            <label>Split the salvaged loot fairly with Baker</label>
            <resultText>Clean work. Refreshing dealing with folks who skip the drama. I'll tell Wes you keep your word.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.gun_q1</flag>
                <value>honest</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>600</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.gunner_combat_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GUNNERS Q2: DEBT OF QUINCY                                            -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_gunners_Q2</defName>
    <label>debt of quincy</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_clint</giver>
    <fallbackGiver>TMM_Hero_captain_bridget</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_gunners_Q1</li>
    </requires>
    <letterLabel>Debt of Quincy</letterLabel>
    <letterText>Some stubborn Minutemen holdouts haven't learned when they're beaten—they're gathering for a midnight strike on my outpost at Quincy. Help me hold the walls and wipe them out, and I'll make sure the Gunners open their heavy armory to your traders.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>quincy_allegiance</key>
        <label>Night falls over Quincy</label>
        <text>On the scarred wall of Quincy's old church, faded militia chalk murals and a shattered laser musket still mark the site of Clint's betrayal. Before midnight strikes, choose whose side you fight on.</text>
        <choices>
          <li>
            <outcome>clint</outcome>
            <label>Hold the walls of Quincy alongside Clint against the Minutemen (Side with Gunners)</label>
            <resultText>That's how it's done! Let them learn who owns Quincy now. Take your cut and our helmet schematics.</resultText>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.gun_q2</flag>
                <value>clint</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>900</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.gunner_helmet_bandana</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>minutemen</outcome>
            <label>Warn the Minutemen and storm Clint's garrison in Quincy (Side with Minutemen)</label>
            <resultText>Backstabbing rat! I'll skin you alive!</resultText>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.gun_q2</flag>
                <value>minutemen</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>600</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gun_AssaultRifle</thing>
                <count>1</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>quincy_hold</key>
        <label>Hold Quincy against the Minutemen counterattack</label>
        <text>Defend Clint's Quincy outpost for 5 hours against the furious midnight militia assault.</text>
        <enemyFaction>TMM_MinutemenHuntersFaction</enemyFaction>
        <objective>Hold</objective>
        <holdHours>5</holdHours>
        <timeoutDays>7</timeoutDays>
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
        <key>quincy_liberate</key>
        <label>Storm Clint's Gunner outpost in Quincy</label>
        <text>Crush Clint's mercenary garrison in the ruins of Quincy and return the town to the Minutemen.</text>
        <enemyFaction>TMM_GunnersFaction</enemyFaction>
        <objective>Clear</objective>
        <timeoutDays>7</timeoutDays>
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
        <key>quincy_aftermath</key>
        <label>Dawn over Quincy</label>
        <text>Gunfire dies out across the ruined church square of Quincy. Word of your decisive intervention reaches every mercenary camp and militia watchtower in the region.</text>
        <days>0.2</days>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GUNNERS Q3: TRIAL BY IRON                                             -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_gunners_Q3</defName>
    <label>trial by iron</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_tessa</giver>
    <fallbackGiver>TMM_Hero_captain_bridget</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_gunners_Q2</li>
    </requires>
    <letterLabel>Trial by Iron</letterLabel>
    <letterText>Hey, tough guy! I've been welding new reinforced plates onto my power armor frame, and I need someone with real guts to test if they hold up in a brawl. Bring your best melee fighters to my scrapyard arena—first one knocked down buys the drinks!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>scrapyard_arena</key>
        <label>Brawl at Tessa's scrapyard arena</label>
        <text>Step into the sand pit surrounded by cheering mercenaries tossing wrenches and caps, and test your mettle against Tessa's armored brawlers.</text>
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
        <key>tessa_respect</key>
        <label>Respect forged in scrap</label>
        <text>Dented plating smoking in the dust, Tessa laughs loudly and offers a heavy power-armored gauntlet for a handshake.</text>
        <choices>
          <li>
            <outcome>won</outcome>
            <label>Shake Tessa's hand and collect the arena purse</label>
            <resultText>Haha! Now THAT was a punch! The plates held and you fought like a beast. Respect!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.gun_q3</flag>
                <value>won</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>750</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>3</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GUNNERS Q4 (FACTION POINT): DOUBLE CONTRACT                           -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_gunners_Q4</defName>
    <label>double contract</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_captain_wes</giver>
    <fallbackGiver>TMM_Hero_captain_bridget</fallbackGiver> <!-- check id -->
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_gunners_Q3</li>
    </requires>
    <letterLabel>Double Contract</letterLabel>
    <letterText>You've earned a seat at the big table. An anonymous client—paying in pre-war gold and Vault-Tec bearer bonds—wants a senior liaison of the British Men of Letters eliminated during a secret meeting. Handle the hit cleanly, and I'll hand you my own modified combat rifle, Overseer's Guardian.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>metro_meeting</key>
        <label>Infiltrate the abandoned metro meeting</label>
        <text>Fight past the British Men of Letters security detail at the abandoned metro station to corner the senior liaison.</text>
        <enemyFaction>TMM_MenOfLettersFaction</enemyFaction>
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
        <key>metro_deal</key>
        <label>Briefcase on the platform</label>
        <text>Cornered beside the rusted subway tracks, the British liaison opens his briefcase—revealing a receipt signed by Dick Roman paying the Gunners for synth-capture cages—and offers you 2000 silver to fake his death and take the evidence.</text>
        <choices>
          <li>
            <outcome>pro_faction</outcome>
            <label>Fulfill Captain Wes's contract and eliminate the liaison (Side with Gunners)</label>
            <resultText>No questions, no witnesses. That is true professionalism. Overseer's Guardian is yours, partner.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.gunners</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_overseers_guardian</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.wes_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_spark</outcome>
            <label>Fake the liaison's death, seize the Consortium dirt, and double-cross Wes (For the Truth)</label>
            <resultText>You burned the Gunners for those British snobs?! There's a bounty on your head in every camp from here to the coast!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.gunners</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.consortiumGunnerReceipt</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MenOfLettersFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>2000</count>
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
        <flag>arc.gunners</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- GUNNERS Q5 (DEFECTOR QUEST): NEW EMPLOYER                             -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_gunners_Q5</defName>
    <label>new employer</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_GunnersFaction</faction>
    <giver>TMM_Hero_tessa</giver>
    <fallbackGiver>TMM_Hero_baker</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_gunners_Q4</li>
      <li>story.gun_q3=won</li>
    </requires>
    <letterLabel>New Employer</letterLabel>
    <letterText>Wes is taking suicidal contracts from Vault-Tec suits and throwing our squads into meat grinders against angels and mutants. Screw that. You beat me fair on the arena and you actually pay on time. Cover my exit from the Quincy overpass, and my armor is yours!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>overpass_breakout</key>
        <label>Cover Tessa's breakout from the Quincy overpass</label>
        <text>Shatter Captain Bridget's punitive Gunner encirclement on the Quincy overpass so Tessa can break free in her custom power armor.</text>
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
        <key>hire_tessa</key>
        <label>Heavy armor for hire</label>
        <text>Stepping through {COLONY}'s gate in her battered custom power armor, Tessa immediately scans your courtyard looking for your toughest melee brawler to spar with.</text>
        <choices>
          <li>
            <outcome>recruit_tessa</outcome>
            <label>Hire Tessa into {COLONY}'s permanent garrison</label>
            <resultText>Deal! Just point me to a proper workbench—gotta weld this pauldron back together after that brawl.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_tessa</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_tessa</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_GunnersFaction</faction>
                <amount>-50</amount>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_gunners.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_gunners_Q1 -->
  <TMM_Story_gunners_Q1.label>крышки вперёд</TMM_Story_gunners_Q1.label>
  <TMM_Story_gunners_Q1.letterLabel>Крышки вперёд</TMM_Story_gunners_Q1.letterLabel>
  <TMM_Story_gunners_Q1.letterText>Ничего личного, поселенец — просто бизнес. Шайка диких рейдеров перекрыла наш маршрут снабжения, и капитан Уэс хочет расчистить дорогу до заката. Прикрой меня с хребта, пока я буду снимать их пулемётчиков, и поделим добычу пятьдесят на пятьдесят.</TMM_Story_gunners_Q1.letterText>
  <TMM_Story_gunners_Q1.stages.0.label>Зачистить блокпост рейдеров на трассе с Бейкером</TMM_Story_gunners_Q1.stages.0.label>
  <TMM_Story_gunners_Q1.stages.0.text>Выбейте рейдеров с баррикад под эстакадой, пока снайпер Бейкер снимает их тяжёлых стрелков сверху.</TMM_Story_gunners_Q1.stages.0.text>
  <TMM_Story_gunners_Q1.stages.1.label>Зарубки на прикладе</TMM_Story_gunners_Q1.stages.1.label>
  <TMM_Story_gunners_Q1.stages.1.text>Бейкер спокойно отмечает каждую цель зарубкой на прикладе винтовки и молча кивает командиру вашего отряда, когда трасса открыта.</TMM_Story_gunners_Q1.stages.1.text>
  <TMM_Story_gunners_Q1.stages.1.choices.0.label>Зачистить блокпост и честно разделить трофеи с Бейкером</TMM_Story_gunners_Q1.stages.1.choices.0.label>
  <TMM_Story_gunners_Q1.stages.1.choices.0.resultText>Чистая работа. С вами приятно иметь дело — без лишних соплей. Передам Уэсу, что вы держите слово.</TMM_Story_gunners_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_gunners_Q2 -->
  <TMM_Story_gunners_Q2.label>долг квинси</TMM_Story_gunners_Q2.label>
  <TMM_Story_gunners_Q2.letterLabel>Долг Квинси</TMM_Story_gunners_Q2.letterLabel>
  <TMM_Story_gunners_Q2.letterText>Упёртые недобитки из Минитменов так и не поняли, что проиграли — они собираются на ночной штурм моего аванпоста в Квинси. Помоги удержать стены и перебить их, и я открою для твоих торговцев тяжёлый арсенал Стрелков.</TMM_Story_gunners_Q2.letterText>
  <TMM_Story_gunners_Q2.stages.0.label>Ночь опускается на Квинси</TMM_Story_gunners_Q2.stages.0.label>
  <TMM_Story_gunners_Q2.stages.0.text>На обгоревшей стене церкви в Квинси всё ещё видны старые рисунки ополченцев и сломанный лазерный мушкет — память о предательстве Клинта. Решите, на чьей стороне вы примете ночной бой.</TMM_Story_gunners_Q2.stages.0.text>
  <TMM_Story_gunners_Q2.stages.0.choices.0.label>Отбить атаку и защитить Клинта (за Стрелков)</TMM_Story_gunners_Q2.stages.0.choices.0.label>
  <TMM_Story_gunners_Q2.stages.0.choices.0.resultText>Вот так! Пусть знают, кто теперь хозяин в Квинси. Держи свою долю и чертёж наших касок.</TMM_Story_gunners_Q2.stages.0.choices.0.resultText>
  <TMM_Story_gunners_Q2.stages.0.choices.1.label>Встать на сторону ополченцев и разгромить отряд Клинта (за Минитменов)</TMM_Story_gunners_Q2.stages.0.choices.1.label>
  <TMM_Story_gunners_Q2.stages.0.choices.1.resultText>Предательская крыса! Я сдеру с тебя шкуру!</TMM_Story_gunners_Q2.stages.0.choices.1.resultText>
  <TMM_Story_gunners_Q2.stages.1.label>Удержать Квинси от штурма Минитменов</TMM_Story_gunners_Q2.stages.1.label>
  <TMM_Story_gunners_Q2.stages.1.text>Обороняйте аванпост Клинта в Квинси 5 часов против яростного ночного штурма мстителей из ополчения.</TMM_Story_gunners_Q2.stages.1.text>
  <TMM_Story_gunners_Q2.stages.2.label>Разгромить гарнизон Клинта в Квинси</TMM_Story_gunners_Q2.stages.2.label>
  <TMM_Story_gunners_Q2.stages.2.text>Выбейте наёмников Клинта из руин Квинси и верните город Минитменам.</TMM_Story_gunners_Q2.stages.2.text>
  <TMM_Story_gunners_Q2.stages.3.label>Рассвет над Квинси</TMM_Story_gunners_Q2.stages.3.label>
  <TMM_Story_gunners_Q2.stages.3.text>Стрельба на разрушенной церковной площади Квинси стихает. Весть о вашем выборе разносится по всем лагерям наёмников и постам ополчения.</TMM_Story_gunners_Q2.stages.3.text>

  <!-- TMM_Story_gunners_Q3 -->
  <TMM_Story_gunners_Q3.label>испытание брони</TMM_Story_gunners_Q3.label>
  <TMM_Story_gunners_Q3.letterLabel>Испытание брони</TMM_Story_gunners_Q3.letterLabel>
  <TMM_Story_gunners_Q3.letterText>Эй, здоровяк! Я тут наварила новые бронепластины на свою силовую броню и ищу ребят с характером, чтобы проверить их в драке. Приводи лучших рукопашников на арену моей автосвалки — кто первый упадёт, тот ставит выпивку!</TMM_Story_gunners_Q3.letterText>
  <TMM_Story_gunners_Q3.stages.0.label>Поединок на арене автосвалки Тессы</TMM_Story_gunners_Q3.stages.0.label>
  <TMM_Story_gunners_Q3.stages.0.text>Выйдите на песчаную арену, где зрители-наёмники ставят крышки и бросают на песок гаечные ключи, и испытайте броню бойцов Тессы в честной драке.</TMM_Story_gunners_Q3.stages.0.text>
  <TMM_Story_gunners_Q3.stages.1.label>Уважение, выкованное в железе</TMM_Story_gunners_Q3.stages.1.label>
  <TMM_Story_gunners_Q3.stages.1.text>С дымящимися вмятинами на пластинах Тесса громко хохочет и протягивает тяжёлую латную перчатку для рукопожатия.</TMM_Story_gunners_Q3.stages.1.text>
  <TMM_Story_gunners_Q3.stages.1.choices.0.label>Одолеть Тессу на арене и пожать ей руку</TMM_Story_gunners_Q3.stages.1.choices.0.label>
  <TMM_Story_gunners_Q3.stages.1.choices.0.resultText>Ха-ха! Вот это был удар! Броня выдержала, а ты дрался как зверь. Уважаю!</TMM_Story_gunners_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_gunners_Q4 -->
  <TMM_Story_gunners_Q4.label>двойной контракт</TMM_Story_gunners_Q4.label>
  <TMM_Story_gunners_Q4.letterLabel>Двойной контракт</TMM_Story_gunners_Q4.letterLabel>
  <TMM_Story_gunners_Q4.letterText>Вы заслужили место за большим столом. Анонимный заказчик — платящий довоенным золотом и облигациями Волт-Тек — хочет убрать высокопоставленного связного Британских Просвещённых во время тайной встречи. Сработайте чисто, и я отдам вам свой модифицированный карабин — «Страж Смотрителя».</TMM_Story_gunners_Q4.letterText>
  <TMM_Story_gunners_Q4.stages.0.label>Проникнуть на тайную встречу в метро</TMM_Story_gunners_Q4.stages.0.label>
  <TMM_Story_gunners_Q4.stages.0.text>Пробейтесь сквозь охрану Британских Просвещённых на заброшенной станции метро и загоните связного в угол.</TMM_Story_gunners_Q4.stages.0.text>
  <TMM_Story_gunners_Q4.stages.1.label>Портфель на платформе</TMM_Story_gunners_Q4.stages.1.label>
  <TMM_Story_gunners_Q4.stages.1.text>Прижатый к ржавым рельсам связной открывает портфель — внутри лежит чек от Дика Романа на закупку Стрелками клеток для синтов — и предлагает 2000 серебра за инсценировку смерти и передачу компромата.</TMM_Story_gunners_Q4.stages.1.text>
  <TMM_Story_gunners_Q4.stages.1.choices.0.label>Выполнить контракт капитана Уэса и устранить цель (за Стрелков)</TMM_Story_gunners_Q4.stages.1.choices.0.label>
  <TMM_Story_gunners_Q4.stages.1.choices.0.resultText>Никаких вопросов, никаких свидетелей. Вот это профессионализм. Карабин твой, партнёр.</TMM_Story_gunners_Q4.stages.1.choices.0.resultText>
  <TMM_Story_gunners_Q4.stages.1.choices.1.label>Инсценировать смерть связного, забрать компромат Консорциума и переиграть Уэса (за правду)</TMM_Story_gunners_Q4.stages.1.choices.1.label>
  <TMM_Story_gunners_Q4.stages.1.choices.1.resultText>Ты кинул Стрелков ради этих британских снобов?! Отныне за твою голову назначена награда в каждом лагере!</TMM_Story_gunners_Q4.stages.1.choices.1.resultText>

  <!-- TMM_Story_gunners_Q5 -->
  <TMM_Story_gunners_Q5.label>новый наниматель</TMM_Story_gunners_Q5.label>
  <TMM_Story_gunners_Q5.letterLabel>Новый наниматель</TMM_Story_gunners_Q5.letterLabel>
  <TMM_Story_gunners_Q5.letterText>Уэс берёт самоубийственные контракты у пиджаков из Волт-Тек и бросает наши отряды в мясорубку против ангелов и мутантов. К чёрту это. Ты честно побил меня на арене и платишь вовремя. Прикрой мой отход с эстакады Квинси, и моя броня твоя!</TMM_Story_gunners_Q5.letterText>
  <TMM_Story_gunners_Q5.stages.0.label>Прикрыть прорыв Тессы с эстакады Квинси</TMM_Story_gunners_Q5.stages.0.label>
  <TMM_Story_gunners_Q5.stages.0.text>Разбейте карательное оцепление капитана Бриджит на эстакаде Квинси, чтобы Тесса вырвалась в своей модифицированной силовой броне.</TMM_Story_gunners_Q5.stages.0.text>
  <TMM_Story_gunners_Q5.stages.1.label>Тяжёлая броня на службе</TMM_Story_gunners_Q5.stages.1.label>
  <TMM_Story_gunners_Q5.stages.1.text>Переступив порог {COLONY} в своей побитой силовой броне, Тесса тут же ищет глазами самого крепкого рукопашника колонии для спарринга.</TMM_Story_gunners_Q5.stages.1.text>
  <TMM_Story_gunners_Q5.stages.1.choices.0.label>Нанять Тессу в постоянный гарнизон колонии</TMM_Story_gunners_Q5.stages.1.choices.0.label>
  <TMM_Story_gunners_Q5.stages.1.choices.0.resultText>Договорились! Только выдели мне нормальный верстак — надо подлатать наплечник после этой заварушки.</TMM_Story_gunners_Q5.stages.1.choices.0.resultText>

</LanguageData>
```

`Defs/StoryDefs/Story_railroad.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- RAILROAD Q1: MERCER SAFEHOUSE                                         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_railroad_Q1</defName>
    <label>mercer safehouse</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_deacon</giver>
    <fallbackGiver>TMM_Hero_desdemona</fallbackGiver>
    <requires>
      <li>act>=2</li>
    </requires>
    <letterLabel>Mercer Safehouse</letterLabel>
    <letterText>Hey, pal—don't look behind you, I'm the guy in the terrible disguise three bushes over. Look, your waystation has great vibes and terrible security, which makes it the perfect blind spot for three runaway synths we need to stash for forty-eight hours. Keep the Gunners and Institute trackers off their scent, okay?</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <key>shelter_synths</key>
        <label>Shelter three runaway synths in the waystation</label>
        <text>Hide Deacon—who swaps disguises three times between drifter and caravan guard—and three runaway synths in {COLONY}'s hotel for 2 days while keeping Gunner hunters away.</text>
        <faction>TMM_RailroadFaction</faction>
        <hero>TMM_Hero_deacon</hero>
        <groupSize>3</groupSize>
        <stayDays>2</stayDays>
        <objective>Hide</objective>
        <hunterFaction>TMM_GunnersFaction</hunterFaction>
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
        <key>safehouse_cleared</key>
        <label>Clean extraction</label>
        <text>The Gunner patrol moves on empty-handed as all three runaway synths slip safely out the back road toward the coast.</text>
        <choices>
          <li>
            <outcome>sheltered</outcome>
            <label>Accept Deacon's payment and conspiracist disguise blueprints</label>
            <resultText>Smooth work, boss! Desdemona already knows—expect word from us soon, along with a few fun toys.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.rr_q1</flag>
                <value>sheltered</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>400</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.stealth_boy</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.conspiracist_hat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- RAILROAD Q2: STATIC ON THE LINE (UNLOCKS BALLISTIC WEAVE)             -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_railroad_Q2</defName>
    <label>static on the line</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_tinker_tom</giver>
    <fallbackGiver>TMM_Hero_deacon</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_railroad_Q1</li>
    </requires>
    <letterLabel>Static on the Line</letterLabel>
    <letterText>Yo, listen close! The Institute and those creepy sky-guys are bouncing quantum tracking pulses off the ionosphere! I rigged a ballistic-encrypted MILA relay that'll scramble their frequencies around your sector—plant it on the highest roof and blast any Coursers that drop in!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Deliver">
        <key>mila_parts</key>
        <label>Assemble Tinker Tom's MILA scrambler relay</label>
        <text>Allocate 4 industrial components from {COLONY}'s stockpiles to mount Tinker Tom's ballistic-encrypted MILA relay on your roof.</text>
        <thing>ComponentIndustrial</thing>
        <count>4</count>
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
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <key>courser_jam_raid</key>
        <label>Defend the MILA relay against Institute hunters</label>
        <text>The MILA relay sparks to life—instantly drawing an Institute Courser strike team teleporting straight at {COLONY}!</text>
        <faction>TMM_InstituteFaction</faction>
        <pointsFactor>1.15</pointsFactor>
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
        <key>ballistic_weave_unlock</key>
        <label>Clear frequencies &amp; ballistic weave</label>
        <text>With the Courser hunters neutralized, Tinker Tom's MILA relay hums steadily overhead—he insists it also blocks "Vault-Tec orbital brain beams," and looking at the telemetry, he's weirdly not wrong.</text>
        <choices>
          <li>
            <outcome>active</outcome>
            <label>Accept Tinker Tom's Railroad ballistic weave schematics</label>
            <resultText>Boom! Frequencies clean as glass! Take our ballistic weave patterns—bullets bounce off like peas!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.rr_q2</flag>
                <value>active</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.railroad_ballistic</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.railroad_armored_coat</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.stealth_boy</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- RAILROAD Q3: HEAVY BREAKTHROUGH                                       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_railroad_Q3</defName>
    <label>heavy breakthrough</label>
    <act>2</act>
    <main>false</main>
    <priority>60</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_glory</giver>
    <fallbackGiver>TMM_Hero_desdemona</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_railroad_Q2</li>
    </requires>
    <letterLabel>Heavy Breakthrough</letterLabel>
    <letterText>No time for stealth, friend. Retention Bureau heavies intercepted a Railroad runner carrying three third-generation synths toward the coast. I'm taking my minigun to hit their armored convoy at the tunnel mouth—bring your best shooters and help me crack their line open!</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>tunnel_convoy</key>
        <label>Crack open the Retention Bureau convoy at the tunnel</label>
        <text>Fight alongside Glory at the highway tunnel mouth to wipe out the Institute escort and free three captured third-generation synths.</text>
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
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
        <key>chosen_name</key>
        <label>Freedom at the tunnel mouth</label>
        <text>Glory's minigun barrels glow orange in the tunnel smoke as the three freed synths emerge—one quietly asking to keep an old soldier's dog tag as a reminder that he chose his own name.</text>
        <choices>
          <li>
            <outcome>rescued</outcome>
            <label>Escort the freed synths clear with Glory</label>
            <resultText>We pulled every single one of them out. You fought like one of ours. The Railroad won't forget it.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.rr_q3</flag>
                <value>rescued</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-35</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>750</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentSpacer</thing>
                <count>1</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.subdermal_armor</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- RAILROAD Q4 (FACTION POINT): MEMORY WIPE                              -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_railroad_Q4</defName>
    <label>memory wipe</label>
    <act>2</act>
    <main>false</main>
    <priority>65</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_desdemona</giver>
    <fallbackGiver>TMM_Hero_deacon</fallbackGiver>
    <requires>
      <li>act>=2</li>
      <li>done:TMM_Story_railroad_Q3</li>
    </requires>
    <letterLabel>Memory Wipe</letterLabel>
    <letterText>We have a crisis that cuts to the very soul of what we fight for. A synth carrying a bright, newly awakened 'Spark' reached our safehouse—but Tinker Tom found an active quantum tracker woven directly into his neural matrix. We can wipe his memories and extinguish his Spark to hide him forever, or leave him intact and risk an all-out Institute and angel assault. Tell me what is right.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>memory_lounger</key>
        <label>Names carved beside the Memory Lounger</label>
        <text>In the Railroad catacombs, the wall beside the Memory Lounger is covered with the carved names of synths who risked death to keep their souls. The trembling synth looks straight at you and begs you not to erase who he is.</text>
        <choices>
          <li>
            <outcome>pro_spark</outcome>
            <label>Preserve his soul and Spark, and stand ready to defend the catacombs (Spark &amp; Freedom)</label>
            <resultText>You didn't just defend a machine—you stood up for a living soul. The Railroad is with you to the last round. Take the Heavy Railway Rifle!</resultText>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.railroad</flag>
                <value>pro_spark</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>60</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_InstituteFaction</faction>
                <amount>-50</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_railway_rifle</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.desdemona_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>pro_faction</outcome>
            <label>Run a complete memory wipe to extinguish the tracker and keep HQ hidden (Network Survival)</label>
            <resultText>He is safe... yet staring into his empty eyes, I wonder what price we just paid. Take our rifle—use it wisely.</resultText>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>choice.railroad</flag>
                <value>pro_faction</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_RailroadFaction</faction>
                <amount>40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.legendary_railway_rifle</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.desdemona_coat</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <key>catacomb_defense</key>
        <label>Defend the Railroad catacombs from Institute Coursers</label>
        <text>The active quantum tracker brought Institute Coursers straight down into the catacombs! Hold the safehouse for 5 hours until Tinker Tom burns out the signal!</text>
        <enemyFaction>TMM_InstituteFaction</enemyFaction>
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
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>catacomb_silence</key>
        <label>Quiet returns to the catacombs</label>
        <text>Whether defended in blood or hidden behind an erased mind, the Railroad safehouse endures—and Desdemona honors her pledge with the Heavy Railway Rifle schematics.</text>
        <days>0.2</days>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_AddInt">
        <flag>arcPoints</flag>
        <amount>1</amount>
      </li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>arc.railroad</flag>
        <value>4</value>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- RAILROAD Q5 (DEFECTOR QUEST): FAKED DEATH                             -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_railroad_Q5</defName>
    <label>faked death</label>
    <act>3</act>
    <main>false</main>
    <priority>75</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_deacon</giver>
    <fallbackGiver>TMM_Hero_tinker_tom</fallbackGiver>
    <requires>
      <li>act>=3</li>
      <li>done:TMM_Story_railroad_Q4</li>
    </requires>
    <letterLabel>Faked Death</letterLabel>
    <letterText>Look, things are getting real ugly out there, and Desdemona is going full scorched-earth. Somebody needs to stay inside your camp and keep a cool head so the surviving synths don't get wiped off the map. Help me stage a convincing 'ambush' at the old bridge so the Railroad thinks I'm dead, and I'm all yours.</letterText>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <key>bridge_staging</key>
        <label>Staging the ambush at the old bridge</label>
        <text>Under cover of fog, your scouts fire blanks into the bridge rails, leaving behind Deacon's cracked dark shades and a scorched dog tag where Railroad lookouts will be sure to spot them.</text>
        <days>0.8</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <key>welcome_deacon</key>
        <label>Rest in peace, old Deacon</label>
        <text>An hour later, a cheerful stranger with a brand-new haircut strolls through {COLONY}'s gate carrying a spare pair of sunglasses—ready to serve under a fresh alias regardless of how hostile the Railroad becomes.</text>
        <choices>
          <li>
            <outcome>recruit_deacon</outcome>
            <label>Finish the ruse and welcome Deacon into {COLONY}</label>
            <resultText>Rest in peace, old Deacon. Say hello to the new guy in your colony! Oh, and here's the pattern for my shades.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_HeroJoins">
                <hero>TMM_Hero_deacon</hero>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>defector.TMM_Hero_deacon</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>unlock.deacon_shades</flag>
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

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_railroad.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_railroad_Q1 -->
  <TMM_Story_railroad_Q1.label>безопасный дом</TMM_Story_railroad_Q1.label>
  <TMM_Story_railroad_Q1.letterLabel>Безопасный дом</TMM_Story_railroad_Q1.letterLabel>
  <TMM_Story_railroad_Q1.letterText>Эй, приятель — не оборачивайся, я тот парень в дурацкой маскировке в трёх кустах от ворот. Слушай, у твоей гостиницы отличная атмосфера и никакая охрана, что делает её идеальной слепой зоной для трёх беглых синтов на сорок восемь часов. Отведи от них Стрелков и ищеек Института, лады?</TMM_Story_railroad_Q1.letterText>
  <TMM_Story_railroad_Q1.stages.0.label>Укрыть трёх беглых синтов в гостинице</TMM_Story_railroad_Q1.stages.0.label>
  <TMM_Story_railroad_Q1.stages.0.text>Разместите в гостинице {COLONY} на 2 дня трёх беглых синтов и Дьякона — который трижды успеет сменить маскировку от бродяги до охранника — и не подпустите патруль Стрелков.</TMM_Story_railroad_Q1.stages.0.text>
  <TMM_Story_railroad_Q1.stages.1.label>Чистый вывод</TMM_Story_railroad_Q1.stages.1.label>
  <TMM_Story_railroad_Q1.stages.1.text>Патруль Стрелков уходит ни с чем, а все трое беглых синтов благополучно уходят просёлочной дорогой к побережью.</TMM_Story_railroad_Q1.stages.1.text>
  <TMM_Story_railroad_Q1.stages.1.choices.0.label>Укрыть синтов и отвести подозрения патруля</TMM_Story_railroad_Q1.stages.1.choices.0.label>
  <TMM_Story_railroad_Q1.stages.1.choices.0.resultText>Гладкая работа, босс! Дездемона уже в курсе — жди от нас вестей и пару крутых игрушек.</TMM_Story_railroad_Q1.stages.1.choices.0.resultText>

  <!-- TMM_Story_railroad_Q2 -->
  <TMM_Story_railroad_Q2.label>помехи в эфире</TMM_Story_railroad_Q2.label>
  <TMM_Story_railroad_Q2.letterLabel>Помехи в эфире</TMM_Story_railroad_Q2.letterLabel>
  <TMM_Story_railroad_Q2.letterText>Йо, слушай внимательно! Институт и эти жуткие парни с небес гоняют квантовые пеленги через ионосферу! Я собрал ретранслятор МИЛА с баллистическим шифрованием, который заглушит их частоты вокруг твоего сектора — ставь его на самую высокую крышу и гаси любых охотников, что телепортируются!</TMM_Story_railroad_Q2.letterText>
  <TMM_Story_railroad_Q2.stages.0.label>Собрать ретранслятор МИЛА Техника Тома</TMM_Story_railroad_Q2.stages.0.label>
  <TMM_Story_railroad_Q2.stages.0.text>Выделите 4 компонента со складов {COLONY}, чтобы смонтировать ретранслятор МИЛА с баллистическим шифрованием.</TMM_Story_railroad_Q2.stages.0.text>
  <TMM_Story_railroad_Q2.stages.1.label>Отразить засаду охотников Института</TMM_Story_railroad_Q2.stages.1.label>
  <TMM_Story_railroad_Q2.stages.1.text>Ретранслятор МИЛА оживает в снопе искр — мгновенно притягивая телепортационный рейд охотников Института на {COLONY}!</TMM_Story_railroad_Q2.stages.1.text>
  <TMM_Story_railroad_Q2.stages.2.label>Чистые частоты и баллистическое волокно</TMM_Story_railroad_Q2.stages.2.label>
  <TMM_Story_railroad_Q2.stages.2.text>Охотники разбиты, а ретранслятор МИЛА ровно гудит над крышей — Техник Том уверяет, что он заодно защищает мозги от «орбитальных лучей Волт-Тек», и частично оказывается прав.</TMM_Story_railroad_Q2.stages.2.text>
  <TMM_Story_railroad_Q2.stages.2.choices.0.label>Запустить ретранслятор и получить выкройки баллистического волокна</TMM_Story_railroad_Q2.stages.2.choices.0.label>
  <TMM_Story_railroad_Q2.stages.2.choices.0.resultText>Бум! Частоты чисты как слеза! В знак благодарности держи выкройки нашего баллистического волокна — пули отскакивают как горох!</TMM_Story_railroad_Q2.stages.2.choices.0.resultText>

  <!-- TMM_Story_railroad_Q3 -->
  <TMM_Story_railroad_Q3.label>тяжёлый прорыв</TMM_Story_railroad_Q3.label>
  <TMM_Story_railroad_Q3.letterLabel>Тяжёлый прорыв</TMM_Story_railroad_Q3.letterLabel>
  <TMM_Story_railroad_Q3.letterText>Времени на скрытность нет, друг. Тяжёлые отряды Бюро возврата перехватили нашего проводника с тремя синтами третьего поколения у побережья. Я беру свой миниган и ударю по их бронеконвою у въезда в туннель — бери лучших стрелков и помоги проломить их строй!</TMM_Story_railroad_Q3.letterText>
  <TMM_Story_railroad_Q3.stages.0.label>Проломить бронеконвой Бюро возврата у туннеля</TMM_Story_railroad_Q3.stages.0.label>
  <TMM_Story_railroad_Q3.stages.0.text>Разбейте охрану Института у въезда в туннель вместе с Глорией и освободите трёх пленных синтов третьего поколения.</TMM_Story_railroad_Q3.stages.0.text>
  <TMM_Story_railroad_Q3.stages.1.label>Свобода у туннеля</TMM_Story_railroad_Q3.stages.1.label>
  <TMM_Story_railroad_Q3.stages.1.text>Стволы минигана Глории раскалены докрасна в дыму, когда выходят трое спасённых синтов — один из них просит оставить ему старый солдатский жетон как память о том, что он сам выбрал себе имя.</TMM_Story_railroad_Q3.stages.1.text>
  <TMM_Story_railroad_Q3.stages.1.choices.0.label>Прорвать оцепление и вывести всех синтов с Глорией</TMM_Story_railroad_Q3.stages.1.choices.0.label>
  <TMM_Story_railroad_Q3.stages.1.choices.0.resultText>Мы вытащили их всех. Ты сражался как один из наших. Подземка этого не забудет.</TMM_Story_railroad_Q3.stages.1.choices.0.resultText>

  <!-- TMM_Story_railroad_Q4 -->
  <TMM_Story_railroad_Q4.label>стирание памяти</TMM_Story_railroad_Q4.label>
  <TMM_Story_railroad_Q4.letterLabel>Стирание памяти</TMM_Story_railroad_Q4.letterLabel>
  <TMM_Story_railroad_Q4.letterText>У нас кризис, затрагивающий саму суть того, за что мы сражаемся. Синт с яркой, только что пробудившейся «Искрой» добрался до нашего убежища — но Техник Том нашёл активный квантовый маячок, вплетённый прямо в его нейроматрицу. Мы можем стереть ему память и погасить Искру, чтобы спрятать его навсегда, или оставить личность нетронутой и рискнуть штурмом Института и ангелов. Скажи, как поступить правильно.</TMM_Story_railroad_Q4.letterText>
  <TMM_Story_railroad_Q4.stages.0.label>Имена у кресла стирания памяти</TMM_Story_railroad_Q4.stages.0.label>
  <TMM_Story_railroad_Q4.stages.0.text>В катакомбах Подземки стена перед креслом памяти исписана именами синтов, рискнувших жизнью ради сохранения своей души. Дрожащий синт смотрит прямо на вас и умоляет не стирать его личность.</TMM_Story_railroad_Q4.stages.0.text>
  <TMM_Story_railroad_Q4.stages.0.choices.0.label>Сохранить личность и Искру синта и отбить штурм катакомб (за Искру и свободу)</TMM_Story_railroad_Q4.stages.0.choices.0.label>
  <TMM_Story_railroad_Q4.stages.0.choices.0.resultText>Ты защитил не просто машину — ты отстоял живую душу. Подземка с тобой до последнего патрона. Возьми «Железнодорожную винтовку»!</TMM_Story_railroad_Q4.stages.0.choices.0.resultText>
  <TMM_Story_railroad_Q4.stages.0.choices.1.label>Провести полное стирание памяти и погасить Искру ради безопасности сети (за выживание Подземки)</TMM_Story_railroad_Q4.stages.0.choices.1.label>
  <TMM_Story_railroad_Q4.stages.0.choices.1.resultText>Он в безопасности... но глядя в его пустые глаза, я спрашиваю себя, какую цену мы заплатили. Вот наше оружие — используй его с умом.</TMM_Story_railroad_Q4.stages.0.choices.1.resultText>
  <TMM_Story_railroad_Q4.stages.1.label>Оборонять катакомбы Подземки от охотников Института</TMM_Story_railroad_Q4.stages.1.label>
  <TMM_Story_railroad_Q4.stages.1.text>Активный квантовый маячок привёл охотников Института прямо в катакомбы! Удерживайте убежище 5 часов, пока Техник Том выжигает сигнал!</TMM_Story_railroad_Q4.stages.1.text>
  <TMM_Story_railroad_Q4.stages.2.label>Тишина возвращается в катакомбы</TMM_Story_railroad_Q4.stages.2.label>
  <TMM_Story_railroad_Q4.stages.2.text>Отстоянный в бою или укрытый за стёртой памятью, штаб Подземки уцелел — и Дездемона держит слово, передавая чертежи «Железнодорожной винтовки».</TMM_Story_railroad_Q4.stages.2.text>

  <!-- TMM_Story_railroad_Q5 -->
  <TMM_Story_railroad_Q5.label>инсценировка смерти</TMM_Story_railroad_Q5.label>
  <TMM_Story_railroad_Q5.letterLabel>Инсценировка смерти</TMM_Story_railroad_Q5.letterLabel>
  <TMM_Story_railroad_Q5.letterText>Слушай, дела снаружи принимают совсем скверный оборот, и Дездемона уходит в глухое подполье. Кто-то должен остаться в твоём лагере с холодной головой, чтобы уцелевших синтов не стёрли с карты. Помоги мне подстроить убедительную «засаду» у старого моста, чтобы Подземка сочла меня погибшим — и я весь твой.</TMM_Story_railroad_Q5.letterText>
  <TMM_Story_railroad_Q5.stages.0.label>Инсценировка засады у старого моста</TMM_Story_railroad_Q5.stages.0.label>
  <TMM_Story_railroad_Q5.stages.0.text>Под покровом тумана ваши разведчики дают холостые очереди по перилам моста, оставляя треснувшие тёмные очки Дьякона и оплавленный жетон на видном месте для наблюдателей Подземки.</TMM_Story_railroad_Q5.stages.0.text>
  <TMM_Story_railroad_Q5.stages.1.label>Покойся с миром, старый Дьякон</TMM_Story_railroad_Q5.stages.1.label>
  <TMM_Story_railroad_Q5.stages.1.text>Спустя час в ворота {COLONY} заходит жизнерадостный незнакомец со свежей стрижкой и запасными тёмными очками — готовый служить под новым именем даже вопреки вражде с Подземкой.</TMM_Story_railroad_Q5.stages.1.text>
  <TMM_Story_railroad_Q5.stages.1.choices.0.label>Завершить инсценировку и принять Дьякона в колонию под новым именем</TMM_Story_railroad_Q5.stages.1.choices.0.label>
  <TMM_Story_railroad_Q5.stages.1.choices.0.resultText>Покойся с миром, старый Дьякон. Привет, новый парень в вашей колонии! Кстати, вот чертёж моих очков.</TMM_Story_railroad_Q5.stages.1.choices.0.resultText>

</LanguageData>
```