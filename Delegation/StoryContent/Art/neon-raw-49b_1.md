`Art/Weapons/TMM_Sig_castiel_angel_blade.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="ab_aura_outer" cx="56%" cy="50%" r="48%" fx="56%" fy="50%">
      <stop offset="0%" stop-color="#8cd6ff" stop-opacity="0.34"/>
      <stop offset="48%" stop-color="#5ca9d6" stop-opacity="0.16"/>
      <stop offset="80%" stop-color="#3c7aa3" stop-opacity="0.05"/>
      <stop offset="100%" stop-color="#2b5d80" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="ab_aura_inner" cx="62%" cy="50%" r="36%">
      <stop offset="0%" stop-color="#d6f4ff" stop-opacity="0.45"/>
      <stop offset="55%" stop-color="#7bc9f5" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#5ca9d6" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="ab_blade_upper" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#b8c9d6"/>
      <stop offset="28%" stop-color="#eef7fc"/>
      <stop offset="65%" stop-color="#c9dde8"/>
      <stop offset="100%" stop-color="#e4f3fa"/>
    </linearGradient>
    <linearGradient id="ab_blade_lower" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#536270"/>
      <stop offset="40%" stop-color="#798c9e"/>
      <stop offset="78%" stop-color="#4e5e6e"/>
      <stop offset="100%" stop-color="#6b8094"/>
    </linearGradient>
    <linearGradient id="ab_blade_flute" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#2e3945"/>
      <stop offset="50%" stop-color="#5d7082"/>
      <stop offset="100%" stop-color="#9cb2c4"/>
    </linearGradient>
    <linearGradient id="ab_grip" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#cad6e0"/>
      <stop offset="28%" stop-color="#92a2b0"/>
      <stop offset="62%" stop-color="#586673"/>
      <stop offset="100%" stop-color="#37424d"/>
    </linearGradient>
    <linearGradient id="ab_collar" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3edf5"/>
      <stop offset="40%" stop-color="#9bb0c2"/>
      <stop offset="75%" stop-color="#5d6e7d"/>
      <stop offset="100%" stop-color="#3c4854"/>
    </linearGradient>
    <linearGradient id="ab_pommel" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#dce7f0"/>
      <stop offset="50%" stop-color="#7c8e9e"/>
      <stop offset="100%" stop-color="#3b454f"/>
    </linearGradient>
    <linearGradient id="ab_ridge_shine" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#9ec0d6" stop-opacity="0.7"/>
      <stop offset="35%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="75%" stop-color="#c9eeff" stop-opacity="0.9"/>
      <stop offset="100%" stop-color="#ffffff" stop-opacity="0.95"/>
    </linearGradient>
    <radialGradient id="ab_spark" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.95"/>
      <stop offset="45%" stop-color="#9be0ff" stop-opacity="0.55"/>
      <stop offset="100%" stop-color="#69b8e8" stop-opacity="0"/>
    </radialGradient>
  </defs>

  <!-- Soft celestial blue aura (no filters) -->
  <ellipse cx="290" cy="256" rx="220" ry="102" fill="url(#ab_aura_outer)"/>
  <ellipse cx="328" cy="256" rx="170" ry="52" fill="url(#ab_aura_inner)"/>

  <!-- Subtle ethereal glints -->
  <circle cx="468" cy="256" r="24" fill="url(#ab_spark)"/>
  <path d="M 358,214 L 362,226 L 374,230 L 362,234 L 358,246 L 354,234 L 342,230 L 354,226 Z" fill="#d6f4ff" opacity="0.65"/>
  <path d="M 278,282 L 280,290 L 288,292 L 280,294 L 278,302 L 276,294 L 268,292 L 276,290 Z" fill="#b3e6ff" opacity="0.55"/>

  <!-- Outer silhouette base stroke for thick crisp dark contour -->
  <path d="M 40,256
           C 40,242 54,234 72,235
           L 132,238
           C 146,238 156,232 166,230
           L 184,226
           C 265,227 374,236 476,256
           C 374,276 265,285 184,286
           L 166,282
           C 156,280 146,274 132,274
           L 72,277
           C 54,278 40,270 40,256 Z"
        fill="#121519" stroke="#121519" stroke-width="14" stroke-linejoin="round" stroke-linecap="round"/>

  <!-- Pommel -->
  <path d="M 42,256 C 42,243 55,236 74,237 L 78,256 L 74,275 C 55,276 42,269 42,256 Z"
        fill="url(#ab_pommel)"/>

  <!-- Cylindrical contoured grip (guardless) -->
  <path d="M 73,237 L 154,235 L 158,256 L 154,277 L 73,275 Z"
        fill="url(#ab_grip)"/>

  <!-- Grip subtle machined rings & grooves -->
  <path d="M 88,237 L 88,275 M 102,237 L 102,275 M 116,236 L 116,276 M 130,236 L 130,276 M 144,235 L 144,277"
        stroke="#262e36" stroke-width="3.5" stroke-linecap="round" opacity="0.8"/>
  <path d="M 91,239 L 91,260 M 105,239 L 105,260 M 119,238 L 119,260 M 133,238 L 133,260 M 147,237 L 147,260"
        stroke="#d7e4ed" stroke-width="2" stroke-linecap="round" opacity="0.45"/>

  <!-- Smooth seamless bolster (no crossguard) -->
  <path d="M 152,235 C 160,234 168,230 183,227 L 190,256 L 183,285 C 168,282 160,278 152,277 Z"
        fill="url(#ab_collar)"/>
  <path d="M 154,235 L 154,277" stroke="#121519" stroke-width="4.5"/>
  <path d="M 183,227 L 188,256 L 183,285" fill="none" stroke="#121519" stroke-width="4.5" stroke-linejoin="round"/>

  <!-- Three-edged silver blade: upper bright facet -->
  <path d="M 184,227 C 266,228 374,237 476,256 L 188,256 Z"
        fill="url(#ab_blade_upper)"/>

  <!-- Three-edged silver blade: lower shaded facet -->
  <path d="M 188,256 L 476,256 C 374,275 266,284 184,285 Z"
        fill="url(#ab_blade_lower)"/>

  <!-- Recessed triangular third-face bevel / flute near bolster -->
  <path d="M 188,243 L 266,256 L 188,269 Z"
        fill="url(#ab_blade_flute)" opacity="0.82"/>
  <path d="M 188,243 L 266,256 L 188,269"
        fill="none" stroke="#222b33" stroke-width="2.5" stroke-linejoin="round" opacity="0.75"/>

  <!-- Reflected cyan edge gleam along upper and lower edges -->
  <path d="M 192,232 C 272,233 372,241 464,255"
        fill="none" stroke="#d9f4ff" stroke-width="3" stroke-linecap="round" opacity="0.8"/>
  <path d="M 196,280 C 274,278 370,270 460,257"
        fill="none" stroke="#7ccbf2" stroke-width="2.5" stroke-linecap="round" opacity="0.55"/>

  <!-- Prominent central triangular spine ridge -->
  <path d="M 188,256 L 474,256"
        stroke="#27323d" stroke-width="5" stroke-linecap="round" opacity="0.65"/>
  <path d="M 188,254.5 L 472,255.5"
        stroke="url(#ab_ridge_shine)" stroke-width="3.2" stroke-linecap="round"/>

  <!-- Specular highlight streak on grip & pommel -->
  <path d="M 52,245 C 62,242 74,242 148,241"
        fill="none" stroke="#edf6fc" stroke-width="3.5" stroke-linecap="round" opacity="0.55"/>
  <path d="M 160,238 L 178,234"
        fill="none" stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.65"/>

  <!-- Re-stroke crisp outer outline to ensure unbroken thick dark edge -->
  <path d="M 40,256
           C 40,242 54,234 72,235
           L 132,238
           C 146,238 156,232 166,230
           L 184,226
           C 265,227 374,236 476,256
           C 374,276 265,285 184,286
           L 166,282
           C 156,280 146,274 132,274
           L 72,277
           C 54,278 40,270 40,256 Z"
        fill="none" stroke="#121519" stroke-width="12" stroke-linejoin="round" stroke-linecap="round"/>
</svg>
```

`Art/Weapons/TMM_Gun_BoraxTurret.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="btg_nozzle_body" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#3a434b"/>
      <stop offset="25%" stop-color="#6c7985"/>
      <stop offset="50%" stop-color="#8795a1"/>
      <stop offset="76%" stop-color="#535e69"/>
      <stop offset="100%" stop-color="#2c333b"/>
    </linearGradient>
    <linearGradient id="btg_nozzle_lip" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#444e57"/>
      <stop offset="30%" stop-color="#8a98a3"/>
      <stop offset="55%" stop-color="#a5b2bd"/>
      <stop offset="82%" stop-color="#5c6773"/>
      <stop offset="100%" stop-color="#313840"/>
    </linearGradient>
    <linearGradient id="btg_nozzle_in" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#191e24"/>
      <stop offset="65%" stop-color="#2e3740"/>
      <stop offset="100%" stop-color="#4e5a66"/>
    </linearGradient>
    <linearGradient id="btg_barrel" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#323a42"/>
      <stop offset="30%" stop-color="#616e7a"/>
      <stop offset="55%" stop-color="#768591"/>
      <stop offset="85%" stop-color="#46515c"/>
      <stop offset="100%" stop-color="#272e36"/>
    </linearGradient>
    <linearGradient id="btg_cradle" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#606b73"/>
      <stop offset="45%" stop-color="#444d54"/>
      <stop offset="100%" stop-color="#2a3036"/>
    </linearGradient>
    <linearGradient id="btg_cap" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7a8791"/>
      <stop offset="50%" stop-color="#4e5861"/>
      <stop offset="100%" stop-color="#2f363d"/>
    </linearGradient>
    <linearGradient id="btg_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#cfb26f"/>
      <stop offset="45%" stop-color="#9c7e42"/>
      <stop offset="100%" stop-color="#5e4921"/>
    </linearGradient>
    <linearGradient id="btg_pipe" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#614c24"/>
      <stop offset="45%" stop-color="#b89854"/>
      <stop offset="100%" stop-color="#57431f"/>
    </linearGradient>
    <linearGradient id="btg_powder" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#c3ced4"/>
      <stop offset="40%" stop-color="#f2f6f7"/>
      <stop offset="75%" stop-color="#dde6ea"/>
      <stop offset="100%" stop-color="#aebbc2"/>
    </linearGradient>
    <linearGradient id="btg_dust" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#eef4f7" stop-opacity="0.7"/>
      <stop offset="100%" stop-color="#d4e0e6" stop-opacity="0"/>
    </linearGradient>
  </defs>

  <!-- Twin side copper/brass pressurization pipes -->
  <path d="M 198,256 L 198,176 C 198,158 212,148 228,148"
        fill="none" stroke="#121519" stroke-width="26" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="M 198,256 L 198,176 C 198,158 212,148 228,148"
        fill="none" stroke="url(#btg_pipe)" stroke-width="11" stroke-linecap="round" stroke-linejoin="round"/>

  <path d="M 314,256 L 314,176 C 314,158 300,148 284,148"
        fill="none" stroke="#121519" stroke-width="26" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="M 314,256 L 314,176 C 314,158 300,148 284,148"
        fill="none" stroke="url(#btg_pipe)" stroke-width="11" stroke-linecap="round" stroke-linejoin="round"/>

  <!-- Rear pressurized feed manifold & counterweight box -->
  <rect x="206" y="316" width="100" height="86" rx="16"
        fill="url(#btg_cradle)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  <!-- Rear hazard accent bar -->
  <rect x="220" y="370" width="72" height="18" rx="4"
        fill="#b58f31" stroke="#121519" stroke-width="5"/>
  <path d="M 232,370 L 224,388 M 250,370 L 242,388 M 268,370 L 260,388 M 286,370 L 278,388"
        stroke="#1c2126" stroke-width="7"/>
  <!-- Rear pressure valve wheel / dial -->
  <circle cx="256" cy="346" r="18" fill="url(#btg_brass)" stroke="#121519" stroke-width="6"/>
  <circle cx="256" cy="346" r="9" fill="#d7e1e6" stroke="#121519" stroke-width="3"/>
  <path d="M 256,346 L 262,340" stroke="#992b24" stroke-width="3" stroke-linecap="round"/>

  <!-- Swivel mount side trunnion brackets (centered on y=256) -->
  <rect x="160" y="216" width="192" height="80" rx="18"
        fill="url(#btg_cradle)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  <!-- Side trunnion pivot bolts -->
  <rect x="148" y="238" width="22" height="36" rx="6"
        fill="url(#btg_brass)" stroke="#121519" stroke-width="9"/>
  <rect x="342" y="238" width="22" height="36" rx="6"
        fill="url(#btg_brass)" stroke="#121519" stroke-width="9"/>

  <!-- Main pressurized barrel & breech body -->
  <rect x="218" y="138" width="76" height="188" rx="12"
        fill="url(#btg_barrel)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

  <!-- Clamp bands on barrel -->
  <rect x="212" y="168" width="88" height="18" rx="5"
        fill="url(#btg_cradle)" stroke="#121519" stroke-width="7"/>
  <rect x="212" y="294" width="88" height="18" rx="5"
        fill="url(#btg_cradle)" stroke="#121519" stroke-width="7"/>

  <!-- Flared borax sprayer diffuser nozzle (pointing North) -->
  <path d="M 222,150
           L 174,78
           L 174,56
           C 174,48 210,42 256,42
           C 302,42 338,48 338,56
           L 338,78
           L 290,150 Z"
        fill="url(#btg_nozzle_body)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

  <!-- Recessed nozzle mouth opening at very top -->
  <ellipse cx="256" cy="56" rx="74" ry="11"
           fill="url(#btg_nozzle_in)" stroke="#121519" stroke-width="7"/>
  <!-- Internal atomizer diffuser bar & brass tip inside nozzle -->
  <line x1="196" y1="56" x2="316" y2="56" stroke="#7d6433" stroke-width="4.5" stroke-linecap="round"/>
  <ellipse cx="256" cy="56" rx="18" ry="5" fill="url(#btg_brass)" stroke="#121519" stroke-width="3"/>

  <!-- Reinforced nozzle lip collar -->
  <path d="M 174,64 C 174,72 210,77 256,77 C 302,77 338,72 338,64 L 338,80 C 338,88 302,93 256,93 C 210,93 174,88 174,80 Z"
        fill="url(#btg_nozzle_lip)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>

  <!-- Subtle white borax powder dusting on nozzle rim -->
  <path d="M 192,76 C 214,82 298,82 320,76 L 306,102 C 284,106 228,106 206,102 Z"
        fill="url(#btg_dust)"/>

  <!-- Longitudinal stiffening ribs on flared nozzle -->
  <path d="M 234,146 L 204,88 M 256,146 L 256,90 M 278,146 L 308,88"
        stroke="#272f36" stroke-width="5.5" stroke-linecap="round"/>
  <path d="M 237,145 L 208,89 M 259,145 L 259,90"
        stroke="#a9b7c2" stroke-width="2" stroke-linecap="round" opacity="0.5"/>

  <!-- Central rotary pivot hub (centered at 256,256) -->
  <circle cx="256" cy="256" r="46"
          fill="url(#btg_cap)" stroke="#121519" stroke-width="10"/>
  <circle cx="256" cy="256" r="32"
          fill="#343c44" stroke="#121519" stroke-width="5"/>

  <!-- Sight glass window on top of central chamber showing white borax powder -->
  <rect x="242" y="200" width="28" height="112" rx="14"
        fill="url(#btg_powder)" stroke="#121519" stroke-width="7"/>
  <!-- Powder grain specks inside sight window -->
  <circle cx="251" cy="224" r="3" fill="#ffffff" opacity="0.9"/>
  <circle cx="261" cy="242" r="2.5" fill="#b7c6cf" opacity="0.7"/>
  <circle cx="253" cy="264" r="3" fill="#ffffff" opacity="0.85"/>
  <circle cx="260" cy="285" r="2.5" fill="#b7c6cf" opacity="0.75"/>
  <!-- Glass specular reflection streak -->
  <path d="M 248,212 L 248,298" stroke="#ffffff" stroke-width="3.5" stroke-linecap="round" opacity="0.7"/>

  <!-- Steel rivets on swivel cradle & nozzle -->
  <g fill="#8c9aa6" stroke="#121519" stroke-width="3.5">
    <circle cx="182" cy="232" r="6"/>
    <circle cx="182" cy="280" r="6"/>
    <circle cx="330" cy="232" r="6"/>
    <circle cx="330" cy="280" r="6"/>
    <circle cx="222" cy="332" r="5"/>
    <circle cx="290" cy="332" r="5"/>
    <circle cx="196" cy="82" r="4.5"/>
    <circle cx="316" cy="82" r="4.5"/>
  </g>
</svg>
```

`Art/Buildings/TMM_Turret_Borax.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="btb_hull" x1="15%" y1="10%" x2="85%" y2="90%">
      <stop offset="0%" stop-color="#64707a"/>
      <stop offset="45%" stop-color="#48525c"/>
      <stop offset="100%" stop-color="#2c333b"/>
    </linearGradient>
    <linearGradient id="btb_deck" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#57636d"/>
      <stop offset="50%" stop-color="#3f4852"/>
      <stop offset="100%" stop-color="#2a3138"/>
    </linearGradient>
    <linearGradient id="btb_foot" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#4c555e"/>
      <stop offset="100%" stop-color="#232930"/>
    </linearGradient>
    <radialGradient id="btb_ring_outer" cx="42%" cy="38%" r="60%">
      <stop offset="0%" stop-color="#7c8a96"/>
      <stop offset="55%" stop-color="#4d5761"/>
      <stop offset="100%" stop-color="#2c333b"/>
    </radialGradient>
    <radialGradient id="btb_ring_pit" cx="50%" cy="50%" r="55%">
      <stop offset="0%" stop-color="#15191f"/>
      <stop offset="72%" stop-color="#232a33"/>
      <stop offset="100%" stop-color="#353f4a"/>
    </radialGradient>
    <linearGradient id="btb_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d4b56c"/>
      <stop offset="50%" stop-color="#997a3c"/>
      <stop offset="100%" stop-color="#5c471e"/>
    </linearGradient>
    <linearGradient id="btb_tank_metal" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#3e4750"/>
      <stop offset="30%" stop-color="#7a8894"/>
      <stop offset="55%" stop-color="#94a3b0"/>
      <stop offset="85%" stop-color="#4f5a66"/>
      <stop offset="100%" stop-color="#2d343b"/>
    </linearGradient>
    <linearGradient id="btb_powder" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#c3cfd6"/>
      <stop offset="35%" stop-color="#f2f6f8"/>
      <stop offset="68%" stop-color="#e3ebef"/>
      <stop offset="100%" stop-color="#b4c2ca"/>
    </linearGradient>
    <linearGradient id="btb_pump" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#606d61"/>
      <stop offset="50%" stop-color="#455046"/>
      <stop offset="100%" stop-color="#2b332c"/>
    </linearGradient>
    <linearGradient id="btb_yellow" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#cfa63c"/>
      <stop offset="100%" stop-color="#9e7a22"/>
    </linearGradient>
    <clipPath id="btb_clip_haz_n">
      <polygon points="168,88 344,88 326,120 186,120"/>
    </clipPath>
    <clipPath id="btb_clip_haz_s">
      <polygon points="186,392 326,392 344,424 168,424"/>
    </clipPath>
  </defs>

  <!-- 6 heavy corner anchor stabilizer lugs/feet -->
  <g fill="url(#btb_foot)" stroke="#121519" stroke-width="12" stroke-linejoin="round">
    <rect x="430" y="232" width="44" height="48" rx="10"/>
    <rect x="38" y="232" width="44" height="48" rx="10"/>
    <polygon points="344,62 384,74 374,112 334,98"/>
    <polygon points="168,62 128,74 138,112 178,98"/>
    <polygon points="344,450 384,438 374,400 334,414"/>
    <polygon points="168,450 128,438 138,400 178,414"/>
  </g>

  <!-- Main hexagonal armored base platform -->
  <polygon points="458,256 357,431 155,431 54,256 155,81 357,81"
           fill="url(#btb_hull)" stroke="#121519" stroke-width="14" stroke-linejoin="round"/>

  <!-- Inner beveled armored deck -->
  <polygon points="424,256 340,402 172,402 88,256 172,110 340,110"
           fill="url(#btb_deck)" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>

  <!-- Subtle welded floor panel seams -->
  <g stroke="#22282e" stroke-width="4" opacity="0.7">
    <line x1="88" y1="256" x2="184" y2="256"/>
    <line x1="328" y1="256" x2="424" y2="256"/>
    <line x1="172" y1="110" x2="218" y2="190"/>
    <line x1="340" y1="110" x2="294" y2="190"/>
    <line x1="172" y1="402" x2="218" y2="322"/>
    <line x1="340" y1="402" x2="294" y2="322"/>
  </g>

  <!-- North yellow-black hazard stripe plate -->
  <g clip-path="url(#btb_clip_haz_n)">
    <rect x="160" y="84" width="192" height="40" fill="url(#btb_yellow)"/>
    <g stroke="#1b2026" stroke-width="15">
      <line x1="174" y1="126" x2="196" y2="82"/>
      <line x1="208" y1="126" x2="230" y2="82"/>
      <line x1="242" y1="126" x2="264" y2="82"/>
      <line x1="276" y1="126" x2="298" y2="82"/>
      <line x1="310" y1="126" x2="332" y2="82"/>
      <line x1="344" y1="126" x2="366" y2="82"/>
    </g>
  </g>
  <polygon points="168,88 344,88 326,120 186,120"
           fill="none" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- South yellow-black hazard stripe plate -->
  <g clip-path="url(#btb_clip_haz_s)">
    <rect x="160" y="388" width="192" height="40" fill="url(#btb_yellow)"/>
    <g stroke="#1b2026" stroke-width="15">
      <line x1="174" y1="430" x2="196" y2="386"/>
      <line x1="208" y1="430" x2="230" y2="386"/>
      <line x1="242" y1="430" x2="264" y2="386"/>
      <line x1="276" y1="430" x2="298" y2="386"/>
      <line x1="310" y1="430" x2="332" y2="386"/>
      <line x1="344" y1="430" x2="366" y2="386"/>
    </g>
  </g>
  <polygon points="186,392 326,392 344,424 168,424"
           fill="none" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Armored feed hoses connecting Borax Tank and Compressor to central turret ring -->
  <path d="M 340,214 C 314,214 306,226 296,236"
        fill="none" stroke="#121519" stroke-width="20" stroke-linecap="round"/>
  <path d="M 340,214 C 314,214 306,226 296,236"
        fill="none" stroke="#7d6433" stroke-width="8" stroke-linecap="round"/>

  <path d="M 340,298 C 314,298 306,286 296,276"
        fill="none" stroke="#121519" stroke-width="20" stroke-linecap="round"/>
  <path d="M 340,298 C 314,298 306,286 296,276"
        fill="none" stroke="#5c6873" stroke-width="8" stroke-linecap="round"/>

  <path d="M 174,256 L 204,256"
        fill="none" stroke="#121519" stroke-width="22" stroke-linecap="round"/>
  <path d="M 174,256 L 204,256"
        fill="none" stroke="#8f723b" stroke-width="9" stroke-linecap="round"/>

  <!-- Left side: compact armored pressure pump / compressor unit -->
  <rect x="114" y="196" width="66" height="120" rx="12"
        fill="url(#btb_pump)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
  <!-- Cooling fins on pump -->
  <g stroke="#222823" stroke-width="4.5" stroke-linecap="round">
    <line x1="126" y1="214" x2="168" y2="214"/>
    <line x1="126" y1="226" x2="168" y2="226"/>
    <line x1="126" y1="238" x2="168" y2="238"/>
  </g>
  <!-- Brass pressure gauge on pump -->
  <circle cx="147" cy="278" r="19" fill="url(#btb_brass)" stroke="#121519" stroke-width="5.5"/>
  <circle cx="147" cy="278" r="11" fill="#dbe3e6" stroke="#121519" stroke-width="2.5"/>
  <path d="M 147,278 L 154,271" stroke="#a63329" stroke-width="3" stroke-linecap="round"/>

  <!-- Right side: pressurized cylindrical Borax Tank with white powder window -->
  <rect x="330" y="174" width="74" height="164" rx="28"
        fill="url(#btb_tank_metal)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
  <!-- Brass valve cap on top end of tank -->
  <circle cx="367" cy="186" r="10" fill="url(#btb_brass)" stroke="#121519" stroke-width="4.5"/>

  <!-- Viewing window showing bright white borax powder -->
  <rect x="348" y="204" width="38" height="104" rx="14"
        fill="url(#btb_powder)" stroke="#121519" stroke-width="6.5"/>
  <!-- Powder mound texture & subtle granular shading inside window -->
  <path d="M 351,232 Q 367,224 383,234 L 383,304 Q 367,308 351,304 Z"
        fill="#dce5ea" opacity="0.55"/>
  <circle cx="359" cy="222" r="3.2" fill="#ffffff"/>
  <circle cx="372" cy="242" r="2.8" fill="#ffffff"/>
  <circle cx="362" cy="266" r="3.5" fill="#ffffff"/>
  <circle cx="374" cy="286" r="3" fill="#ffffff"/>
  <circle cx="366" cy="252" r="2.2" fill="#aebcc4"/>
  <circle cx="358" cy="280" r="2.4" fill="#aebcc4"/>
  <!-- Measurement tick lines on glass -->
  <g stroke="#4c5c66" stroke-width="2.5" stroke-linecap="round" opacity="0.65">
    <line x1="373" y1="226" x2="382" y2="226"/>
    <line x1="373" y1="246" x2="382" y2="246"/>
    <line x1="373" y1="266" x2="382" y2="266"/>
    <line x1="373" y1="286" x2="382" y2="286"/>
  </g>
  <!-- Crisp specular glass highlight -->
  <path d="M 355,214 L 355,296" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.8"/>

  <!-- Heavy steel tank mounting straps (clamps) -->
  <rect x="325" y="216" width="84" height="14" rx="4"
        fill="#39424a" stroke="#121519" stroke-width="5.5"/>
  <rect x="325" y="282" width="84" height="14" rx="4"
        fill="#39424a" stroke="#121519" stroke-width="5.5"/>

  <!-- Central turret rotary bearing ring (socket for TMM_Gun_BoraxTurret) -->
  <circle cx="256" cy="256" r="68"
          fill="url(#btb_ring_outer)" stroke="#121519" stroke-width="11"/>
  <!-- Recessed dark mechanical pit -->
  <circle cx="256" cy="256" r="46"
          fill="url(#btb_ring_pit)" stroke="#121519" stroke-width="6"/>
  <!-- Internal bearing race dashes -->
  <circle cx="256" cy="256" r="34"
          fill="none" stroke="#535f6b" stroke-width="6" stroke-dasharray="12 9"/>
  <!-- Center brass/steel spindle mount -->
  <circle cx="256" cy="256" r="18"
          fill="url(#btb_brass)" stroke="#121519" stroke-width="5.5"/>
  <circle cx="256" cy="256" r="7"
          fill="#1b2026"/>

  <!-- Bolts on central turret ring -->
  <g fill="#94a3b0" stroke="#121519" stroke-width="3">
    <circle cx="256" cy="199" r="5"/>
    <circle cx="296" cy="216" r="5"/>
    <circle cx="313" cy="256" r="5"/>
    <circle cx="296" cy="296" r="5"/>
    <circle cx="256" cy="313" r="5"/>
    <circle cx="216" cy="296" r="5"/>
    <circle cx="199" cy="256" r="5"/>
    <circle cx="216" cy="216" r="5"/>
  </g>

  <!-- Perimeter armored deck rivets -->
  <g fill="#8c9ba8" stroke="#121519" stroke-width="3.5">
    <circle cx="152" cy="116" r="6.5"/>
    <circle cx="360" cy="116" r="6.5"/>
    <circle cx="424" cy="222" r="6.5"/>
    <circle cx="424" cy="290" r="6.5"/>
    <circle cx="360" cy="396" r="6.5"/>
    <circle cx="152" cy="396" r="6.5"/>
    <circle cx="88" cy="290" r="6.5"/>
    <circle cx="88" cy="222" r="6.5"/>
    <circle cx="118" cy="168" r="6"/>
    <circle cx="394" cy="168" r="6"/>
    <circle cx="394" cy="344" r="6"/>
    <circle cx="118" cy="344" r="6"/>
  </g>
</svg>
```