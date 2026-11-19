`Defs/StoryDefs/Story_Act4_Main.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- ===================================================================== -->
  <!-- ACT 4, STEP 1: THE BLOOD ECLIPSE (OPENER OF ACT 4)                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_BloodEclipse</defName>
    <label>the blood eclipse</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>98</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_preston_garvey</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A3_BuildSparkResonator</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>A crimson sky over the wastes</label>
        <text>Beneath the blood-red sun of the Eclipse, the Rift above the continent groans like tearing steel. Gravity fluctuates around the Spark Resonator, compass needles spin wildly, and every faction leader in the wasteland is marching toward {COLONY} for the final summit before the machine is engaged.</text>
        <days>0.8</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Eve of the final summit</label>
        <text>The Spark Resonator hums at critical harmonic pitch while armed delegations approach your perimeter. Whether backed by your recruited defectors or your colony's own veteran engineers, your Borax countermeasures and perimeter wards stand ready. How do you prepare the waystation for the leaders' arrival?</text>
        <choices>
          <li>
            <outcome>steady_the_synth</outcome>
            <label>Stand beside the Zero Synth at the console and reinforce the Enochian wards</label>
            <resultText>Blue light steadies inside the Resonator's crystal as the Zero Synth breathes evenly beside you. Outside the gates, the first motorcades and vertibirds descend through the crimson haze.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_eclipsePrep</flag>
                <value>warded</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>prime_defenses</outcome>
            <label>Prime the Borax sprayers, load heavy turrets, and put the garrison on hair-trigger alert</label>
            <resultText>Heavy bolts clack home across your battlements. Every faction leader entering {COLONY} will see immediately that you intend to dictate terms from strength.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Steel</thing>
                <count>80</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_eclipsePrep</flag>
                <value>armed</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ACT 4, STEP 1B: LAST NEGOTIATIONS (BRANCHED BY ponr1)                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Summit_Protect</defName>
    <label>summit beneath the blood sun: defenders</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_preston_garvey</giver>
    <fallbackGiver>TMM_Hero_desdemona</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_BloodEclipse</li>
      <li>ponr1=protect</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_MinutemenHuntersFaction</faction>
        <hero>TMM_Hero_preston_garvey</hero>
        <groupSize>4</groupSize>
        <stayDays>2</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_MinutemenHuntersFaction</faction>
                <amount>10</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>The defenders' council</label>
        <text>Preston Garvey, Desdemona, and the Winchester brothers gather in your waystation hall alongside wary observers from the Brotherhood and Vault-Tec. Preston sets his hat on the table: "We came because you swore to protect the living. Name the alliance that will stand at that console when the Rift tears wide open."</text>
        <choices>
          <li>
            <outcome>welcome_freedom_pact</outcome>
            <label>Pledge to unite the Minutemen, Hunters, and Railroad around the Spark</label>
            <resultText>Desdemona and Preston exchange a firm nod. The coalition of the Free Wasteland stands ready at your command.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>keep_counsel_open</outcome>
            <label>Hear out the Brotherhood and Vault-Tec observers before locking the console</label>
            <resultText>You listen to every final pitch in silence. Every faction leader takes their place around the Resonator chamber for the irreversible vote.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Summit_Purify</defName>
    <label>summit beneath the blood sun: iron &amp; ash</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_arthur_maxson</giver>
    <fallbackGiver>TMM_Hero_colonel_autumn</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_BloodEclipse</li>
      <li>ponr1=purify</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_BrotherhoodFaction</faction>
        <hero>TMM_Hero_arthur_maxson</hero>
        <groupSize>4</groupSize>
        <stayDays>2</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_BrotherhoodFaction</faction>
                <amount>10</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Warlords in the hall</label>
        <text>Arthur Maxson's power armor rattles the floorboards as he faces Colonel Autumn and Henry Casdin across your waystation table, while a lone Railroad courier watches from the shadows. Maxson slams his gauntlet down: "Humanity's hour of reckoning is here. Will you purge this world with steel—or with the Enclave's poison?"</text>
        <choices>
          <li>
            <outcome>endorse_purge</outcome>
            <label>Confirm that {COLONY} is ready to cleanse the wasteland once and for all</label>
            <resultText>Maxson and Autumn both nod curtly, each expecting you to side with their vision when the Resonator powers up.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>question_the_slaughter</outcome>
            <label>Demand guarantees that innocent settlers and the Zero Synth won't be butchered</label>
            <resultText>Maxson scowls coldly, while the hidden Railroad courier catches your eye—leaving the door open for a dramatic last-minute reversal.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Summit_Order</defName>
    <label>summit beneath the blood sun: architects</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_michael</giver>
    <fallbackGiver>TMM_Hero_dr_hess</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_BloodEclipse</li>
      <li>ponr1=order</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_HeavenFaction</faction>
        <hero>TMM_Hero_michael</hero>
        <groupSize>4</groupSize>
        <stayDays>2</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HeavenFaction</faction>
                <amount>10</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Heaven and the Architects</label>
        <text>Blinding celestial light dims the lanterns as Archangel Michael steps into your waystation beside Dr. Hess, while Father and Dr. Braun wait by the comms terminal. Michael speaks with absolute certainty: "Earth has bled in chaos for two centuries. Surrender the Sparks to Heaven's Host, and we shall impose eternal peace."</text>
        <choices>
          <li>
            <outcome>bow_to_order</outcome>
            <label>Acknowledge that only absolute higher authority can save this broken world</label>
            <resultText>Michael inclines his head in solemn approval as the delegations file toward the Spark Resonator.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>weigh_technocracy</outcome>
            <label>Compare Heaven's terms against the Institute and Vault-Tec proposals</label>
            <resultText>Dr. Braun smiles thinly from the corner while Michael's eyes narrow. The final decision awaits at the console.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Summit_Abyss</defName>
    <label>summit beneath the blood sun: lords of night</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>95</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_dick_roman</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_BloodEclipse</li>
      <li>ponr1=abyss</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HotelVisit">
        <faction>TMM_HellFaction</faction>
        <hero>TMM_Hero_crowley</hero>
        <groupSize>4</groupSize>
        <stayDays>2</stayDays>
        <objective>Host</objective>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>1</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Goodwill">
                <faction>TMM_HellFaction</faction>
                <amount>10</amount>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>1</nextStage>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Drinks at the edge of the Pit</label>
        <text>The air in the waystation reeks of brimstone and expensive cologne as Crowley swirls a glass of scotch across from Dick Roman and the Alpha Vampire. Crowley grins wickedly: "The angels are starving, the humans are terrified, and the Resonator is primed. Tell us, darling—are we cracking open Hell's cellar tonight, or cutting a corporate deal?"</text>
        <choices>
          <li>
            <outcome>toast_the_abyss</outcome>
            <label>Raise a glass to shattering the Cage and unleashing the Dark Covenant</label>
            <resultText>Crowley clinks his glass against yours with a triumphant laugh as the entourage moves toward the Resonator.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>keep_cards_close</outcome>
            <label>Remind everyone that {COLONY} controls the switch—and demand their best offers</label>
            <resultText>Dick Roman chuckles approvingly at your cutthroat instincts. Everything comes down to the console now.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_summitDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ACT 4, STEP 2: POINT OF NO RETURN #2 — THE RESONATOR CONSOLE          -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_ResonatorConsole</defName>
    <label>choice at the console</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>99</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_sam_winchester</giver>
    <fallbackGiver>TMM_Hero_castiel</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>story.a4_summitDone=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Totality of the Blood Eclipse</label>
        <text>The crimson sun locks directly over {COLONY}. Lightning arcs between the Spark Resonator's gold filaments and the storm clouds above. There can be no more stalling—the moment you engage a coalition protocol on the main console, the world's alliances will be forged in stone forever.</text>
        <days>0.5</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Point of No Return #2: Choose Your Final Coalition</label>
        <text>Every eye in the chamber is fixed on your hands above the Spark Resonator's main control levers. This choice is ABSOLUTE AND IRREVERSIBLE: your chosen coalition will become your permanent allies until the end of the war, while all opposing factions will turn into mortal enemies forever.\n\nWhich coalition will shape the fate of Earth?</text>
        <choices>
          <!-- COALITION 1: FREE WASTELAND (Minutemen & Hunters + Railroad) [Available under protect or purify] -->
          <li>
            <outcome>coalition_free_wasteland_protect</outcome>
            <label>[Free Wasteland] Ally with Minutemen &amp; Hunters + Railroad — seal the Rift and shield all Sparks (IRREVERSIBLE)</label>
            <resultText>You lock in the Harmonic Containment Protocol! Preston Garvey, the Winchesters, and Desdemona take their stations beside the Zero Synth—while Heaven, Hell, the Enclave, and the Institute declare total war on {COLONY}!</resultText>
            <requires>
              <li>ponr1=protect</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>free_wasteland</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>35</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>35</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>-80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-90</amount></li>
            </effects>
          </li>
          <li>
            <outcome>coalition_free_wasteland_pivot</outcome>
            <label>[Free Wasteland] Break from the purists and ally with Minutemen &amp; Hunters + Railroad (IRREVERSIBLE)</label>
            <resultText>Turning your back on the warlords at the final second, you engage the Harmonic Containment Protocol! Desdemona and Preston Garvey rally to defend the Zero Synth as the Enclave and Brotherhood storm out in fury!</resultText>
            <requires>
              <li>ponr1=purify</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>free_wasteland</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>35</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>35</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>-80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-90</amount></li>
            </effects>
          </li>

          <!-- COALITION 2: STERILE EMPIRE (Enclave + Institute) [Available under purify or order] -->
          <li>
            <outcome>coalition_sterile_empire_purify</outcome>
            <label>[Sterile Empire] Ally with Enclave + Institute — sterilize mutations and reclaim Earth (IRREVERSIBLE)</label>
            <resultText>You engage the Sterilization Matrix! Colonel Autumn and Father clasp hands over the console as plasma cohorts and synth strike teams mobilize to cleanse the wasteland!</resultText>
            <requires>
              <li>ponr1=purify</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>sterile_empire</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-85</amount></li>
            </effects>
          </li>
          <li>
            <outcome>coalition_sterile_empire_order</outcome>
            <label>[Sterile Empire] Ally with Enclave + Institute — impose human technocratic order (IRREVERSIBLE)</label>
            <resultText>Rejecting celestial overlords, you lock in the Sterilization Matrix with Colonel Autumn and Father! Synth legions and Hellfire squads rally to {COLONY} as angels and hunters declare war!</resultText>
            <requires>
              <li>ponr1=order</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>sterile_empire</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-85</amount></li>
            </effects>
          </li>

          <!-- COALITION 3: IRON CRUSADE (Brotherhood + Outcasts) [Available under purify or protect] -->
          <li>
            <outcome>coalition_iron_crusade_purify</outcome>
            <label>[Iron Crusade] Ally with Brotherhood + Outcasts — suppress all magic and rogue tech (IRREVERSIBLE)</label>
            <resultText>Arthur Maxson and Henry Casdin bury their schism at your console! The Resonator shifts into a continental dampener as the reunited steel order marches to purge anomalies, synths, and demons!</resultText>
            <requires>
              <li>ponr1=purify</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>iron_crusade</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-20</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>0</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>-10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-80</amount></li>
            </effects>
          </li>
          <li>
            <outcome>coalition_iron_crusade_protect</outcome>
            <label>[Iron Crusade] Ally with Brotherhood + Outcasts — shield humanity behind walls of steel (IRREVERSIBLE)</label>
            <resultText>Believing only armored discipline can hold back the Rift, you unite Arthur Maxson and Henry Casdin under the Iron Crusade! The Institute, Heaven, and Hell mobilize to crush {COLONY}!</resultText>
            <requires>
              <li>ponr1=protect</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>iron_crusade</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-15</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>0</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>-10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-80</amount></li>
            </effects>
          </li>

          <!-- COALITION 4: CELESTIAL ORDER (Heaven + British Men of Letters) [Available under order or abyss] -->
          <li>
            <outcome>coalition_celestial_order_order</outcome>
            <label>[Celestial Order] Ally with Heaven + British Men of Letters — rekindle grace and impose divine peace (IRREVERSIBLE)</label>
            <resultText>Blinding golden fire erupts from the Resonator! Archangel Michael and Dr. Hess assume command of the celestial relay network, while Hell, the Brotherhood, and the surface rebels vow to burn {COLONY} to ash!</resultText>
            <requires>
              <li>ponr1=order</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>celestial_order</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-30</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>0</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>-5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-90</amount></li>
            </effects>
          </li>
          <li>
            <outcome>coalition_celestial_order_penance</outcome>
            <label>[Celestial Order] Turn from the Pit at the last hour and ally with Heaven + Men of Letters (IRREVERSIBLE)</label>
            <resultText>Refusing to plunge Earth into sulfur and ash, you kneel before the Resonator and pledge the Sparks to Archangel Michael and Dr. Hess! Demons shriek in fury as Heaven's Host descends to guard your gates!</resultText>
            <requires>
              <li>ponr1=abyss</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>celestial_order</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>0</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>-5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-15</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-90</amount></li>
            </effects>
          </li>

          <!-- COALITION 5: DARK COVENANT (Hell + Monster Clans) [Available under abyss] -->
          <li>
            <outcome>coalition_dark_covenant</outcome>
            <label>[Dark Covenant] Ally with Hell + Monster Clans — feed the Sparks to the Pit and rule the twilight (IRREVERSIBLE)</label>
            <resultText>Crimson lightning shatters the chamber ceiling! Hell's lords and the nocturnal clans bind their blood pact to {COLONY}'s Resonator—plunging the wasteland into eternal twilight as Heaven and humanity march to destroy you!</resultText>
            <requires>
              <li>ponr1=abyss</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>dark_covenant</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-40</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-85</amount></li>
            </effects>
          </li>

          <!-- COALITION 6: SHADOW CONSORTIUM (Vault-Tec + Gunners) [Available under order, abyss, or protect] -->
          <li>
            <outcome>coalition_shadow_consortium_order</outcome>
            <label>[Shadow Consortium] Ally with Vault-Tec + Gunners — monopolize the Resonator and buy the wasteland (IRREVERSIBLE)</label>
            <resultText>Dr. Braun and Captain Wes sign the executive charter right on the Resonator housing! Mercenary battalions and Vault security deploy around {COLONY} as zealots and rebels prepare a massive assault!</resultText>
            <requires>
              <li>ponr1=order</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>shadow_consortium</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-80</amount></li>
            </effects>
          </li>
          <li>
            <outcome>coalition_shadow_consortium_pragmatic</outcome>
            <label>[Shadow Consortium] Cut a cold corporate pact with Vault-Tec + Gunners to hold the line (IRREVERSIBLE)</label>
            <resultText>Choosing hard contracts and mercenary firepower over idealism, you lock in the Shadow Consortium protocol with Dr. Braun and Captain Wes! Half the wasteland brands you a traitor—and prepares to march!</resultText>
            <anyOf>
              <li>ponr1=protect</li>
              <li>ponr1=abyss</li>
            </anyOf>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>ponr2</flag>
                <value>shadow_consortium</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-25</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_VaultTecFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GunnersFaction</faction><amount>80</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_InstituteFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MenOfLettersFaction</faction><amount>10</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MonsterClansFaction</faction><amount>5</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_MinutemenHuntersFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_RailroadFaction</faction><amount>-100</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_BrotherhoodFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_OutcastsFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_EnclaveFaction</faction><amount>-90</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HellFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_HeavenFaction</faction><amount>-95</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_GhoulsFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_FriendlyMutantsFaction</faction><amount>-85</amount></li>
              <li Class="TurboMegaMod.StoryEffect_Goodwill"><faction>TMM_WildMutantsFaction</faction><amount>-80</amount></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag">
        <flag>story.a4_ponr2Done</flag>
        <value>true</value>
      </li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>The Die Is Cast</label>
        <text>The Spark Resonator's harmonic beam has locked onto your coalition's frequency. Across the wasteland, coups erupt inside rival factions and rejected heroes draw their blades—the Night of the Long Knives has begun.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- NIGHT OF LONG KNIVES 1: COUPS & LUCIFER'S RELEASE                     -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_NightOfLongKnives_Coups</defName>
    <label>night of the long knives: coups</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>92</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_lucifer</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_ResonatorConsole</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Shockwaves across the factions</label>
        <text>Within hours of your choice at the Resonator console, encrypted emergency channels light up with news of bloody internal coups across the region's superpowers.</text>
        <days>1.0</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>New masters on the thrones</label>
        <text>In the Institute, hardline director Justin Ayo seizes absolute security control—ordering total eradication of all surface synths. Meanwhile, in the depths of Hell, the absence of Crowley's restraint triggers a catastrophic breach of Lucifer's Cage!</text>
        <choices>
          <li>
            <outcome>lucifer_unleashed</outcome>
            <label>[Crowley Defected] Brace for Lucifer's wrath as the Archangel of the Pit takes Hell's throne</label>
            <resultText>Crowley pours himself a stiff drink in your mess hall, pale beneath his smirk: "Well, darling... my former employees just let the boss out of the basement. Lucifer is running Hell now, and he's coming straight for us."</resultText>
            <requires>
              <li>defector.TMM_Hero_crowley=true</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.luciferReleased</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_instituteCoup</flag>
                <value>justin_ayo</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>hell_hardens</outcome>
            <label>[Crowley in Hell] Log the intelligence intercepts on Justin Ayo and Hell's war council</label>
            <resultText>Your comms officer records the new enemy command frequencies: Justin Ayo rules the Institute's synth legions, while Crowley mobilizes Hell's crossroads demons for total war.</resultText>
            <requires>
              <li>!defector.TMM_Hero_crowley=true</li>
              <li>!ponr2=dark_covenant</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_instituteCoup</flag>
                <value>justin_ayo</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>dark_covenant_ascendant</outcome>
            <label>[Dark Covenant] Coordinate with Hell's war council as Michael and Maxson unite against you</label>
            <resultText>With Hell allied to {COLONY}, infernal scouts report that Archangel Michael and Elder Maxson have formed an emergency crusade to destroy your Resonator.</resultText>
            <requires>
              <li>ponr2=dark_covenant</li>
            </requires>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_instituteCoup</flag>
                <value>justin_ayo</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- NIGHT OF LONG KNIVES 2A: RETALIATION BY TECH/MILITARY COMMANDER       -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Challenge_TechVanguard</defName>
    <label>vengeance in steel</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>90</priority>
    <faction>TMM_InstituteFaction</faction>
    <giver>TMM_Hero_x6_88</giver>
    <fallbackGiver>TMM_Hero_justin_ayo</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_NightOfLongKnives_Coups</li>
    </requires>
    <anyOf>
      <li>ponr2=free_wasteland</li>
      <li>ponr2=celestial_order</li>
      <li>ponr2=dark_covenant</li>
      <li>ponr2=shadow_consortium</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Teleportation signature detected</label>
        <text>Justin Ayo wastes no time testing your perimeter: Coursers and heavy synth shock troops are locking onto {COLONY}'s courtyard to eliminate your leadership before your new coalition can reinforce you.</text>
        <days>1.1</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_InstituteFaction</faction>
        <pointsFactor>1.35</pointsFactor>
        <delayHours>4</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentSpacer</thing>
                <count>2</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Courser vanguard shattered</label>
                <text>Your garrison cut down the Institute's elite vanguard in the courtyard, proving your defenses can withstand high-tech precision strikes.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>-10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Heavy damage from synth raiders</label>
                <text>The Courser strike team inflicted severe casualties before teleporting out, leaving your mechanics scrambling to repair the perimeter.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Salvaging the battlefield</label>
        <text>Smoking synth chassis and shattered pulse emitters litter your courtyard. Your technicians can strip the fallen Coursers for advanced components or broadcast their defeat across the Institute's relay band.</text>
        <choices>
          <li>
            <outcome>salvage_tech</outcome>
            <label>Strip the fallen Coursers for plasteel and high-grade components</label>
            <resultText>Your engineers harvest valuable pre-war alloys from the wreckage to reinforce your defenses.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Plasteel</thing>
                <count>45</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_vanguardBeaten</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>taunt_ayo</outcome>
            <label>Broadcast a warning to Justin Ayo over his own Courser transponder</label>
            <resultText>The Zero Synth watches the transmission go out with quiet pride—{COLONY} will not bow to the Institute.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_vanguardBeaten</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- NIGHT OF LONG KNIVES 2B: CHALLENGE FROM REBEL/HUNTER VETERANS         -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Challenge_RebelAmbush</defName>
    <label>the price of betrayal</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>90</priority>
    <faction>TMM_RailroadFaction</faction>
    <giver>TMM_Hero_glory</giver>
    <fallbackGiver>TMM_Hero_desdemona</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_NightOfLongKnives_Coups</li>
    </requires>
    <anyOf>
      <li>ponr2=sterile_empire</li>
      <li>ponr2=iron_crusade</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Ambush on the supply road</label>
        <text>Enraged by your alliance with the purists, Railroad heavy Glory and veteran militia fighters have seized a vital supply junction outside {COLONY}, challenging your commanders to face them on open ground.</text>
        <days>1.1</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Site">
        <enemyFaction>TMM_RailroadFaction</enemyFaction>
        <objective>KillHero</objective>
        <hero>TMM_Hero_glory</hero>
        <timeoutDays>5</timeoutDays>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Plasteel</thing>
                <count>50</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Junction cleared</label>
                <text>In a brutal firefight at the junction, Glory fell fighting to the last round—breaking the Railroad's blockade of your supply lines.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>timeout</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Blockade abandoned</label>
                <text>Before your main force arrived, Glory's squad rigged the junction with charges and melted back into the underground network.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <nextStage>2</nextStage>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Bloody stalemate</label>
                <text>Your strike team was forced to pull back under heavy minigun fire, though Allied reinforcements soon drove the rebels away from the road.</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Counting the cost</label>
        <text>Silence falls over the scorched highway junction. Even your hardened officers recognize the grim reality of the Night of the Long Knives: former friends are now mortal enemies.</text>
        <choices>
          <li>
            <outcome>press_forward</outcome>
            <label>Order your squad to secure the rebel weapons and march back to {COLONY}</label>
            <resultText>Your troops recover valuable heavy ammunition and return behind your walls to prepare for the final war.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>ComponentIndustrial</thing>
                <count>3</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_vanguardBeaten</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- NIGHT OF LONG KNIVES 3A: LEVIATHAN / OCCULT RECKONING                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Reckoning_AbyssalStrike</defName>
    <label>jaws in the blood mist</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>89</priority>
    <faction>TMM_MonsterClansFaction</faction>
    <giver>TMM_Hero_edgar</giver>
    <fallbackGiver>TMM_Hero_dick_roman</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_NightOfLongKnives_Coups</li>
    </requires>
    <anyOf>
      <li>ponr2=free_wasteland</li>
      <li>ponr2=sterile_empire</li>
      <li>ponr2=iron_crusade</li>
      <li>ponr2=celestial_order</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Black ichor at the gates</label>
        <text>Refusing to let {COLONY} ruin their centuries-old harvest, Leviathan enforcers and feral night-stalkers surge out of the crimson fog—testing whether your Borax defenses are truly ready for the final war.</text>
        <days>1.2</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_MonsterClansFaction</faction>
        <pointsFactor>1.35</pointsFactor>
        <delayHours>4</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Silver</thing>
                <count>500</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_reckoningSurvived</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Leviathan strike broken</label>
                <text>Hissing clouds of white Borax mist dissolved the Leviathans' invulnerability, allowing your defenders to cut them down before they reached the Resonator!</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_reckoningSurvived</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Brutal midnight clash</label>
                <text>The Leviathan pack tore through your outer barricades before withdrawing at dawn—make sure every squad carries Borax sprayers before the final siege!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- NIGHT OF LONG KNIVES 3B: CELESTIAL PUNITIVE STRIKE                    -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Reckoning_CelestialStrike</defName>
    <label>wrath of the host</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>89</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_zachariah</giver>
    <fallbackGiver>TMM_Hero_michael</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>done:TMM_Story_A4_NightOfLongKnives_Coups</li>
    </requires>
    <anyOf>
      <li>ponr2=dark_covenant</li>
      <li>ponr2=shadow_consortium</li>
    </anyOf>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Columns of searing light</label>
        <text>Enraged that {COLONY} aligned with the Pit or the Consortium Board, Zachariah leads an angelic strike force down through the crimson clouds to smite your Resonator chamber.</text>
        <days>1.2</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_ColonyRaid">
        <faction>TMM_HeavenFaction</faction>
        <pointsFactor>1.35</pointsFactor>
        <delayHours>4</delayHours>
        <outcomes>
          <li>
            <outcome>success</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>Gold</thing>
                <count>15</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_reckoningSurvived</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Angelic strike repelled</label>
                <text>Your Enochian wards and heavy weapons drove back Heaven's punitive squad, leaving scorched feathers and shattered celestial steel across the courtyard.</text>
              </li>
            </effects>
          </li>
          <li>
            <outcome>failed</outcome>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_reckoningSurvived</flag>
                <value>true</value>
              </li>
              <li Class="TurboMegaMod.StoryEffect_Letter">
                <label>Scorched battlements</label>
                <text>Heaven's strike force burned half your outer defenses before retreating—reinforce your Enochian wards before the main armies arrive!</text>
              </li>
            </effects>
          </li>
        </outcomes>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ACT 4 INTERLUDE: VIGIL BEFORE THE END                                 -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Interlude_FinalVigil</defName>
    <label>vigil beneath the crimson sun</label>
    <act>4</act>
    <main>false</main>
    <autoStart>true</autoStart>
    <priority>82</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_dean_winchester</giver>
    <fallbackGiver>TMM_Hero_castiel</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_Wait">
        <label>Quiet before the storm</label>
        <text>A heavy, unnatural calm settles over {COLONY} after the Night of the Long Knives. Colonists sharpen blades, check power-armor seals, and share quiet words around the dining tables while the Spark Resonator pulses steadily.</text>
        <days>1.0</days>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Last words before war</label>
        <text>You find the Zero Synth standing quietly beside the Resonator, gazing into the blue crystal. Whether bound to you by deep trust or wary necessity, they know the final act is about to begin.</text>
        <choices>
          <li>
            <outcome>inspire_colony</outcome>
            <label>Hold a quiet toast with your defenders and reaffirm your resolve</label>
            <resultText>Glasses clink in the dim light. Whatever tomorrow brings, {COLONY} will face it unbroken.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_AddInt">
                <flag>zeroSynth.trust</flag>
                <amount>10</amount>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_vigilDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
          <li>
            <outcome>check_arsenal</outcome>
            <label>Inspect the armory and distribute final emergency medical kits</label>
            <resultText>Every squad receives fresh trauma kits and ammunition. Your settlement is ready for total war.</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing">
                <thing>MedicineIndustrial</thing>
                <count>8</count>
              </li>
              <li Class="TurboMegaMod.StoryEffect_SetFlag">
                <flag>story.a4_vigilDone</flag>
                <value>true</value>
              </li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
  </TurboMegaMod.StoryQuestDef>

  <!-- ===================================================================== -->
  <!-- ACT 4 FINALE: PREPARING FOR THE FINAL BATTLE (6 COALITION VARIANTS)   -->
  <!-- ===================================================================== -->
  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Prep_FreeWasteland</defName>
    <label>banner of the free wasteland</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_MinutemenHuntersFaction</faction>
    <giver>TMM_Hero_preston_garvey</giver>
    <fallbackGiver>TMM_Hero_desdemona</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>ponr2=free_wasteland</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HeroJoins">
        <hero>TMM_Hero_preston_garvey</hero>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Allied vanguard arrives</label>
        <text>Supply drop-pods and ranger outriders from the Minutemen, Hunters, and Railroad arrive at {COLONY}, bringing heavy munitions and salt-forged wards. Preston Garvey salutes crisply: "The Free Wasteland stands with you, General. Our scouts have located the first Enochian beacon sites—say the word, and we launch our campaign!"</text>
        <choices>
          <li>
            <outcome>launch_campaign</outcome>
            <label>Accept the allied war chest and launch Act V: Armageddon of the Wastes</label>
            <resultText>Signals flash across every allied settlement. The final campaign for a free wasteland begins now!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>75</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentIndustrial</thing><count>6</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>4</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>5</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a4_readyForFinale</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act V: Armageddon of the Wastes</label>
        <text>Preston Garvey has joined your garrison alongside Free Wasteland war supplies! Your first coalition war quest—raising the Shield of Salt and Steel—is now unlocked.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Prep_SterileEmpire</defName>
    <label>directive of the sterile empire</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_EnclaveFaction</faction>
    <giver>TMM_Hero_colonel_autumn</giver>
    <fallbackGiver>TMM_Hero_father_shaun</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>ponr2=sterile_empire</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HeroJoins">
        <hero>TMM_Hero_captain_richards</hero>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Enclave &amp; Institute task force arrives</label>
        <text>A black Enclave vertibird touches down in a column of Institute teleportation lightning, delivering plasma ordnance and Captain Richards to reinforce your command. Richards snaps to attention: "Colonel Autumn and Father have synchronized the atmospheric dispersal grid. Give the order, and we begin cleansing the wasteland!"</text>
        <choices>
          <li>
            <outcome>launch_campaign</outcome>
            <label>Accept the imperial war chest and launch Act V: Armageddon of the Wastes</label>
            <resultText>Orbital relays lock onto resistance strongholds. The campaign to forge the Sterile Empire begins!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>90</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentSpacer</thing><count>3</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>20</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>5</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a4_readyForFinale</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act V: Armageddon of the Wastes</label>
        <text>Captain Richards has joined your garrison alongside Enclave and Institute war materiel! Your first coalition operation—Toxic Horizon—is now unlocked.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Prep_IronCrusade</defName>
    <label>vanguard of the iron crusade</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_BrotherhoodFaction</faction>
    <giver>TMM_Hero_arthur_maxson</giver>
    <fallbackGiver>TMM_Hero_henry_casdin</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>ponr2=iron_crusade</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HeroJoins">
        <hero>TMM_Hero_specialist_olin</hero>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Reunited steel corps arrives</label>
        <text>Heavy Brotherhood and Outcast supply pods slam into your landing zone, bringing uranium cores, plasteel armor plates, and master quartermaster Specialist Olin. Olin adjusts her welding goggles with a grim grin: "Maxson and Casdin finally stopped arguing long enough to ship the heavy iron. Point us at the Institute and the monsters!"</text>
        <choices>
          <li>
            <outcome>launch_campaign</outcome>
            <label>Accept the crusade armory and launch Act V: Armageddon of the Wastes</label>
            <resultText>Vertibird engines roar across the blood-red sky. Ad Victoriam—the Iron Crusade marches to war!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>85</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Uranium</thing><count>25</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentIndustrial</thing><count>6</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>5</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a4_readyForFinale</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act V: Armageddon of the Wastes</label>
        <text>Specialist Olin has joined your garrison alongside Brotherhood and Outcast war supplies! Your first coalition operation—Hammer and Anvil—is now unlocked.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Prep_CelestialOrder</defName>
    <label>heralds of the celestial order</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_HeavenFaction</faction>
    <giver>TMM_Hero_michael</giver>
    <fallbackGiver>TMM_Hero_dr_hess</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>ponr2=celestial_order</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HeroJoins">
        <hero>TMM_Hero_naomi</hero>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Heaven's liaison &amp; London's arsenal arrive</label>
        <text>In a flash of blinding white light, angelic intelligence officer Naomi appears beside supply cases stamped with the British Men of Letters crest. Naomi folds her hands calmly: "Archangel Michael and Dr. Hess have authorized full support for your garrison. Together, we shall harvest the Sparks and bring eternal order to Earth."</text>
        <choices>
          <li>
            <outcome>launch_campaign</outcome>
            <label>Accept the celestial reliquary and launch Act V: Armageddon of the Wastes</label>
            <resultText>Enochian beacons flare across the horizon. The campaign of the Celestial Order begins!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>30</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>70</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>MedicineUltratech</thing><count>5</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>5</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a4_readyForFinale</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act V: Armageddon of the Wastes</label>
        <text>Naomi has joined your garrison alongside Heaven and Men of Letters supplies! Your first coalition mission—Harvest of Sparks—is now unlocked.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Prep_DarkCovenant</defName>
    <label>legion of the dark covenant</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_HellFaction</faction>
    <giver>TMM_Hero_crowley</giver>
    <fallbackGiver>TMM_Hero_alpha_vampire</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>ponr2=dark_covenant</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HeroJoins">
        <hero>TMM_Hero_meg</hero>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Emissaries of the Pit &amp; Night arrive</label>
        <text>Black smoke coils across your courtyard as rebel-turned-enforcer Meg steps forward with blood packs, pre-war gold, and demonic war-chests from Hell and the Monster Clans. Meg smirks darkly: "Hope you're ready to get your hands dirty, boss. The Pit and the Alpha's packs are locked and loaded—let's go burn Heaven's gates down."</text>
        <choices>
          <li>
            <outcome>launch_campaign</outcome>
            <label>Accept the infernal tribute and launch Act V: Armageddon of the Wastes</label>
            <resultText>Sulfur winds howl across the crimson wasteland. The Dark Covenant rides to war!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Gold</thing><count>35</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>65</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>800</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>5</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a4_readyForFinale</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act V: Armageddon of the Wastes</label>
        <text>Meg has joined your garrison alongside tribute from Hell and the Monster Clans! Your first coalition mission—Blood Tithe—is now unlocked.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

  <TurboMegaMod.StoryQuestDef>
    <defName>TMM_Story_A4_Prep_ShadowConsortium</defName>
    <label>retainers of the shadow consortium</label>
    <act>4</act>
    <main>true</main>
    <autoStart>true</autoStart>
    <priority>97</priority>
    <faction>TMM_VaultTecFaction</faction>
    <giver>TMM_Hero_stanislaus_braun</giver>
    <fallbackGiver>TMM_Hero_captain_wes</fallbackGiver>
    <requires>
      <li>act=4</li>
      <li>ponr2=shadow_consortium</li>
      <li>story.a4_reckoningSurvived=true</li>
    </requires>
    <stages>
      <li Class="TurboMegaMod.StoryStage_HeroJoins">
        <hero>TMM_Hero_baker</hero>
      </li>
      <li Class="TurboMegaMod.StoryStage_Choice">
        <label>Corporate drop-pods &amp; elite sniper arrive</label>
        <text>Heavy Vault-Tec cargo containers touch down in your courtyard alongside Gunner master marksman Baker, carrying bullion, plasteel, and pre-war components. Baker checks his scope calmly: "Dr. Braun's transfer cleared, and Captain Wes has our battalions staged along the ridge. Tell us which competitors get liquidated first."</text>
        <choices>
          <li>
            <outcome>launch_campaign</outcome>
            <label>Accept the Consortium capital and launch Act V: Armageddon of the Wastes</label>
            <resultText>Contract orders flash across mercenary frequencies. The hostile takeover of the wasteland begins!</resultText>
            <effects>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Silver</thing><count>1200</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>Plasteel</thing><count>75</count></li>
              <li Class="TurboMegaMod.StoryEffect_GiveThing"><thing>ComponentIndustrial</thing><count>6</count></li>
            </effects>
          </li>
        </choices>
      </li>
    </stages>
    <onComplete>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>act</flag><value>5</value></li>
      <li Class="TurboMegaMod.StoryEffect_SetFlag"><flag>story.a4_readyForFinale</flag><value>true</value></li>
      <li Class="TurboMegaMod.StoryEffect_Letter">
        <label>Act V: Armageddon of the Wastes</label>
        <text>Baker has joined your garrison alongside Vault-Tec and Gunner war capital! Your first coalition operation—Hostile Takeover—is now unlocked.</text>
      </li>
    </onComplete>
  </TurboMegaMod.StoryQuestDef>

</Defs>
```

`Languages/Russian/DefInjected/TurboMegaMod.StoryQuestDef/Story_Act4_Main.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData>

  <!-- TMM_Story_A4_BloodEclipse -->
  <TMM_Story_A4_BloodEclipse.label>кровавое затмение</TMM_Story_A4_BloodEclipse.label>
  <TMM_Story_A4_BloodEclipse.stages.0.label>Багровое небо над Пустошью</TMM_Story_A4_BloodEclipse.stages.0.label>
  <TMM_Story_A4_BloodEclipse.stages.0.text>Под кроваво-красным солнцем Затмения Разлом над континентом стонет словно разрываемая сталь. Вокруг Резонатора Искр пляшет гравитация, стрелки компасов бешено крутятся, а лидеры всех фракций Пустоши движутся к {COLONY} на последние переговоры перед запуском машины.</TMM_Story_A4_BloodEclipse.stages.0.text>
  <TMM_Story_A4_BloodEclipse.stages.1.label>Канун решающего совета</TMM_Story_A4_BloodEclipse.stages.1.label>
  <TMM_Story_A4_BloodEclipse.stages.1.text>Резонатор Искр гудит на критической частоте, пока вооружённые делегации подходят к периметру. Опираясь на помощь перебежчиков или собственных опытных инженеров, вы уже подготовили защиту с бурой и обереги. Как вы встретите прибывающих вождей?</TMM_Story_A4_BloodEclipse.stages.1.text>
  <TMM_Story_A4_BloodEclipse.stages.1.choices.0.label>Встать рядом с Нулевым Синтом у пульта и усилить енохианские печати</TMM_Story_A4_BloodEclipse.stages.1.choices.0.label>
  <TMM_Story_A4_BloodEclipse.stages.1.choices.0.resultText>Голубой свет в кристалле Резонатора выравнивается, а Нулевой Синт спокойно дышит рядом с вами. За воротами сквозь багровую мглу спускаются первые винтокрылы и кортежи.</TMM_Story_A4_BloodEclipse.stages.1.choices.0.resultText>
  <TMM_Story_A4_BloodEclipse.stages.1.choices.1.label>Зарядить распылители буры, взвести турели и поднять гарнизон по тревоге</TMM_Story_A4_BloodEclipse.stages.1.choices.1.label>
  <TMM_Story_A4_BloodEclipse.stages.1.choices.1.resultText>На стенах лязгают затворы. Каждый лидер, переступивший порог {COLONY}, сразу поймёт, что вы намерены говорить с позиции силы.</TMM_Story_A4_BloodEclipse.stages.1.choices.1.resultText>

  <!-- TMM_Story_A4_Summit_Protect -->
  <TMM_Story_A4_Summit_Protect.label>совет под кровавым солнцем: защитники</TMM_Story_A4_Summit_Protect.label>
  <TMM_Story_A4_Summit_Protect.stages.1.label>Совет защитников</TMM_Story_A4_Summit_Protect.stages.1.label>
  <TMM_Story_A4_Summit_Protect.stages.1.text>Престон Гарви, Дездемона и братья Винчестеры собираются в зале вашей гостиницы в окружении настороженных наблюдателей Братства Стали и Волт-Тек. Престон кладёт шляпу на стол: «Мы пришли, потому что вы поклялись защищать живых. Назовите союз, который встанет у этого пульта, когда Разлом распахнётся».</TMM_Story_A4_Summit_Protect.stages.1.text>
  <TMM_Story_A4_Summit_Protect.stages.1.choices.0.label>Пообещать объединить Минитменов, охотников и Подземку вокруг Искры</TMM_Story_A4_Summit_Protect.stages.1.choices.0.label>
  <TMM_Story_A4_Summit_Protect.stages.1.choices.0.resultText>Дездемона и Престон обмениваются твёрдым кивком. Коалиция Свободной Пустоши готова выступить по вашему слову.</TMM_Story_A4_Summit_Protect.stages.1.choices.0.resultText>
  <TMM_Story_A4_Summit_Protect.stages.1.choices.1.label>Выслушать также предложения Братства и Волт-Тек перед блокировкой пульта</TMM_Story_A4_Summit_Protect.stages.1.choices.1.label>
  <TMM_Story_A4_Summit_Protect.stages.1.choices.1.resultText>Вы молча выслушиваете все доводы. Лидеры фракций занимают места вокруг зала Резонатора перед необратимым решением.</TMM_Story_A4_Summit_Protect.stages.1.choices.1.resultText>

  <!-- TMM_Story_A4_Summit_Purify -->
  <TMM_Story_A4_Summit_Purify.label>совет под кровавым солнцем: сталь и пепел</TMM_Story_A4_Summit_Purify.label>
  <TMM_Story_A4_Summit_Purify.stages.1.label>Вожди в зале</TMM_Story_A4_Summit_Purify.stages.1.label>
  <TMM_Story_A4_Summit_Purify.stages.1.text>Силовая броня Артура Мэксона сотрясает половицы, пока он смотрит через стол на полковника Отема и Генри Касдина, а в тени у двери затаился связной Подземки. Мэксон бьёт латной перчаткой по столу: «Час расплаты настал. Вы очистите этот мир сталью — или ядом Анклава?»</TMM_Story_A4_Summit_Purify.stages.1.text>
  <TMM_Story_A4_Summit_Purify.stages.1.choices.0.label>Подтвердить, что {COLONY} готова выжечь скверну из Пустоши раз и навсегда</TMM_Story_A4_Summit_Purify.stages.1.choices.0.label>
  <TMM_Story_A4_Summit_Purify.stages.1.choices.0.resultText>Мэксон и Отем коротко кивают, каждый рассчитывая, что у пульта Резонатора вы выберете именно его путь.</TMM_Story_A4_Summit_Purify.stages.1.choices.0.resultText>
  <TMM_Story_A4_Summit_Purify.stages.1.choices.1.label>Потребовать гарантий, что мирных жителей и Нулевого Синта не пустят под нож</TMM_Story_A4_Summit_Purify.stages.1.choices.1.label>
  <TMM_Story_A4_Summit_Purify.stages.1.choices.1.resultText>Мэксон холодно хмурится, а связной Подземки в тени ловит ваш взгляд — оставляя шанс на резкий разворот в последнюю секунду.</TMM_Story_A4_Summit_Purify.stages.1.choices.1.resultText>

  <!-- TMM_Story_A4_Summit_Order -->
  <TMM_Story_A4_Summit_Order.label>совет под кровавым солнцем: архитекторы</TMM_Story_A4_Summit_Order.label>
  <TMM_Story_A4_Summit_Order.stages.1.label>Небеса и Архитекторы</TMM_Story_A4_Summit_Order.stages.1.label>
  <TMM_Story_A4_Summit_Order.stages.1.text>Ослепительный небесный свет затмевает лампы, когда архангел Михаил входит в гостиницу рядом с доктором Хесс, а у терминала связи ждут Отец и доктор Брон. Михаил говорит с ледяной твёрдостью: «Земля два века истекала кровью в хаосе. Отдайте Искры небесному воинству, и мы установим вечный покой».</TMM_Story_A4_Summit_Order.stages.1.text>
  <TMM_Story_A4_Summit_Order.stages.1.choices.0.label>Признать, что лишь абсолютная высшая власть спасёт этот сломанный мир</TMM_Story_A4_Summit_Order.stages.1.choices.0.label>
  <TMM_Story_A4_Summit_Order.stages.1.choices.0.resultText>Михаил торжественно склоняет голову, и делегации направляются к залу Резонатора Искр.</TMM_Story_A4_Summit_Order.stages.1.choices.0.resultText>
  <TMM_Story_A4_Summit_Order.stages.1.choices.1.label>Сравнить условия Небес с предложениями Института и Волт-Тек</TMM_Story_A4_Summit_Order.stages.1.choices.1.label>
  <TMM_Story_A4_Summit_Order.stages.1.choices.1.resultText>Доктор Брон тонко улыбается из угла, а глаза Михаила сужаются. Всё решится у пульта.</TMM_Story_A4_Summit_Order.stages.1.choices.1.resultText>

  <!-- TMM_Story_A4_Summit_Abyss -->
  <TMM_Story_A4_Summit_Abyss.label>совет под кровавым солнцем: владыки ночи</TMM_Story_A4_Summit_Abyss.label>
  <TMM_Story_A4_Summit_Abyss.stages.1.label>Бокалы на краю бездны</TMM_Story_A4_Summit_Abyss.stages.1.label>
  <TMM_Story_A4_Summit_Abyss.stages.1.text>Воздух в гостинице пропитан серой и дорогим одеколоном: Кроули вращает бокал виски напротив Дика Романа и Альфа-вампира. Кроули хищно ухмыляется: «Ангелы дохнут с голоду, люди в ужасе, а Резонатор прогрет. Скажи, солнышко — мы сегодня вскрываем винный погреб Ада или подписываем корпоративный контракт?»</TMM_Story_A4_Summit_Abyss.stages.1.text>
  <TMM_Story_A4_Summit_Abyss.stages.1.choices.0.label>Поднять бокал за крушение Клетки и торжество Тёмного Завета</TMM_Story_A4_Summit_Abyss.stages.1.choices.0.label>
  <TMM_Story_A4_Summit_Abyss.stages.1.choices.0.resultText>Кроули со звоном чокается с вами и торжествующе смеётся, пока свита движется к Резонатору.</TMM_Story_A4_Summit_Abyss.stages.1.choices.0.resultText>
  <TMM_Story_A4_Summit_Abyss.stages.1.choices.1.label>Напомнить всем, что рубильник в руках {COLONY} — и потребовать максимальную цену</TMM_Story_A4_Summit_Abyss.stages.1.choices.1.label>
  <TMM_Story_A4_Summit_Abyss.stages.1.choices.1.resultText>Дик Роман одобрительно усмехается вашей деловой хватке. Теперь всё решится у пульта.</TMM_Story_A4_Summit_Abyss.stages.1.choices.1.resultText>

  <!-- TMM_Story_A4_ResonatorConsole -->
  <TMM_Story_A4_ResonatorConsole.label>выбор у пульта</TMM_Story_A4_ResonatorConsole.label>
  <TMM_Story_A4_ResonatorConsole.stages.0.label>Пик Кровавого затмения</TMM_Story_A4_ResonatorConsole.stages.0.label>
  <TMM_Story_A4_ResonatorConsole.stages.0.text>Багровое солнце замирает точно над {COLONY}. Молнии бьют между золотыми нитями Резонатора Искр и грозовыми тучами в небе. Тянуть больше нельзя — стоит вам запустить протокол выбранной коалиции на главном пульте, и расклад сил в мире закрепится навсегда.</TMM_Story_A4_ResonatorConsole.stages.0.text>
  <TMM_Story_A4_ResonatorConsole.stages.1.label>Точка невозврата №2: Выберите финальную коалицию</TMM_Story_A4_ResonatorConsole.stages.1.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.text>Все взгляды в зале прикованы к вашим рукам над рычагами Резонатора Искр. Этот выбор АБСОЛЮТЕН И НЕОБРАТИМ: выбранная коалиция станет вашими вечными союзниками до конца войны, а все враждебные блоки превратятся в смертельных врагов без шанса на примирение.\n\nКакая коалиция определит судьбу Земли?</TMM_Story_A4_ResonatorConsole.stages.1.text>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.0.label>[Свободная Пустошь] Союз с Минитменами и охотниками + Подземкой — запечатать Разлом и защитить все Искры (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.0.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.0.resultText>Вы запускаете Протокол Гармонического Сдерживания! Престон Гарви, братья Винчестеры и Дездемона занимают посты рядом с Нулевым Синтом — а Небеса, Ад, Анклав и Институт объявляют {COLONY} тотальную войну!</TMM_Story_A4_ResonatorConsole.stages.1.choices.0.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.1.label>[Свободная Пустошь] Порвать с чистильщиками и встать на сторону Минитменов и охотников + Подземки (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.1.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.1.resultText>В последний миг отвернувшись от вождей зачистки, вы включаете Протокол Гармонического Сдерживания! Дездемона и Престон Гарви встают на защиту Нулевого Синта, пока Анклав и Братство в ярости покидают зал!</TMM_Story_A4_ResonatorConsole.stages.1.choices.1.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.2.label>[Стерильная Империя] Союз с Анклавом + Институтом — выжечь мутации и вернуть Землю людям (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.2.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.2.resultText>Вы запускаете Матрицу Стерилизации! Полковник Отем и Отец скрепляют союз рукопожатием над пультом, а плазменные отряды и штурмовики синтов начинают зачистку Пустоши!</TMM_Story_A4_ResonatorConsole.stages.1.choices.2.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.3.label>[Стерильная Империя] Союз с Анклавом + Институтом — установить человеческую технократию (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.3.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.3.resultText>Отвергнув небесных владык, вы запускаете Матрицу Стерилизации вместе с полковником Отемом и Отцом! Легионы синтов и отряды «Адского пламени» стягиваются к {COLONY}, а ангелы и охотники объявляют войну!</TMM_Story_A4_ResonatorConsole.stages.1.choices.3.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.4.label>[Железный Крестовый Поход] Союз с Братством + Изгоями — подавить магию и опасные технологии (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.4.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.4.resultText>Артур Мэксон и Генри Касдин забывают старый раскол у вашего пульта! Резонатор переходит в режим континентального подавителя, а объединённый орден стали выступает в поход против аномалий, синтов и демонов!</TMM_Story_A4_ResonatorConsole.stages.1.choices.4.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.5.label>[Железный Крестовый Поход] Союз с Братством + Изгоями — укрыть человечество за стеной стали (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.5.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.5.resultText>Решив, что только железная дисциплина сдержит Разлом, вы объединяете Артура Мэксона и Генри Касдина под знаменем Железного Крестового Похода! Институт, Небеса и Ад поднимают армии против {COLONY}!</TMM_Story_A4_ResonatorConsole.stages.1.choices.5.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.6.label>[Небесный Порядок] Союз с Небесами + Британскими Просвещёнными — возродить благодать и установить мир (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.6.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.6.resultText>Ослепительный золотой огонь бьёт из Резонатора! Архангел Михаил и доктор Хесс берут под контроль сеть небесных маяков, а Ад, Братство и повстанцы клянутся сжечь {COLONY} дотла!</TMM_Story_A4_ResonatorConsole.stages.1.choices.6.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.7.label>[Небесный Порядок] Отвернуться от бездны в последний час и заключить союз с Небесами + Просвещёнными (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.7.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.7.resultText>Отказавшись погружать Землю в серу и пепел, вы передаёте управление Искрой архангелу Михаилу и доктору Хесс! Демоны воют от ярости, а небесное воинство спускается к вашим стенам!</TMM_Story_A4_ResonatorConsole.stages.1.choices.7.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.8.label>[Тёмный Завет] Союз с Адом + Кланами нечисти — напитаться Искрой и править вечными сумерками (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.8.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.8.resultText>Багровые молнии разрывают потолок зала! Владыки Ада и ночные кланы скрепляют кровавый пакт на Резонаторе {COLONY}, погружая Пустошь в вечные сумерки, пока Небеса и люди собирают крестовый поход!</TMM_Story_A4_ResonatorConsole.stages.1.choices.8.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.9.label>[Теневой Консорциум] Союз с Волт-Тек + Стрелками — монополизировать Резонатор и купить Пустошь (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.9.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.9.resultText>Доктор Брон и капитан Уэс подписывают устав прямо на корпусе Резонатора! Батальоны наёмников и охрана Убежищ занимают рубежи вокруг {COLONY}, пока фанатики и повстанцы готовят штурм!</TMM_Story_A4_ResonatorConsole.stages.1.choices.9.resultText>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.10.label>[Теневой Консорциум] Заключить холодный контракт с Волт-Тек + Стрелками ради выживания (НЕОБРАТИМО)</TMM_Story_A4_ResonatorConsole.stages.1.choices.10.label>
  <TMM_Story_A4_ResonatorConsole.stages.1.choices.10.resultText>Предпочтя жёсткие контракты и наёмные штыки идеализму, вы запускаете протокол Теневого Консорциума с доктором Броном и капитаном Уэсом! Половина Пустоши объявляет вас предателями!</TMM_Story_A4_ResonatorConsole.stages.1.choices.10.resultText>
  <TMM_Story_A4_ResonatorConsole.onComplete.1.label>Жребий брошен</TMM_Story_A4_ResonatorConsole.onComplete.1.label>
  <TMM_Story_A4_ResonatorConsole.onComplete.1.text>Луч Резонатора Искр зафиксирован на частоте вашей коалиции. По всей Пустоши во вражеских фракциях вспыхивают перевороты, а неперебежавшие герои обнажают клинки — началась Ночь длинных клинков.</TMM_Story_A4_ResonatorConsole.onComplete.1.text>

  <!-- TMM_Story_A4_NightOfLongKnives_Coups -->
  <TMM_Story_A4_NightOfLongKnives_Coups.label>ночь длинных клинков: перевороты</TMM_Story_A4_NightOfLongKnives_Coups.label>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.0.label>Ударная волна по фракциям</TMM_Story_A4_NightOfLongKnives_Coups.stages.0.label>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.0.text>Спустя считанные часы после вашего выбора у пульта Резонатора закрытые аварийные частоты взрываются сообщениями о кровавых переворотах в штабах сверхдержав региона.</TMM_Story_A4_NightOfLongKnives_Coups.stages.0.text>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.label>Новые владыки на тронах</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.label>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.text>В Институте всю силовую власть захватывает безжалостный Джастин Айо, отдавая приказ на тотальное истребление синтов поверхности. А в глубинах Ада ослабление контроля приводит к катастрофическому взлому Клетки Люцифера!</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.text>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.0.label>[Кроули перебежал] Приготовиться к ярости Люцифера, захватившего трон Ада</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.0.label>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.0.resultText>Кроули наливает себе полный стакан виски в вашей столовой, побледнев под привычной ухмылкой: «Что ж, солнышко... мои бывшие подчинённые только что выпустили босса из подвала. Теперь Адом правит Люцифер, и он идёт прямо по нашу душу».</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.0.resultText>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.1.label>[Кроули в Аду] Записать перехваты о Джастине Айо и военном совете преисподней</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.1.label>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.1.resultText>Связист фиксирует новые частоты противника: Джастин Айо бросает в бой легионы синтов, а Кроули поднимает демонов перекрёстка на тотальную войну.</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.1.resultText>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.2.label>[Тёмный Завет] Согласовать удары с Адом, пока Михаил и Мэксон объединяются против вас</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.2.label>
  <TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.2.resultText>Разведчики преисподней доносят, что архангел Михаил и старейшина Мэксон заключили временный крестовый союз ради уничтожения вашего Резонатора.</TMM_Story_A4_NightOfLongKnives_Coups.stages.1.choices.2.resultText>

  <!-- TMM_Story_A4_Challenge_TechVanguard -->
  <TMM_Story_A4_Challenge_TechVanguard.label>месть в стали</TMM_Story_A4_Challenge_TechVanguard.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.0.label>Засечён импульс телепортации</TMM_Story_A4_Challenge_TechVanguard.stages.0.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.0.text>Джастин Айо не теряет ни минуты: охотники Института и тяжёлые ударные синты наводятся прямо на двор {COLONY}, пытаясь обезглавить поселение до прибытия ваших новых союзников.</TMM_Story_A4_Challenge_TechVanguard.stages.0.text>
  <TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.0.effects.1.label>Авангард охотников разбит</TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.0.effects.1.text>Гарнизон перебил элитный отряд Института прямо во дворе, доказав, что ваша оборона выдержит высокотехнологичные удары.</TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.1.effects.1.label>Тяжёлый урон от налёта синтов</TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.1.effects.1.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.1.effects.1.text>Ударная группа охотников нанесла серьёзный урон перед телепортацией назад, заставив механиков срочно латать периметр.</TMM_Story_A4_Challenge_TechVanguard.stages.1.outcomes.1.effects.1.text>
  <TMM_Story_A4_Challenge_TechVanguard.stages.2.label>Разбор поля боя</TMM_Story_A4_Challenge_TechVanguard.stages.2.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.2.text>Дымящиеся остовы синтов и разбитые излучатели усеивают двор. Техники могут разобрать павших охотников на редкие сплавы или передать послание об их разгроме по частоте Института.</TMM_Story_A4_Challenge_TechVanguard.stages.2.text>
  <TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.0.label>Разобрать павших охотников на пласталь и передовые компоненты</TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.0.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.0.resultText>Инженеры извлекают ценные довоенные сплавы из обломков для укрепления обороны.</TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.0.resultText>
  <TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.1.label>Передать предупреждение Джастину Айо через передатчик его же охотника</TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.1.label>
  <TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.1.resultText>Нулевой Синт с тихой гордостью смотрит, как уходит сигнал — {COLONY} не склонится перед Институтом.</TMM_Story_A4_Challenge_TechVanguard.stages.2.choices.1.resultText>

  <!-- TMM_Story_A4_Challenge_RebelAmbush -->
  <TMM_Story_A4_Challenge_RebelAmbush.label>цена предательства</TMM_Story_A4_Challenge_RebelAmbush.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.0.label>Засада на дороге снабжения</TMM_Story_A4_Challenge_RebelAmbush.stages.0.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.0.text>Разъярённая вашим союзом с чистильщиками, тяжёлый боец Подземки Глория вместе с ветеранами ополчения захватила ключевую развязку у {COLONY}, вызывая ваших командиров на открытый бой.</TMM_Story_A4_Challenge_RebelAmbush.stages.0.text>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.0.effects.1.label>Развязка зачищена</TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.0.effects.1.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.0.effects.1.text>В ожесточённой перестрелке у развязки Глория пала, сражаясь до последнего патрона — блокада ваших путей снабжения прорвана.</TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.0.effects.1.text>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.1.effects.0.label>Блокада снята</TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.1.effects.0.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.1.effects.0.text>До подхода ваших основных сил отряд Глории заминировал перекрёсток и растворился в подземной сети.</TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.1.effects.0.text>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.2.effects.0.label>Кровавая ничья</TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.2.effects.0.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.2.effects.0.text>Ударной группе пришлось отойти под огнём минигана Глории, хотя подошедшие союзники вскоре отбросили повстанцев от трассы.</TMM_Story_A4_Challenge_RebelAmbush.stages.1.outcomes.2.effects.0.text>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.2.label>Подсчёт потерь</TMM_Story_A4_Challenge_RebelAmbush.stages.2.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.2.text>Над выжженной развязкой повисает тишина. Даже закалённые офицеры понимают суровую правду Ночи длинных клинков: вчерашние друзья стали заклятыми врагами.</TMM_Story_A4_Challenge_RebelAmbush.stages.2.text>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.2.choices.0.label>Собрать трофейное оружие повстанцев и вернуться в {COLONY}</TMM_Story_A4_Challenge_RebelAmbush.stages.2.choices.0.label>
  <TMM_Story_A4_Challenge_RebelAmbush.stages.2.choices.0.resultText>Бойцы забирают тяжёлые боеприпасы и возвращаются за стены, готовясь к решающей войне.</TMM_Story_A4_Challenge_RebelAmbush.stages.2.choices.0.resultText>

  <!-- TMM_Story_A4_Reckoning_AbyssalStrike -->
  <TMM_Story_A4_Reckoning_AbyssalStrike.label>челюсти в кровавом тумане</TMM_Story_A4_Reckoning_AbyssalStrike.label>
  <TMM_Story_A4_Reckoning_AbyssalStrike.stages.0.label>Чёрный ихор у ворот</TMM_Story_A4_Reckoning_AbyssalStrike.stages.0.label>
  <TMM_Story_A4_Reckoning_AbyssalStrike.stages.0.text>Не желая позволить {COLONY} сорвать их многовековую жатву, штурмовики левиафанов и ночные твари вырываются из багрового тумана — проверяя, готова ли ваша оборона с бурой к последней войне.</TMM_Story_A4_Reckoning_AbyssalStrike.stages.0.text>
  <TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.0.effects.2.label>Удар левиафанов отбит</TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.0.effects.2.text>Шипящие облака белой буры разъели неуязвимость левиафанов, позволив защитникам перебить тварей на подступах к Резонатору!</TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.1.effects.1.label>Яростная ночная схватка</TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.1.effects.1.label>
  <TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.1.effects.1.text>Стая левиафанов проломила внешние баррикады перед отступлением на рассвете — обязательно вооружите отряды распылителями буры перед финальной осадой!</TMM_Story_A4_Reckoning_AbyssalStrike.stages.1.outcomes.1.effects.1.text>

  <!-- TMM_Story_A4_Reckoning_CelestialStrike -->
  <TMM_Story_A4_Reckoning_CelestialStrike.label>кара небесного воинства</TMM_Story_A4_Reckoning_CelestialStrike.label>
  <TMM_Story_A4_Reckoning_CelestialStrike.stages.0.label>Столпы испепеляющего света</TMM_Story_A4_Reckoning_CelestialStrike.stages.0.label>
  <TMM_Story_A4_Reckoning_CelestialStrike.stages.0.text>Разгневанный союзом {COLONY} с преисподней или Советом Консорциума, Захария ведёт карательный отряд ангелов сквозь багровые тучи, чтобы покарать ваш зал Резонатора.</TMM_Story_A4_Reckoning_CelestialStrike.stages.0.text>
  <TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.0.effects.2.label>Небесный десант отбит</TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.0.effects.2.label>
  <TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.0.effects.2.text>Енохианские обереги и тяжёлый огонь отбросили карателей Небес, оставив во дворе опалённые перья и осколки небесной стали.</TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.0.effects.2.text>
  <TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.1.effects.1.label>Опалённые укрепления</TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.1.effects.1.label>
  <TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.1.effects.1.text>Отряд Небес сжёг половину внешних укреплений перед отходом — усильте енохианские обереги до подхода главных армий!</TMM_Story_A4_Reckoning_CelestialStrike.stages.1.outcomes.1.effects.1.text>

  <!-- TMM_Story_A4_Interlude_FinalVigil -->
  <TMM_Story_A4_Interlude_FinalVigil.label>бдение под багровым солнцем</TMM_Story_A4_Interlude_FinalVigil.label>
  <TMM_Story_A4_Interlude_FinalVigil.stages.0.label>Затишье перед бурей</TMM_Story_A4_Interlude_FinalVigil.stages.0.label>
  <TMM_Story_A4_Interlude_FinalVigil.stages.0.text>После Ночи длинных клинков над {COLONY} опускается тяжёлое, неестественное затишье. Колонисты точат клинки, проверяют уплотнители силовой брони и тихо переговариваются в столовой под ровный гул Резонатора Искр.</TMM_Story_A4_Interlude_FinalVigil.stages.0.text>
  <TMM_Story_A4_Interlude_FinalVigil.stages.1.label>Последние слова перед войной</TMM_Story_A4_Interlude_FinalVigil.stages.1.label>
  <TMM_Story_A4_Interlude_FinalVigil.stages.1.text>Вы застаёте Нулевого Синта у Резонатора — он молча всматривается в синий кристалл. Связанный с вами глубоким доверием или суровой необходимостью, он знает, что финальный акт вот-вот начнётся.</TMM_Story_A4_Interlude_FinalVigil.stages.1.text>
  <TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.0.label>Поднять тихий тост с защитниками и укрепить боевой дух</TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.0.label>
  <TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.0.resultText>В полумраке звенят кружки. Что бы ни принёс завтрашний день, {COLONY} встретит его не дрогнув.</TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.0.resultText>
  <TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.1.label>Проверить оружейную и раздать бойцам передовые аптечки</TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.1.label>
  <TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.1.resultText>Каждый отряд получает свежие медпакеты и боекомплект. Поселение готово к тотальной войне.</TMM_Story_A4_Interlude_FinalVigil.stages.1.choices.1.resultText>

  <!-- TMM_Story_A4_Prep_FreeWasteland -->
  <TMM_Story_A4_Prep_FreeWasteland.label>знамя свободной пустоши</TMM_Story_A4_Prep_FreeWasteland.label>
  <TMM_Story_A4_Prep_FreeWasteland.stages.1.label>Прибытие союзного авангарда</TMM_Story_A4_Prep_FreeWasteland.stages.1.label>
  <TMM_Story_A4_Prep_FreeWasteland.stages.1.text>В {COLONY} прибывают грузовые капсулы и разведчики Минитменов, охотников и Подземки с боеприпасами и соляными оберегами. Престон Гарви чётко отдаёт честь: «Свободная Пустошь с вами, генерал. Разведка засекла точки для первых енохианских маяков — дайте приказ, и мы начинаем кампанию!»</TMM_Story_A4_Prep_FreeWasteland.stages.1.text>
  <TMM_Story_A4_Prep_FreeWasteland.stages.1.choices.0.label>Принять припасы союзников и начать Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_FreeWasteland.stages.1.choices.0.label>
  <TMM_Story_A4_Prep_FreeWasteland.stages.1.choices.0.resultText>Сигнальные ракеты взмывают над союзными поселениями. Финальная битва за свободную Пустошь начинается!</TMM_Story_A4_Prep_FreeWasteland.stages.1.choices.0.resultText>
  <TMM_Story_A4_Prep_FreeWasteland.onComplete.2.label>Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_FreeWasteland.onComplete.2.label>
  <TMM_Story_A4_Prep_FreeWasteland.onComplete.2.text>Престон Гарви вступил в ваш гарнизон вместе с военными припасами Свободной Пустоши! Открыт первый финальный квест вашей коалиции — «Щит из соли и стали».</TMM_Story_A4_Prep_FreeWasteland.onComplete.2.text>

  <!-- TMM_Story_A4_Prep_SterileEmpire -->
  <TMM_Story_A4_Prep_SterileEmpire.label>директива стерильной империи</TMM_Story_A4_Prep_SterileEmpire.label>
  <TMM_Story_A4_Prep_SterileEmpire.stages.1.label>Прибытие ударной группы Анклава и Института</TMM_Story_A4_Prep_SterileEmpire.stages.1.label>
  <TMM_Story_A4_Prep_SterileEmpire.stages.1.text>Чёрный винтокрыл Анклава садится в сполохах молний телепортации Института, доставляя плазменное снаряжение и капитана Ричардса для усиления вашего штаба. Ричардс вытягивается по струнке: «Полковник Отем и Отец синхронизировали сеть распыления. Отдайте приказ — и мы начнём очистку Пустоши!»</TMM_Story_A4_Prep_SterileEmpire.stages.1.text>
  <TMM_Story_A4_Prep_SterileEmpire.stages.1.choices.0.label>Принять имперские резервы и начать Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_SterileEmpire.stages.1.choices.0.label>
  <TMM_Story_A4_Prep_SterileEmpire.stages.1.choices.0.resultText>Орбитальные ретрансляторы наводятся на базы сопротивления. Кампания Стерильной Империи начинается!</TMM_Story_A4_Prep_SterileEmpire.stages.1.choices.0.resultText>
  <TMM_Story_A4_Prep_SterileEmpire.onComplete.2.label>Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_SterileEmpire.onComplete.2.label>
  <TMM_Story_A4_Prep_SterileEmpire.onComplete.2.text>Капитан Ричардс вступил в ваш гарнизон вместе с припасами Анклава и Института! Открыта первая финальная операция вашей коалиции — «Токсичный горизонт».</TMM_Story_A4_Prep_SterileEmpire.onComplete.2.text>

  <!-- TMM_Story_A4_Prep_IronCrusade -->
  <TMM_Story_A4_Prep_IronCrusade.label>авангард железного крестового похода</TMM_Story_A4_Prep_IronCrusade.label>
  <TMM_Story_A4_Prep_IronCrusade.stages.1.label>Прибытие объединённого стального корпуса</TMM_Story_A4_Prep_IronCrusade.stages.1.label>
  <TMM_Story_A4_Prep_IronCrusade.stages.1.text>Тяжёлые десантные капсулы Братства Стали и Изгоев врезаются в посадочную зону, доставляя урановые стержни, пласталь и главного квартирмейстера специалиста Олин. Олин поправляет сварочные очки с мрачной ухмылкой: «Мэксон и Касдин наконец перестали спорить и прислали тяжёлое железо. Укажите, где Институт и чудовища!»</TMM_Story_A4_Prep_IronCrusade.stages.1.text>
  <TMM_Story_A4_Prep_IronCrusade.stages.1.choices.0.label>Принять арсенал ордена и начать Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_IronCrusade.stages.1.choices.0.label>
  <TMM_Story_A4_Prep_IronCrusade.stages.1.choices.0.resultText>Винтокрылы с рёвом поднимаются в багровое небо. Ad Victoriam — Железный Крестовый Поход выступает в бой!</TMM_Story_A4_Prep_IronCrusade.stages.1.choices.0.resultText>
  <TMM_Story_A4_Prep_IronCrusade.onComplete.2.label>Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_IronCrusade.onComplete.2.label>
  <TMM_Story_A4_Prep_IronCrusade.onComplete.2.text>Специалист Олин вступила в ваш гарнизон вместе с припасами Братства и Изгоев! Открыта первая финальная операция вашей коалиции — «Молот и наковальня».</TMM_Story_A4_Prep_IronCrusade.onComplete.2.text>

  <!-- TMM_Story_A4_Prep_CelestialOrder -->
  <TMM_Story_A4_Prep_CelestialOrder.label>вестники небесного порядка</TMM_Story_A4_Prep_CelestialOrder.label>
  <TMM_Story_A4_Prep_CelestialOrder.stages.1.label>Прибытие связной Небес и арсенала Лондона</TMM_Story_A4_Prep_CelestialOrder.stages.1.label>
  <TMM_Story_A4_Prep_CelestialOrder.stages.1.text>Во вспышке ослепительного белого света появляется офицер разведки Небес Наоми рядом с контейнерами, помеченными гербом Британских Просвещённых. Наоми спокойно складывает руки: «Архангел Михаил и доктор Хесс санкционировали полную поддержку вашего гарнизона. Вместе мы соберём Искры и принесём на Землю вечный порядок».</TMM_Story_A4_Prep_CelestialOrder.stages.1.text>
  <TMM_Story_A4_Prep_CelestialOrder.stages.1.choices.0.label>Принять небесные реликвии и начать Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_CelestialOrder.stages.1.choices.0.label>
  <TMM_Story_A4_Prep_CelestialOrder.stages.1.choices.0.resultText>Енохианские маяки вспыхивают вдоль горизонта. Кампания Небесного Порядка начинается!</TMM_Story_A4_Prep_CelestialOrder.stages.1.choices.0.resultText>
  <TMM_Story_A4_Prep_CelestialOrder.onComplete.2.label>Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_CelestialOrder.onComplete.2.label>
  <TMM_Story_A4_Prep_CelestialOrder.onComplete.2.text>Наоми вступила в ваш гарнизон вместе с припасами Небес и Просвещённых! Открыта первая финальная миссия вашей коалиции — «Жатва Искр».</TMM_Story_A4_Prep_CelestialOrder.onComplete.2.text>

  <!-- TMM_Story_A4_Prep_DarkCovenant -->
  <TMM_Story_A4_Prep_DarkCovenant.label>легион тёмного завета</TMM_Story_A4_Prep_DarkCovenant.label>
  <TMM_Story_A4_Prep_DarkCovenant.stages.1.label>Прибытие посланников бездны и ночи</TMM_Story_A4_Prep_DarkCovenant.stages.1.label>
  <TMM_Story_A4_Prep_DarkCovenant.stages.1.text>Чёрный дым стелется по двору, когда вперёд выходит Мэг с пакетами крови, довоенным золотом и военными сундуками Ада и Кланов нечисти. Мэг мрачно усмехается: «Надеюсь, ты готов испачкать руки, босс. Преисподняя и стаи Альфы на взводе — пойдём сожжём Небесные врата».</TMM_Story_A4_Prep_DarkCovenant.stages.1.text>
  <TMM_Story_A4_Prep_DarkCovenant.stages.1.choices.0.label>Принять дары бездны и начать Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_DarkCovenant.stages.1.choices.0.label>
  <TMM_Story_A4_Prep_DarkCovenant.stages.1.choices.0.resultText>Серные ветры воют над багровой Пустошью. Тёмный Завет выступает в поход!</TMM_Story_A4_Prep_DarkCovenant.stages.1.choices.0.resultText>
  <TMM_Story_A4_Prep_DarkCovenant.onComplete.2.label>Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_DarkCovenant.onComplete.2.label>
  <TMM_Story_A4_Prep_DarkCovenant.onComplete.2.text>Мэг вступила в ваш гарнизон вместе с дарами Ада и Кланов нечисти! Открыта первая финальная миссия вашей коалиции — «Кровавая десятина».</TMM_Story_A4_Prep_DarkCovenant.onComplete.2.text>

  <!-- TMM_Story_A4_Prep_ShadowConsortium -->
  <TMM_Story_A4_Prep_ShadowConsortium.label>наёмники теневого консорциума</TMM_Story_A4_Prep_ShadowConsortium.label>
  <TMM_Story_A4_Prep_ShadowConsortium.stages.1.label>Прибытие грузовых капсул и элитного снайпера</TMM_Story_A4_Prep_ShadowConsortium.stages.1.label>
  <TMM_Story_A4_Prep_ShadowConsortium.stages.1.text>Тяжёлые грузовые контейнеры Волт-Тек опускаются во двор вместе со снайпером Стрелков Бейкером, доставляя слитки золота, пласталь и довоенные компоненты. Бейкер спокойно проверяет оптику: «Перевод доктора Брона прошёл, а капитан Уэс уже вывел батальоны на хребет. Укажите, кого из конкурентов ликвидировать первым».</TMM_Story_A4_Prep_ShadowConsortium.stages.1.text>
  <TMM_Story_A4_Prep_ShadowConsortium.stages.1.choices.0.label>Принять капитал Консорциума и начать Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_ShadowConsortium.stages.1.choices.0.label>
  <TMM_Story_A4_Prep_ShadowConsortium.stages.1.choices.0.resultText>Приказы на зачистку разлетаются по частотам наёмников. Враждебное поглощение Пустоши начинается!</TMM_Story_A4_Prep_ShadowConsortium.stages.1.choices.0.resultText>
  <TMM_Story_A4_Prep_ShadowConsortium.onComplete.2.label>Акт V: Армагеддон Пустоши</TMM_Story_A4_Prep_ShadowConsortium.onComplete.2.label>
  <TMM_Story_A4_Prep_ShadowConsortium.onComplete.2.text>Бейкер вступил в ваш гарнизон вместе с военным капиталом Волт-Тек и Стрелков! Открыта первая финальная операция вашей коалиции — «Враждебное поглощение».</TMM_Story_A4_Prep_ShadowConsortium.onComplete.2.text>

</LanguageData>
```