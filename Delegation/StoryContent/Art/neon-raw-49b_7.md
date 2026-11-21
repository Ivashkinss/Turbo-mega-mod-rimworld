`Art/Projectiles/TMM_Bullet_RailwayRifle.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="rrs_heat_aura" cx="50%" cy="32%" r="48%">
      <stop offset="0%" stop-color="#ffb85c" stop-opacity="0.46"/>
      <stop offset="48%" stop-color="#d94e1f" stop-opacity="0.2"/>
      <stop offset="100%" stop-color="#6e1909" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="rrs_steam_trail" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffaa52" stop-opacity="0.62"/>
      <stop offset="32%" stop-color="#c46e47" stop-opacity="0.34"/>
      <stop offset="68%" stop-color="#7c8791" stop-opacity="0.22"/>
      <stop offset="100%" stop-color="#4e5861" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="rrs_shaft_hot" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fffce0"/>
      <stop offset="18%" stop-color="#ffd566"/>
      <stop offset="44%" stop-color="#eb6d24"/>
      <stop offset="72%" stop-color="#9e2a1b"/>
      <stop offset="90%" stop-color="#4d2624"/>
      <stop offset="100%" stop-color="#2c2426"/>
    </linearGradient>
    <linearGradient id="rrs_bevel_light" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="28%" stop-color="#ffe794"/>
      <stop offset="62%" stop-color="#ff8c38"/>
      <stop offset="100%" stop-color="#6b342b"/>
    </linearGradient>
    <linearGradient id="rrs_bevel_dark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffd470"/>
      <stop offset="35%" stop-color="#c94b1c"/>
      <stop offset="74%" stop-color="#5e1d18"/>
      <stop offset="100%" stop-color="#1e1b1f"/>
    </linearGradient>
    <linearGradient id="rrs_head_iron" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#873c2b"/>
      <stop offset="45%" stop-color="#4f3533"/>
      <stop offset="100%" stop-color="#252226"/>
    </linearGradient>
    <radialGradient id="rrs_tip_glow" cx="50%" cy="48%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="42%" stop-color="#ffdc7a" stop-opacity="0.58"/>
      <stop offset="100%" stop-color="#e8581c" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Molten heat radiance envelope -->
  <ellipse cx="256" cy="212" rx="80" ry="164" fill="url(#rrs_heat_aura)"/>

  <!-- Pressurized steam & superheated exhaust wake trailing South -->
  <path d="M 218,286
           C 202,336 220,388 256,440
           C 292,388 310,336 294,286 Z"
        fill="url(#rrs_steam_trail)"/>

  <!-- Swirling steam & heat shimmer streaks behind the spike -->
  <path d="M 226,322 C 212,354 244,380 238,420"
        fill="none" stroke="#b6c2cc" stroke-width="5" stroke-linecap="round" opacity="0.45"/>
  <path d="M 284,322 C 296,356 266,384 274,424"
        fill="none" stroke="#ffb366" stroke-width="4.5" stroke-linecap="round" opacity="0.5"/>
  <path d="M 256,324 L 256,418"
        fill="none" stroke="#ffcf80" stroke-width="4" stroke-linecap="round" opacity="0.52"/>

  <!-- Forged iron railroad spike main silhouette (pointing North, offset flanged head at South) -->
  <!-- Offset hooked spike head (characteristic railroad spike anvil lip) -->
  <path d="M 222,290
           L 290,290
           L 304,302
           C 309,308 308,320 298,324
           L 222,324
           C 212,324 206,316 208,306
           L 214,296 Z"
        fill="url(#rrs_head_iron)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>
  <!-- Highlight & forging crease on spike head -->
  <path d="M 218,306 L 296,306" fill="none" stroke="#994b36" stroke-width="3.5" stroke-linecap="round" opacity="0.72"/>
  <path d="M 218,316 L 294,316" fill="none" stroke="#181519" stroke-width="3" stroke-linecap="round" opacity="0.7"/>

  <!-- Square-forged tapered railroad spike shaft glowing white-hot toward the chisel tip -->
  <path d="M 256,74
           L 272,114
           L 276,294
           L 236,294
           L 240,114 Z"
        fill="url(#rrs_shaft_hot)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Faceted 3D forged bevels on square spike shank -->
  <!-- Left bright forged facet -->
  <polygon points="256,82 245,116 242,288 256,288" fill="url(#rrs_bevel_light)" opacity="0.72"/>
  <!-- Right shaded forged facet -->
  <polygon points="256,82 267,116 270,288 256,288" fill="url(#rrs_bevel_dark)" opacity="0.62"/>

  <!-- Chisel wedge flat face near tip -->
  <polygon points="256,78 268,116 256,128 244,116" fill="#fffbe6" opacity="0.88"/>

  <!-- Central square-bar ridge line & forged hammer dents -->
  <line x1="256" y1="84" x2="256" y2="288" stroke="#fff3c4" stroke-width="3" stroke-linecap="round" opacity="0.78"/>
  <path d="M 241,210 L 252,214 M 261,246 L 272,243 M 240,264 L 250,267"
        stroke="#2b1414" stroke-width="3" stroke-linecap="round" opacity="0.65"/>

  <!-- Incandescent white-yellow glow flare at the molten leading tip -->
  <circle cx="256" cy="94" r="28" fill="url(#rrs_tip_glow)"/>
  <path d="M 256,64 L 260,84 L 278,88 L 260,92 L 256,112 L 252,92 L 234,88 L 252,84 Z"
        fill="#fffbe0" opacity="0.86"/>

  <!-- Flying molten forge sparks & embers -->
  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <polygon points="214,226 220,236 214,246 208,236" fill="#ffd166"/>
    <polygon points="298,202 303,212 298,222 293,212" fill="#ff9f43"/>
    <circle cx="218" cy="356" r="5.5" fill="#ffba52"/>
    <circle cx="294" cy="348" r="5" fill="#ff8438"/>
    <circle cx="240" cy="396" r="4.5" fill="#ffe18c"/>
    <circle cx="272" cy="414" r="4" fill="#ff9138"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Projectile_BigBoy.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="bbn_aura" cx="50%" cy="46%" r="50%">
      <stop offset="0%" stop-color="#f5c94c" stop-opacity="0.32"/>
      <stop offset="52%" stop-color="#b87e1d" stop-opacity="0.14"/>
      <stop offset="100%" stop-color="#5e3a0a" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="bbn_yellow_hull" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#6e5314"/>
      <stop offset="22%" stop-color="#d9ae38"/>
      <stop offset="44%" stop-color="#f7dc74"/>
      <stop offset="72%" stop-color="#b88b23"/>
      <stop offset="100%" stop-color="#4f390c"/>
    </linearGradient>
    <linearGradient id="bbn_black_metal" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#191d22"/>
      <stop offset="28%" stop-color="#48525c"/>
      <stop offset="50%" stop-color="#606d7a"/>
      <stop offset="78%" stop-color="#2c333b"/>
      <stop offset="100%" stop-color="#14181c"/>
    </linearGradient>
    <linearGradient id="bbn_fin_steel" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#58636e"/>
      <stop offset="55%" stop-color="#333b42"/>
      <stop offset="100%" stop-color="#1a1f24"/>
    </linearGradient>
    <linearGradient id="bbn_exhaust_outer" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffd76b" stop-opacity="0.82"/>
      <stop offset="38%" stop-color="#d96e26" stop-opacity="0.48"/>
      <stop offset="76%" stop-color="#783016" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#3d170b" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="bbn_exhaust_core" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.94"/>
      <stop offset="40%" stop-color="#ffe894" stop-opacity="0.7"/>
      <stop offset="100%" stop-color="#e87a24" stop-opacity="0"/>
    </linearGradient>
    <clipPath id="bbn_warhead_clip">
      <path d="M 256,74
               C 294,88 318,142 316,198
               C 314,250 294,284 276,304
               L 236,304
               C 218,284 198,250 196,198
               C 194,142 218,88 256,74 Z"/>
    </clipPath>
    <clipPath id="bbn_haz_band">
      <rect x="190" y="218" width="132" height="28"/>
    </clipPath>
  </defs>

  <!-- Subtle warm radioactive-amber ambient glow -->
  <ellipse cx="256" cy="242" rx="82" ry="184" fill="url(#bbn_aura)"/>

  <!-- Propellant smoke & fiery exhaust wake streaming South from tail nozzle -->
  <path d="M 226,348
           C 212,378 228,414 256,444
           C 284,414 300,378 286,348 Z"
        fill="url(#bbn_exhaust_outer)"/>
  <path d="M 240,348
           C 236,374 244,398 256,422
           C 268,398 276,374 272,348 Z"
        fill="url(#bbn_exhaust_core)"/>

  <!-- Stabilizer fins (left, right, and center dorsal fin) -->
  <g stroke="#121519" stroke-width="7" stroke-linejoin="round">
    <!-- Left angled stabilizer fin -->
    <polygon points="236,294 194,322 194,362 232,348" fill="url(#bbn_fin_steel)"/>
    <polygon points="230,302 201,324 201,340 228,326" fill="url(#bbn_yellow_hull)" stroke-width="4.5"/>

    <!-- Right angled stabilizer fin -->
    <polygon points="276,294 318,322 318,362 280,348" fill="url(#bbn_fin_steel)"/>
    <polygon points="282,302 311,324 311,340 284,326" fill="url(#bbn_yellow_hull)" stroke-width="4.5"/>

    <!-- Tail exhaust cone / booster shroud -->
    <path d="M 234,298 L 278,298 L 284,352 L 228,352 Z" fill="url(#bbn_black_metal)"/>

    <!-- Circular rear box-tail stabilizer ring connecting the fins -->
    <rect x="192" y="336" width="128" height="22" rx="5" fill="url(#bbn_black_metal)"/>
    <line x1="202" y1="347" x2="310" y2="347" stroke="#d9ae38" stroke-width="4" stroke-linecap="round"/>

    <!-- Center vertical dorsal fin -->
    <rect x="250" y="294" width="12" height="66" rx="4" fill="url(#bbn_fin_steel)" stroke-width="6"/>
  </g>

  <!-- Main football-shaped mini-nuke warhead capsule (yellow & black) -->
  <path d="M 256,74
           C 294,88 318,142 316,198
           C 314,250 294,284 276,304
           L 236,304
           C 218,284 198,250 196,198
           C 194,142 218,88 256,74 Z"
        fill="url(#bbn_yellow_hull)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Dark armored nose cap, black lower hemisphere & hazard stripe band clipped to warhead -->
  <g clip-path="url(#bbn_warhead_clip)">
    <!-- Dark matte steel contact-fuse nose tip -->
    <path d="M 218,70 L 294,70 L 294,108 Q 256,118 218,108 Z" fill="url(#bbn_black_metal)"/>

    <!-- Lower dark black-steel casing section -->
    <path d="M 190,244 L 322,244 L 322,310 L 190,310 Z" fill="url(#bbn_black_metal)"/>

    <!-- Diagonal yellow-black hazard warning band across lower waist -->
    <g clip-path="url(#bbn_haz_band)">
      <rect x="190" y="218" width="132" height="28" fill="url(#bbn_yellow_hull)"/>
      <g stroke="#181c21" stroke-width="14">
        <line x1="200" y1="250" x2="218" y2="214"/>
        <line x1="230" y1="250" x2="248" y2="214"/>
        <line x1="260" y1="250" x2="278" y2="214"/>
        <line x1="290" y1="250" x2="308" y2="214"/>
      </g>
    </g>

    <!-- Heavy machined dark-steel equatorial seam rings -->
    <line x1="192" y1="108" x2="320" y2="108" stroke="#121519" stroke-width="6"/>
    <line x1="192" y1="218" x2="320" y2="218" stroke="#121519" stroke-width="6"/>
    <line x1="192" y1="246" x2="320" y2="246" stroke="#121519" stroke-width="6"/>

    <!-- Metallic curved highlight along left flank of warhead -->
    <path d="M 224,116 C 212,150 212,188 220,214"
          fill="none" stroke="#fff6cf" stroke-width="7" stroke-linecap="round" opacity="0.52"/>
  </g>

  <!-- Stylized radiation trefoil warning emblem on yellow warhead belly (no text) -->
  <g transform="translate(256 164)">
    <circle cx="0" cy="0" r="28" fill="none" stroke="#181c21" stroke-width="4" opacity="0.88"/>
    <circle cx="0" cy="0" r="6" fill="#181c21"/>
    <path d="M -9,-4 L -20,-14 A 22,22 0 0 1 -4,-22 Z" fill="#181c21"/>
    <path d="M 9,-4 L 20,-14 A 22,22 0 0 0 4,-22 Z" fill="#181c21"/>
    <path d="M -5,9 L 5,9 L 11,20 A 22,22 0 0 1 -11,20 Z" fill="#181c21"/>
  </g>

  <!-- Top protruding impact detonator pin -->
  <path d="M 248,76 L 252,60 L 260,60 L 264,76 Z" fill="url(#bbn_black_metal)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>

  <!-- Re-stroke crisp outer warhead perimeter -->
  <path d="M 256,74
           C 294,88 318,142 316,198
           C 314,250 294,284 276,304
           L 236,304
           C 218,284 198,250 196,198
           C 194,142 218,88 256,74 Z"
        fill="none" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Rivet bolts along lower black armor housing -->
  <g fill="#7d8a96" stroke="#121519" stroke-width="3">
    <circle cx="222" cy="264" r="4"/>
    <circle cx="256" cy="268" r="4"/>
    <circle cx="290" cy="264" r="4"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_TheEqualizer.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="teq_quantum_aura" cx="50%" cy="38%" r="50%">
      <stop offset="0%" stop-color="#d4f4ff" stop-opacity="0.5"/>
      <stop offset="44%" stop-color="#4aa8ff" stop-opacity="0.24"/>
      <stop offset="100%" stop-color="#17448a" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="teq_wake_outer" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#dff8ff" stop-opacity="0.82"/>
      <stop offset="35%" stop-color="#4fb3ff" stop-opacity="0.48"/>
      <stop offset="72%" stop-color="#2165c4" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#0f2c66" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="teq_wake_core" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.96"/>
      <stop offset="38%" stop-color="#c9f1ff" stop-opacity="0.72"/>
      <stop offset="76%" stop-color="#4fa8ff" stop-opacity="0.28"/>
      <stop offset="100%" stop-color="#1d54b0" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="teq_bullet_blue" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#2057a8"/>
      <stop offset="26%" stop-color="#78ccff"/>
      <stop offset="50%" stop-color="#ffffff"/>
      <stop offset="74%" stop-color="#61bdff"/>
      <stop offset="100%" stop-color="#1a478f"/>
    </linearGradient>
    <linearGradient id="teq_bullet_tip" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="48%" stop-color="#e3f7ff"/>
      <stop offset="100%" stop-color="#66c4ff"/>
    </linearGradient>
    <radialGradient id="teq_shimmer_flare" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.98"/>
      <stop offset="38%" stop-color="#9ee2ff" stop-opacity="0.58"/>
      <stop offset="100%" stop-color="#2b82e6" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Wide quantum blue-white radiance envelope -->
  <ellipse cx="256" cy="236" rx="84" ry="184" fill="url(#teq_quantum_aura)"/>

  <!-- Quantum phase-slip stream tapering South -->
  <path d="M 218,188
           C 206,258 224,352 256,442
           C 288,352 306,258 294,188 Z"
        fill="url(#teq_wake_outer)"/>
  <path d="M 238,192
           C 234,254 242,330 256,412
           C 270,330 278,254 274,192 Z"
        fill="url(#teq_wake_core)"/>

  <!-- Shimmering phase-shifted ghost afterimages flanking the main bullet -->
  <path d="M 232,102 C 216,120 206,148 206,184 L 206,204 L 222,204 L 222,158 Z"
        fill="#7ad4ff" opacity="0.46"/>
  <path d="M 280,102 C 296,120 306,148 306,184 L 306,204 L 290,204 L 290,158 Z"
        fill="#7ad4ff" opacity="0.46"/>

  <!-- Quantum harmonic wave rings & double-helix energy filaments -->
  <path d="M 222,204 C 198,252 302,286 276,348 C 266,374 260,398 256,426"
        fill="none" stroke="#9ce6ff" stroke-width="4.5" stroke-linecap="round" opacity="0.78"/>
  <path d="M 290,204 C 314,252 210,286 236,348 C 246,374 252,398 256,426"
        fill="none" stroke="#ffffff" stroke-width="3.5" stroke-linecap="round" opacity="0.72"/>

  <!-- Trailing quantum shimmer ellipses -->
  <ellipse cx="256" cy="236" rx="54" ry="12" fill="none" stroke="#121519" stroke-width="5.5" opacity="0.58"/>
  <ellipse cx="256" cy="236" rx="54" ry="12" fill="none" stroke="#ccf4ff" stroke-width="3"/>
  <ellipse cx="256" cy="292" rx="40" ry="9" fill="none" stroke="#75d0ff" stroke-width="3" opacity="0.78"/>
  <ellipse cx="256" cy="348" rx="26" ry="6" fill="none" stroke="#ffffff" stroke-width="2.5" opacity="0.72"/>

  <!-- Leading crystalline white-blue quantum bullet silhouette (pointing North) -->
  <path d="M 256,72
           C 276,92 288,124 288,160
           L 288,200
           L 272,216
           L 240,216
           L 224,200
           L 224,160
           C 224,124 236,92 256,72 Z"
        fill="url(#teq_bullet_blue)" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>

  <!-- Inner luminous white quantum core diamond faceted along the bullet -->
  <polygon points="256,84 274,148 256,208 238,148" fill="url(#teq_bullet_tip)" opacity="0.94"/>
  <polygon points="256,96 265,148 256,196 247,148" fill="#ffffff"/>

  <!-- Crisp arcane-tech containment bands across the blue-white casing -->
  <path d="M 227,174 Q 256,184 285,174" fill="none" stroke="#184791" stroke-width="4" stroke-linecap="round"/>
  <path d="M 227,190 Q 256,200 285,190" fill="none" stroke="#184791" stroke-width="3.5" stroke-linecap="round"/>
  <path d="M 232,171 Q 256,179 280,171" fill="none" stroke="#ffffff" stroke-width="2" stroke-linecap="round" opacity="0.88"/>

  <!-- Intense shimmering star flare at leading nose & phase points -->
  <circle cx="256" cy="86" r="30" fill="url(#teq_shimmer_flare)"/>
  <path d="M 256,56 L 261,80 L 288,86 L 261,92 L 256,116 L 251,92 L 224,86 L 251,80 Z"
        fill="#ffffff" opacity="0.95"/>

  <!-- Shimmering quantum diamond motes & flickering 4-point sparkles -->
  <path d="M 206,154 L 209,165 L 220,168 L 209,171 L 206,182 L 203,171 L 192,168 L 203,165 Z"
        fill="#e6faff" opacity="0.92"/>
  <path d="M 306,180 L 309,189 L 318,192 L 309,195 L 306,204 L 303,195 L 294,192 L 303,189 Z"
        fill="#ffffff" opacity="0.92"/>
  <path d="M 218,318 L 220,326 L 228,328 L 220,330 L 218,338 L 216,330 L 208,328 L 216,326 Z"
        fill="#b8ecff" opacity="0.88"/>
  <path d="M 294,344 L 296,352 L 304,354 L 296,356 L 294,364 L 292,356 L 284,354 L 292,352 Z"
        fill="#ffffff" opacity="0.88"/>

  <!-- Outlined trailing quantum crystal shards -->
  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <polygon points="228,266 234,276 228,286 222,276" fill="#e6faff"/>
    <polygon points="284,290 290,300 284,310 278,300" fill="#8ad8ff"/>
    <polygon points="256,372 263,384 256,396 249,384" fill="#ffffff"/>
    <circle cx="238" cy="404" r="4.5" fill="#99e1ff"/>
    <circle cx="274" cy="414" r="4" fill="#ffffff"/>
  </g>
</svg>
```