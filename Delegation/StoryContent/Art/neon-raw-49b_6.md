`Art/Projectiles/TMM_Projectile_LorenzoWave.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="lw_aura_outer" cx="50%" cy="38%" r="50%">
      <stop offset="0%" stop-color="#ffe58f" stop-opacity="0.44"/>
      <stop offset="50%" stop-color="#d49b35" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#7a5112" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="lw_wake_stream" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fff2bd" stop-opacity="0.72"/>
      <stop offset="38%" stop-color="#e3ad42" stop-opacity="0.38"/>
      <stop offset="76%" stop-color="#9e6d1c" stop-opacity="0.14"/>
      <stop offset="100%" stop-color="#5e3e0b" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="lw_ring_gold" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#fff9db"/>
      <stop offset="28%" stop-color="#f7cf6a"/>
      <stop offset="68%" stop-color="#c98f29"/>
      <stop offset="100%" stop-color="#7a5010"/>
    </linearGradient>
    <linearGradient id="lw_crest_hot" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="32%" stop-color="#ffeaa3"/>
      <stop offset="72%" stop-color="#d99b2e"/>
      <stop offset="100%" stop-color="#875812"/>
    </linearGradient>
    <radialGradient id="lw_core_orb" cx="50%" cy="46%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.96"/>
      <stop offset="42%" stop-color="#ffdf80" stop-opacity="0.72"/>
      <stop offset="80%" stop-color="#c98824" stop-opacity="0.22"/>
      <stop offset="100%" stop-color="#7d4f0d" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="lw_ring_fade" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe699"/>
      <stop offset="55%" stop-color="#cf9229"/>
      <stop offset="100%" stop-color="#784f10"/>
    </linearGradient>
  </defs>

  <!-- Warm supernatural golden-amber aura envelope -->
  <ellipse cx="256" cy="236" rx="84" ry="184" fill="url(#lw_aura_outer)"/>

  <!-- Tapered telekinetic distortion wake streaming southward -->
  <path d="M 192,154
           C 188,248 214,344 256,440
           C 298,344 324,248 320,154 Z"
        fill="url(#lw_wake_stream)"/>

  <!-- Swirling arcane harmonic filaments linking the wave rings -->
  <path d="M 206,154 C 194,214 294,254 274,324 C 266,356 260,390 256,426"
        fill="none" stroke="#ffea9e" stroke-width="4.5" stroke-linecap="round" opacity="0.68"/>
  <path d="M 306,154 C 318,214 218,254 238,324 C 246,356 252,390 256,426"
        fill="none" stroke="#e8b246" stroke-width="4" stroke-linecap="round" opacity="0.6"/>

  <!-- Trailing harmonic wave ring #3 (small, near tail) -->
  <path d="M 224,376
           A 32,12 0 1 1 288,376
           A 32,12 0 1 1 224,376 Z
           M 235,376
           A 21,6 0 1 0 277,376
           A 21,6 0 1 0 235,376 Z"
        fill="url(#lw_ring_fade)" fill-rule="evenodd" stroke="#121519" stroke-width="6" stroke-linejoin="round" opacity="0.88"/>

  <!-- Trailing harmonic wave ring #2 (medium, mid-lower) -->
  <path d="M 206,314
           A 50,18 0 1 1 306,314
           A 50,18 0 1 1 206,314 Z
           M 220,314
           A 36,10 0 1 0 292,314
           A 36,10 0 1 0 220,314 Z"
        fill="url(#lw_ring_gold)" fill-rule="evenodd" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>
  <path d="M 218,308 Q 256,296 294,308" fill="none" stroke="#fff7d6" stroke-width="3" stroke-linecap="round" opacity="0.82"/>

  <!-- Secondary shock ring #1 (wide, upper-mid) -->
  <path d="M 188,244
           A 68,24 0 1 1 324,244
           A 68,24 0 1 1 188,244 Z
           M 206,244
           A 50,14 0 1 0 306,244
           A 50,14 0 1 0 206,244 Z"
        fill="url(#lw_ring_gold)" fill-rule="evenodd" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>
  <path d="M 204,234 Q 256,218 308,234" fill="none" stroke="#ffffff" stroke-width="3.5" stroke-linecap="round" opacity="0.88"/>

  <!-- Central telekinetic glowing energy core inside primary ring -->
  <circle cx="256" cy="156" r="46" fill="url(#lw_core_orb)"/>

  <!-- Primary leading golden ring-wave (bold sculptural telekinetic ring moving North) -->
  <path d="M 182,158
           A 74,74 0 1 1 330,158
           A 74,74 0 1 1 182,158 Z
           M 204,158
           A 52,52 0 1 0 308,158
           A 52,52 0 1 0 204,158 Z"
        fill="url(#lw_ring_gold)" fill-rule="evenodd" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Forward-bowed high-energy wave crest crowning the leading North edge -->
  <path d="M 186,138
           C 198,94 226,74 256,74
           C 286,74 314,94 326,138
           C 308,112 284,98 256,98
           C 228,98 204,112 186,138 Z"
        fill="url(#lw_crest_hot)" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Bright white-gold specular rim highlight along North crest -->
  <path d="M 206,104 C 224,86 240,81 256,81 C 272,81 288,86 306,104"
        fill="none" stroke="#ffffff" stroke-width="4.5" stroke-linecap="round" opacity="0.94"/>

  <!-- Inner eldritch geometric resonance diamond inside main golden ring -->
  <polygon points="256,120 278,158 256,196 234,158"
           fill="#fff2bd" fill-opacity="0.35" stroke="#fff7d6" stroke-width="3" stroke-linejoin="round"/>
  <circle cx="256" cy="158" r="10" fill="#ffffff" opacity="0.92"/>

  <!-- Trailing golden energy motes -->
  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <polygon points="210,278 215,286 210,294 205,286" fill="#ffefad"/>
    <polygon points="302,272 307,280 302,288 297,280" fill="#ffefad"/>
    <circle cx="228" cy="348" r="5" fill="#ffe180"/>
    <circle cx="284" cy="354" r="4.5" fill="#fff2bd"/>
    <circle cx="256" cy="418" r="5.5" fill="#ffe794"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_TeslaCannon.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="tc_aura_outer" cx="50%" cy="38%" r="50%">
      <stop offset="0%" stop-color="#80e8ff" stop-opacity="0.46"/>
      <stop offset="48%" stop-color="#2c9be6" stop-opacity="0.2"/>
      <stop offset="100%" stop-color="#124a80" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="tc_orb_body" cx="48%" cy="44%" r="54%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="30%" stop-color="#c9f6ff"/>
      <stop offset="65%" stop-color="#47c2ff"/>
      <stop offset="100%" stop-color="#1b6bb8"/>
    </radialGradient>
    <linearGradient id="tc_arc_tail" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3fbff"/>
      <stop offset="32%" stop-color="#5cd1ff"/>
      <stop offset="72%" stop-color="#2285d6"/>
      <stop offset="100%" stop-color="#114480"/>
    </linearGradient>
    <linearGradient id="tc_wake_glow" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7ce6ff" stop-opacity="0.65"/>
      <stop offset="50%" stop-color="#2994e0" stop-opacity="0.28"/>
      <stop offset="100%" stop-color="#0e3c70" stop-opacity="0"/>
    </linearGradient>
  </defs>

  <!-- High-voltage cyan-blue plasma aura -->
  <ellipse cx="256" cy="236" rx="82" ry="184" fill="url(#tc_aura_outer)"/>

  <!-- Ionized air trail cone behind electric orb -->
  <path d="M 218,178
           C 206,258 224,348 256,440
           C 288,348 306,258 294,178 Z"
        fill="url(#tc_wake_glow)"/>

  <!-- Main jagged trailing lightning bolt spine (dark-outlined for RimWorld readability) -->
  <path d="M 240,188
           L 272,188
           L 288,244
           L 262,258
           L 284,316
           L 258,332
           L 272,390
           L 256,438
           L 238,386
           L 254,362
           L 228,318
           L 252,296
           L 224,242 Z"
        fill="url(#tc_arc_tail)" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- White-hot inner electric filament running down the jagged tail -->
  <polyline points="256,192 240,240 270,262 242,312 268,336 248,384 256,424"
            fill="none" stroke="#ffffff" stroke-width="5.5" stroke-linecap="round" stroke-linejoin="round"/>

  <!-- Branching lateral arc tendrils whipping out from the orb -->
  <g stroke-linecap="round" stroke-linejoin="round">
    <!-- Left whip arc -->
    <polyline points="220,164 188,194 206,222 184,264 208,292"
              fill="none" stroke="#121519" stroke-width="11"/>
    <polyline points="220,164 188,194 206,222 184,264 208,292"
              fill="none" stroke="#6ee3ff" stroke-width="5"/>
    <polyline points="220,164 188,194 206,222 184,264 208,292"
              fill="none" stroke="#ffffff" stroke-width="2"/>

    <!-- Right whip arc -->
    <polyline points="292,164 324,198 304,228 326,274 300,308"
              fill="none" stroke="#121519" stroke-width="11"/>
    <polyline points="292,164 324,198 304,228 326,274 300,308"
              fill="none" stroke="#6ee3ff" stroke-width="5"/>
    <polyline points="292,164 324,198 304,228 326,274 300,308"
              fill="none" stroke="#ffffff" stroke-width="2"/>

    <!-- Forward leading discharge crown spikes pointing North -->
    <polyline points="256,118 246,96 260,82 256,70"
              fill="none" stroke="#121519" stroke-width="11"/>
    <polyline points="256,118 246,96 260,82 256,70"
              fill="none" stroke="#8ff0ff" stroke-width="5"/>
    <polyline points="256,118 246,96 260,82 256,70"
              fill="none" stroke="#ffffff" stroke-width="2.2"/>

    <polyline points="230,126 210,104 220,88"
              fill="none" stroke="#121519" stroke-width="9"/>
    <polyline points="230,126 210,104 220,88"
              fill="none" stroke="#78e8ff" stroke-width="4"/>

    <polyline points="282,126 302,104 292,88"
              fill="none" stroke="#121519" stroke-width="9"/>
    <polyline points="282,126 302,104 292,88"
              fill="none" stroke="#78e8ff" stroke-width="4"/>
  </g>

  <!-- Main crackling Tesla ball-lightning orb -->
  <circle cx="256" cy="160" r="46" fill="url(#tc_orb_body)" stroke="#121519" stroke-width="7.5"/>

  <!-- Internal swirling electric discharge arcs across the sphere -->
  <path d="M 222,148 L 242,138 L 254,154 L 274,142 L 290,156"
        fill="none" stroke="#1c6ab5" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" opacity="0.55"/>
  <path d="M 224,174 L 244,164 L 262,178 L 284,168"
        fill="none" stroke="#1c6ab5" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" opacity="0.5"/>

  <!-- Bright plasma containment arc hugging sphere curvature -->
  <path d="M 222,144 C 232,124 256,118 278,128"
        fill="none" stroke="#ffffff" stroke-width="4.5" stroke-linecap="round" opacity="0.88"/>

  <!-- Incandescent white-cyan electric nucleus star -->
  <path d="M 256,124 L 265,148 L 290,160 L 265,172 L 256,196 L 247,172 L 222,160 L 247,148 Z"
        fill="#ffffff" opacity="0.94"/>
  <circle cx="256" cy="160" r="16" fill="#ffffff"/>

  <!-- Detached ionized blue spark nodes in the wake -->
  <g stroke="#121519" stroke-width="4.5" stroke-linejoin="round">
    <polygon points="212,334 218,344 212,354 206,344" fill="#b8f5ff"/>
    <polygon points="300,350 306,360 300,370 294,360" fill="#b8f5ff"/>
    <circle cx="226" cy="396" r="5.5" fill="#82ebff"/>
    <circle cx="286" cy="404" r="5" fill="#d4f9ff"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_OverseersGuardian.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="og_aura" cx="50%" cy="38%" r="48%">
      <stop offset="0%" stop-color="#ffd98a" stop-opacity="0.34"/>
      <stop offset="52%" stop-color="#d48c35" stop-opacity="0.14"/>
      <stop offset="100%" stop-color="#7a4410" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="og_trail_outer" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe29e" stop-opacity="0.82"/>
      <stop offset="35%" stop-color="#e09438" stop-opacity="0.46"/>
      <stop offset="72%" stop-color="#9e521b" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#59280b" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="og_trail_core" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="32%" stop-color="#ffeaad" stop-opacity="0.72"/>
      <stop offset="72%" stop-color="#e89a3c" stop-opacity="0.25"/>
      <stop offset="100%" stop-color="#a65217" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="og_brass_body" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#5e4518"/>
      <stop offset="24%" stop-color="#e0be67"/>
      <stop offset="46%" stop-color="#fff0b8"/>
      <stop offset="74%" stop-color="#b58d35"/>
      <stop offset="100%" stop-color="#4a3410"/>
    </linearGradient>
    <linearGradient id="og_copper_tip" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#693b1c"/>
      <stop offset="28%" stop-color="#d99457"/>
      <stop offset="48%" stop-color="#ffd2a1"/>
      <stop offset="76%" stop-color="#ad6832"/>
      <stop offset="100%" stop-color="#4d2911"/>
    </linearGradient>
    <radialGradient id="og_ember_base" cx="50%" cy="35%" r="55%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="45%" stop-color="#ffd478"/>
      <stop offset="85%" stop-color="#d96b21"/>
      <stop offset="100%" stop-color="#87330c" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Subtle warm dual-tracer ambient glow -->
  <ellipse cx="256" cy="238" rx="80" ry="182" fill="url(#og_aura)"/>

  <!-- LEFT PARALLEL BULLET + TRACER TRAIL (x=218) -->
  <g transform="translate(218 0)">
    <!-- Tapered warm supersonic tracer plume -->
    <path d="M -21,196 C -25,264 -16,348 0,432 C 16,348 25,264 21,196 Z" fill="url(#og_trail_outer)"/>
    <path d="M -11,198 C -12,256 -6,322 0,390 C 6,322 12,256 11,198 Z" fill="url(#og_trail_core)"/>

    <!-- Subtle supersonic slipstream chevron lines -->
    <path d="M -28,238 L 0,214 L 28,238" fill="none" stroke="#ffe7a8" stroke-width="3" stroke-linecap="round" opacity="0.55"/>
    <path d="M -22,284 L 0,264 L 22,284" fill="none" stroke="#e6a24c" stroke-width="2.5" stroke-linecap="round" opacity="0.45"/>

    <!-- Glowing hot tracer ignition base -->
    <ellipse cx="0" cy="202" rx="18" ry="12" fill="url(#og_ember_base)"/>

    <!-- Machined brass full-metal-jacket rifle bullet (pointing North) -->
    <path d="M 0,76
             C 14,94 23,124 23,156
             L 23,194
             C 23,199 13,202 0,202
             C -13,202 -23,199 -23,194
             L -23,156
             C -23,124 -14,94 0,76 Z"
          fill="url(#og_brass_body)" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>

    <!-- Copper-brass aerodynamic ogive nose tip accent -->
    <path d="M 0,80 C 10,94 17,114 20,136 Q 0,141 -20,136 C -17,114 -10,94 0,80 Z"
          fill="url(#og_copper_tip)" opacity="0.62"/>

    <!-- Cannelure crimp groove & metallic banding -->
    <path d="M -20,174 Q 0,180 20,174" fill="none" stroke="#3d2b0d" stroke-width="3.5" stroke-linecap="round"/>
    <path d="M -20,184 Q 0,190 20,184" fill="none" stroke="#3d2b0d" stroke-width="2.5" stroke-linecap="round"/>

    <!-- Crisp metallic specular highlight along left curvature -->
    <path d="M -6,96 C -12,116 -14,140 -14,186" fill="none" stroke="#fffbe6" stroke-width="4" stroke-linecap="round" opacity="0.88"/>
  </g>

  <!-- RIGHT PARALLEL BULLET + TRACER TRAIL (x=294, slight 10px dynamic stagger) -->
  <g transform="translate(294 10)">
    <!-- Tapered warm supersonic tracer plume -->
    <path d="M -21,196 C -25,264 -16,348 0,432 C 16,348 25,264 21,196 Z" fill="url(#og_trail_outer)"/>
    <path d="M -11,198 C -12,256 -6,322 0,390 C 6,322 12,256 11,198 Z" fill="url(#og_trail_core)"/>

    <!-- Subtle supersonic slipstream chevron lines -->
    <path d="M -28,238 L 0,214 L 28,238" fill="none" stroke="#ffe7a8" stroke-width="3" stroke-linecap="round" opacity="0.55"/>
    <path d="M -22,284 L 0,264 L 22,284" fill="none" stroke="#e6a24c" stroke-width="2.5" stroke-linecap="round" opacity="0.45"/>

    <!-- Glowing hot tracer ignition base -->
    <ellipse cx="0" cy="202" rx="18" ry="12" fill="url(#og_ember_base)"/>

    <!-- Machined brass full-metal-jacket rifle bullet (pointing North) -->
    <path d="M 0,76
             C 14,94 23,124 23,156
             L 23,194
             C 23,199 13,202 0,202
             C -13,202 -23,199 -23,194
             L -23,156
             C -23,124 -14,94 0,76 Z"
          fill="url(#og_brass_body)" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>

    <!-- Copper-brass aerodynamic ogive nose tip accent -->
    <path d="M 0,80 C 10,94 17,114 20,136 Q 0,141 -20,136 C -17,114 -10,94 0,80 Z"
          fill="url(#og_copper_tip)" opacity="0.62"/>

    <!-- Cannelure crimp groove & metallic banding -->
    <path d="M -20,174 Q 0,180 20,174" fill="none" stroke="#3d2b0d" stroke-width="3.5" stroke-linecap="round"/>
    <path d="M -20,184 Q 0,190 20,184" fill="none" stroke="#3d2b0d" stroke-width="2.5" stroke-linecap="round"/>

    <!-- Crisp metallic specular highlight along left curvature -->
    <path d="M -6,96 C -12,116 -14,140 -14,186" fill="none" stroke="#fffbe6" stroke-width="4" stroke-linecap="round" opacity="0.88"/>
  </g>

  <!-- Crisp twin nose glints & trailing hot gunpowder embers -->
  <path d="M 218,68 L 220,76 L 228,78 L 220,80 L 218,88 L 216,80 L 208,78 L 216,76 Z" fill="#fff7d6" opacity="0.9"/>
  <path d="M 294,78 L 296,86 L 304,88 L 296,90 L 294,98 L 292,90 L 284,88 L 292,86 Z" fill="#fff7d6" opacity="0.9"/>

  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <circle cx="204" cy="346" r="4.5" fill="#ffdf8a"/>
    <circle cx="256" cy="320" r="5" fill="#ffeab0"/>
    <circle cx="308" cy="364" r="4.5" fill="#ffcf6e"/>
    <circle cx="232" cy="404" r="4" fill="#f2a849"/>
    <circle cx="282" cy="414" r="4" fill="#ffe299"/>
  </g>
</svg>
```