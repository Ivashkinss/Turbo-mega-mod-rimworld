`Art/Projectiles/TMM_Bullet_Borax.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="brx_mist_aura" cx="50%" cy="38%" r="48%">
      <stop offset="0%" stop-color="#f2f8fa" stop-opacity="0.48"/>
      <stop offset="52%" stop-color="#bfd0d9" stop-opacity="0.22"/>
      <stop offset="100%" stop-color="#7c919e" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="brx_wake_trail" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#edf5f7" stop-opacity="0.78"/>
      <stop offset="40%" stop-color="#c6d6de" stop-opacity="0.46"/>
      <stop offset="76%" stop-color="#8fa3ad" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#5d6f78" stop-opacity="0"/>
    </linearGradient>
    <radialGradient id="brx_cloud_head" cx="44%" cy="34%" r="64%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="44%" stop-color="#edf4f7"/>
      <stop offset="78%" stop-color="#c2d2da"/>
      <stop offset="100%" stop-color="#8fa2ad"/>
    </radialGradient>
    <radialGradient id="brx_cloud_mid" cx="46%" cy="38%" r="62%">
      <stop offset="0%" stop-color="#f7fbfc"/>
      <stop offset="52%" stop-color="#d5e2e8"/>
      <stop offset="100%" stop-color="#96a9b3"/>
    </radialGradient>
    <linearGradient id="brx_cloud_shade" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#dce7ec"/>
      <stop offset="60%" stop-color="#aebfc9"/>
      <stop offset="100%" stop-color="#7c8f99"/>
    </linearGradient>
    <linearGradient id="brx_grain_light" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="60%" stop-color="#dbe6eb"/>
      <stop offset="100%" stop-color="#9eb1bc"/>
    </linearGradient>
  </defs>

  <!-- Soft airborne mineral dust halo -->
  <ellipse cx="256" cy="238" rx="72" ry="172" fill="url(#brx_mist_aura)"/>

  <!-- Tapered powdery slipstream plume trailing South -->
  <path d="M 218,214
           C 206,274 226,348 256,422
           C 286,348 306,274 294,214 Z"
        fill="url(#brx_wake_trail)"/>

  <!-- Secondary detached trailing powder puffs -->
  <path d="M 232,298
           C 220,308 218,328 232,338
           C 236,352 252,360 264,352
           C 278,354 290,340 284,324
           C 292,312 284,296 270,296 Z"
        fill="url(#brx_cloud_mid)" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>
  <path d="M 240,312 C 234,322 238,334 250,338" fill="none" stroke="#ffffff" stroke-width="3.5" stroke-linecap="round" opacity="0.72"/>

  <!-- Small trailing tail puff -->
  <path d="M 244,368
           C 236,374 238,388 248,392
           C 254,398 266,396 270,386
           C 276,378 268,366 258,368 Z"
        fill="url(#brx_cloud_shade)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>

  <!-- Main billowing white borax powder cloud head (pointing North) -->
  <path d="M 256,92
           C 274,92 286,108 284,126
           C 302,134 312,156 304,176
           C 316,192 314,220 298,234
           C 304,254 292,276 274,280
           C 268,294 244,294 238,280
           C 220,276 208,254 214,234
           C 198,220 196,192 208,176
           C 200,156 210,134 228,126
           C 226,108 238,92 256,92 Z"
        fill="url(#brx_cloud_head)" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>

  <!-- Internal billowing cloud lobes & mineral shading for fluffy powdery volume -->
  <path d="M 224,226 C 236,244 268,246 284,228 C 292,250 278,274 256,276 C 232,276 218,252 224,226 Z"
        fill="url(#brx_cloud_shade)" opacity="0.58"/>
  <path d="M 232,148 C 244,132 272,134 282,154"
        fill="none" stroke="#99acb6" stroke-width="4" stroke-linecap="round" opacity="0.65"/>
  <path d="M 218,194 C 232,182 252,184 264,198"
        fill="none" stroke="#99acb6" stroke-width="4" stroke-linecap="round" opacity="0.62"/>
  <path d="M 262,182 C 276,174 292,182 296,198"
        fill="none" stroke="#99acb6" stroke-width="3.5" stroke-linecap="round" opacity="0.58"/>
  <path d="M 236,232 C 250,224 270,228 280,242"
        fill="none" stroke="#8ea2ad" stroke-width="3.8" stroke-linecap="round" opacity="0.6"/>

  <!-- Bright sunlit chalk-white leading powder highlights -->
  <path d="M 244,104 C 252,98 264,99 272,108" fill="none" stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.92"/>
  <path d="M 218,144 C 212,154 212,166 218,176" fill="none" stroke="#ffffff" stroke-width="4.5" stroke-linecap="round" opacity="0.86"/>
  <path d="M 210,196 C 206,208 210,220 220,228" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.8"/>

  <!-- Granular white borax powder specks & mineral dust grains inside and trailing -->
  <g fill="#ffffff" opacity="0.95">
    <circle cx="256" cy="122" r="4"/>
    <circle cx="238" cy="164" r="3.5"/>
    <circle cx="272" cy="168" r="3"/>
    <circle cx="248" cy="208" r="4"/>
    <circle cx="276" cy="216" r="3.2"/>
    <circle cx="258" cy="256" r="3.5"/>
  </g>

  <!-- Crisp outlined borax crystal grains dispersing around the puff -->
  <g stroke="#121519" stroke-width="4.5" stroke-linejoin="round">
    <polygon points="204,132 212,126 218,136 210,142" fill="url(#brx_grain_light)"/>
    <polygon points="304,142 312,138 316,148 308,152" fill="url(#brx_grain_light)"/>
    <circle cx="198" cy="254" r="6" fill="#edf4f7"/>
    <circle cx="312" cy="264" r="5.5" fill="#dce7ec"/>
    <polygon points="218,354 226,350 230,360 222,364" fill="#ffffff"/>
    <circle cx="288" cy="368" r="5" fill="#e6f0f5"/>
    <circle cx="256" cy="414" r="5" fill="#d5e2e8"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_BoraxTurret.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="bxt_mist_aura" cx="50%" cy="38%" r="52%">
      <stop offset="0%" stop-color="#f5fafc" stop-opacity="0.54"/>
      <stop offset="52%" stop-color="#bfd2db" stop-opacity="0.26"/>
      <stop offset="100%" stop-color="#79909c" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="bxt_wake_plume" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f2f8fa" stop-opacity="0.86"/>
      <stop offset="38%" stop-color="#cadbe3" stop-opacity="0.56"/>
      <stop offset="76%" stop-color="#8fa5b0" stop-opacity="0.24"/>
      <stop offset="100%" stop-color="#5c6f78" stop-opacity="0"/>
    </linearGradient>
    <radialGradient id="bxt_cloud_main" cx="44%" cy="32%" r="66%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="42%" stop-color="#eef5f8"/>
      <stop offset="76%" stop-color="#c3d4dc"/>
      <stop offset="100%" stop-color="#8da2ad"/>
    </radialGradient>
    <radialGradient id="bxt_cloud_mid" cx="45%" cy="36%" r="64%">
      <stop offset="0%" stop-color="#f7fbfc"/>
      <stop offset="50%" stop-color="#d4e2e8"/>
      <stop offset="100%" stop-color="#92a7b2"/>
    </radialGradient>
    <linearGradient id="bxt_cloud_deep" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d9e6ec"/>
      <stop offset="55%" stop-color="#a8bcc7"/>
      <stop offset="100%" stop-color="#768b96"/>
    </linearGradient>
    <linearGradient id="bxt_chunk" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="55%" stop-color="#dce7ed"/>
      <stop offset="100%" stop-color="#98adb8"/>
    </linearGradient>
  </defs>

  <!-- Broad pressurized airborne borax mist envelope (larger than handheld puff) -->
  <ellipse cx="256" cy="244" rx="110" ry="194" fill="url(#bxt_mist_aura)"/>

  <!-- Heavy pressurized diffuser spray wake tapering South -->
  <path d="M 184,218
           C 172,298 206,380 256,456
           C 306,380 340,298 328,218 Z"
        fill="url(#bxt_wake_plume)"/>

  <!-- Pressurized swirling airflow streaks flanking the cloud -->
  <path d="M 182,148 C 154,194 160,256 196,304" fill="none" stroke="#e4eff4" stroke-width="5" stroke-linecap="round" opacity="0.65"/>
  <path d="M 330,148 C 358,194 352,256 316,304" fill="none" stroke="#d5e4eb" stroke-width="5" stroke-linecap="round" opacity="0.65"/>

  <!-- Lower trailing dense billowing sub-cloud -->
  <path d="M 212,306
           C 192,318 190,348 208,364
           C 210,384 230,398 248,392
           C 256,406 278,404 286,390
           C 306,390 322,368 312,346
           C 324,330 314,306 292,304 Z"
        fill="url(#bxt_cloud_mid)" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>
  <path d="M 218,328 C 212,342 218,356 232,364" fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" opacity="0.75"/>
  <path d="M 238,348 C 254,338 278,342 290,358" fill="none" stroke="#8da2ad" stroke-width="4" stroke-linecap="round" opacity="0.62"/>

  <!-- Tail vortex puff at southern end -->
  <path d="M 234,404
           C 224,414 228,432 242,438
           C 250,448 268,446 274,434
           C 286,426 282,406 266,404 Z"
        fill="url(#bxt_cloud_deep)" stroke="#121519" stroke-width="6.5" stroke-linejoin="round"/>

  <!-- Massive billowing pressurized turret borax cloud head (pointing North) -->
  <path d="M 256,58
           C 282,58 302,78 300,102
           C 326,110 344,136 336,164
           C 356,184 358,218 340,242
           C 352,266 340,298 316,308
           C 306,328 282,336 264,328
           C 252,338 228,334 218,318
           C 192,316 168,292 174,264
           C 156,242 156,204 174,182
           C 164,154 180,122 208,112
           C 206,82 228,58 256,58 Z"
        fill="url(#bxt_cloud_main)" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>

  <!-- Volumetric shadowed under-billows inside main heavy cloud -->
  <path d="M 192,248 C 216,274 282,278 318,252 C 330,282 310,318 268,322 C 222,326 184,292 192,248 Z"
        fill="url(#bxt_cloud_deep)" opacity="0.62"/>
  <path d="M 198,176 C 212,196 240,204 266,194 C 244,218 206,216 188,194 Z"
        fill="#a3b7c2" opacity="0.48"/>

  <!-- Sculpted rolling cumulus crease lines inside the powder blast -->
  <g fill="none" stroke="#8ea3ae" stroke-width="4.5" stroke-linecap="round" opacity="0.68">
    <path d="M 222,126 C 238,106 276,108 292,130"/>
    <path d="M 190,178 C 208,158 238,160 254,180"/>
    <path d="M 264,166 C 284,150 314,160 322,186"/>
    <path d="M 212,238 C 232,220 264,222 284,240"/>
    <path d="M 278,224 C 298,214 322,226 326,248"/>
    <path d="M 226,286 C 246,276 274,278 290,294"/>
  </g>

  <!-- Crisp sunlit chalk-white rim highlights across leading North billows -->
  <g fill="none" stroke="#ffffff" stroke-linecap="round" opacity="0.94">
    <path d="M 234,74 C 248,65 268,66 282,78" stroke-width="6"/>
    <path d="M 190,136 C 180,150 180,166 188,180" stroke-width="5.5"/>
    <path d="M 174,202 C 168,218 174,238 188,250" stroke-width="5"/>
    <path d="M 314,124 C 326,134 330,148 326,162" stroke-width="4.5" opacity="0.8"/>
    <path d="M 232,144 C 246,134 266,136 278,148" stroke-width="4.5" opacity="0.88"/>
  </g>

  <!-- Dense white powder granular highlights inside the blast -->
  <g fill="#ffffff" opacity="0.95">
    <circle cx="256" cy="96" r="5"/>
    <circle cx="222" cy="146" r="4.5"/>
    <circle cx="288" cy="152" r="4"/>
    <circle cx="254" cy="196" r="5"/>
    <circle cx="210" cy="222" r="4"/>
    <circle cx="302" cy="212" r="4.5"/>
    <circle cx="246" cy="264" r="4.5"/>
    <circle cx="284" cy="272" r="3.8"/>
    <circle cx="256" cy="348" r="4"/>
  </g>

  <!-- Heavy outlined borax powder clumps & crystal granules bursting outward -->
  <g stroke="#121519" stroke-width="5" stroke-linejoin="round">
    <polygon points="178,98 190,90 198,104 186,112" fill="url(#bxt_chunk)"/>
    <polygon points="322,94 334,88 340,102 328,108" fill="url(#bxt_chunk)"/>
    <polygon points="154,274 166,268 172,282 158,288" fill="url(#bxt_chunk)"/>
    <polygon points="344,282 356,276 360,290 348,296" fill="url(#bxt_chunk)"/>
    <circle cx="176" cy="336" r="7.5" fill="#edf4f7"/>
    <circle cx="336" cy="342" r="7" fill="#dce7ec"/>
    <polygon points="200,398 210,392 216,404 206,410" fill="#ffffff"/>
    <circle cx="306" cy="404" r="6" fill="#e5eff4"/>
    <circle cx="224" cy="442" r="5.5" fill="#d5e2e8"/>
    <circle cx="290" cy="446" r="5" fill="#e6f0f5"/>
  </g>
</svg>
```

`Art/Projectiles/TMM_Bullet_DeanShotgun.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <radialGradient id="dsg_smoke_aura" cx="50%" cy="42%" r="50%">
      <stop offset="0%" stop-color="#ffcf7d" stop-opacity="0.34"/>
      <stop offset="48%" stop-color="#a8764a" stop-opacity="0.16"/>
      <stop offset="100%" stop-color="#4d3b2e" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="dsg_streak_hot" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fff4c4" stop-opacity="0.92"/>
      <stop offset="28%" stop-color="#f2a241" stop-opacity="0.62"/>
      <stop offset="68%" stop-color="#9e5932" stop-opacity="0.24"/>
      <stop offset="100%" stop-color="#523f36" stop-opacity="0"/>
    </linearGradient>
    <linearGradient id="dsg_streak_smoke" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe5a3" stop-opacity="0.72"/>
      <stop offset="35%" stop-color="#b88b5a" stop-opacity="0.36"/>
      <stop offset="72%" stop-color="#7c838a" stop-opacity="0.18"/>
      <stop offset="100%" stop-color="#454c54" stop-opacity="0"/>
    </linearGradient>
    <radialGradient id="dsg_pellet_lead" cx="38%" cy="32%" r="62%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="24%" stop-color="#c2ced6"/>
      <stop offset="60%" stop-color="#5e6973"/>
      <stop offset="100%" stop-color="#252b30"/>
    </radialGradient>
    <radialGradient id="dsg_pellet_hot" cx="40%" cy="30%" r="64%">
      <stop offset="0%" stop-color="#fffce6"/>
      <stop offset="28%" stop-color="#e3c79e"/>
      <stop offset="62%" stop-color="#6c757d"/>
      <stop offset="100%" stop-color="#262c30"/>
    </radialGradient>
    <linearGradient id="dsg_wad_cup" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#69231d"/>
      <stop offset="45%" stop-color="#b54338"/>
      <stop offset="100%" stop-color="#4d1814"/>
    </linearGradient>
  </defs>

  <!-- Warm gunsmoke & muzzle-blast radiance cone -->
  <ellipse cx="256" cy="244" rx="88" ry="188" fill="url(#dsg_smoke_aura)"/>

  <!-- Conical supersonic slipstream & powder-smoke velocity streaks trailing South from each buckshot pellet -->
  <!-- Far-left pellet trail -->
  <path d="M 192,162 L 210,162 L 228,342 L 218,348 Z" fill="url(#dsg_streak_smoke)"/>
  <!-- Far-right pellet trail -->
  <path d="M 302,172 L 320,172 L 296,356 L 286,350 Z" fill="url(#dsg_streak_smoke)"/>

  <!-- Mid-left pellet trail -->
  <path d="M 214,116 L 234,116 L 242,356 L 228,358 Z" fill="url(#dsg_streak_hot)"/>
  <!-- Mid-right pellet trail -->
  <path d="M 278,124 L 298,124 L 284,366 L 270,362 Z" fill="url(#dsg_streak_hot)"/>

  <!-- Center lead-pellet primary hot tracer wake -->
  <path d="M 244,86 L 268,86 L 264,398 L 248,398 Z" fill="url(#dsg_streak_hot)"/>

  <!-- Lower-left & lower-right secondary pellet trails -->
  <path d="M 206,232 L 224,232 L 238,412 L 226,412 Z" fill="url(#dsg_streak_smoke)"/>
  <path d="M 288,242 L 306,242 L 288,420 L 276,418 Z" fill="url(#dsg_streak_smoke)"/>
  <path d="M 246,196 L 266,196 L 262,434 L 250,434 Z" fill="url(#dsg_streak_hot)"/>

  <!-- Crisp thin incandescent streak cores directly behind pellets -->
  <g stroke-linecap="round" opacity="0.82">
    <line x1="256" y1="102" x2="256" y2="274" stroke="#fff4cc" stroke-width="4.5"/>
    <line x1="224" y1="132" x2="232" y2="282" stroke="#ffd88a" stroke-width="3.5"/>
    <line x1="288" y1="140" x2="280" y2="292" stroke="#ffd88a" stroke-width="3.5"/>
    <line x1="201" y1="176" x2="218" y2="298" stroke="#f0be73" stroke-width="3"/>
    <line x1="311" y1="186" x2="294" y2="306" stroke="#f0be73" stroke-width="3"/>
    <line x1="256" y1="214" x2="256" y2="352" stroke="#ffe5a3" stroke-width="3.5"/>
  </g>

  <!-- Tumbling open shotgun shot-cup wad trailing at rear of spread -->
  <g transform="translate(256 382) rotate(-8)">
    <path d="M -18,-14 L -12,14 L 12,14 L 18,-14 L 8,-14 L 6,2 L -6,2 L -8,-14 Z"
          fill="url(#dsg_wad_cup)" stroke="#121519" stroke-width="6" stroke-linejoin="round"/>
  </g>

  <!-- Tight lethal cluster of heavy lead buckshot pellets flying North -->
  <g stroke="#121519" stroke-width="6.5">
    <!-- Point lead pellet (highest North) -->
    <circle cx="256" cy="88" r="15" fill="url(#dsg_pellet_hot)"/>
    <!-- Upper-left & upper-right flanking pellets -->
    <circle cx="224" cy="118" r="13.5" fill="url(#dsg_pellet_lead)"/>
    <circle cx="288" cy="126" r="13.5" fill="url(#dsg_pellet_hot)"/>
    <!-- Outer wide spread pellets -->
    <circle cx="201" cy="166" r="12" fill="url(#dsg_pellet_lead)"/>
    <circle cx="311" cy="174" r="12" fill="url(#dsg_pellet_lead)"/>
    <!-- Center core heavy pellet -->
    <circle cx="256" cy="196" r="14" fill="url(#dsg_pellet_hot)"/>
    <!-- Lower trailing pellets -->
    <circle cx="216" cy="236" r="12.5" fill="url(#dsg_pellet_lead)"/>
    <circle cx="296" cy="244" r="12.5" fill="url(#dsg_pellet_lead)"/>
    <circle cx="254" cy="288" r="11.5" fill="url(#dsg_pellet_lead)"/>
  </g>

  <!-- Leading metallic-heat rim gleams on upper pellet arcs -->
  <g fill="none" stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.9">
    <path d="M 248,80 A 10,10 0 0 1 262,80"/>
    <path d="M 218,111 A 9,9 0 0 1 229,111"/>
    <path d="M 282,119 A 9,9 0 0 1 293,119"/>
    <path d="M 250,189 A 9,9 0 0 1 261,189"/>
  </g>

  <!-- Sharp burning gunpowder sparks & hot muzzle embers in the buckshot spread -->
  <g stroke="#121519" stroke-width="4" stroke-linejoin="round">
    <polygon points="236,154 241,162 236,170 231,162" fill="#ffe694"/>
    <polygon points="278,176 283,184 278,192 273,184" fill="#ffb84d"/>
    <polygon points="236,266 240,274 236,282 232,274" fill="#ffd978"/>
    <circle cx="280" cy="316" r="5" fill="#ffae42"/>
    <circle cx="228" cy="336" r="4.5" fill="#ffe18c"/>
    <circle cx="268" cy="422" r="4.5" fill="#ff9838"/>
  </g>
</svg>
```