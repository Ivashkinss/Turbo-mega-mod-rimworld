`Art/Projectiles/TMM_Bullet_Experiment18A.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="e18_aura_outer" cx="50%" cy="38%" r="50%">
      <stop offset="0%" stop-color="#6dff9e" stop-opacity="0.42"/>
      <stop offset="48%" stop-color="#29b85c" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#146632" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="e18_trail_glow" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#78ffaa" stop-opacity="0.72"/>
      <stop offset="45%" stop-color="#2ec766" stop-opacity="0.38"/>
      <stop offset="100%" stop-color="#14592d" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="e18_plasma_body" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#d4ffe2"/>
      <stop offset="22%" stop-color="#63ff99"/>
      <stop offset="58%" stop-color="#28b85c"/>
      <stop offset="100%" stop-color="#145c30"/>
    </linearGradient>
    <linearGradient id="e18_plasma_inner" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="35%" stop-color="#c4ffd9"/>
      <stop offset="78%" stop-color="#52f089"/>
      <stop offset="100%" stop-color="#249e50" stop-opacity="0.2"/>
    </linearGradient>
    <radialGradient id="e18_head_hot" cx="50%" cy="42%" r="52%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="45%" stop-color="#b8ffd0"/>
      <stop offset="85%" stop-color="#47e880"/>
      <stop offset="100%" stop-color="#1f9449" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="e18_drop" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#b3ffce"/>
      <stop offset="55%" stop-color="#3ad470"/>
      <stop offset="100%" stop-color="#186e37"/>
    </linearGradient>
  </defs>

  <!-- Soft radioactive green plasma halo (centered, narrow vertical envelope) -->
  <ellipse cx="256" cy="226" rx="82" ry="172" fill="url(#e18_aura_outer)"/>

  <!-- Faint trailing ionized plasma vapor plume -->
  <path d="M 222,188
           C 210,256 226,338 256,438
           C 286,338 302,256 290,188 Z"
        fill="url(#e18_trail_glow)"/>

  <!-- Unstable energy arc wisps swirling around the bolt -->
  <path d="M 226,142 C 194,182 204,234 234,274 C 218,308 228,348 248,386"
        fill="none" stroke="#6dff9e" stroke-width="5" stroke-linecap="round" opacity="0.72"/>
  <path d="M 286,154 C 314,196 304,244 276,282 C 292,318 282,356 264,394"
        fill="none" stroke="#52e685" stroke-width="4.5" stroke-linecap="round" opacity="0.68"/>

  <!-- Main dense green plasma globule silhouette with dark outline (6-8px) -->
  <path d="M 256,76
           C 282,102 304,144 302,190
           C 300,236 280,276 272,318
           C 266,350 264,376 256,398
           C 248,376 246,350 240,318
           C 232,276 212,236 210,190
           C 208,144 230,102 256,76 Z"
        fill="url(#e18_plasma_body)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Swirling internal plasma eddies for dense molten energy texture -->
  <path d="M 232,162 C 224,198 236,232 254,264 C 242,230 242,196 248,164 Z"
        fill="#19783c" opacity="0.45"/>
  <path d="M 280,174 C 288,208 274,242 258,274 C 270,240 272,206 266,176 Z"
        fill="#19783c" opacity="0.4"/>

  <!-- Luminous inner superheated plasma spine -->
  <path d="M 256,94
           C 272,118 285,152 282,188
           C 279,224 266,262 262,304
           C 259,328 258,348 256,364
           C 254,348 253,328 250,304
           C 246,262 233,224 230,188
           C 227,152 240,118 256,94 Z"
        fill="url(#e18_plasma_inner)"/>

  <!-- Incandescent white-green leading plasma nucleus -->
  <ellipse cx="256" cy="158" rx="22" ry="44" fill="url(#e18_head_hot)"/>
  <path d="M 256,110 C 264,132 266,158 256,184 C 246,158 248,132 256,110 Z"
        fill="#ffffff" opacity="0.92"/>

  <!-- Bright specular energy glint along leading left edge -->
  <path d="M 234,132 C 224,154 222,180 228,204"
        fill="none" stroke="#e8fff0" stroke-width="4" stroke-linecap="round" opacity="0.78"/>

  <!-- Detached trailing glowing plasma droplets in the wake -->
  <g stroke="#121519" stroke-width="6" stroke-linejoin="round">
    <path d="M 224,296 C 232,308 232,324 224,334 C 216,324 216,308 224,296 Z" fill="url(#e18_drop)"/>
    <path d="M 288,314 C 296,326 296,340 288,350 C 280,340 280,326 288,314 Z" fill="url(#e18_drop)"/>
    <path d="M 256,406 C 263,416 263,430 256,438 C 249,430 249,416 256,406 Z" fill="url(#e18_drop)"/>
    <circle cx="236" cy="378" r="8" fill="url(#e18_drop)"/>
    <circle cx="276" cy="392" r="6.5" fill="url(#e18_drop)"/>
  </g>
  <!-- Bright pinpoint highlights on droplets -->
  <g fill="#ebfff2" opacity="0.9">
    <circle cx="223" cy="312" r="2.8"/>
    <circle cx="287" cy="329" r="2.6"/>
    <circle cx="255" cy="420" r="2.5"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_FinalJudgment.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="fj_aura_beam" cx="50%" cy="46%" r="50%">
      <stop offset="0%" stop-color="#ff5e57" stop-opacity="0.45"/>
      <stop offset="48%" stop-color="#cf2323" stop-opacity="0.2"/>
      <stop offset="100%" stop-color="#7a0c12" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="fj_tip_flare" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.96"/>
      <stop offset="35%" stop-color="#ff9994" stop-opacity="0.65"/>
      <stop offset="72%" stop-color="#e62929" stop-opacity="0.22"/>
      <stop offset="100%" stop-color="#a81116" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="fj_sheath" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffb5b0"/>
      <stop offset="16%" stop-color="#ff4747"/>
      <stop offset="58%" stop-color="#c91c22"/>
      <stop offset="88%" stop-color="#7d1016"/>
      <stop offset="100%" stop-color="#47090d"/>
    </linearGradient>
    <linearGradient id="fj_core_hot" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="32%" stop-color="#ffe8e6"/>
      <stop offset="72%" stop-color="#ff7a75"/>
      <stop offset="100%" stop-color="#d6242a" stop-opacity="0.25"/>
    </linearGradient>
    <linearGradient id="fj_side_streak" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ff7873" stop-opacity="0.85"/>
      <stop offset="55%" stop-color="#cf2127" stop-opacity="0.45"/>
      <stop offset="100%" stop-color="#750c12" stop-opacity="0"/>
    </linearGradient>
  </defs>

  <!-- Vertical narrow crimson laser glow envelope -->
  <ellipse cx="256" cy="252" rx="68" ry="188" fill="url(#fj_aura_beam)"/>

  <!-- Parallel high-energy harmonic side-streaks -->
  <path d="M 222,126 L 228,126 L 232,374 L 224,398 Z" fill="url(#fj_side_streak)"/>
  <path d="M 290,126 L 284,126 L 280,374 L 288,398 Z" fill="url(#fj_side_streak)"/>

  <!-- Sharp cross-flare at leading coherent beam tip -->
  <circle cx="256" cy="112" r="46" fill="url(#fj_tip_flare)"/>
  <path d="M 256,64 L 263,106 L 310,112 L 263,118 L 256,154 L 249,118 L 202,112 L 249,106 Z"
        fill="#ffd9d6" opacity="0.72"/>

  <!-- Main coherent red laser bolt-streak silhouette with dark outline (6.5px) -->
  <path d="M 256,68
           L 276,120
           L 274,316
           L 264,412
           L 256,442
           L 248,412
           L 238,316
           L 236,120 Z"
        fill="url(#fj_sheath)" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Faceted internal ruby bevel edges for crisp weaponized laser look -->
  <polygon points="256,82 269,122 267,310 256,422 245,310 243,122"
           fill="#ff5954" opacity="0.6"/>

  <!-- Incandescent white-hot inner laser needle core -->
  <path d="M 256,80
           L 263,124
           L 262,316
           L 256,416
           L 250,316
           L 249,124 Z"
        fill="url(#fj_core_hot)"/>

  <!-- Pure white axial centerline peak -->
  <line x1="256" y1="90" x2="256" y2="352" stroke="#ffffff" stroke-width="5.5" stroke-linecap="round" opacity="0.95"/>

  <!-- Pulsed energy compression nodes / shock rings along the beam -->
  <g stroke="#121519" stroke-width="5" stroke-linejoin="round">
    <path d="M 220,176 Q 256,162 292,176 Q 256,184 220,176 Z" fill="#ff7d78"/>
    <path d="M 226,258 Q 256,246 286,258 Q 256,265 226,258 Z" fill="#ff6661"/>
    <path d="M 232,336 Q 256,326 280,336 Q 256,342 232,336 Z" fill="#e63e3e"/>
  </g>
  <!-- Inner bright highlights on pulse nodes -->
  <g stroke="#ffffff" stroke-width="2.5" stroke-linecap="round" opacity="0.88">
    <line x1="238" y1="175" x2="274" y2="175"/>
    <line x1="242" y1="257" x2="270" y2="257"/>
    <line x1="245" y1="335" x2="267" y2="335"/>
  </g>

  <!-- Trailing coherent red photon sparks -->
  <g fill="#ff8f8a" stroke="#121519" stroke-width="4.5" stroke-linejoin="round">
    <polygon points="234,394 238,410 234,424 230,410"/>
    <polygon points="278,386 282,402 278,416 274,402"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_TheColt.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="tcb_aura" cx="50%" cy="32%" r="52%">
      <stop offset="0%" stop-color="#d6f0ff" stop-opacity="0.45"/>
      <stop offset="48%" stop-color="#87bde3" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#487194" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="tcb_trail_outer" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3f5ff" stop-opacity="0.85"/>
      <stop offset="35%" stop-color="#8cc8f0" stop-opacity="0.48"/>
      <stop offset="72%" stop-color="#5c8fb8" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#355573" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="tcb_trail_core" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.96"/>
      <stop offset="28%" stop-color="#fff4d6" stop-opacity="0.78"/>
      <stop offset="62%" stop-color="#9ed9ff" stop-opacity="0.4"/>
      <stop offset="100%" stop-color="#5a92bf" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="tcb_silver_body" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#566675"/>
      <stop offset="24%" stop-color="#cedee8"/>
      <stop offset="46%" stop-color="#f7fcff"/>
      <stop offset="74%" stop-color="#94a6b5"/>
      <stop offset="100%" stop-color="#43505c"/>
    </linearGradient>
    <linearGradient id="tcb_bullet_base" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe7b0"/>
      <stop offset="55%" stop-color="#c9a559"/>
      <stop offset="100%" stop-color="#6e5628"/>
    </linearGradient>
    <radialGradient id="tcb_tip_spark" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="42%" stop-color="#cdeeff" stop-opacity="0.5"/>
      <stop offset="100%" stop-color="#7ec0eb" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Soft supernatural silver-blue halo around projectile & trail -->
  <ellipse cx="256" cy="236" rx="78" ry="184" fill="url(#tcb_aura)"/>

  <!-- Luminous tapered supernatural trail streaming downward from bullet base -->
  <path d="M 224,186
           C 214,256 226,350 256,442
           C 286,350 298,256 288,186 Z"
        fill="url(#tcb_trail_outer)"/>

  <!-- Intertwining arcane silver-light ribbons in the wake -->
  <path d="M 232,196 C 216,246 276,282 256,348 C 244,386 250,416 256,436"
        fill="none" stroke="#c9ecff" stroke-width="5" stroke-linecap="round" opacity="0.72"/>
  <path d="M 280,196 C 296,246 236,282 256,348"
        fill="none" stroke="#ffe9b8" stroke-width="4" stroke-linecap="round" opacity="0.65"/>

  <!-- Hot inner luminous core plume immediately behind the bullet -->
  <path d="M 236,190
           C 234,250 242,324 256,406
           C 270,324 278,250 276,190 Z"
        fill="url(#tcb_trail_core)"/>

  <!-- Subtle dark-framed luminous shockwave ring near bullet base -->
  <ellipse cx="256" cy="212" rx="46" ry="11"
           fill="none" stroke="#121519" stroke-width="6" opacity="0.65"/>
  <ellipse cx="256" cy="212" rx="46" ry="11"
           fill="none" stroke="#d9f2ff" stroke-width="3"/>

  <ellipse cx="256" cy="252" rx="34" ry="8"
           fill="none" stroke="#a8dcff" stroke-width="3" opacity="0.7"/>

  <!-- Glowing warm muzzle-ignition ember core right at base of slug -->
  <ellipse cx="256" cy="196" rx="24" ry="14" fill="url(#tcb_bullet_base)"/>

  <!-- Main solid antique silver Colt bullet (pointing North) -->
  <path d="M 256,74
           C 274,90 285,118 285,152
           L 285,190
           C 285,196 272,200 256,200
           C 240,200 227,196 227,190
           L 227,152
           C 227,118 238,90 256,74 Z"
        fill="url(#tcb_silver_body)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Machined antique cannelure grooves & supernatural engraved pentagram ward -->
  <g fill="none" stroke="#283440" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">
    <path d="M 230,178 Q 256,184 282,178"/>
    <path d="M 230,166 Q 256,172 282,166"/>
    <!-- Tiny engraved mystic star on silver ogive -->
    <polygon points="256,118 263,140 245,126 267,126 249,140" stroke-width="2.4"/>
    <circle cx="256" cy="130" r="14" stroke-width="2.4" opacity="0.85"/>
  </g>

  <!-- Crisp metallic specular highlight along left curvature of silver bullet -->
  <path d="M 248,94 C 240,114 238,138 238,176"
        fill="none" stroke="#ffffff" stroke-width="4.5" stroke-linecap="round" opacity="0.88"/>

  <!-- Celestial silver-white glint at the nose tip -->
  <circle cx="256" cy="82" r="24" fill="url(#tcb_tip_spark)"/>
  <path d="M 256,62 L 259,78 L 274,82 L 259,86 L 256,102 L 253,86 L 238,82 L 253,78 Z"
        fill="#ffffff" opacity="0.92"/>

  <!-- Trailing glowing silver-gold arcane embers -->
  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <polygon points="222,282 227,292 222,302 217,292" fill="#e6f7ff"/>
    <polygon points="290,308 295,317 290,326 285,317" fill="#fff0c4"/>
    <circle cx="236" cy="364" r="5.5" fill="#d4f0ff"/>
    <circle cx="274" cy="382" r="4.5" fill="#ffe8b0"/>
    <circle cx="256" cy="426" r="5" fill="#ffffff"/>
  </g>
</svg>
```