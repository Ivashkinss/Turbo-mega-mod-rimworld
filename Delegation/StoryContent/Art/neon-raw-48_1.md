# 48_1 — Легендарное оружие (SVG 1–3)

## Список всех 15 файлов
1. `Art/Weapons/TMM_Legendary_Experiment18A.svg` — Experiment 18-A
2. `Art/Weapons/TMM_Legendary_FinalJudgment.svg` — Final Judgment
3. `Art/Weapons/TMM_Legendary_TheColt.svg` — The Colt
4. `Art/Weapons/TMM_Legendary_LorenzosArtifact.svg` — Lorenzo's Artifact Gun
5. `Art/Weapons/TMM_Legendary_AtomsJudgement.svg` — Atom's Judgement
6. `Art/Weapons/TMM_Legendary_TeslaCannon.svg` — Enclave Tesla Cannon
7. `Art/Weapons/TMM_Legendary_OverseersGuardian.svg` — Overseer's Guardian
8. `Art/Weapons/TMM_Legendary_RailwayRifle.svg` — Heavy Railway Rifle
9. `Art/Weapons/TMM_Legendary_BigBoy.svg` — The Big Boy
10. `Art/Weapons/TMM_Legendary_TheEqualizer.svg` — The Equalizer
11. `Art/Weapons/TMM_Legendary_FirstBlade.svg` — The First Blade
12. `Art/Weapons/TMM_Legendary_ArchangelBlade.svg` — Archangel Blade
13. `Art/Weapons/TMM_Legendary_AlphaPredator.svg` — Alpha Predator
14. `Art/Weapons/TMM_Gun_BoraxSprayer.svg` — Распылитель буры
15. `Art/Weapons/TMM_Mod_HolyWaterTank.svg` — Бак святой воды

---

`Art/Weapons/TMM_Legendary_Experiment18A.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="ex18_whiteBody" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e8eef2" />
      <stop offset="45%" stop-color="#c6d0d8" />
      <stop offset="85%" stop-color="#9aa6b0" />
      <stop offset="100%" stop-color="#76828c" />
    </linearGradient>

    <linearGradient id="ex18_whiteShade" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#b4c0c9" />
      <stop offset="100%" stop-color="#697580" />
    </linearGradient>

    <linearGradient id="ex18_gunmetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#58636e" />
      <stop offset="45%" stop-color="#373f47" />
      <stop offset="100%" stop-color="#20252b" />
    </linearGradient>

    <linearGradient id="ex18_coilCopper" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#dfaa72" />
      <stop offset="45%" stop-color="#b3753f" />
      <stop offset="100%" stop-color="#693e1d" />
    </linearGradient>

    <linearGradient id="ex18_plasmaTube" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#d8ffcf" />
      <stop offset="45%" stop-color="#5cf06e" />
      <stop offset="100%" stop-color="#1c8c38" />
    </linearGradient>

    <radialGradient id="ex18_coreGlow" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#f2ffed" />
      <stop offset="35%" stop-color="#7dff88" />
      <stop offset="72%" stop-color="#27ba46" />
      <stop offset="100%" stop-color="#114d20" />
    </radialGradient>

    <radialGradient id="ex18_coreHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#6eff7d" stop-opacity="0.52" />
      <stop offset="55%" stop-color="#2cc94b" stop-opacity="0.18" />
      <stop offset="100%" stop-color="#2cc94b" stop-opacity="0" />
    </radialGradient>

    <radialGradient id="ex18_muzzleHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#b8ffba" stop-opacity="0.65" />
      <stop offset="50%" stop-color="#3de05c" stop-opacity="0.22" />
      <stop offset="100%" stop-color="#3de05c" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Мягкие ореолы нестабильной плазмы -->
  <g id="ex18_glows">
    <circle cx="232" cy="232" r="76" fill="url(#ex18_coreHalo)" />
    <circle cx="464" cy="232" r="44" fill="url(#ex18_muzzleHalo)" />
  </g>

  <!-- Нижняя энергоячейка и пистолетная рукоять -->
  <g id="ex18_lower">
    <!-- Спусковая скоба и курок -->
    <path d="M168,274 L216,274 L208,308 L166,308 Z"
          fill="none" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <path d="M190,276 L184,298" fill="none" stroke="#121519" stroke-width="6" stroke-linecap="round" />

    <!-- Рукоять -->
    <path d="M138,270 L176,270 L160,348 L124,344 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <line x1="138" y1="294" x2="164" y2="298" stroke="#1b2026" stroke-width="4" />
    <line x1="134" y1="312" x2="160" y2="316" stroke="#1b2026" stroke-width="4" />
    <line x1="130" y1="330" x2="156" y2="334" stroke="#1b2026" stroke-width="4" />

    <!-- Магазин-батарея с зелёными индикаторами -->
    <path d="M222,272 L292,272 L286,326 L228,326 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <rect x="236" y="284" width="44" height="12" rx="3" fill="url(#ex18_plasmaTube)" stroke="#121519" stroke-width="4" />
    <rect x="239" y="304" width="38" height="10" rx="3" fill="url(#ex18_plasmaTube)" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Приклад Института -->
  <g id="ex18_stock">
    <path d="M38,214 L146,204 L146,282 L44,312 L34,298 L34,224 Z"
          fill="url(#ex18_whiteBody)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <!-- Тёмный тыльник приклада -->
    <path d="M34,220 L54,218 L54,306 L40,310 L34,298 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <!-- Облегчающий вырез приклада -->
    <path d="M72,240 L122,232 L122,262 L72,276 Z"
          fill="#262c33" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
    <line x1="68" y1="224" x2="132" y2="216" stroke="#ffffff" stroke-width="4" opacity="0.55" stroke-linecap="round" />
  </g>

  <!-- Открытый плазменный ствол и ускоритель -->
  <g id="ex18_barrel">
    <!-- Нижняя направляющая шина ствола -->
    <path d="M300,256 L444,256 L436,274 L300,274 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />

    <!-- Центральный плазменный канал -->
    <rect x="302" y="216" width="152" height="32" rx="6"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" />
    <rect x="306" y="224" width="144" height="16" rx="4"
          fill="url(#ex18_plasmaTube)" stroke="#121519" stroke-width="4" />

    <!-- Трёхлепестковый излучатель на дуле -->
    <path d="M446,202 L478,208 L478,220 L452,222 Z"
          fill="url(#ex18_whiteShade)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <path d="M446,262 L478,256 L478,244 L452,242 Z"
          fill="url(#ex18_whiteShade)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <circle cx="464" cy="232" r="10" fill="url(#ex18_coreGlow)" stroke="#121519" stroke-width="5" />

    <!-- Обнажённые медные магнитные катушки -->
    <g id="ex18_coils">
      <rect x="322" y="198" width="22" height="68" rx="6"
            fill="url(#ex18_coilCopper)" stroke="#121519" stroke-width="9" />
      <rect x="358" y="198" width="22" height="68" rx="6"
            fill="url(#ex18_coilCopper)" stroke="#121519" stroke-width="9" />
      <rect x="394" y="200" width="22" height="64" rx="6"
            fill="url(#ex18_coilCopper)" stroke="#121519" stroke-width="9" />
      <rect x="426" y="204" width="18" height="56" rx="5"
            fill="url(#ex18_coilCopper)" stroke="#121519" stroke-width="9" />

      <!-- Внутренние витки и дуги между катушками -->
      <line x1="333" y1="206" x2="333" y2="258" stroke="#633a1a" stroke-width="4" />
      <line x1="369" y1="206" x2="369" y2="258" stroke="#633a1a" stroke-width="4" />
      <line x1="405" y1="208" x2="405" y2="256" stroke="#633a1a" stroke-width="4" />
      <path d="M344,216 L352,210 L358,218 M380,248 L388,254 L394,244 M416,216 L422,222 L426,214"
            fill="none" stroke="#9cff9e" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />
    </g>
  </g>

  <!-- Верхний прицел-коллиматор -->
  <g id="ex18_sight">
    <path d="M168,190 L182,162 L248,162 L262,190 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <rect x="192" y="169" width="46" height="12" rx="3" fill="url(#ex18_plasmaTube)" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Белый корпус ресивера Института -->
  <g id="ex18_receiver">
    <path d="M134,192 L298,188 L314,204 L314,264 L298,278 L134,278 L124,262 L124,206 Z"
          fill="url(#ex18_whiteBody)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Нижняя тень корпуса -->
    <path d="M134,258 L308,258 L296,272 L136,272 Z" fill="#5e6a75" opacity="0.45" />
    <!-- Верхний блик -->
    <path d="M142,200 L292,197" fill="none" stroke="#ffffff" stroke-width="5" opacity="0.7" stroke-linecap="round" />

    <!-- Светящиеся зелёные контуры корпуса -->
    <path d="M146,214 L182,214 L190,224 M146,252 L182,252 L190,242"
          fill="none" stroke="#47e362" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
    <line x1="274" y1="214" x2="300" y2="214" stroke="#47e362" stroke-width="5" stroke-linecap="round" />
    <line x1="274" y1="250" x2="300" y2="250" stroke="#47e362" stroke-width="5" stroke-linecap="round" />

    <!-- Нестабильное плазменное ядро в центре ресивера -->
    <rect x="192" y="204" width="80" height="56" rx="10"
          fill="#151b18" stroke="#121519" stroke-width="8" />
    <circle cx="232" cy="232" r="24" fill="url(#ex18_coreGlow)" stroke="#121519" stroke-width="5" />
    <!-- Энергетические разряды внутри нестабильного ядра -->
    <path d="M204,224 L218,234 L228,220 L242,238 L260,226"
          fill="none" stroke="#ffffff" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />
    <!-- Защитные рёбра камеры ядра -->
    <line x1="216" y1="204" x2="216" y2="260" stroke="#2c343b" stroke-width="6" />
    <line x1="248" y1="204" x2="248" y2="260" stroke="#2c343b" stroke-width="6" />

    <!-- Панельные швы и заклёпки -->
    <line x1="164" y1="194" x2="164" y2="274" stroke="#64707a" stroke-width="4" />
    <circle cx="146" cy="233" r="4" fill="#3a434c" stroke="#121519" stroke-width="3" />
    <circle cx="292" cy="232" r="4" fill="#3a434c" stroke="#121519" stroke-width="3" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_FinalJudgment.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="fj_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#a5b0ba" />
      <stop offset="40%" stop-color="#707b85" />
      <stop offset="85%" stop-color="#475059" />
      <stop offset="100%" stop-color="#30373e" />
    </linearGradient>

    <linearGradient id="fj_darkMetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#555f69" />
      <stop offset="50%" stop-color="#343b42" />
      <stop offset="100%" stop-color="#1d2227" />
    </linearGradient>

    <linearGradient id="fj_barrel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b96a0" />
      <stop offset="45%" stop-color="#4e5761" />
      <stop offset="100%" stop-color="#272d33" />
    </linearGradient>

    <linearGradient id="fj_crimson" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#bd3f33" />
      <stop offset="55%" stop-color="#8f2920" />
      <stop offset="100%" stop-color="#5e1913" />
    </linearGradient>

    <linearGradient id="fj_gold" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e5be63" />
      <stop offset="50%" stop-color="#b58932" />
      <stop offset="100%" stop-color="#6e4f19" />
    </linearGradient>

    <radialGradient id="fj_coreGlow" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#fff4cc" />
      <stop offset="35%" stop-color="#ffaa38" />
      <stop offset="72%" stop-color="#e8451e" />
      <stop offset="100%" stop-color="#78180a" />
    </radialGradient>

    <radialGradient id="fj_heatHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ff8533" stop-opacity="0.58" />
      <stop offset="55%" stop-color="#e63e1c" stop-opacity="0.20" />
      <stop offset="100%" stop-color="#e63e1c" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Ореолы перегретых лазеров и микроядерного ядра -->
  <g id="fj_halos">
    <circle cx="212" cy="296" r="62" fill="url(#fj_heatHalo)" />
    <circle cx="464" cy="236" r="52" fill="url(#fj_heatHalo)" />
  </g>

  <!-- Верхняя скоба переноски и задние рукояти -->
  <g id="fj_handles">
    <!-- Верхняя силовая ручка -->
    <path d="M104,192 L120,152 L228,152 L242,192 L220,192 L210,170 L136,170 L126,192 Z"
          fill="url(#fj_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Задний плечевой упор -->
    <path d="M34,202 L84,196 L84,284 L38,292 L32,276 L32,214 Z"
          fill="url(#fj_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <line x1="52" y1="210" x2="52" y2="278" stroke="#181c21" stroke-width="6" />

    <!-- Нижняя рукоять управления -->
    <path d="M108,284 L148,284 L138,348 L104,344 Z"
          fill="url(#fj_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M144,286 L172,286 L166,314 L142,314"
          fill="none" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
  </g>

  <!-- Вращающийся блок лазерных стволов -->
  <g id="fj_barrels">
    <!-- Центральная раскалённая ось между стволами -->
    <rect x="272" y="220" width="194" height="32" rx="4"
          fill="url(#fj_coreGlow)" stroke="#121519" stroke-width="6" />

    <!-- Четыре параллельных ствола -->
    <rect x="272" y="196" width="204" height="16" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="8" />
    <rect x="272" y="216" width="208" height="16" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="8" />
    <rect x="272" y="240" width="208" height="16" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="8" />
    <rect x="272" y="260" width="204" height="16" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="8" />

    <!-- Светящиеся линзы на срезах стволов -->
    <rect x="464" y="200" width="12" height="8" rx="2" fill="#ffcf70" />
    <rect x="468" y="220" width="12" height="8" rx="2" fill="#ffe9a6" />
    <rect x="468" y="244" width="12" height="8" rx="2" fill="#ffe9a6" />
    <rect x="464" y="264" width="12" height="8" rx="2" fill="#ffcf70" />

    <!-- Два тяжёлых хомута вращения стволов -->
    <rect x="334" y="188" width="24" height="96" rx="6"
          fill="url(#fj_steel)" stroke="#121519" stroke-width="10" />
    <line x1="346" y1="196" x2="346" y2="276" stroke="#2c333a" stroke-width="4" />

    <rect x="418" y="188" width="24" height="96" rx="6"
          fill="url(#fj_steel)" stroke="#121519" stroke-width="10" />
    <line x1="430" y1="196" x2="430" y2="276" stroke="#2c333a" stroke-width="4" />
  </g>

  <!-- Основной бронированный корпус Братства Стали -->
  <g id="fj_body">
    <path d="M76,192 L262,184 L280,202 L280,276 L262,290 L76,288 Z"
          fill="url(#fj_steel)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхний металлический блик -->
    <line x1="90" y1="198" x2="254" y2="193" stroke="#d9e2ea" stroke-width="4" opacity="0.65" stroke-linecap="round" />

    <!-- Гербовые полосы Братства Стали (алый + золото) -->
    <polygon points="104,194 128,193 114,284 90,284" fill="url(#fj_crimson)" stroke="#121519" stroke-width="4" />
    <polygon points="134,193 150,192 136,284 120,284" fill="url(#fj_gold)" stroke="#121519" stroke-width="4" />

    <!-- Раскалённые решётки охлаждения сверху -->
    <rect x="172" y="198" width="84" height="26" rx="4"
          fill="#261410" stroke="#121519" stroke-width="6" />
    <line x1="186" y1="202" x2="186" y2="220" stroke="#ff7e2e" stroke-width="6" stroke-linecap="round" />
    <line x1="204" y1="202" x2="204" y2="220" stroke="#ffae42" stroke-width="6" stroke-linecap="round" />
    <line x1="222" y1="202" x2="222" y2="220" stroke="#ffae42" stroke-width="6" stroke-linecap="round" />
    <line x1="240" y1="202" x2="240" y2="220" stroke="#ff7e2e" stroke-width="6" stroke-linecap="round" />

    <!-- Стилизованная эмблема шестерни и клинка Братства -->
    <circle cx="158" cy="244" r="14" fill="url(#fj_darkMetal)" stroke="#e5be63" stroke-width="4" />
    <line x1="158" y1="228" x2="158" y2="260" stroke="#e5be63" stroke-width="4" stroke-linecap="round" />
    <line x1="146" y1="244" x2="170" y2="244" stroke="#e5be63" stroke-width="4" stroke-linecap="round" />

    <!-- Заклёпки -->
    <circle cx="90" cy="208" r="4" fill="#2b3238" stroke="#121519" stroke-width="3" />
    <circle cx="264" cy="212" r="4" fill="#2b3238" stroke="#121519" stroke-width="3" />
    <circle cx="264" cy="264" r="4" fill="#2b3238" stroke="#121519" stroke-width="3" />
  </g>

  <!-- Боковое микроядерное ядро (источник бесконечного боезапаса) -->
  <g id="fj_fusion_core">
    <!-- Силовой кабель от ядра к корпусу -->
    <path d="M242,298 C272,306 286,292 276,268"
          fill="none" stroke="#121519" stroke-width="14" stroke-linecap="round" />
    <path d="M242,298 C272,306 286,292 276,268"
          fill="none" stroke="#b58932" stroke-width="6" stroke-linecap="round" />

    <!-- Корпус микроядерного блока -->
    <rect x="174" y="256" width="76" height="72" rx="12"
          fill="url(#fj_gold)" stroke="#121519" stroke-width="11" />
    <rect x="184" y="268" width="56" height="48" rx="6"
          fill="#22120e" stroke="#121519" stroke-width="6" />

    <!-- Пылающее красно-оранжевое ядро внутри -->
    <rect x="190" y="274" width="44" height="36" rx="4" fill="url(#fj_coreGlow)" />
    <line x1="204" y1="270" x2="204" y2="314" stroke="#121519" stroke-width="5" />
    <line x1="220" y1="270" x2="220" y2="314" stroke="#121519" stroke-width="5" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_TheColt.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="colt_silver" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3eaf0" />
      <stop offset="38%" stop-color="#aeb8c2" />
      <stop offset="78%" stop-color="#6d7882" />
      <stop offset="100%" stop-color="#485159" />
    </linearGradient>

    <linearGradient id="colt_silverLight" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f2f6fa" />
      <stop offset="100%" stop-color="#9aa5b0" />
    </linearGradient>

    <linearGradient id="colt_silverDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7c8791" />
      <stop offset="100%" stop-color="#353d45" />
    </linearGradient>

    <linearGradient id="colt_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#edd087" />
      <stop offset="48%" stop-color="#b88e3e" />
      <stop offset="100%" stop-color="#6e501c" />
    </linearGradient>

    <linearGradient id="colt_wood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#73452c" />
      <stop offset="45%" stop-color="#4a2918" />
      <stop offset="100%" stop-color="#24120a" />
    </linearGradient>

    <radialGradient id="colt_cometHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.72" />
      <stop offset="35%" stop-color="#ffeaa8" stop-opacity="0.34" />
      <stop offset="68%" stop-color="#8ae2ff" stop-opacity="0.14" />
      <stop offset="100%" stop-color="#8ae2ff" stop-opacity="0" />
    </radialGradient>

    <linearGradient id="colt_cometTail" x1="0%" y1="50%" x2="100%" y2="50%">
      <stop offset="0%" stop-color="#8ae2ff" stop-opacity="0" />
      <stop offset="60%" stop-color="#ffeaa8" stop-opacity="0.55" />
      <stop offset="100%" stop-color="#ffffff" stop-opacity="0.95" />
    </linearGradient>
  </defs>

  <!-- Едва заметное «кометное» свечение у мушки и барабана -->
  <g id="colt_comet_glow">
    <circle cx="448" cy="186" r="52" fill="url(#colt_cometHalo)" />
    <path d="M368,176 Q412,172 448,186"
          fill="none" stroke="url(#colt_cometTail)" stroke-width="5" stroke-linecap="round" />
  </g>

  <!-- Курок и спусковой механизм -->
  <g id="colt_hammer_trigger">
    <!-- Изогнутый старинный курок -->
    <path d="M126,184 C118,162 132,148 150,156 C142,166 154,174 166,184 Z"
          fill="url(#colt_brass)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />

    <!-- Резная латунная спусковая скоба -->
    <path d="M150,256 L220,256 C214,292 184,306 156,294 L144,274 Z"
          fill="none" stroke="#121519" stroke-width="14" stroke-linejoin="round" />
    <path d="M150,256 L220,256 C214,292 184,306 156,294 L144,274 Z"
          fill="none" stroke="url(#colt_brass)" stroke-width="6" stroke-linejoin="round" />
    <!-- Спусковой крючок -->
    <path d="M184,258 C184,274 176,282 170,286"
          fill="none" stroke="#121519" stroke-width="7" stroke-linecap="round" />
  </g>

  <!-- Тёмная деревянная рукоять с латунной оковкой -->
  <g id="colt_grip">
    <path d="M128,224 L164,236 C152,278 136,314 114,348 C90,360 54,352 40,336 C68,296 90,260 104,226 Z"
          fill="url(#colt_wood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Латунная затылочная и передняя накладки рукояти -->
    <path d="M40,336 C54,352 90,360 114,348 L122,334 C96,344 68,338 50,322 Z"
          fill="url(#colt_brass)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
    <path d="M104,226 C92,262 72,294 48,326"
          fill="none" stroke="url(#colt_brass)" stroke-width="6" stroke-linecap="round" />

    <!-- Блик на полированном орехе и инкрустация -->
    <path d="M114,248 C102,276 86,302 68,322"
          fill="none" stroke="#996140" stroke-width="4" opacity="0.65" stroke-linecap="round" />
    <polygon points="104,284 116,292 102,304 92,294"
             fill="url(#colt_brass)" stroke="#121519" stroke-width="4" stroke-linejoin="round" />
  </g>

  <!-- Длинный восьмигранный ствол и подствольный рычаг заряжания -->
  <g id="colt_barrel">
    <!-- Подствольный рычаг-шомпол (Colt Paterson / 1851) -->
    <path d="M250,236 L432,228 L428,246 L252,256 Z"
          fill="url(#colt_silverDark)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <circle cx="274" cy="242" r="7" fill="url(#colt_brass)" stroke="#121519" stroke-width="4" />

    <!-- Мушка из латуни -->
    <polygon points="452,184 468,184 464,166 456,168"
             fill="url(#colt_brass)" stroke="#121519" stroke-width="6" stroke-linejoin="round" />

    <!-- Восьмигранный ствол (три читаемые грани) -->
    <path d="M248,184 L474,186 L474,232 L248,236 Z"
          fill="url(#colt_silver)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Верхняя светлая грань -->
    <polygon points="252,188 470,190 470,200 252,199" fill="url(#colt_silverLight)" />
    <!-- Нижняя тёмная грань -->
    <polygon points="252,221 470,219 470,228 252,231" fill="url(#colt_silverDark)" />

    <!-- Латунный поясок и клин ствола -->
    <rect x="274" y="198" width="18" height="22" rx="3" fill="url(#colt_brass)" stroke="#121519" stroke-width="4" />
    <!-- Гравировка на стволе -->
    <path d="M308,209 C320,202 332,216 344,209 C356,202 368,216 380,209 C392,202 404,216 418,209"
          fill="none" stroke="url(#colt_brass)" stroke-width="4" stroke-linecap="round" />
    <line x1="432" y1="209" x2="458" y2="209" stroke="url(#colt_brass)" stroke-width="4" stroke-linecap="round" />
  </g>

  <!-- Рама револьвера и украшенный барабан -->
  <g id="colt_frame_cylinder">
    <!-- Задняя рама с латунной отделкой -->
    <path d="M122,202 L156,184 L256,184 L256,262 L138,262 L114,232 Z"
          fill="url(#colt_silver)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Барабан с гравировкой -->
    <rect x="172" y="182" width="76" height="68" rx="8"
          fill="url(#colt_silver)" stroke="#121519" stroke-width="10" />
    <!-- Латунные кольца по краям барабана -->
    <rect x="176" y="186" width="10" height="60" fill="url(#colt_brass)" stroke="#121519" stroke-width="3.5" />
    <rect x="234" y="186" width="10" height="60" fill="url(#colt_brass)" stroke="#121519" stroke-width="3.5" />

    <!-- Долы (выемки) барабана -->
    <rect x="192" y="194" width="36" height="10" rx="4" fill="#465059" stroke="#121519" stroke-width="3.5" />
    <rect x="192" y="211" width="36" height="11" rx="4" fill="#5e6973" stroke="#121519" stroke-width="3.5" />
    <rect x="192" y="229" width="36" height="10" rx="4" fill="#3a434b" stroke="#121519" stroke-width="3.5" />

    <!-- Виньетка и знак пентаграммы/звезды на раме -->
    <path d="M132,216 C142,206 154,218 162,208 M134,236 C146,246 158,234 166,244"
          fill="none" stroke="url(#colt_brass)" stroke-width="4" stroke-linecap="round" />
    <circle cx="150" cy="226" r="5" fill="url(#colt_brass)" stroke="#121519" stroke-width="3" />
  </g>

  <!-- Едва заметная «кометная» искра на срезе ствола -->
  <g id="colt_comet_spark">
    <polygon points="448,166 452,182 468,186 452,190 448,206 444,190 428,186 444,182"
             fill="#fff9e2" stroke="#d4af55" stroke-width="3" stroke-linejoin="round" />
  </g>
</svg>
```