`Art/Weapons/TMM_Legendary_TheColt.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="colt_silver" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e8eff5" />
      <stop offset="38%" stop-color="#b4bec8" />
      <stop offset="76%" stop-color="#707b86" />
      <stop offset="100%" stop-color="#454e57" />
    </linearGradient>

    <linearGradient id="colt_silverTop" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#f5f9fc" />
      <stop offset="65%" stop-color="#cdd6df" />
      <stop offset="100%" stop-color="#9da8b3" />
    </linearGradient>

    <linearGradient id="colt_silverDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7c8792" />
      <stop offset="100%" stop-color="#313940" />
    </linearGradient>

    <linearGradient id="colt_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f2d58a" />
      <stop offset="50%" stop-color="#bd903c" />
      <stop offset="100%" stop-color="#6e4e19" />
    </linearGradient>

    <linearGradient id="colt_wood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7c4b30" />
      <stop offset="48%" stop-color="#4e2b19" />
      <stop offset="100%" stop-color="#27140b" />
    </linearGradient>

    <radialGradient id="colt_muzzleHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.78" />
      <stop offset="38%" stop-color="#d8f1ff" stop-opacity="0.38" />
      <stop offset="72%" stop-color="#ffe7a3" stop-opacity="0.16" />
      <stop offset="100%" stop-color="#ffe7a3" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Серебристо-кометный ореол у дульного среза -->
  <g id="colt_halo">
    <circle cx="468" cy="212" r="46" fill="url(#colt_muzzleHalo)" />
  </g>

  <!-- Курок и спусковая скоба -->
  <g id="colt_mechanism">
    <!-- Изогнутый кавалерийский курок -->
    <path d="M124,196 C112,168 128,150 152,158 C142,172 154,182 168,194 Z"
          fill="url(#colt_brass)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Латунная спусковая скоба -->
    <path d="M150,268 L224,268 C218,308 186,322 156,308 L144,286 Z"
          fill="none" stroke="#121519" stroke-width="16" stroke-linejoin="round" />
    <path d="M150,268 L224,268 C218,308 186,322 156,308 L144,286 Z"
          fill="none" stroke="url(#colt_brass)" stroke-width="6" stroke-linejoin="round" />

    <!-- Спусковой крючок -->
    <path d="M186,270 C186,288 178,296 170,300"
          fill="none" stroke="#121519" stroke-width="8" stroke-linecap="round" />
  </g>

  <!-- Деревянная рукоять с пентаграммой -->
  <g id="colt_grip">
    <path d="M126,232 L166,246 C152,292 134,332 108,366 C82,378 46,368 34,348 C62,306 86,268 102,234 Z"
          fill="url(#colt_wood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхний светлый блик на дереве -->
    <path d="M108,244 C94,274 76,304 54,332"
          fill="none" stroke="#a66c49" stroke-width="5" opacity="0.55" stroke-linecap="round" />

    <!-- Латунный тыльник рукояти -->
    <path d="M34,348 C46,368 82,378 108,366 L118,350 C92,360 64,352 46,334 Z"
          fill="url(#colt_brass)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />

    <!-- Серебряная пентаграмма в круге на рукояти -->
    <g id="colt_pentagram">
      <circle cx="98" cy="306" r="28" fill="#211109" stroke="#e4eef7" stroke-width="5" />
      <polygon points="98,282 113,325 76,298 120,298 83,325"
               fill="none" stroke="#e8f3fc" stroke-width="4.5" stroke-linejoin="round" />
    </g>
  </g>

  <!-- Длинный восьмигранный кавалерийский ствол -->
  <g id="colt_barrel">
    <!-- Подствольный рычаг-шомпол -->
    <path d="M250,248 L434,240 L430,260 L252,270 Z"
          fill="url(#colt_silverDark)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <circle cx="276" cy="256" r="8" fill="url(#colt_brass)" stroke="#121519" stroke-width="5" />

    <!-- Мушка -->
    <polygon points="454,194 472,194 468,172 458,174"
             fill="url(#colt_brass)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />

    <!-- Основной восьмигранный ствол -->
    <path d="M248,194 L478,196 L478,244 L248,248 Z"
          fill="url(#colt_silver)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <!-- Верхняя светлая грань -->
    <polygon points="254,200 472,202 472,212 254,211" fill="url(#colt_silverTop)" />
    <!-- Нижняя теневая грань -->
    <polygon points="254,232 472,230 472,240 254,242" fill="url(#colt_silverDark)" />

    <!-- Латунный клин крепления ствола -->
    <rect x="274" y="208" width="20" height="24" rx="3" fill="url(#colt_brass)" stroke="#121519" stroke-width="5" />

    <!-- Серебряно-светлая волнистая гравировка вдоль ствола -->
    <path d="M312,221 C324,212 336,230 348,221 C360,212 372,230 384,221 C396,212 408,230 422,221 L458,221"
          fill="none" stroke="#f5fbff" stroke-width="5" stroke-linecap="round" />
    <path d="M312,225 C324,216 336,234 348,225 C360,216 372,234 384,225 C396,216 408,234 422,225"
          fill="none" stroke="#2c343d" stroke-width="4" opacity="0.55" stroke-linecap="round" />
  </g>

  <!-- Рама и гравированный барабан -->
  <g id="colt_frame_cylinder">
    <!-- Рама револьвера -->
    <path d="M120,212 L156,192 L258,192 L258,274 L136,274 L112,242 Z"
          fill="url(#colt_silver)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Барабан -->
    <rect x="172" y="190" width="78" height="72" rx="10"
          fill="url(#colt_silver)" stroke="#121519" stroke-width="11" />

    <!-- Латунные ободки барабана -->
    <rect x="178" y="196" width="10" height="60" fill="url(#colt_brass)" stroke="#121519" stroke-width="4" />
    <rect x="234" y="196" width="10" height="60" fill="url(#colt_brass)" stroke="#121519" stroke-width="4" />

    <!-- Долы барабана и серебряная насечка -->
    <rect x="194" y="202" width="34" height="12" rx="4" fill="#46505a" stroke="#121519" stroke-width="4" />
    <rect x="194" y="220" width="34" height="12" rx="4" fill="#636f7a" stroke="#121519" stroke-width="4" />
    <rect x="194" y="238" width="34" height="12" rx="4" fill="#3a434c" stroke="#121519" stroke-width="4" />

    <!-- Серебряная гравировка на боковой пластине рамы -->
    <path d="M130,224 C142,214 152,226 162,216 M132,248 C144,258 156,246 164,256"
          fill="none" stroke="#f5fbff" stroke-width="4.5" stroke-linecap="round" />
    <circle cx="148" cy="236" r="6" fill="url(#colt_brass)" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Звезда-искра у дула -->
  <g id="colt_spark">
    <polygon points="468,194 472,208 486,212 472,216 468,230 464,216 450,212 464,208"
             fill="#ffffff" stroke="#a6d8ff" stroke-width="4" stroke-linejoin="round" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_Experiment18A.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="ex18_whiteBody" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#eff4f7" />
      <stop offset="45%" stop-color="#c9d3dc" />
      <stop offset="84%" stop-color="#96a3ae" />
      <stop offset="100%" stop-color="#6e7a85" />
    </linearGradient>

    <linearGradient id="ex18_whiteShade" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#b5c1cc" />
      <stop offset="100%" stop-color="#64707b" />
    </linearGradient>

    <linearGradient id="ex18_gunmetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#5a6570" />
      <stop offset="48%" stop-color="#373f47" />
      <stop offset="100%" stop-color="#1f242a" />
    </linearGradient>

    <linearGradient id="ex18_copper" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3ad72" />
      <stop offset="50%" stop-color="#b4753d" />
      <stop offset="100%" stop-color="#693e1c" />
    </linearGradient>

    <linearGradient id="ex18_plasmaGreen" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e0ffd6" />
      <stop offset="45%" stop-color="#56f26b" />
      <stop offset="100%" stop-color="#198c36" />
    </linearGradient>

    <radialGradient id="ex18_coreGlow" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#f4ffef" />
      <stop offset="35%" stop-color="#7cff88" />
      <stop offset="72%" stop-color="#24b844" />
      <stop offset="100%" stop-color="#0f4c1f" />
    </radialGradient>

    <radialGradient id="ex18_coilHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#6eff7d" stop-opacity="0.58" />
      <stop offset="55%" stop-color="#28c748" stop-opacity="0.22" />
      <stop offset="100%" stop-color="#28c748" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Зелёное свечение перегруженных катушек и ядра -->
  <g id="ex18_halos">
    <circle cx="232" cy="240" r="78" fill="url(#ex18_coilHalo)" />
    <circle cx="382" cy="240" r="72" fill="url(#ex18_coilHalo)" />
    <circle cx="468" cy="240" r="46" fill="url(#ex18_coilHalo)" />
  </g>

  <!-- Пистолетная рукоять, спусковая скоба и энергоячейка -->
  <g id="ex18_lower">
    <path d="M168,284 L218,284 L210,320 L166,320 Z"
          fill="none" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M192,286 L186,308" fill="none" stroke="#121519" stroke-width="7" stroke-linecap="round" />

    <path d="M138,280 L178,280 L162,360 L124,356 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <line x1="138" y1="306" x2="166" y2="310" stroke="#191e24" stroke-width="5" />
    <line x1="134" y1="326" x2="162" y2="330" stroke="#191e24" stroke-width="5" />

    <!-- Плазменный магазин-батарея -->
    <path d="M222,282 L294,282 L288,338 L228,338 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <rect x="236" y="294" width="44" height="14" rx="4" fill="url(#ex18_plasmaGreen)" stroke="#121519" stroke-width="4.5" />
    <rect x="240" y="314" width="36" height="12" rx="4" fill="url(#ex18_plasmaGreen)" stroke="#121519" stroke-width="4.5" />
  </g>

  <!-- Футуристичный приклад Института -->
  <g id="ex18_stock">
    <path d="M34,222 L146,212 L146,292 L42,324 L30,308 L30,232 Z"
          fill="url(#ex18_whiteBody)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <!-- Тыльник -->
    <path d="M30,228 L52,226 L52,318 L38,322 L30,308 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <!-- Облегчающий вырез -->
    <path d="M72,248 L122,240 L122,272 L72,286 Z"
          fill="#22282f" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
    <line x1="68" y1="230" x2="132" y2="222" stroke="#ffffff" stroke-width="5" opacity="0.65" stroke-linecap="round" />
  </g>

  <!-- Открытый ствол с перегруженными плазменными катушками -->
  <g id="ex18_barrel">
    <!-- Нижняя шина -->
    <path d="M302,266 L446,266 L438,284 L302,284 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />

    <!-- Светящийся плазменный канал -->
    <rect x="304" y="222" width="154" height="36" rx="6"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="10" />
    <rect x="308" y="230" width="146" height="20" rx="4"
          fill="url(#ex18_plasmaGreen)" stroke="#121519" stroke-width="4.5" />

    <!-- Дульный излучатель -->
    <path d="M448,208 L482,214 L482,228 L454,230 Z"
          fill="url(#ex18_whiteShade)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <path d="M448,272 L482,266 L482,252 L454,250 Z"
          fill="url(#ex18_whiteShade)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <circle cx="468" cy="240" r="12" fill="url(#ex18_coreGlow)" stroke="#121519" stroke-width="5" />

    <!-- Медные магнитные катушки с зелёным перегрузочным свечением -->
    <g id="ex18_coils">
      <rect x="322" y="202" width="24" height="76" rx="7"
            fill="url(#ex18_copper)" stroke="#121519" stroke-width="10" />
      <rect x="360" y="202" width="24" height="76" rx="7"
            fill="url(#ex18_copper)" stroke="#121519" stroke-width="10" />
      <rect x="398" y="204" width="24" height="72" rx="7"
            fill="url(#ex18_copper)" stroke="#121519" stroke-width="10" />
      <rect x="430" y="210" width="18" height="60" rx="6"
            fill="url(#ex18_copper)" stroke="#121519" stroke-width="10" />

      <!-- Яркие зелёные энергетические полосы на катушках -->
      <line x1="334" y1="212" x2="334" y2="268" stroke="#85ff90" stroke-width="5" stroke-linecap="round" />
      <line x1="372" y1="212" x2="372" y2="268" stroke="#85ff90" stroke-width="5" stroke-linecap="round" />
      <line x1="410" y1="214" x2="410" y2="266" stroke="#85ff90" stroke-width="5" stroke-linecap="round" />

      <!-- Плазменные дуги между катушками -->
      <path d="M346,220 L353,212 L360,222 M384,258 L391,266 L398,254 M422,222 L426,230 L430,220"
            fill="none" stroke="#c8ffcb" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
    </g>
  </g>

  <!-- Верхний прицел Института -->
  <g id="ex18_sight">
    <path d="M168,198 L184,168 L250,168 L264,198 Z"
          fill="url(#ex18_gunmetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <rect x="192" y="176" width="48" height="14" rx="4" fill="url(#ex18_plasmaGreen)" stroke="#121519" stroke-width="4.5" />
  </g>

  <!-- Белый корпус ресивера -->
  <g id="ex18_receiver">
    <path d="M134,198 L300,194 L316,210 L316,274 L300,288 L134,288 L124,272 L124,212 Z"
          fill="url(#ex18_whiteBody)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Нижняя тень и верхний блик -->
    <path d="M134,268 L310,268 L298,282 L136,282 Z" fill="#5b6773" opacity="0.45" />
    <line x1="142" y1="206" x2="294" y2="203" stroke="#ffffff" stroke-width="6" opacity="0.72" stroke-linecap="round" />

    <!-- Светящиеся зелёные контуры корпуса -->
    <path d="M144,222 L180,222 L188,232 M144,260 L180,260 L188,250"
          fill="none" stroke="#45e862" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" />
    <line x1="276" y1="222" x2="302" y2="222" stroke="#45e862" stroke-width="6" stroke-linecap="round" />
    <line x1="276" y1="258" x2="302" y2="258" stroke="#45e862" stroke-width="6" stroke-linecap="round" />

    <!-- Центральное нестабильное плазменное ядро -->
    <rect x="190" y="210" width="84" height="60" rx="10"
          fill="#121a14" stroke="#121519" stroke-width="9" />
    <circle cx="232" cy="240" r="24" fill="url(#ex18_coreGlow)" stroke="#121519" stroke-width="5" />
    <path d="M202,232 L218,242 L230,226 L244,248 L262,234"
          fill="none" stroke="#ffffff" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
    <line x1="216" y1="210" x2="216" y2="270" stroke="#2c353d" stroke-width="6" />
    <line x1="248" y1="210" x2="248" y2="270" stroke="#2c353d" stroke-width="6" />

    <!-- Шов и лёгкий износ -->
    <line x1="164" y1="200" x2="164" y2="284" stroke="#64707c" stroke-width="4.5" />
    <circle cx="146" cy="241" r="5" fill="#3a434c" stroke="#121519" stroke-width="4" />
    <circle cx="294" cy="240" r="5" fill="#3a434c" stroke="#121519" stroke-width="4" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_FinalJudgment.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="fj_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#abb6c0" />
      <stop offset="40%" stop-color="#737e89" />
      <stop offset="85%" stop-color="#48515a" />
      <stop offset="100%" stop-color="#2e353c" />
    </linearGradient>

    <linearGradient id="fj_darkMetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#58626c" />
      <stop offset="50%" stop-color="#343b43" />
      <stop offset="100%" stop-color="#1b2026" />
    </linearGradient>

    <linearGradient id="fj_barrel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#929da8" />
      <stop offset="45%" stop-color="#4f5862" />
      <stop offset="100%" stop-color="#252b31" />
    </linearGradient>

    <linearGradient id="fj_crimson" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#c43d31" />
      <stop offset="55%" stop-color="#91261e" />
      <stop offset="100%" stop-color="#5c1610" />
    </linearGradient>

    <linearGradient id="fj_gold" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e8c368" />
      <stop offset="50%" stop-color="#b88c34" />
      <stop offset="100%" stop-color="#6e4f18" />
    </linearGradient>

    <radialGradient id="fj_redLens" cx="45%" cy="45%" r="55%">
      <stop offset="0%" stop-color="#ffd6d6" />
      <stop offset="35%" stop-color="#ff4038" />
      <stop offset="75%" stop-color="#cc1414" />
      <stop offset="100%" stop-color="#660808" />
    </radialGradient>

    <radialGradient id="fj_redHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ff3b30" stop-opacity="0.62" />
      <stop offset="55%" stop-color="#d91e18" stop-opacity="0.22" />
      <stop offset="100%" stop-color="#d91e18" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Алые ореолы красных лазерных линз и ядерного блока -->
  <g id="fj_halos">
    <circle cx="470" cy="242" r="58" fill="url(#fj_redHalo)" />
    <circle cx="212" cy="304" r="58" fill="url(#fj_redHalo)" />
  </g>

  <!-- Верхняя силовая ручка, тыльник и рукоять -->
  <g id="fj_handles">
    <path d="M100,196 L118,154 L230,154 L246,196 L222,196 L212,174 L136,174 L124,196 Z"
          fill="url(#fj_darkMetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <path d="M30,206 L82,200 L82,292 L34,300 L28,284 L28,218 Z"
          fill="url(#fj_darkMetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <line x1="48" y1="214" x2="48" y2="286" stroke="#181c22" stroke-width="6" />

    <path d="M104,292 L148,292 L138,358 L100,354 Z"
          fill="url(#fj_darkMetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <path d="M144,294 L174,294 L168,324 L142,324"
          fill="none" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
  </g>

  <!-- Вращающийся блок лазерных стволов с красными линзами -->
  <g id="fj_barrels">
    <!-- Раскалённая красная ось блока стволов -->
    <rect x="272" y="224" width="194" height="36" rx="4"
          fill="url(#fj_redLens)" stroke="#121519" stroke-width="7" />

    <!-- Четыре параллельных тяжёлых ствола -->
    <rect x="272" y="198" width="204" height="18" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="9" />
    <rect x="272" y="220" width="208" height="18" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="9" />
    <rect x="272" y="246" width="208" height="18" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="9" />
    <rect x="272" y="268" width="204" height="18" rx="4"
          fill="url(#fj_barrel)" stroke="#121519" stroke-width="9" />

    <!-- Два массивных хомута вращения -->
    <rect x="332" y="190" width="26" height="104" rx="6"
          fill="url(#fj_steel)" stroke="#121519" stroke-width="11" />
    <line x1="345" y1="200" x2="345" y2="284" stroke="#2c333b" stroke-width="5" />

    <rect x="412" y="190" width="26" height="104" rx="6"
          fill="url(#fj_steel)" stroke="#121519" stroke-width="11" />
    <line x1="425" y1="200" x2="425" y2="284" stroke="#2c333b" stroke-width="5" />

    <!-- Крупные светящиеся красные лазерные линзы на дульных срезах -->
    <rect x="462" y="199" width="20" height="16" rx="4" fill="url(#fj_redLens)" stroke="#121519" stroke-width="5" />
    <rect x="466" y="221" width="20" height="16" rx="4" fill="url(#fj_redLens)" stroke="#121519" stroke-width="5" />
    <rect x="466" y="247" width="20" height="16" rx="4" fill="url(#fj_redLens)" stroke="#121519" stroke-width="5" />
    <rect x="462" y="269" width="20" height="16" rx="4" fill="url(#fj_redLens)" stroke="#121519" stroke-width="5" />
  </g>

  <!-- Бронированный корпус Братства Стали -->
  <g id="fj_body">
    <path d="M74,196 L264,188 L282,206 L282,284 L264,298 L74,296 Z"
          fill="url(#fj_steel)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхний блик -->
    <line x1="88" y1="204" x2="256" y2="198" stroke="#dce5ee" stroke-width="5" opacity="0.65" stroke-linecap="round" />

    <!-- Гербовые полосы Братства Стали -->
    <polygon points="100,198 126,197 112,292 86,292" fill="url(#fj_crimson)" stroke="#121519" stroke-width="5" />
    <polygon points="132,197 150,196 136,292 118,292" fill="url(#fj_gold)" stroke="#121519" stroke-width="5" />

    <!-- Раскалённые красные решётки охлаждения -->
    <rect x="172" y="204" width="88" height="28" rx="5"
          fill="#260c0c" stroke="#121519" stroke-width="7" />
    <line x1="188" y1="210" x2="188" y2="226" stroke="#ff4238" stroke-width="7" stroke-linecap="round" />
    <line x1="208" y1="210" x2="208" y2="226" stroke="#ff7a70" stroke-width="7" stroke-linecap="round" />
    <line x1="228" y1="210" x2="228" y2="226" stroke="#ff7a70" stroke-width="7" stroke-linecap="round" />
    <line x1="246" y1="210" x2="246" y2="226" stroke="#ff4238" stroke-width="7" stroke-linecap="round" />

    <!-- Эмблема шестерни и меча Братства -->
    <circle cx="158" cy="252" r="15" fill="url(#fj_darkMetal)" stroke="#e8c368" stroke-width="5" />
    <line x1="158" y1="234" x2="158" y2="270" stroke="#e8c368" stroke-width="5" stroke-linecap="round" />
    <line x1="146" y1="252" x2="170" y2="252" stroke="#e8c368" stroke-width="5" stroke-linecap="round" />

    <!-- Заклёпки -->
    <circle cx="88" cy="214" r="5" fill="#2a3138" stroke="#121519" stroke-width="4" />
    <circle cx="266" cy="220" r="5" fill="#2a3138" stroke="#121519" stroke-width="4" />
    <circle cx="266" cy="272" r="5" fill="#2a3138" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Боковой микроядерный блок -->
  <g id="fj_core">
    <path d="M244,308 C274,316 290,302 280,276"
          fill="none" stroke="#121519" stroke-width="15" stroke-linecap="round" />
    <path d="M244,308 C274,316 290,302 280,276"
          fill="none" stroke="#c4963a" stroke-width="6" stroke-linecap="round" />

    <rect x="172" y="264" width="78" height="76" rx="12"
          fill="url(#fj_gold)" stroke="#121519" stroke-width="12" />
    <rect x="184" y="276" width="54" height="52" rx="6"
          fill="#240a0a" stroke="#121519" stroke-width="6" />
    <rect x="190" y="282" width="42" height="40" rx="4" fill="url(#fj_redLens)" />
    <line x1="204" y1="278" x2="204" y2="326" stroke="#121519" stroke-width="6" />
    <line x1="218" y1="278" x2="218" y2="326" stroke="#121519" stroke-width="6" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_LorenzosArtifact.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="lor_bronze" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#d4ad6a" />
      <stop offset="45%" stop-color="#9e7438" />
      <stop offset="85%" stop-color="#66461c" />
      <stop offset="100%" stop-color="#422b0f" />
    </linearGradient>

    <linearGradient id="lor_bronzeBright" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#f5dc99" />
      <stop offset="55%" stop-color="#c49343" />
      <stop offset="100%" stop-color="#78521c" />
    </linearGradient>

    <linearGradient id="lor_gunmetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#6c757d" />
      <stop offset="50%" stop-color="#434b52" />
      <stop offset="100%" stop-color="#22272c" />
    </linearGradient>

    <linearGradient id="lor_gripWood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#6b4a33" />
      <stop offset="50%" stop-color="#452d1d" />
      <stop offset="100%" stop-color="#24160d" />
    </linearGradient>

    <radialGradient id="lor_orbCore" cx="40%" cy="38%" r="58%">
      <stop offset="0%" stop-color="#ffffff" />
      <stop offset="28%" stop-color="#fff099" />
      <stop offset="65%" stop-color="#f2a922" />
      <stop offset="100%" stop-color="#995206" />
    </radialGradient>

    <radialGradient id="lor_orbHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffe47a" stop-opacity="0.72" />
      <stop offset="48%" stop-color="#f5a623" stop-opacity="0.30" />
      <stop offset="78%" stop-color="#d97b16" stop-opacity="0.10" />
      <stop offset="100%" stop-color="#d97b16" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Золотой телекинетический ореол вокруг шара и переднего излучателя -->
  <g id="lor_halo">
    <circle cx="384" cy="236" r="102" fill="url(#lor_orbHalo)" />
  </g>

  <!-- Рукоять и спусковая скоба (слева) -->
  <g id="lor_grip">
    <!-- Спусковая скоба -->
    <path d="M134,274 L196,274 L188,314 L132,314 Z"
          fill="none" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M162,276 L154,302" fill="none" stroke="#121519" stroke-width="7" stroke-linecap="round" />

    <!-- Рукоять гамма-пушки -->
    <path d="M84,264 L136,264 L112,358 L62,350 Z"
          fill="url(#lor_gripWood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <!-- Латунная оковка рукояти -->
    <path d="M62,350 L112,358 L118,338 L68,330 Z"
          fill="url(#lor_bronze)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
    <line x1="88" y1="286" x2="120" y2="292" stroke="#24160d" stroke-width="5" />
    <line x1="82" y1="308" x2="114" y2="314" stroke="#24160d" stroke-width="5" />
  </g>

  <!-- Нижний силовой кабель к излучателю -->
  <g id="lor_cable">
    <path d="M188,284 C232,326 306,326 352,292"
          fill="none" stroke="#121519" stroke-width="18" stroke-linecap="round" />
    <path d="M188,284 C232,326 306,326 352,292"
          fill="none" stroke="#c49343" stroke-width="7" stroke-linecap="round" />
  </g>

  <!-- Основной корпус-генератор -->
  <g id="lor_body">
    <!-- Задний колпак-конденсатор слева -->
    <path d="M36,214 C36,192 54,182 82,182 L106,182 L106,274 L82,274 C54,274 36,262 36,240 Z"
          fill="url(#lor_gunmetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Центральный корпус -->
    <path d="M96,178 L272,174 L294,194 L294,276 L272,290 L96,282 Z"
          fill="url(#lor_gunmetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Светлый верхний блик корпуса -->
    <line x1="108" y1="188" x2="266" y2="185" stroke="#b8c2cc" stroke-width="5" opacity="0.6" stroke-linecap="round" />

    <!-- Две древние бронзовые индукционные катушки сверху -->
    <rect x="132" y="150" width="38" height="32" rx="6"
          fill="url(#lor_bronzeBright)" stroke="#121519" stroke-width="10" />
    <rect x="192" y="148" width="44" height="34" rx="6"
          fill="url(#lor_bronzeBright)" stroke="#121519" stroke-width="10" />

    <!-- Бронзовая оккультная пластина с золотым индикатором в центре -->
    <rect x="122" y="206" width="136" height="54" rx="10"
          fill="url(#lor_bronze)" stroke="#121519" stroke-width="9" />
    <circle cx="158" cy="233" r="14" fill="url(#lor_orbCore)" stroke="#121519" stroke-width="5" />
    <!-- Гравировка артефакта (резные линии и руна) -->
    <line x1="188" y1="220" x2="240" y2="220" stroke="#ffe79e" stroke-width="5" stroke-linecap="round" />
    <line x1="188" y1="234" x2="232" y2="234" stroke="#ffe79e" stroke-width="5" stroke-linecap="round" />
    <line x1="188" y1="248" x2="240" y2="248" stroke="#ffe79e" stroke-width="5" stroke-linecap="round" />
  </g>

  <!-- Задняя часть каркаса-короны Лоренцо -->
  <g id="lor_cage_back">
    <!-- Соединительная шейка излучателя -->
    <rect x="288" y="206" width="44" height="60" rx="6"
          fill="url(#lor_bronze)" stroke="#121519" stroke-width="11" />

    <!-- Задняя дуга чаши-короны -->
    <path d="M322,158 C362,158 386,192 386,236 C386,280 362,314 322,314"
          fill="none" stroke="#121519" stroke-width="20" stroke-linecap="round" />
    <path d="M322,158 C362,158 386,192 386,236 C386,280 362,314 322,314"
          fill="none" stroke="url(#lor_bronze)" stroke-width="8" stroke-linecap="round" />
  </g>

  <!-- Золотой светящийся шар-артефакт в центре каркаса -->
  <g id="lor_golden_orb">
    <circle cx="384" cy="236" r="46" fill="url(#lor_orbCore)" stroke="#121519" stroke-width="10" />
    <!-- Резное внутреннее кольцо и блик -->
    <circle cx="384" cy="236" r="28" fill="none" stroke="#fff7cc" stroke-width="4.5" opacity="0.7" />
    <circle cx="370" cy="222" r="10" fill="#ffffff" opacity="0.88" />
  </g>

  <!-- Передние рёбра древнего каркаса и расходящиеся дуги излучения -->
  <g id="lor_cage_front">
    <!-- Верхний изогнутый зубец короны -->
    <path d="M308,186 C348,144 416,146 462,186 L446,204 C410,174 358,176 322,206 Z"
          fill="url(#lor_bronzeBright)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Нижний изогнутый зубец короны -->
    <path d="M308,286 C348,328 416,326 462,286 L446,268 C410,298 358,296 322,266 Z"
          fill="url(#lor_bronze)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Центральный охватывающий обруч поверх шара -->
    <path d="M352,190 C376,208 376,264 352,282"
          fill="none" stroke="#121519" stroke-width="16" stroke-linecap="round" />
    <path d="M352,190 C376,208 376,264 352,282"
          fill="none" stroke="url(#lor_bronzeBright)" stroke-width="6" stroke-linecap="round" />

    <!-- Золотые телекинетические волны на выходе справа -->
    <path d="M452,204 C468,220 468,252 452,268"
          fill="none" stroke="#fff0a6" stroke-width="6" stroke-linecap="round" />
    <path d="M470,192 C488,214 488,258 470,280"
          fill="none" stroke="#ffd24d" stroke-width="5" stroke-linecap="round" opacity="0.85" />
  </g>
</svg>
```