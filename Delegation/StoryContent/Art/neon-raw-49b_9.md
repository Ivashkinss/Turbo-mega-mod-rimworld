`Art/Projectiles/TMM_Bullet_KelloggRevolver.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="k44_aura" cx="50%" cy="38%" r="50%">
      <stop offset="0%" stop-color="#ffc978" stop-opacity="0.38"/>
      <stop offset="48%" stop-color="#c46b2b" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#61280c" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="k44_wake_outer" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffda8f" stop-opacity="0.82"/>
      <stop offset="32%" stop-color="#d97a30" stop-opacity="0.48"/>
      <stop offset="70%" stop-color="#87421e" stop-opacity="0.2"/>
      <stop offset="100%" stop-color="#47281a" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="k44_wake_core" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="30%" stop-color="#ffebb3" stop-opacity="0.76"/>
      <stop offset="68%" stop-color="#e38232" stop-opacity="0.28"/>
      <stop offset="100%" stop-color="#8c3614" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="k44_jacket_copper" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#592e14"/>
      <stop offset="22%" stop-color="#d4884c"/>
      <stop offset="44%" stop-color="#ffd0a1"/>
      <stop offset="72%" stop-color="#a85c29"/>
      <stop offset="100%" stop-color="#45210d"/>
    </linearGradient>
    <linearGradient id="k44_lead_nose" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#353d45"/>
      <stop offset="28%" stop-color="#8a97a3"/>
      <stop offset="48%" stop-color="#cfd9e0"/>
      <stop offset="76%" stop-color="#5d6873"/>
      <stop offset="100%" stop-color="#24292e"/>
    </linearGradient>
    <radialGradient id="k44_base_flare" cx="50%" cy="34%" r="54%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="40%" stop-color="#ffd57a"/>
      <stop offset="80%" stop-color="#d95f1e"/>
      <stop offset="100%" stop-color="#752308" stop-opacity="0"/>
    </radialGradient>
    <clipPath id="k44_slug_clip">
      <path d="M 238,76
               L 274,76
               C 286,96 293,120 293,146
               L 293,210
               C 293,217 278,222 256,222
               C 234,222 219,217 219,210
               L 219,146
               C 219,120 226,96 238,76 Z"/>
    </clipPath>
  </defs>

  <!-- Heavy magnum muzzle-heat & air-compression radiance -->
  <ellipse cx="256" cy="240" rx="82" ry="184" fill="url(#k44_aura)"/>

  <!-- Heavy supersonic propellant wake trailing South -->
  <path d="M 218,208
           C 208,278 224,358 256,442
           C 288,358 304,278 294,208 Z"
        fill="url(#k44_wake_outer)"/>

  <!-- Incandescent hot inner tracer core behind base of heavy slug -->
  <path d="M 234,212
           C 230,272 240,338 256,408
           C 272,338 282,272 278,212 Z"
        fill="url(#k44_wake_core)"/>

  <!-- Supersonic Mach shockwave chevrons & rifling spin streaks -->
  <path d="M 196,174 C 216,138 238,118 256,112 C 274,118 296,138 316,174"
        fill="none" stroke="#ffdfa1" stroke-width="4" stroke-linecap="round" opacity="0.52"/>
  <path d="M 210,254 L 256,224 L 302,254"
        fill="none" stroke="#ffd280" stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round" opacity="0.65"/>
  <path d="M 222,304 L 256,280 L 290,304"
        fill="none" stroke="#e69242" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" opacity="0.5"/>

  <!-- Subtle dark-outlined pressure shock ring behind heavy .44 base -->
  <ellipse cx="256" cy="236" rx="48" ry="11" fill="none" stroke="#121519" stroke-width="6" opacity="0.58"/>
  <ellipse cx="256" cy="236" rx="48" ry="11" fill="none" stroke="#ffe2a3" stroke-width="3"/>

  <!-- White-hot propellant ignition flash at base of slug -->
  <ellipse cx="256" cy="220" rx="30" ry="16" fill="url(#k44_base_flare)"/>

  <!-- Heavy .44 Magnum semi-jacketed flat-nose slug (pointing North) -->
  <path d="M 238,76
           L 274,76
           C 286,96 293,120 293,146
           L 293,210
           C 293,217 278,222 256,222
           C 234,222 219,217 219,210
           L 219,146
           C 219,120 226,96 238,76 Z"
        fill="url(#k44_jacket_copper)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Internal .44 semi-jacketed details clipped cleanly to bullet body -->
  <g clip-path="url(#k44_slug_clip)">
    <!-- Exposed heavy cast-lead flat-nose tip (classic .44 Magnum semi-jacketed hollow point) -->
    <path d="M 216,70 L 296,70 L 296,120
             L 282,114 L 269,124 L 256,114 L 243,124 L 230,114 L 216,120 Z"
          fill="url(#k44_lead_nose)"/>
    <!-- Scalloped expansion notches where copper jacket grips lead core -->
    <path d="M 216,120 L 230,114 L 243,124 L 256,114 L 269,124 L 282,114 L 296,120"
          fill="none" stroke="#121519" stroke-width="4.5" stroke-linejoin="round"/>

    <!-- Deep machined knurled cannelure crimp groove -->
    <path d="M 216,174 Q 256,182 296,174 L 296,188 Q 256,196 216,188 Z"
          fill="#381d0c" stroke="#121519" stroke-width="3.5"/>
    <!-- Vertical knurling ticks inside cannelure -->
    <g stroke="#b8723d" stroke-width="2.5" stroke-linecap="round" opacity="0.72">
      <line x1="230" y1="178" x2="230" y2="188"/>
      <line x1="242" y1="180" x2="242" y2="190"/>
      <line x1="256" y1="181" x2="256" y2="191"/>
      <line x1="270" y1="180" x2="270" y2="190"/>
      <line x1="282" y1="178" x2="282" y2="188"/>
    </g>

    <!-- Rifling barrel engraving marks across copper jacket -->
    <g stroke="#4a250e" stroke-width="3" stroke-linecap="round" opacity="0.55">
      <line x1="238" y1="132" x2="246" y2="168"/>
      <line x1="266" y1="132" x2="274" y2="168"/>
    </g>

    <!-- Bright metallic specular sheen along left flank -->
    <path d="M 241,82 C 234,102 232,126 232,208"
          fill="none" stroke="#fff3e0" stroke-width="5.5" stroke-linecap="round" opacity="0.78"/>
  </g>

  <!-- Hollow-point recessed flat meplat rim at very top -->
  <ellipse cx="256" cy="78" rx="17" ry="4.5" fill="#23282e" stroke="#121519" stroke-width="5"/>

  <!-- Re-stroke crisp outer .44 bullet perimeter -->
  <path d="M 238,76
           L 274,76
           C 286,96 293,120 293,146
           L 293,210
           C 293,217 278,222 256,222
           C 234,222 219,217 219,210
           L 219,146
           C 219,120 226,96 238,76 Z"
        fill="none" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Trailing burning magnum powder sparks & embers -->
  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <polygon points="214,284 220,294 214,304 208,294" fill="#ffe18c"/>
    <polygon points="298,276 304,286 298,296 292,286" fill="#ffb84d"/>
    <circle cx="228" cy="356" r="5.5" fill="#ffcf70"/>
    <circle cx="284" cy="364" r="5" fill="#ff9e42"/>
    <circle cx="256" cy="422" r="5" fill="#ffe89e"/>
  </g>
</svg>
```