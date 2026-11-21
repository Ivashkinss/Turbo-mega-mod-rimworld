`Art/Items/TMM_Mod_RailroadBallisticWeave.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="rbw_cloth_roll" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#1e242b"/>
      <stop offset="28%" stop-color="#3c4652"/>
      <stop offset="52%" stop-color="#4e5a69"/>
      <stop offset="80%" stop-color="#2c343d"/>
      <stop offset="100%" stop-color="#191e24"/>
    </linearGradient>
    <linearGradient id="rbw_cloth_flap" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#424d59"/>
      <stop offset="48%" stop-color="#2e3740"/>
      <stop offset="100%" stop-color="#1e242b"/>
    </linearGradient>
    <linearGradient id="rbw_cloth_back" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#1c2229"/>
      <stop offset="100%" stop-color="#313a45"/>
    </linearGradient>
    <linearGradient id="rbw_plate" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7c8b99"/>
      <stop offset="45%" stop-color="#4f5b66"/>
      <stop offset="100%" stop-color="#2b333b"/>
    </linearGradient>
    <linearGradient id="rbw_plate_edge" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#9aaab8"/>
      <stop offset="100%" stop-color="#566370"/>
    </linearGradient>
    <linearGradient id="rbw_strap" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#3b291d"/>
      <stop offset="45%" stop-color="#6b4b35"/>
      <stop offset="100%" stop-color="#2e1f15"/>
    </linearGradient>
    <linearGradient id="rbw_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d9be75"/>
      <stop offset="50%" stop-color="#9e7f3c"/>
      <stop offset="100%" stop-color="#5c461c"/>
    </linearGradient>
    <radialGradient id="rbw_lantern_glow" cx="50%" cy="52%" r="50%">
      <stop offset="0%" stop-color="#ffe7a8" stop-opacity="0.85"/>
      <stop offset="45%" stop-color="#d9a54c" stop-opacity="0.35"/>
      <stop offset="100%" stop-color="#a87628" stop-opacity="0"/>
    </radialGradient>
    <clipPath id="rbw_flap_clip">
      <path d="M 164,132 L 384,128 C 400,128 412,140 410,156 L 396,360 C 394,376 382,386 366,386 L 154,382 Z"/>
    </clipPath>
    <clipPath id="rbw_roll_clip">
      <rect x="92" y="106" width="112" height="296" rx="50"/>
    </clipPath>
  </defs>

  <g transform="rotate(-10 256 256)">
    <!-- Unrolled heavy ballistic fabric flap extending to the right -->
    <path d="M 164,132 L 384,128 C 400,128 412,140 410,156 L 396,360 C 394,376 382,386 366,386 L 154,382 Z"
          fill="url(#rbw_cloth_flap)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

    <!-- Woven Kevlar grid & interwoven armored plates inside flap -->
    <g clip-path="url(#rbw_flap_clip)">
      <!-- Subtle diagonal Kevlar cross-weave texture -->
      <g stroke="#1b2128" stroke-width="3" opacity="0.55">
        <line x1="150" y1="180" x2="220" y2="110"/>
        <line x1="150" y1="220" x2="260" y2="110"/>
        <line x1="150" y1="260" x2="300" y2="110"/>
        <line x1="150" y1="300" x2="340" y2="110"/>
        <line x1="150" y1="340" x2="380" y2="110"/>
        <line x1="150" y1="380" x2="420" y2="110"/>
        <line x1="185" y1="395" x2="425" y2="155"/>
        <line x1="225" y1="395" x2="425" y2="195"/>
        <line x1="265" y1="395" x2="425" y2="235"/>
        <line x1="305" y1="395" x2="425" y2="275"/>
        <line x1="345" y1="395" x2="425" y2="315"/>
      </g>
      <g stroke="#596775" stroke-width="2" opacity="0.28">
        <line x1="155" y1="125" x2="415" y2="385"/>
        <line x1="195" y1="125" x2="415" y2="345"/>
        <line x1="235" y1="125" x2="415" y2="305"/>
        <line x1="275" y1="125" x2="415" y2="265"/>
        <line x1="315" y1="125" x2="415" y2="225"/>
        <line x1="155" y1="165" x2="375" y2="385"/>
        <line x1="155" y1="205" x2="335" y2="385"/>
        <line x1="155" y1="245" x2="295" y2="385"/>
        <line x1="155" y1="285" x2="255" y2="385"/>
      </g>

      <!-- Interwoven ceramic/titanium ballistic armor plates stitched into fabric -->
      <g fill="url(#rbw_plate)" stroke="#121519" stroke-width="5.5" stroke-linejoin="round">
        <!-- Upper row of woven plates -->
        <polygon points="212,146 254,146 262,156 262,188 254,196 212,196 204,188 204,156"/>
        <polygon points="274,146 316,146 324,156 324,188 316,196 274,196 266,188 266,156"/>
        <polygon points="336,146 378,146 386,156 386,188 378,196 336,196 328,188 328,156"/>
        <!-- Lower row of woven plates -->
        <polygon points="208,324 250,324 258,334 258,364 250,372 208,372 200,364 200,334"/>
        <polygon points="270,324 312,324 320,334 320,364 312,372 270,372 262,364 262,334"/>
        <polygon points="332,324 374,324 382,334 382,364 374,372 332,372 324,364 324,334"/>
      </g>
      <!-- Metallic top-edge glints on plates -->
      <g stroke="url(#rbw_plate_edge)" stroke-width="2.5" stroke-linecap="round" opacity="0.7">
        <line x1="214" y1="154" x2="250" y2="154"/>
        <line x1="276" y1="154" x2="312" y2="154"/>
        <line x1="338" y1="154" x2="374" y2="154"/>
        <line x1="210" y1="332" x2="246" y2="332"/>
        <line x1="272" y1="332" x2="308" y2="332"/>
        <line x1="334" y1="332" x2="370" y2="332"/>
      </g>

      <!-- Heavy stitched edge trim along right & bottom border -->
      <path d="M 392,138 L 380,372" fill="none" stroke="#8596a6" stroke-width="3" stroke-dasharray="10 8" opacity="0.5"/>
      <path d="M 196,372 L 378,374" fill="none" stroke="#8596a6" stroke-width="3" stroke-dasharray="10 8" opacity="0.5"/>

      <!-- Shadow cast by the thick fabric roll onto the flap -->
      <rect x="188" y="124" width="24" height="268" fill="#121519" opacity="0.48"/>
    </g>

    <!-- Re-stroke flap perimeter cleanly -->
    <path d="M 164,132 L 384,128 C 400,128 412,140 410,156 L 396,360 C 394,376 382,386 366,386 L 154,382 Z"
          fill="none" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

    <!-- Railroad Lantern Emblem (фонарь-символ Подземки) stenciled on center of ballistic cloth -->
    <g transform="translate(298 258)">
      <!-- Subtle warm lantern glow -->
      <circle cx="0" cy="4" r="64" fill="url(#rbw_lantern_glow)"/>
      <!-- Chalk-white / aged stencil outer ring -->
      <circle cx="0" cy="2" r="56" fill="#20262e" fill-opacity="0.72" stroke="#d8cfc0" stroke-width="5.5" stroke-dasharray="28 7 18 6"/>
      <!-- Stylized Railroad Lantern -->
      <!-- Top carrying ring -->
      <circle cx="0" cy="-37" r="7.5" fill="none" stroke="#e8decb" stroke-width="4.5"/>
      <!-- Lantern cap / hood -->
      <path d="M -22,-22 Q 0,-34 22,-22 L 16,-15 L -16,-15 Z" fill="#e8decb" stroke="#121519" stroke-width="3" stroke-linejoin="round"/>
      <!-- Lantern side cage struts -->
      <path d="M -16,-15 C -26,-2 -26,18 -16,28 M 16,-15 C 26,-2 26,18 16,28"
            fill="none" stroke="#d8cfc0" stroke-width="4.5" stroke-linecap="round"/>
      <!-- Glowing lantern glass globe & inner flame -->
      <ellipse cx="0" cy="6" rx="15" ry="19" fill="#d9a84e" stroke="#121519" stroke-width="3"/>
      <path d="M 0,-6 C 7,4 8,14 0,20 C -8,14 -7,4 0,-6 Z" fill="#fff3cc"/>
      <!-- Lantern horizontal guard bands -->
      <line x1="-18" y1="6" x2="18" y2="6" stroke="#20262e" stroke-width="3.2"/>
      <!-- Lantern base pedestal -->
      <path d="M -20,26 L 20,26 L 25,36 L -25,36 Z" fill="#e8decb" stroke="#121519" stroke-width="3" stroke-linejoin="round"/>
      <!-- Subtle radiating light ticks -->
      <g stroke="#f0d28c" stroke-width="3.5" stroke-linecap="round" opacity="0.85">
        <line x1="-38" y1="6" x2="-30" y2="6"/>
        <line x1="30" y1="6" x2="38" y2="6"/>
        <line x1="-34" y1="-10" x2="-27" y2="-6"/>
        <line x1="27" y1="-6" x2="34" y2="-10"/>
        <line x1="-34" y1="22" x2="-27" y2="18"/>
        <line x1="27" y1="18" x2="34" y2="22"/>
      </g>
    </g>

    <!-- Main thick rolled cylinder of ballistic weave on the left -->
    <rect x="92" y="106" width="112" height="296" rx="50"
          fill="url(#rbw_cloth_roll)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

    <!-- Quilted armor rib seams wrapping around the fabric roll -->
    <g clip-path="url(#rbw_roll_clip)">
      <path d="M 92,188 Q 148,200 204,188" fill="none" stroke="#181d24" stroke-width="5"/>
      <path d="M 92,232 Q 148,244 204,232" fill="none" stroke="#181d24" stroke-width="5"/>
      <path d="M 92,318 Q 148,330 204,318" fill="none" stroke="#181d24" stroke-width="5"/>
      <path d="M 92,356 Q 148,368 204,356" fill="none" stroke="#181d24" stroke-width="5"/>
      <!-- Soft vertical metallic-weave sheen along left cylinder face -->
      <path d="M 122,164 L 122,380" stroke="#7d8f9e" stroke-width="10" stroke-linecap="round" opacity="0.28"/>
      <!-- Woven plate glint peeking on the roll -->
      <rect x="114" y="198" width="44" height="26" rx="5" fill="url(#rbw_plate)" stroke="#121519" stroke-width="4.5" opacity="0.85"/>
      <rect x="148" y="324" width="42" height="24" rx="5" fill="url(#rbw_plate)" stroke="#121519" stroke-width="4.5" opacity="0.85"/>
    </g>

    <!-- Top spiral end of rolled fabric showing layered cloth & metallic weave core -->
    <ellipse cx="148" cy="134" rx="44" ry="22" fill="url(#rbw_cloth_back)" stroke="#121519" stroke-width="9"/>
    <!-- Spiral concentric weave layers -->
    <path d="M 118,134 C 118,120 176,120 178,134 C 180,147 126,148 128,134 C 130,124 166,124 166,134 C 166,141 140,141 142,134"
          fill="none" stroke="#6f7f8f" stroke-width="4.5" stroke-linecap="round"/>
    <ellipse cx="148" cy="134" rx="8" ry="4" fill="#121519"/>

    <!-- Leather binding strap & brass buckle cinched around the roll -->
    <path d="M 86,262 Q 148,278 210,262 L 210,294 Q 148,310 86,294 Z"
          fill="url(#rbw_strap)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <!-- Brass buckle -->
    <rect x="130" y="264" width="38" height="40" rx="7"
          fill="none" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>
    <rect x="130" y="264" width="38" height="40" rx="7"
          fill="none" stroke="url(#rbw_brass)" stroke-width="7" stroke-linejoin="round"/>
    <line x1="146" y1="284" x2="168" y2="284" stroke="#d9be75" stroke-width="5" stroke-linecap="round"/>

    <!-- Crisp final cylinder border stroke -->
    <rect x="92" y="106" width="112" height="296" rx="50"
          fill="none" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  </g>
</svg>
```

`Art/Items/TMM_Ammo_ColtSilverBullet.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="csb_aura" cx="52%" cy="46%" r="44%">
      <stop offset="0%" stop-color="#c9ecff" stop-opacity="0.34"/>
      <stop offset="55%" stop-color="#7eb3d9" stop-opacity="0.12"/>
      <stop offset="100%" stop-color="#4e7899" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="csb_silver_v" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#606f7d"/>
      <stop offset="26%" stop-color="#d2e0eb"/>
      <stop offset="48%" stop-color="#f5fbff"/>
      <stop offset="74%" stop-color="#9aaab8"/>
      <stop offset="100%" stop-color="#4a5763"/>
    </linearGradient>
    <linearGradient id="csb_silver_h" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7c8c9c"/>
      <stop offset="28%" stop-color="#f2f9ff"/>
      <stop offset="56%" stop-color="#b5c6d4"/>
      <stop offset="85%" stop-color="#697987"/>
      <stop offset="100%" stop-color="#404c57"/>
    </linearGradient>
    <linearGradient id="csb_case_v" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#4f4534"/>
      <stop offset="28%" stop-color="#b5a382"/>
      <stop offset="50%" stop-color="#d9cbb0"/>
      <stop offset="76%" stop-color="#7d6e54"/>
      <stop offset="100%" stop-color="#3d3427"/>
    </linearGradient>
    <linearGradient id="csb_case_h" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#665944"/>
      <stop offset="30%" stop-color="#d9cbb0"/>
      <stop offset="58%" stop-color="#9e8e70"/>
      <stop offset="88%" stop-color="#594d3b"/>
      <stop offset="100%" stop-color="#362e22"/>
    </linearGradient>
    <linearGradient id="csb_copper" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d99a73"/>
      <stop offset="55%" stop-color="#9e6240"/>
      <stop offset="100%" stop-color="#59321e"/>
    </linearGradient>
    <radialGradient id="csb_glint" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="45%" stop-color="#b8e6ff" stop-opacity="0.45"/>
      <stop offset="100%" stop-color="#7abce6" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Subtle supernatural silver aura -->
  <circle cx="256" cy="248" r="196" fill="url(#csb_aura)"/>

  <!-- BULLET 1 (Back-left, tilted left) -->
  <g transform="translate(174 236) rotate(-22)">
    <!-- Outer silhouette base -->
    <path d="M 0,-138 C 20,-122 31,-90 31,-50 L 33,104 L 38,108 L 38,124 L -38,124 L -38,108 L -33,104 L -31,-50 C -31,-90 -20,-122 0,-138 Z"
          fill="#121519" stroke="#121519" stroke-width="14" stroke-linejoin="round"/>
    <!-- Pure silver ogive projectile -->
    <path d="M 0,-136 C 20,-120 30,-88 30,-48 L -30,-48 C -30,-88 -20,-120 0,-136 Z"
          fill="url(#csb_silver_v)"/>
    <!-- Engraved arcane band & pentacle filigree on silver bullet -->
    <g fill="none" stroke="#2c3742" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round">
      <path d="M -25,-64 Q 0,-56 25,-64"/>
      <path d="M -22,-80 Q 0,-72 22,-80"/>
      <polygon points="0,-106 8,-86 -10,-98 10,-98 -8,-86" stroke-width="2.4"/>
    </g>
    <!-- Antique silver-brass cartridge casing -->
    <path d="M -32,-48 L 32,-48 L 33,104 L -33,104 Z" fill="url(#csb_case_v)"/>
    <!-- Casing neck crimp & antique scrollwork -->
    <line x1="-32" y1="-48" x2="32" y2="-48" stroke="#121519" stroke-width="6"/>
    <path d="M -32,-34 Q 0,-28 32,-34" fill="none" stroke="#3d3427" stroke-width="3.5"/>
    <path d="M -18,10 C -8,-6 8,-6 18,10 M -18,46 C -8,62 8,62 18,46 M 0,-4 L 0,60"
          fill="none" stroke="#3d3427" stroke-width="3.2" stroke-linecap="round"/>
    <!-- Rimmed revolver base -->
    <rect x="-38" y="106" width="76" height="18" rx="4" fill="url(#csb_case_v)" stroke="#121519" stroke-width="6"/>
    <!-- Specular shine -->
    <path d="M -9,-112 C -14,-90 -15,-68 -15,-54" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.75"/>
    <!-- Crisp outer outline -->
    <path d="M 0,-138 C 20,-122 31,-90 31,-50 L 33,104 L 38,108 L 38,124 L -38,124 L -38,108 L -33,104 L -31,-50 C -31,-90 -20,-122 0,-138 Z"
          fill="none" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
  </g>

  <!-- BULLET 2 (Back-right, tilted right) -->
  <g transform="translate(334 238) rotate(24)">
    <path d="M 0,-138 C 20,-122 31,-90 31,-50 L 33,104 L 38,108 L 38,124 L -38,124 L -38,108 L -33,104 L -31,-50 C -31,-90 -20,-122 0,-138 Z"
          fill="#121519" stroke="#121519" stroke-width="14" stroke-linejoin="round"/>
    <path d="M 0,-136 C 20,-120 30,-88 30,-48 L -30,-48 C -30,-88 -20,-120 0,-136 Z"
          fill="url(#csb_silver_v)"/>
    <!-- Engraved ward lines on silver bullet -->
    <g fill="none" stroke="#2c3742" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round">
      <path d="M -25,-64 Q 0,-56 25,-64"/>
      <path d="M -18,-88 L 0,-108 L 18,-88 L 0,-68 Z"/>
      <circle cx="0" cy="-88" r="4" fill="#2c3742"/>
    </g>
    <path d="M -32,-48 L 32,-48 L 33,104 L -33,104 Z" fill="url(#csb_case_v)"/>
    <line x1="-32" y1="-48" x2="32" y2="-48" stroke="#121519" stroke-width="6"/>
    <path d="M -32,-34 Q 0,-28 32,-34" fill="none" stroke="#3d3427" stroke-width="3.5"/>
    <path d="M -16,0 Q 0,18 16,0 M -16,32 Q 0,50 16,32 M 0,-12 L 0,58"
          fill="none" stroke="#3d3427" stroke-width="3.2" stroke-linecap="round"/>
    <rect x="-38" y="106" width="76" height="18" rx="4" fill="url(#csb_case_v)" stroke="#121519" stroke-width="6"/>
    <path d="M -9,-112 C -14,-90 -15,-68 -15,-54" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.75"/>
    <path d="M 0,-138 C 20,-122 31,-90 31,-50 L 33,104 L 38,108 L 38,124 L -38,124 L -38,108 L -33,104 L -31,-50 C -31,-90 -20,-122 0,-138 Z"
          fill="none" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
  </g>

  <!-- BULLET 3 (Center-standing hero antique silver Colt round) -->
  <g transform="translate(254 218) rotate(-3)">
    <path d="M 0,-152 C 23,-134 36,-98 36,-54 L 38,112 L 44,117 L 44,135 L -44,135 L -44,117 L -38,112 L -36,-54 C -36,-98 -23,-134 0,-152 Z"
          fill="#121519" stroke="#121519" stroke-width="14" stroke-linejoin="round"/>
    <!-- Pure gleaming silver conical-ogive bullet -->
    <path d="M 0,-150 C 22,-132 35,-96 35,-52 L -35,-52 C -35,-96 -22,-132 0,-150 Z"
          fill="url(#csb_silver_v)"/>
    <!-- Detailed supernatural filigree & protective pentagram engraving on silver tip -->
    <g fill="none" stroke="#283440" stroke-width="3.6" stroke-linecap="round" stroke-linejoin="round">
      <!-- Double grease/engraving bands -->
      <path d="M -31,-68 Q 0,-59 31,-68"/>
      <path d="M -29,-80 Q 0,-71 29,-80"/>
      <!-- Engraved star/pentagram seal -->
      <circle cx="0" cy="-102" r="16" stroke-width="3"/>
      <polygon points="0,-116 9,-90 -13,-106 13,-106 -9,-90" stroke-width="2.6"/>
      <!-- Tiny decorative scrolls flanking band -->
      <path d="M -22,-66 C -16,-58 -8,-58 -4,-64 M 22,-66 C 16,-58 8,-58 4,-64" stroke-width="2.6"/>
    </g>
    <!-- Antique weathered silver-pewter/brass casing -->
    <path d="M -37,-52 L 37,-52 L 38,112 L -38,112 Z" fill="url(#csb_case_v)"/>
    <line x1="-37" y1="-52" x2="37" y2="-52" stroke="#121519" stroke-width="6.5"/>
    <!-- Casing antique decorative scrollwork -->
    <g fill="none" stroke="#3d3325" stroke-width="3.5" stroke-linecap="round">
      <path d="M -36,-36 Q 0,-28 36,-36"/>
      <path d="M -22,-14 C -8,-24 8,-24 22,-14"/>
      <path d="M -20,16 C -10,0 10,0 20,16 C 10,32 -10,32 -20,16 Z"/>
      <circle cx="0" cy="16" r="4" fill="#3d3325"/>
      <path d="M -22,48 C -8,38 8,38 22,48"/>
      <path d="M -36,88 Q 0,96 36,88"/>
    </g>
    <!-- Rimmed revolver base -->
    <rect x="-44" y="114" width="88" height="20" rx="5" fill="url(#csb_case_v)" stroke="#121519" stroke-width="6.5"/>
    <!-- Bright specular highlights -->
    <path d="M -11,-124 C -17,-100 -18,-76 -18,-58" fill="none" stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.82"/>
    <path d="M -16,-40 L -16,96" fill="none" stroke="#fff6e0" stroke-width="4" stroke-linecap="round" opacity="0.45"/>
    <!-- Crisp outer outline -->
    <path d="M 0,-152 C 23,-134 36,-98 36,-54 L 38,112 L 44,117 L 44,135 L -44,135 L -44,117 L -38,112 L -36,-54 C -36,-98 -23,-134 0,-152 Z"
          fill="none" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  </g>

  <!-- BULLET 4 (Foreground lying diagonally, showcasing rimmed primer base & engraved silver tip) -->
  <g transform="translate(260 364) rotate(-14)">
    <!-- Outer silhouette base -->
    <path d="M -138,-38 L -118,-38 L -112,-33 L 48,-31 C 92,-31 124,-20 144,0 C 124,20 92,31 48,31 L -112,33 L -118,38 L -138,38 Z"
          fill="#121519" stroke="#121519" stroke-width="14" stroke-linejoin="round"/>
    <!-- Casing body -->
    <path d="M -114,-32 L 48,-30 L 48,30 L -114,32 Z" fill="url(#csb_case_h)"/>
    <!-- Engraved filigree on horizontal casing -->
    <g fill="none" stroke="#3d3325" stroke-width="3.2" stroke-linecap="round">
      <path d="M 32,-30 Q 24,0 32,30"/>
      <path d="M -84,-30 Q -76,0 -84,30"/>
      <path d="M -60,-14 C -40,-22 -20,-6 0,-14 M -60,14 C -40,22 -20,6 0,14"/>
      <circle cx="-30" cy="0" r="8"/>
    </g>
    <!-- Pure silver engraved tip pointing right -->
    <path d="M 48,-30 C 90,-30 122,-19 142,0 C 122,19 90,30 48,30 Z" fill="url(#csb_silver_h)"/>
    <line x1="48" y1="-31" x2="48" y2="31" stroke="#121519" stroke-width="6"/>
    <!-- Engraving on silver projectile -->
    <g fill="none" stroke="#283440" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round">
      <path d="M 64,-26 Q 57,0 64,26"/>
      <path d="M 76,-24 Q 70,0 76,24"/>
      <polygon points="114,0 92,-12 82,0 92,12" stroke-width="2.8"/>
      <line x1="88" y1="0" x2="108" y2="0" stroke-width="2.5"/>
    </g>
    <!-- Rimmed base & copper percussion primer on the left -->
    <rect x="-138" y="-38" width="22" height="76" rx="5" fill="url(#csb_case_h)" stroke="#121519" stroke-width="6"/>
    <ellipse cx="-136" cy="0" rx="13" ry="34" fill="url(#csb_case_h)" stroke="#121519" stroke-width="6.5"/>
    <ellipse cx="-136" cy="0" rx="6.5" ry="15" fill="url(#csb_copper)" stroke="#121519" stroke-width="3.5"/>
    <!-- Specular shine along top edge -->
    <path d="M -98,-14 L 34,-14" stroke="#fff6e0" stroke-width="4" stroke-linecap="round" opacity="0.5"/>
    <path d="M 56,-14 C 82,-14 104,-9 120,-3" fill="none" stroke="#ffffff" stroke-width="4.5" stroke-linecap="round" opacity="0.85"/>
    <!-- Crisp outer outline -->
    <path d="M -138,-38 L -118,-38 L -112,-33 L 48,-31 C 92,-31 124,-20 144,0 C 124,20 92,31 48,31 L -112,33 L -118,38 L -138,38 Z"
          fill="none" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
  </g>

  <!-- Crisp silver star glints -->
  <circle cx="246" cy="84" r="24" fill="url(#csb_glint)"/>
  <path d="M 246,66 L 249,80 L 263,84 L 249,88 L 246,102 L 243,88 L 229,84 L 243,80 Z" fill="#ffffff" opacity="0.92"/>
  <path d="M 382,326 L 384,334 L 392,336 L 384,338 L 382,346 L 380,338 L 372,336 L 380,334 Z" fill="#e6f7ff" opacity="0.85"/>
</svg>
```

`Art/Items/TMM_Backpack_Canvas.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="bpc_canvas_main" x1="15%" y1="0%" x2="85%" y2="100%">
      <stop offset="0%" stop-color="#a89472"/>
      <stop offset="48%" stop-color="#877353"/>
      <stop offset="100%" stop-color="#594a34"/>
    </linearGradient>
    <linearGradient id="bpc_canvas_flap" x1="20%" y1="0%" x2="80%" y2="100%">
      <stop offset="0%" stop-color="#baa682"/>
      <stop offset="52%" stop-color="#96815f"/>
      <stop offset="100%" stop-color="#69583e"/>
    </linearGradient>
    <linearGradient id="bpc_canvas_side" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7d6a4c"/>
      <stop offset="100%" stop-color="#4d3f2b"/>
    </linearGradient>
    <linearGradient id="bpc_canvas_pocket" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#9c8765"/>
      <stop offset="55%" stop-color="#7d694b"/>
      <stop offset="100%" stop-color="#544530"/>
    </linearGradient>
    <linearGradient id="bpc_patch_olive" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#6e7357"/>
      <stop offset="100%" stop-color="#484d39"/>
    </linearGradient>
    <linearGradient id="bpc_patch_denim" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#66717a"/>
      <stop offset="100%" stop-color="#424b52"/>
    </linearGradient>
    <linearGradient id="bpc_patch_tan" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#b59872"/>
      <stop offset="100%" stop-color="#7d6447"/>
    </linearGradient>
    <linearGradient id="bpc_leather" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#3d281b"/>
      <stop offset="35%" stop-color="#6e4a32"/>
      <stop offset="70%" stop-color="#523522"/>
      <stop offset="100%" stop-color="#2e1d13"/>
    </linearGradient>
    <linearGradient id="bpc_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#dec37a"/>
      <stop offset="50%" stop-color="#a3843e"/>
      <stop offset="100%" stop-color="#5e491d"/>
    </linearGradient>
    <linearGradient id="bpc_scuff" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#2b2217" stop-opacity="0"/>
      <stop offset="100%" stop-color="#2b2217" stop-opacity="0.45"/>
    </linearGradient>
  </defs>

  <!-- Top canvas carry handle loop -->
  <path d="M 218,128 C 218,76 294,76 294,128"
        fill="none" stroke="#121519" stroke-width="30" stroke-linecap="round"/>
  <path d="M 218,128 C 218,76 294,76 294,128"
        fill="none" stroke="url(#bpc_leather)" stroke-width="12" stroke-linecap="round"/>

  <!-- Shoulder strap loops peeking out from top shoulders -->
  <path d="M 148,144 C 122,146 112,172 118,208"
        fill="none" stroke="#121519" stroke-width="26" stroke-linecap="round"/>
  <path d="M 148,144 C 122,146 112,172 118,208"
        fill="none" stroke="url(#bpc_leather)" stroke-width="10" stroke-linecap="round"/>
  <path d="M 364,144 C 390,146 400,172 394,208"
        fill="none" stroke="#121519" stroke-width="26" stroke-linecap="round"/>
  <path d="M 364,144 C 390,146 400,172 394,208"
        fill="none" stroke="url(#bpc_leather)" stroke-width="10" stroke-linecap="round"/>

  <!-- Left & right bulging side utility pouches -->
  <path d="M 134,242 L 106,250 C 94,254 88,266 90,280 L 96,368 C 98,382 108,390 122,390 L 146,386 Z"
        fill="url(#bpc_canvas_side)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  <path d="M 378,242 L 406,250 C 418,254 424,266 422,280 L 416,368 C 414,382 404,390 390,390 L 366,386 Z"
        fill="url(#bpc_canvas_side)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  <!-- Side pouch cinch lines -->
  <path d="M 94,282 Q 112,288 134,282" fill="none" stroke="#121519" stroke-width="6"/>
  <path d="M 378,282 Q 400,288 418,282" fill="none" stroke="#121519" stroke-width="6"/>

  <!-- Main rugged canvas backpack sack body -->
  <path d="M 152,132
           C 192,116 320,116 360,132
           C 380,142 388,166 388,196
           L 392,374
           C 392,406 366,422 332,424
           L 180,424
           C 146,422 120,406 120,374
           L 124,196
           C 124,166 132,142 152,132 Z"
        fill="url(#bpc_canvas_main)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

  <!-- Bottom grime & wear gradient -->
  <path d="M 126,340 L 386,340 L 386,374 C 386,402 362,417 330,418 L 182,418 C 150,417 126,402 126,374 Z"
        fill="url(#bpc_scuff)"/>

  <!-- Canvas fabric fold & crease lines -->
  <path d="M 142,248 Q 160,262 154,288 M 370,248 Q 352,262 358,288"
        fill="none" stroke="#3d3222" stroke-width="4.5" stroke-linecap="round" opacity="0.65"/>

  <!-- Sewn patch #1 on right side of main body (muted tan canvas patch) -->
  <g transform="translate(342 292) rotate(7)">
    <polygon points="-24,-22 24,-20 22,22 -22,24"
             fill="url(#bpc_patch_tan)" stroke="#121519" stroke-width="5.5" stroke-linejoin="round"/>
    <g stroke="#292117" stroke-width="3" stroke-linecap="round">
      <line x1="-28" y1="-10" x2="-18" y2="-10"/>
      <line x1="-28" y1="8" x2="-18" y2="8"/>
      <line x1="18" y1="-10" x2="28" y2="-10"/>
      <line x1="18" y1="8" x2="28" y2="8"/>
      <line x1="-8" y1="-26" x2="-8" y2="-16"/>
      <line x1="10" y1="-25" x2="10" y2="-15"/>
    </g>
  </g>

  <!-- Large front canvas cargo pocket -->
  <path d="M 164,288 L 348,288 C 358,288 364,296 364,306 L 360,384 C 358,396 348,404 334,404 L 178,404 C 164,404 154,396 152,384 L 148,306 C 148,296 154,288 164,288 Z"
        fill="url(#bpc_canvas_pocket)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
  <!-- Front pocket fold-over lip & central snap button -->
  <path d="M 148,308 Q 256,326 364,308" fill="none" stroke="#121519" stroke-width="7"/>
  <circle cx="256" cy="316" r="8" fill="url(#bpc_brass)" stroke="#121519" stroke-width="4"/>

  <!-- Sewn patch #2 on front pocket (worn slate-denim patch with visible hand stitches) -->
  <g transform="translate(212 362) rotate(-5)">
    <polygon points="-38,-22 36,-24 40,22 -34,24"
             fill="url(#bpc_patch_denim)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
    <!-- Subtle worn weave crosshatch on patch -->
    <path d="M -24,-10 L 24,-10 M -24,6 L 24,6" stroke="#85929c" stroke-width="2" opacity="0.35"/>
    <!-- Hand-sewn rough whip stitches around patch edges -->
    <g stroke="#d6ccb8" stroke-width="3.2" stroke-linecap="round">
      <line x1="-26" y1="-27" x2="-24" y2="-17"/>
      <line x1="-8" y1="-28" x2="-8" y2="-18"/>
      <line x1="10" y1="-28" x2="10" y2="-18"/>
      <line x1="26" y1="-27" x2="25" y2="-17"/>
      <line x1="-22" y1="18" x2="-24" y2="28"/>
      <line x1="-2" y1="18" x2="-2" y2="28"/>
      <line x1="18" y1="18" x2="20" y2="28"/>
      <line x1="-41" y1="-6" x2="-31" y2="-6"/>
      <line x1="-40" y1="10" x2="-30" y2="10"/>
      <line x1="33" y1="-6" x2="43" y2="-6"/>
      <line x1="34" y1="10" x2="44" y2="10"/>
    </g>
  </g>

  <!-- Small stitched tear/mend on right side of front pocket -->
  <g stroke="#292117" stroke-width="4" stroke-linecap="round">
    <path d="M 304,352 L 334,370"/>
    <line x1="308" y1="364" x2="316" y2="348"/>
    <line x1="316" y1="369" x2="324" y2="353"/>
    <line x1="324" y1="374" x2="332" y2="358"/>
  </g>

  <!-- Fold-over top canvas lid flap -->
  <path d="M 140,148
           C 178,116 334,116 372,148
           C 384,162 390,192 386,226
           C 384,242 370,252 350,256
           Q 256,274 162,256
           C 142,252 128,242 126,226
           C 122,192 128,162 140,148 Z"
        fill="url(#bpc_canvas_flap)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

  <!-- Subtle canvas top highlight & edge stitching on flap -->
  <path d="M 164,142 Q 256,126 348,142" fill="none" stroke="#d4c3a3" stroke-width="5" stroke-linecap="round" opacity="0.5"/>
  <path d="M 146,238 Q 256,258 366,238" fill="none" stroke="#473a27" stroke-width="3.5" stroke-dasharray="10 7" stroke-linecap="round" opacity="0.7"/>

  <!-- Sewn patch #3 on top lid flap (weathered olive patch with cross stitches) -->
  <g transform="translate(274 186) rotate(4)">
    <polygon points="-36,-22 38,-18 34,24 -38,22"
             fill="url(#bpc_patch_olive)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
    <g stroke="#d6ccb8" stroke-width="3.2" stroke-linecap="round">
      <line x1="-22" y1="-26" x2="-22" y2="-16"/>
      <line x1="0" y1="-25" x2="0" y2="-15"/>
      <line x1="22" y1="-23" x2="22" y2="-13"/>
      <line x1="-24" y1="16" x2="-24" y2="26"/>
      <line x1="-2" y1="17" x2="-2" y2="27"/>
      <line x1="20" y1="18" x2="20" y2="28"/>
      <line x1="-42" y1="0" x2="-32" y2="0"/>
      <line x1="30" y1="2" x2="40" y2="2"/>
    </g>
  </g>

  <!-- Twin worn leather closure straps & brass buckles -->
  <!-- Left strap -->
  <rect x="172" y="132" width="32" height="170" rx="7"
        fill="url(#bpc_leather)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
  <!-- Left brass buckle -->
  <rect x="166" y="232" width="44" height="36" rx="7"
        fill="none" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>
  <rect x="166" y="232" width="44" height="36" rx="7"
        fill="none" stroke="url(#bpc_brass)" stroke-width="7" stroke-linejoin="round"/>
  <line x1="188" y1="234" x2="188" y2="254" stroke="#dec37a" stroke-width="5" stroke-linecap="round"/>
  <!-- Strap eyelets -->
  <circle cx="188" cy="278" r="3" fill="#121519"/>
  <circle cx="188" cy="291" r="3" fill="#121519"/>

  <!-- Right strap -->
  <rect x="308" y="132" width="32" height="170" rx="7"
        fill="url(#bpc_leather)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
  <!-- Right brass buckle -->
  <rect x="302" y="232" width="44" height="36" rx="7"
        fill="none" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>
  <rect x="302" y="232" width="44" height="36" rx="7"
        fill="none" stroke="url(#bpc_brass)" stroke-width="7" stroke-linejoin="round"/>
  <line x1="324" y1="234" x2="324" y2="254" stroke="#dec37a" stroke-width="5" stroke-linecap="round"/>
  <!-- Strap eyelets -->
  <circle cx="324" cy="278" r="3" fill="#121519"/>
  <circle cx="324" cy="291" r="3" fill="#121519"/>

  <!-- Leather reinforcement corners at bottom of rucksack -->
  <path d="M 122,374 C 122,404 146,422 178,424 L 178,396 L 122,366 Z"
        fill="url(#bpc_leather)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>
  <path d="M 390,374 C 390,404 366,422 334,424 L 334,396 L 390,366 Z"
        fill="url(#bpc_leather)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>
</svg>
```