`Art/Items/TMM_Backpack_Military.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="bpm_olive_main" x1="15%" y1="0%" x2="85%" y2="100%">
      <stop offset="0%" stop-color="#6e7a56"/>
      <stop offset="48%" stop-color="#515c3f"/>
      <stop offset="100%" stop-color="#343c28"/>
    </linearGradient>
    <linearGradient id="bpm_olive_lid" x1="15%" y1="0%" x2="85%" y2="100%">
      <stop offset="0%" stop-color="#7c8a62"/>
      <stop offset="52%" stop-color="#5d6949"/>
      <stop offset="100%" stop-color="#3c452e"/>
    </linearGradient>
    <linearGradient id="bpm_olive_pouch" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#65704f"/>
      <stop offset="55%" stop-color="#4b543b"/>
      <stop offset="100%" stop-color="#2f3624"/>
    </linearGradient>
    <linearGradient id="bpm_olive_side" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#596345"/>
      <stop offset="100%" stop-color="#2e3624"/>
    </linearGradient>
    <linearGradient id="bpm_bedroll" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7a7358"/>
      <stop offset="40%" stop-color="#5c5640"/>
      <stop offset="100%" stop-color="#363225"/>
    </linearGradient>
    <linearGradient id="bpm_webbing" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#272e20"/>
      <stop offset="45%" stop-color="#454f39"/>
      <stop offset="100%" stop-color="#20261a"/>
    </linearGradient>
    <linearGradient id="bpm_webbing_h" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#48523c"/>
      <stop offset="100%" stop-color="#252b1f"/>
    </linearGradient>
    <linearGradient id="bpm_buckle" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#5a6570"/>
      <stop offset="50%" stop-color="#39414a"/>
      <stop offset="100%" stop-color="#20262b"/>
    </linearGradient>
    <linearGradient id="bpm_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d4ba72"/>
      <stop offset="50%" stop-color="#997d3b"/>
      <stop offset="100%" stop-color="#57441b"/>
    </linearGradient>
    <linearGradient id="bpm_shade_bot" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#171c12" stop-opacity="0"/>
      <stop offset="100%" stop-color="#171c12" stop-opacity="0.48"/>
    </linearGradient>
  </defs>

  <!-- Padded tactical shoulder harness loops peeking behind top shoulders -->
  <path d="M 152,136 C 122,138 110,164 114,202"
        fill="none" stroke="#121519" stroke-width="28" stroke-linecap="round"/>
  <path d="M 152,136 C 122,138 110,164 114,202"
        fill="none" stroke="url(#bpm_webbing)" stroke-width="12" stroke-linecap="round"/>
  <path d="M 360,136 C 390,138 402,164 398,202"
        fill="none" stroke="#121519" stroke-width="28" stroke-linecap="round"/>
  <path d="M 360,136 C 390,138 402,164 398,202"
        fill="none" stroke="url(#bpm_webbing)" stroke-width="12" stroke-linecap="round"/>

  <!-- Top heavy-duty nylon drag handle -->
  <path d="M 216,118 C 216,74 296,74 296,118"
        fill="none" stroke="#121519" stroke-width="28" stroke-linecap="round"/>
  <path d="M 216,118 C 216,74 296,74 296,118"
        fill="none" stroke="url(#bpm_webbing)" stroke-width="11" stroke-linecap="round"/>

  <!-- Left & right boxy military side utility pouches -->
  <g stroke="#121519" stroke-linejoin="round">
    <!-- Left side pouch -->
    <rect x="86" y="216" width="54" height="162" rx="14" fill="url(#bpm_olive_side)" stroke-width="12"/>
    <!-- Left pouch top flap & compression strap -->
    <path d="M 86,256 Q 112,268 138,258" fill="none" stroke-width="7"/>
    <rect x="86" y="314" width="52" height="16" rx="4" fill="url(#bpm_webbing_h)" stroke-width="6"/>
    <circle cx="110" cy="252" r="5" fill="url(#bpm_brass)" stroke-width="3.5"/>

    <!-- Right side pouch -->
    <rect x="372" y="216" width="54" height="162" rx="14" fill="url(#bpm_olive_side)" stroke-width="12"/>
    <!-- Right pouch top flap & compression strap -->
    <path d="M 374,258 Q 400,268 426,256" fill="none" stroke-width="7"/>
    <rect x="374" y="314" width="52" height="16" rx="4" fill="url(#bpm_webbing_h)" stroke-width="6"/>
    <circle cx="402" cy="252" r="5" fill="url(#bpm_brass)" stroke-width="3.5"/>
  </g>

  <!-- Rolled tactical poncho/bedroll strapped beneath bottom of pack -->
  <rect x="148" y="394" width="216" height="46" rx="22"
        fill="url(#bpm_bedroll)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  <path d="M 164,408 L 348,408" stroke="#999171" stroke-width="3.5" stroke-linecap="round" opacity="0.45"/>

  <!-- Main structured rectangular-tapered military rucksack body -->
  <path d="M 146,120
           L 366,120
           C 380,120 388,132 388,148
           L 392,380
           C 392,398 378,410 360,410
           L 152,410
           C 134,410 120,398 120,380
           L 124,148
           C 124,132 132,120 146,120 Z"
        fill="url(#bpm_olive_main)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

  <!-- Subtle bottom depth shading -->
  <rect x="126" y="340" width="260" height="64" rx="14" fill="url(#bpm_shade_bot)"/>

  <!-- Horizontal MOLLE / PALS tactical webbing rows across mid torso -->
  <g stroke="#121519" stroke-width="5.5" stroke-linejoin="round">
    <rect x="136" y="246" width="240" height="18" rx="4" fill="url(#bpm_webbing_h)"/>
    <!-- Vertical bar-tack stitch divisions on MOLLE strip -->
    <g stroke="#121519" stroke-width="4.5">
      <line x1="168" y1="246" x2="168" y2="264"/>
      <line x1="212" y1="246" x2="212" y2="264"/>
      <line x1="256" y1="246" x2="256" y2="264"/>
      <line x1="300" y1="246" x2="300" y2="264"/>
      <line x1="344" y1="246" x2="344" y2="264"/>
    </g>
  </g>

  <!-- Twin boxy front tactical ammo / gear sub-pouches -->
  <!-- Left front pouch -->
  <g>
    <rect x="142" y="274" width="106" height="118" rx="14"
          fill="url(#bpm_olive_pouch)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <!-- Pouch lid flap -->
    <path d="M 142,288 C 142,278 150,274 160,274 L 230,274 C 240,274 248,278 248,288 L 248,312 Q 195,326 142,312 Z"
          fill="url(#bpm_olive_lid)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>
    <!-- Lower MOLLE strip on left pouch -->
    <rect x="150" y="344" width="90" height="14" rx="3" fill="url(#bpm_webbing_h)" stroke="#121519" stroke-width="4.5"/>
    <line x1="180" y1="344" x2="180" y2="358" stroke="#121519" stroke-width="4"/>
    <line x1="210" y1="344" x2="210" y2="358" stroke="#121519" stroke-width="4"/>
    <!-- Snap button -->
    <circle cx="195" cy="306" r="6.5" fill="url(#bpm_brass)" stroke="#121519" stroke-width="4"/>
  </g>

  <!-- Right front pouch -->
  <g>
    <rect x="264" y="274" width="106" height="118" rx="14"
          fill="url(#bpm_olive_pouch)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <!-- Pouch lid flap -->
    <path d="M 264,288 C 264,278 272,274 282,274 L 352,274 C 362,274 370,278 370,288 L 370,312 Q 317,326 264,312 Z"
          fill="url(#bpm_olive_lid)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>
    <!-- Lower MOLLE strip on right pouch -->
    <rect x="272" y="344" width="90" height="14" rx="3" fill="url(#bpm_webbing_h)" stroke="#121519" stroke-width="4.5"/>
    <line x1="302" y1="344" x2="302" y2="358" stroke="#121519" stroke-width="4"/>
    <line x1="332" y1="344" x2="332" y2="358" stroke="#121519" stroke-width="4"/>
    <!-- Snap button -->
    <circle cx="317" cy="306" r="6.5" fill="url(#bpm_brass)" stroke="#121519" stroke-width="4"/>
  </g>

  <!-- Heavy angular military top storm-lid hood -->
  <path d="M 140,118
           L 372,118
           C 386,118 394,128 394,144
           L 390,214
           C 388,228 376,236 360,238
           L 152,238
           C 136,236 124,228 122,214
           L 118,144
           C 118,128 126,118 140,118 Z"
        fill="url(#bpm_olive_lid)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

  <!-- Top lid highlight & tactical seam stitching -->
  <path d="M 148,132 L 364,132" stroke="#9eab80" stroke-width="5" stroke-linecap="round" opacity="0.5"/>
  <path d="M 136,222 L 376,222" stroke="#2c3322" stroke-width="3.5" stroke-dasharray="10 7" stroke-linecap="round" opacity="0.8"/>

  <!-- Centered Velcro morale patch panel with faded tactical chevron/star emblem (no text) -->
  <rect x="220" y="152" width="72" height="50" rx="6"
        fill="#39402d" stroke="#121519" stroke-width="6"/>
  <polygon points="256,161 261,173 274,174 264,182 267,194 256,187 245,194 248,182 238,174 251,173"
           fill="#b3a678" opacity="0.85"/>

  <!-- Twin vertical heavy tactical harness straps running full length (clamping lid & bottom bedroll) -->
  <!-- Left main vertical strap -->
  <rect x="172" y="114" width="32" height="330" rx="6"
        fill="url(#bpm_webbing)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
  <!-- Right main vertical strap -->
  <rect x="308" y="114" width="32" height="330" rx="6"
        fill="url(#bpm_webbing)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>

  <!-- Reinforced X-box stitching on straps -->
  <g stroke="#697558" stroke-width="2.5" opacity="0.65">
    <line x1="178" y1="126" x2="198" y2="146"/>
    <line x1="198" y1="126" x2="178" y2="146"/>
    <line x1="314" y1="126" x2="334" y2="146"/>
    <line x1="334" y1="126" x2="314" y2="146"/>
  </g>

  <!-- Chunky matte gunmetal quick-release tactical side-release buckles -->
  <!-- Left buckle -->
  <g transform="translate(188 228)">
    <rect x="-23" y="-24" width="46" height="48" rx="8"
          fill="url(#bpm_buckle)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
    <line x1="-22" y1="0" x2="22" y2="0" stroke="#121519" stroke-width="6"/>
    <rect x="-12" y="-13" width="24" height="8" rx="2" fill="#20252b"/>
    <rect x="-12" y="6" width="24" height="8" rx="2" fill="#20252b"/>
    <line x1="-15" y1="-19" x2="15" y2="-19" stroke="#808e9c" stroke-width="2.5" stroke-linecap="round" opacity="0.55"/>
  </g>

  <!-- Right buckle -->
  <g transform="translate(324 228)">
    <rect x="-23" y="-24" width="46" height="48" rx="8"
          fill="url(#bpm_buckle)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
    <line x1="-22" y1="0" x2="22" y2="0" stroke="#121519" stroke-width="6"/>
    <rect x="-12" y="-13" width="24" height="8" rx="2" fill="#20252b"/>
    <rect x="-12" y="6" width="24" height="8" rx="2" fill="#20252b"/>
    <line x1="-15" y1="-19" x2="15" y2="-19" stroke="#808e9c" stroke-width="2.5" stroke-linecap="round" opacity="0.55"/>
  </g>

  <!-- Lower bedroll compression buckles -->
  <rect x="169" y="396" width="38" height="22" rx="5" fill="url(#bpm_buckle)" stroke="#121519" stroke-width="7"/>
  <rect x="305" y="396" width="38" height="22" rx="5" fill="url(#bpm_buckle)" stroke="#121519" stroke-width="7"/>

  <!-- Dull brass tactical D-ring / carabiner clipped to right MOLLE strap -->
  <g transform="translate(358 264) rotate(12)">
    <rect x="-10" y="-14" width="20" height="34" rx="9"
          fill="none" stroke="#121519" stroke-width="12"/>
    <rect x="-10" y="-14" width="20" height="34" rx="9"
          fill="none" stroke="url(#bpm_brass)" stroke-width="5.5"/>
  </g>
</svg>
```

`Art/Items/TMM_Backpack_Power.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="bpp_glow_outer" cx="50%" cy="48%" r="48%">
      <stop offset="0%" stop-color="#69ffbe" stop-opacity="0.34"/>
      <stop offset="50%" stop-color="#30bd85" stop-opacity="0.14"/>
      <stop offset="100%" stop-color="#1b7854" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="bpp_steel_frame" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#8796a3"/>
      <stop offset="45%" stop-color="#586470"/>
      <stop offset="100%" stop-color="#2e353d"/>
    </linearGradient>
    <linearGradient id="bpp_armor_body" x1="15%" y1="0%" x2="85%" y2="100%">
      <stop offset="0%" stop-color="#6a7680"/>
      <stop offset="50%" stop-color="#475159"/>
      <stop offset="100%" stop-color="#293036"/>
    </linearGradient>
    <linearGradient id="bpp_armor_dark" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#3d454d"/>
      <stop offset="100%" stop-color="#1d2226"/>
    </linearGradient>
    <linearGradient id="bpp_side_pod" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#374047"/>
      <stop offset="35%" stop-color="#707e8a"/>
      <stop offset="70%" stop-color="#4d5761"/>
      <stop offset="100%" stop-color="#272e33"/>
    </linearGradient>
    <linearGradient id="bpp_core_yellow" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#755a1c"/>
      <stop offset="28%" stop-color="#dfb74e"/>
      <stop offset="52%" stop-color="#f0ce6e"/>
      <stop offset="80%" stop-color="#a8842c"/>
      <stop offset="100%" stop-color="#5c4514"/>
    </linearGradient>
    <linearGradient id="bpp_core_cap" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#2d343b"/>
      <stop offset="38%" stop-color="#758390"/>
      <stop offset="72%" stop-color="#49535c"/>
      <stop offset="100%" stop-color="#23292e"/>
    </linearGradient>
    <linearGradient id="bpp_copper" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#693e24"/>
      <stop offset="45%" stop-color="#c48154"/>
      <stop offset="100%" stop-color="#522f1a"/>
    </linearGradient>
    <radialGradient id="bpp_led_glow" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#e3fff2"/>
      <stop offset="38%" stop-color="#5effb5"/>
      <stop offset="75%" stop-color="#1db876" stop-opacity="0.55"/>
      <stop offset="100%" stop-color="#0f7a4c" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="bpp_energy_bar" x1="0%" y1="100%" x2="0%" y2="0%">
      <stop offset="0%" stop-color="#24b875"/>
      <stop offset="55%" stop-color="#5cffb0"/>
      <stop offset="100%" stop-color="#ccffe6"/>
    </linearGradient>
    <clipPath id="bpp_haz_clip">
      <rect x="172" y="366" width="168" height="24" rx="4"/>
    </clipPath>
  </defs>

  <!-- Subtle radioactive / plasma indicator ambient aura -->
  <circle cx="256" cy="248" r="196" fill="url(#bpp_glow_outer)"/>

  <!-- Heavy tubular steel exoskeleton roll-cage frame (металлический каркас) -->
  <path d="M 142,418
           L 124,394
           L 124,132
           L 158,92
           L 354,92
           L 388,132
           L 388,394
           L 370,418
           L 338,418
           L 338,390
           L 174,390
           L 174,418 Z"
        fill="none" stroke="#121519" stroke-width="34" stroke-linejoin="round" stroke-linecap="round"/>
  <path d="M 142,418
           L 124,394
           L 124,132
           L 158,92
           L 354,92
           L 388,132
           L 388,394
           L 370,418
           L 338,418
           L 338,390
           L 174,390
           L 174,418 Z"
        fill="none" stroke="url(#bpp_steel_frame)" stroke-width="14" stroke-linejoin="round" stroke-linecap="round"/>

  <!-- Twin insulated copper/armored power conduits looping from side pods -->
  <path d="M 116,184 C 92,196 92,318 120,334"
        fill="none" stroke="#121519" stroke-width="24" stroke-linecap="round"/>
  <path d="M 116,184 C 92,196 92,318 120,334"
        fill="none" stroke="url(#bpp_copper)" stroke-width="10" stroke-linecap="round"/>
  <path d="M 396,184 C 420,196 420,318 392,334"
        fill="none" stroke="#121519" stroke-width="24" stroke-linecap="round"/>
  <path d="M 396,184 C 420,196 420,318 392,334"
        fill="none" stroke="url(#bpp_copper)" stroke-width="10" stroke-linecap="round"/>

  <!-- Left & right cylindrical capacitor / stabilizer exhaust pods -->
  <g stroke="#121519" stroke-linejoin="round">
    <!-- Left pod -->
    <rect x="94" y="204" width="46" height="118" rx="14" fill="url(#bpp_side_pod)" stroke-width="11"/>
    <line x1="98" y1="234" x2="136" y2="234" stroke-width="6"/>
    <line x1="98" y1="292" x2="136" y2="292" stroke-width="6"/>
    <!-- Right pod -->
    <rect x="372" y="204" width="46" height="118" rx="14" fill="url(#bpp_side_pod)" stroke-width="11"/>
    <line x1="376" y1="234" x2="414" y2="234" stroke-width="6"/>
    <line x1="376" y1="292" x2="414" y2="292" stroke-width="6"/>
  </g>

  <!-- Main armored metal power-pack chassis inside the exo-frame -->
  <path d="M 156,120
           L 356,120
           L 378,150
           L 378,378
           L 352,402
           L 160,402
           L 134,378
           L 134,150 Z"
        fill="url(#bpp_armor_body)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

  <!-- Recessed dark mechanical core bay -->
  <rect x="166" y="142" width="180" height="214" rx="16"
        fill="url(#bpp_armor_dark)" stroke="#121519" stroke-width="8"/>

  <!-- Heat-sink cooling louvres on lower-left and upper-left of chassis -->
  <g stroke="#1b2026" stroke-width="5.5" stroke-linecap="round">
    <line x1="178" y1="292" x2="210" y2="292"/>
    <line x1="178" y1="306" x2="210" y2="306"/>
    <line x1="178" y1="320" x2="210" y2="320"/>
    <line x1="178" y1="334" x2="210" y2="334"/>
  </g>

  <!-- Lower yellow-black hazard strip plate -->
  <g clip-path="url(#bpp_haz_clip)">
    <rect x="170" y="364" width="172" height="28" fill="#c9a038"/>
    <g stroke="#1a1f24" stroke-width="12">
      <line x1="184" y1="394" x2="202" y2="362"/>
      <line x1="214" y1="394" x2="232" y2="362"/>
      <line x1="244" y1="394" x2="262" y2="362"/>
      <line x1="274" y1="394" x2="292" y2="362"/>
      <line x1="304" y1="394" x2="322" y2="362"/>
    </g>
  </g>
  <rect x="172" y="366" width="168" height="24" rx="4" fill="none" stroke="#121519" stroke-width="6"/>

  <!-- NUCLEAR BATTERY / FUSION CORE (ядерная батарея) locked in central-left core bay -->
  <g transform="translate(232 246)">
    <!-- Core top armored socket cap -->
    <rect x="-36" y="-92" width="72" height="24" rx="8"
          fill="url(#bpp_core_cap)" stroke="#121519" stroke-width="9"/>
    <!-- Core bottom armored socket base -->
    <rect x="-36" y="68" width="72" height="24" rx="8"
          fill="url(#bpp_core_cap)" stroke="#121519" stroke-width="9"/>
    <!-- Main cylindrical yellow industrial fusion core casing -->
    <rect x="-42" y="-72" width="84" height="144" rx="14"
          fill="url(#bpp_core_yellow)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <!-- Core machined cooling rings / ribs -->
    <line x1="-40" y1="-44" x2="40" y2="-44" stroke="#121519" stroke-width="6"/>
    <line x1="-40" y1="44" x2="40" y2="44" stroke="#121519" stroke-width="6"/>
    <!-- Center dark armored containment band -->
    <rect x="-44" y="-22" width="88" height="44" rx="6"
          fill="url(#bpp_core_cap)" stroke="#121519" stroke-width="7"/>
    <!-- Glowing nuclear core energy slits inside containment band -->
    <rect x="-26" y="-9" width="52" height="18" rx="6"
          fill="#10291e" stroke="#121519" stroke-width="4.5"/>
    <rect x="-22" y="-5" width="44" height="10" rx="4" fill="#63ffba"/>
    <line x1="-10" y1="-8" x2="-10" y2="8" stroke="#121519" stroke-width="4"/>
    <line x1="10" y1="-8" x2="10" y2="8" stroke="#121519" stroke-width="4"/>
    <!-- Stylized radiation trefoil dot emblem on upper yellow casing (no text) -->
    <g transform="translate(0 -58)">
      <circle cx="0" cy="0" r="3.5" fill="#1a1f24"/>
      <path d="M -5,-2 L -11,-8 A 12,12 0 0 1 -2,-12 Z" fill="#1a1f24"/>
      <path d="M 5,-2 L 11,-8 A 12,12 0 0 0 2,-12 Z" fill="#1a1f24"/>
      <path d="M -3,5 L 3,5 L 6,11 A 12,12 0 0 1 -6,11 Z" fill="#1a1f24"/>
    </g>
    <!-- Specular metallic shine along left of core -->
    <line x1="-22" y1="-64" x2="-22" y2="-48" stroke="#fff5cc" stroke-width="4" stroke-linecap="round" opacity="0.75"/>
    <line x1="-22" y1="50" x2="-22" y2="64" stroke="#fff5cc" stroke-width="4" stroke-linecap="round" opacity="0.75"/>
  </g>

  <!-- Heavy steel retaining clamps locking the nuclear battery to the frame -->
  <path d="M 162,188 L 194,188 L 198,204 L 162,204 Z" fill="url(#bpp_steel_frame)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
  <path d="M 270,188 L 298,188 L 298,204 L 266,204 Z" fill="url(#bpp_steel_frame)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
  <path d="M 162,288 L 198,288 L 194,304 L 162,304 Z" fill="url(#bpp_steel_frame)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
  <path d="M 266,288 L 298,288 L 298,304 L 270,304 Z" fill="url(#bpp_steel_frame)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>

  <!-- GLOWING STATUS INDICATOR MODULE (светящийся индикатор) on right side of bay -->
  <g transform="translate(316 246)">
    <!-- Armored indicator housing -->
    <rect x="-24" y="-84" width="48" height="168" rx="12"
          fill="#283038" stroke="#121519" stroke-width="8"/>

    <!-- Top circular glowing reactor status beacon -->
    <circle cx="0" cy="-48" r="34" fill="url(#bpp_led_glow)"/>
    <circle cx="0" cy="-48" r="16" fill="#0e2b1f" stroke="#121519" stroke-width="5"/>
    <circle cx="0" cy="-48" r="11" fill="#5effb5"/>
    <circle cx="-3" cy="-51" r="4" fill="#ffffff" opacity="0.9"/>

    <!-- Vertical segmented glowing power charge bar -->
    <rect x="-12" y="-18" width="24" height="86" rx="6"
          fill="#0d1c16" stroke="#121519" stroke-width="5"/>
    <!-- Glowing green-cyan charge segments -->
    <rect x="-8" y="-12" width="16" height="74" rx="3" fill="url(#bpp_energy_bar)"/>
    <!-- Segment divider bars -->
    <g stroke="#10211a" stroke-width="4">
      <line x1="-10" y1="6" x2="10" y2="6"/>
      <line x1="-10" y1="24" x2="10" y2="24"/>
      <line x1="-10" y1="42" x2="10" y2="42"/>
    </g>
    <!-- Glass specular highlight over indicator bar -->
    <line x1="-5" y1="-9" x2="-5" y2="58" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round" opacity="0.65"/>
  </g>

  <!-- Corner steel gusset plates & heavy rivets on exo-frame -->
  <g fill="url(#bpp_steel_frame)" stroke="#121519" stroke-width="8" stroke-linejoin="round">
    <polygon points="130,136 172,114 172,148 130,162"/>
    <polygon points="382,136 340,114 340,148 382,162"/>
    <polygon points="130,368 172,390 172,358 130,344"/>
    <polygon points="382,368 340,390 340,358 382,344"/>
  </g>
  <g fill="#95a4b2" stroke="#121519" stroke-width="3.5">
    <circle cx="152" cy="138" r="5"/>
    <circle cx="360" cy="138" r="5"/>
    <circle cx="152" cy="368" r="5"/>
    <circle cx="360" cy="368" r="5"/>
    <circle cx="204" cy="100" r="5"/>
    <circle cx="308" cy="100" r="5"/>
  </g>
</svg>
```

`Art/Items/TMM_Legendary_GoldenPipBoy.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="gpb_gold_aura" cx="50%" cy="50%" r="48%">
      <stop offset="0%" stop-color="#ffe285" stop-opacity="0.32"/>
      <stop offset="52%" stop-color="#cc9b33" stop-opacity="0.12"/>
      <stop offset="100%" stop-color="#8c6414" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="gpb_screen_aura" cx="52%" cy="48%" r="42%">
      <stop offset="0%" stop-color="#5eff7c" stop-opacity="0.34"/>
      <stop offset="65%" stop-color="#22ad49" stop-opacity="0.12"/>
      <stop offset="100%" stop-color="#146e2e" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="gpb_gold_main" x1="12%" y1="5%" x2="88%" y2="95%">
      <stop offset="0%" stop-color="#fae18c"/>
      <stop offset="28%" stop-color="#d9ae48"/>
      <stop offset="54%" stop-color="#f2d272"/>
      <stop offset="80%" stop-color="#a67a21"/>
      <stop offset="100%" stop-color="#694a10"/>
    </linearGradient>
    <linearGradient id="gpb_gold_bezel" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#c99d3c"/>
      <stop offset="50%" stop-color="#8f671b"/>
      <stop offset="100%" stop-color="#543a0c"/>
    </linearGradient>
    <linearGradient id="gpb_gold_knob" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fce69a"/>
      <stop offset="45%" stop-color="#c99c38"/>
      <stop offset="100%" stop-color="#664810"/>
    </linearGradient>
    <linearGradient id="gpb_cuff_leather" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#4d3524"/>
      <stop offset="35%" stop-color="#332217"/>
      <stop offset="100%" stop-color="#1c120b"/>
    </linearGradient>
    <radialGradient id="gpb_crt_bg" cx="46%" cy="44%" r="64%">
      <stop offset="0%" stop-color="#276e38"/>
      <stop offset="55%" stop-color="#154221"/>
      <stop offset="100%" stop-color="#0b2412"/>
    </radialGradient>
    <linearGradient id="gpb_crt_glare" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d4ffdc" stop-opacity="0.42"/>
      <stop offset="45%" stop-color="#8cff9f" stop-opacity="0.08"/>
      <stop offset="100%" stop-color="#8cff9f" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="gpb_steel_dark" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#4a535c"/>
      <stop offset="100%" stop-color="#21262b"/>
    </linearGradient>
    <clipPath id="gpb_crt_clip">
      <rect x="200" y="182" width="166" height="126" rx="18"/>
    </clipPath>
  </defs>

  <!-- Warm legendary gilded aura -->
  <circle cx="256" cy="256" r="206" fill="url(#gpb_gold_aura)"/>

  <g transform="rotate(-7 256 256)">
    <!-- Padded dark leather wrist cuff extending left and right -->
    <path d="M 78,200
             C 62,202 58,230 58,266
             C 58,302 64,334 80,338
             L 432,338
             C 448,334 454,302 454,266
             C 454,230 450,202 434,200 Z"
          fill="url(#gpb_cuff_leather)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>
    <!-- Cuff padded rib stitching -->
    <path d="M 86,214 C 76,244 76,290 88,324" fill="none" stroke="#6b4b34" stroke-width="4" stroke-linecap="round" opacity="0.6"/>
    <path d="M 426,214 C 436,244 436,290 424,324" fill="none" stroke="#6b4b34" stroke-width="4" stroke-linecap="round" opacity="0.6"/>

    <!-- Top protruding Geiger-counter / Rad-meter gauge housing (upper-left) -->
    <path d="M 152,162 L 152,128 C 152,110 174,98 202,98 C 230,98 252,110 252,128 L 252,162 Z"
          fill="url(#gpb_gold_main)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
    <!-- Recessed analog rad-meter dial -->
    <path d="M 172,148 A 30,28 0 0 1 232,148 Z"
          fill="#e8e2c8" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
    <!-- Meter tick marks & red danger zone arc -->
    <path d="M 212,124 A 24,22 0 0 1 228,144" fill="none" stroke="#b53328" stroke-width="5" stroke-linecap="round"/>
    <line x1="202" y1="146" x2="218" y2="128" stroke="#121519" stroke-width="4" stroke-linecap="round"/>
    <circle cx="202" cy="146" r="4.5" fill="#121519"/>

    <!-- Top-right ribbed gold heat-vent / radio receiver bar -->
    <rect x="272" y="124" width="96" height="34" rx="10"
          fill="url(#gpb_gold_bezel)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <g stroke="#291d08" stroke-width="4.5" stroke-linecap="round">
      <line x1="288" y1="136" x2="288" y2="148"/>
      <line x1="302" y1="136" x2="302" y2="148"/>
      <line x1="316" y1="136" x2="316" y2="148"/>
      <line x1="330" y1="136" x2="330" y2="148"/>
      <line x1="344" y1="136" x2="344" y2="148"/>
    </g>

    <!-- Bottom gold wrist clamp hinge & latch -->
    <rect x="200" y="362" width="112" height="32" rx="10"
          fill="url(#gpb_gold_bezel)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <line x1="224" y1="364" x2="224" y2="392" stroke="#121519" stroke-width="5"/>
    <line x1="288" y1="364" x2="288" y2="392" stroke="#121519" stroke-width="5"/>

    <!-- Main sculpted gilded Pip-Boy armored casing -->
    <path d="M 118,162
             C 118,148 130,140 146,140
             L 372,140
             C 394,140 410,156 410,178
             L 410,334
             C 410,356 394,372 372,372
             L 146,372
             C 130,372 118,360 118,344
             L 106,316
             L 106,196 Z"
          fill="url(#gpb_gold_main)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

    <!-- Gilded beveled highlight edge along top and left -->
    <path d="M 136,154 L 372,154 C 386,154 396,164 396,178"
          fill="none" stroke="#fff2bf" stroke-width="5" stroke-linecap="round" opacity="0.65"/>

    <!-- Knurled gold rotary tuning dial protruding from left side -->
    <rect x="86" y="218" width="28" height="76" rx="9"
          fill="url(#gpb_gold_knob)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <g stroke="#3d2908" stroke-width="4" stroke-linecap="round">
      <line x1="90" y1="234" x2="110" y2="234"/>
      <line x1="90" y1="248" x2="110" y2="248"/>
      <line x1="90" y1="262" x2="110" y2="262"/>
      <line x1="90" y1="276" x2="110" y2="276"/>
    </g>

    <!-- Knurled gold scroll wheel on right bezel -->
    <rect x="402" y="226" width="22" height="58" rx="8"
          fill="url(#gpb_gold_knob)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <g stroke="#3d2908" stroke-width="3.5" stroke-linecap="round">
      <line x1="405" y1="242" x2="421" y2="242"/>
      <line x1="405" y1="255" x2="421" y2="255"/>
      <line x1="405" y1="268" x2="421" y2="268"/>
    </g>

    <!-- Left control column: rotary dial, speaker vents & LED status pips -->
    <!-- Upper speaker grille slots -->
    <g stroke="#332207" stroke-width="5" stroke-linecap="round">
      <line x1="130" y1="176" x2="174" y2="176"/>
      <line x1="130" y1="189" x2="174" y2="189"/>
      <line x1="130" y1="202" x2="174" y2="202"/>
    </g>

    <!-- Round mechanical selector knob -->
    <circle cx="152" cy="252" r="24" fill="url(#gpb_gold_bezel)" stroke="#121519" stroke-width="8"/>
    <circle cx="152" cy="252" r="14" fill="url(#gpb_gold_knob)" stroke="#121519" stroke-width="4"/>
    <line x1="152" y1="252" x2="162" y2="242" stroke="#121519" stroke-width="4.5" stroke-linecap="round"/>

    <!-- Status indicator LEDs (green & amber/red) -->
    <circle cx="138" cy="306" r="8" fill="#59ff7a" stroke="#121519" stroke-width="4.5"/>
    <circle cx="164" cy="306" r="8" fill="#e0583f" stroke="#121519" stroke-width="4.5"/>

    <!-- Recessed armored bezel framing the CRT screen -->
    <rect x="188" y="170" width="190" height="150" rx="24"
          fill="url(#gpb_gold_bezel)" stroke="#121519" stroke-width="9"/>

    <!-- Glowing phosphor-green CRT screen (зелёный экран) -->
    <rect x="200" y="182" width="166" height="126" rx="18"
          fill="url(#gpb_crt_bg)" stroke="#121519" stroke-width="7"/>

    <!-- CRT screen contents (pure vector UI — no text) -->
    <g clip-path="url(#gpb_crt_clip)">
      <!-- Inner phosphor glow -->
      <ellipse cx="283" cy="245" rx="92" ry="68" fill="url(#gpb_screen_aura)"/>

      <!-- Subtle CRT scanlines -->
      <g stroke="#091f10" stroke-width="2" opacity="0.55">
        <line x1="198" y1="194" x2="368" y2="194"/>
        <line x1="198" y1="204" x2="368" y2="204"/>
        <line x1="198" y1="214" x2="368" y2="214"/>
        <line x1="198" y1="224" x2="368" y2="224"/>
        <line x1="198" y1="234" x2="368" y2="234"/>
        <line x1="198" y1="244" x2="368" y2="244"/>
        <line x1="198" y1="254" x2="368" y2="254"/>
        <line x1="198" y1="264" x2="368" y2="264"/>
        <line x1="198" y1="274" x2="368" y2="274"/>
        <line x1="198" y1="284" x2="368" y2="284"/>
        <line x1="198" y1="294" x2="368" y2="294"/>
      </g>

      <!-- Top & bottom retro Pip-Boy UI header/footer brackets -->
      <path d="M 212,202 L 212,194 L 354,194 L 354,202" fill="none" stroke="#5eff7c" stroke-width="3" stroke-linecap="round"/>
      <path d="M 212,286 L 212,294 L 354,294 L 354,286" fill="none" stroke="#5eff7c" stroke-width="3" stroke-linecap="round"/>

      <!-- Left mini radar / vault-gear circular emblem on CRT -->
      <g transform="translate(242 244)" fill="none" stroke="#6bff87" stroke-width="3">
        <circle cx="0" cy="0" r="22"/>
        <circle cx="0" cy="0" r="11" stroke-dasharray="6 4"/>
        <line x1="-26" y1="0" x2="-16" y2="0"/>
        <line x1="16" y1="0" x2="26" y2="0"/>
        <line x1="0" y1="-26" x2="0" y2="-16"/>
        <line x1="0" y1="16" x2="0" y2="26"/>
        <circle cx="0" cy="0" r="4" fill="#a8ffb8" stroke="none"/>
      </g>

      <!-- Right biometric ECG heartbeat waveform & status bars on CRT -->
      <polyline points="276,242 290,242 297,222 305,260 313,234 320,242 350,242"
                fill="none" stroke="#99ffab" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
      <!-- Health / AP / Rad segmented status bars -->
      <rect x="278" y="210" width="70" height="7" rx="2" fill="#5eff7c" opacity="0.9"/>
      <rect x="278" y="270" width="54" height="7" rx="2" fill="#5eff7c" opacity="0.8"/>
      <rect x="336" y="270" width="12" height="7" rx="2" fill="#2b803f"/>

      <!-- Curved CRT glass specular reflection -->
      <path d="M 204,186 L 318,186 L 238,304 L 204,304 Z" fill="url(#gpb_crt_glare)"/>
      <path d="M 212,192 Q 246,188 278,192" fill="none" stroke="#d4ffdc" stroke-width="3.5" stroke-linecap="round" opacity="0.6"/>
    </g>

    <!-- Crisp final CRT screen rim -->
    <rect x="200" y="182" width="166" height="126" rx="18"
          fill="none" stroke="#121519" stroke-width="7"/>

    <!-- Bottom row of tactile control buttons beneath CRT screen -->
    <g stroke="#121519" stroke-width="6" stroke-linejoin="round">
      <rect x="198" y="332" width="32" height="20" rx="5" fill="url(#gpb_steel_dark)"/>
      <rect x="242" y="332" width="32" height="20" rx="5" fill="url(#gpb_steel_dark)"/>
      <rect x="286" y="332" width="32" height="20" rx="5" fill="url(#gpb_steel_dark)"/>
      <rect x="330" y="332" width="32" height="20" rx="5" fill="url(#gpb_gold_knob)"/>
    </g>

    <!-- Corner gilded hex bolts -->
    <g fill="#f7dc86" stroke="#121519" stroke-width="4">
      <circle cx="136" cy="158" r="5.5"/>
      <circle cx="390" cy="160" r="5.5"/>
      <circle cx="136" cy="350" r="5.5"/>
      <circle cx="390" cy="350" r="5.5"/>
    </g>
  </g>

  <!-- Crisp legendary golden star glints -->
  <path d="M 388,128 L 392,143 L 407,147 L 392,151 L 388,166 L 384,151 L 369,147 L 384,143 Z" fill="#fff7d1" opacity="0.95"/>
  <path d="M 114,358 L 117,368 L 127,371 L 117,374 L 114,384 L 111,374 L 101,371 L 111,368 Z" fill="#ffe999" opacity="0.88"/>
  <path d="M 218,88 L 220,95 L 227,97 L 220,99 L 218,106 L 216,99 L 209,97 L 216,95 Z" fill="#fff7d1" opacity="0.85"/>
</svg>
```