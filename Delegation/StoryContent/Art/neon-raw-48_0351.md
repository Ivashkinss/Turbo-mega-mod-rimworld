`Art/Weapons/TMM_Legendary_AlphaPredator.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="ap_bioferrite" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#523c4e" />
      <stop offset="38%" stop-color="#2b1e2a" />
      <stop offset="78%" stop-color="#170f18" />
      <stop offset="100%" stop-color="#0b070c" />
    </linearGradient>

    <linearGradient id="ap_bioHighlight" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#8c637e" />
      <stop offset="50%" stop-color="#b8849f" />
      <stop offset="100%" stop-color="#5e3d54" />
    </linearGradient>

    <linearGradient id="ap_fangs" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fff0f2" />
      <stop offset="55%" stop-color="#d9b2ba" />
      <stop offset="100%" stop-color="#7a2c3b" />
    </linearGradient>

    <linearGradient id="ap_grip" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#731d2c" />
      <stop offset="50%" stop-color="#420e18" />
      <stop offset="100%" stop-color="#1c050a" />
    </linearGradient>

    <radialGradient id="ap_bloodCore" cx="42%" cy="42%" r="58%">
      <stop offset="0%" stop-color="#ffd6df" />
      <stop offset="32%" stop-color="#ff2e55" />
      <stop offset="74%" stop-color="#a80c28" />
      <stop offset="100%" stop-color="#420310" />
    </radialGradient>

    <radialGradient id="ap_bloodHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ff244c" stop-opacity="0.68" />
      <stop offset="46%" stop-color="#9e0e2b" stop-opacity="0.30" />
      <stop offset="80%" stop-color="#4a071c" stop-opacity="0.10" />
      <stop offset="100%" stop-color="#4a071c" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Багрово-алый хищный ореол крови Альфа-вампира -->
  <g id="ap_halo">
    <circle cx="296" cy="216" r="142" fill="url(#ap_bloodHalo)" />
  </g>

  <!-- Живой клинок повёрнут по диагонали (рукоять слева-снизу, хищное лезвие справа-сверху) -->
  <g id="ap_sword_group" transform="rotate(-45 256 256)">
    <!-- Зазубренные костяные клыки вдоль нижней режущей кромки -->
    <g id="ap_fangs_edge">
      <polygon points="214,274 228,308 248,274" fill="url(#ap_fangs)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="252,272 268,310 288,270" fill="url(#ap_fangs)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="292,268 310,306 328,266" fill="url(#ap_fangs)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="332,264 350,298 368,260" fill="url(#ap_fangs)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="372,258 392,288 408,254" fill="url(#ap_fangs)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
    </g>

    <!-- Живое биоферритовое лезвие -->
    <g id="ap_blade">
      <path d="M180,224 C252,212 344,202 426,216 C458,222 480,234 494,248
               C466,266 424,274 366,276 C298,278 236,282 180,282 Z"
            fill="url(#ap_bioferrite)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

      <!-- Светлый органический обух лезвия -->
      <path d="M192,232 C266,220 348,214 422,224 C446,228 462,234 474,242"
            fill="none" stroke="url(#ap_bioHighlight)" stroke-width="5" opacity="0.7" stroke-linecap="round" />

      <!-- Хитиновые чешуйчатые насечки вдоль обуха -->
      <path d="M242,220 L252,236 M298,214 L306,232 M356,214 L362,232 M412,218 L416,234"
            fill="none" stroke="#121519" stroke-width="5" stroke-linecap="round" />

      <!-- Пульсирующая артерия живой крови внутри биоферрита -->
      <path d="M198,254 C248,248 296,258 348,248 C392,240 432,244 472,248"
            fill="none" stroke="#ff244c" stroke-width="7.5" stroke-linecap="round" />
      <path d="M202,254 C250,248 298,258 348,248 C390,240 428,244 464,248"
            fill="none" stroke="#ffb3c2" stroke-width="3" stroke-linecap="round" opacity="0.88" />
      <!-- Боковые капилляры к клыкам -->
      <path d="M266,252 L272,272 M308,252 L314,270 M352,248 L358,266 M396,244 L402,260"
            fill="none" stroke="#ff2e55" stroke-width="4" stroke-linecap="round" />
    </g>

    <!-- Хищная крылато-рёберная гарда с живым сердцем крови -->
    <g id="ap_guard">
      <path d="M148,204 C166,174 202,166 226,178 C206,196 198,220 200,254
               C198,288 206,312 226,330 C202,342 166,334 148,304 Z"
            fill="url(#ap_bioferrite)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

      <!-- Костяные шипы на концах гарды -->
      <path d="M170,192 L204,182 M166,216 L194,210 M166,292 L194,298 M170,316 L204,326"
            fill="none" stroke="#a67a90" stroke-width="4.5" stroke-linecap="round" opacity="0.65" />

      <!-- Пульсирующий рубиновый кристалл крови в сердце гарды -->
      <circle cx="176" cy="254" r="18" fill="url(#ap_bloodCore)" stroke="#121519" stroke-width="6" />
      <circle cx="171" cy="249" r="5" fill="#ffffff" opacity="0.82" />
    </g>

    <!-- Живая рукоять и когтистое навершие -->
    <g id="ap_hilt">
      <rect x="64" y="236" width="90" height="36" rx="8"
            fill="url(#ap_grip)" stroke="#121519" stroke-width="10" />
      <!-- Сухожильные рёбра обмотки -->
      <path d="M84,238 L94,270 M106,238 L116,270 M128,238 L138,270"
            fill="none" stroke="#121519" stroke-width="6" stroke-linecap="round" />
      <path d="M90,242 L98,266 M112,242 L120,266 M134,242 L142,266"
            fill="none" stroke="#bf344e" stroke-width="3" opacity="0.7" stroke-linecap="round" />

      <!-- Когтистое навершие с каплей крови -->
      <path d="M28,254 L54,224 L74,234 L74,274 L54,284 Z"
            fill="url(#ap_bioferrite)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
      <circle cx="54" cy="254" r="10" fill="url(#ap_bloodCore)" stroke="#121519" stroke-width="4.5" />
    </g>
  </g>

  <!-- Парящие капли алой крови вокруг живого клинка -->
  <g id="ap_droplets">
    <path d="M358,116 C366,128 366,138 358,142 C350,138 350,128 358,116 Z"
          fill="#ff385c" stroke="#121519" stroke-width="4" stroke-linejoin="round" />
    <circle cx="418" cy="184" r="6" fill="#ff99ad" stroke="#a80c28" stroke-width="3" />
    <circle cx="240" cy="166" r="5" fill="#ff99ad" stroke="#a80c28" stroke-width="3" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Gun_BoraxSprayer.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="bx_enamel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f7fbff" />
      <stop offset="48%" stop-color="#d2e2ed" />
      <stop offset="86%" stop-color="#9cb3c4" />
      <stop offset="100%" stop-color="#6b8191" />
    </linearGradient>

    <linearGradient id="bx_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#98a5b2" />
      <stop offset="46%" stop-color="#5e6a75" />
      <stop offset="100%" stop-color="#2e353d" />
    </linearGradient>

    <linearGradient id="bx_darkMetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#55606b" />
      <stop offset="50%" stop-color="#313840" />
      <stop offset="100%" stop-color="#181c21" />
    </linearGradient>

    <linearGradient id="bx_chemBand" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8ff0ff" />
      <stop offset="50%" stop-color="#2bb8db" />
      <stop offset="100%" stop-color="#126885" />
    </linearGradient>

    <linearGradient id="bx_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f2d680" />
      <stop offset="55%" stop-color="#bd9034" />
      <stop offset="100%" stop-color="#6e4e14" />
    </linearGradient>

    <linearGradient id="bx_valveRed" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ff6b5c" />
      <stop offset="55%" stop-color="#c93022" />
      <stop offset="100%" stop-color="#6e160f" />
    </linearGradient>

    <radialGradient id="bx_mistHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.82" />
      <stop offset="42%" stop-color="#b8f4ff" stop-opacity="0.40" />
      <stop offset="76%" stop-color="#5ce1ff" stop-opacity="0.14" />
      <stop offset="100%" stop-color="#5ce1ff" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Едкое бело-голубое облако буры на выходе из форсунки -->
  <g id="bx_mist_halo">
    <circle cx="458" cy="236" r="76" fill="url(#bx_mistHalo)" />
  </g>

  <!-- Рукояти удержания и шланг высокого давления снизу -->
  <g id="bx_lower">
    <!-- Армированный шланг подачи раствора -->
    <path d="M168,284 C216,336 292,336 336,274"
          fill="none" stroke="#121519" stroke-width="20" stroke-linecap="round" />
    <path d="M168,284 C216,336 292,336 336,274"
          fill="none" stroke="#63d8f2" stroke-width="7" stroke-linecap="round" />

    <!-- Спусковая скоба и курок -->
    <path d="M128,282 L182,282 L174,318 L126,318 Z"
          fill="none" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M154,284 L148,308" fill="none" stroke="#121519" stroke-width="7" stroke-linecap="round" />

    <!-- Задняя рукоять -->
    <path d="M90,278 L134,278 L118,360 L78,354 Z"
          fill="url(#bx_darkMetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <line x1="94" y1="304" x2="122" y2="308" stroke="#181c21" stroke-width="5" />
    <line x1="90" y1="324" x2="118" y2="328" stroke="#181c21" stroke-width="5" />

    <!-- Передняя рукоять насоса -->
    <path d="M276,278 L314,278 L322,344 L286,344 Z"
          fill="url(#bx_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
  </g>

  <!-- Ствол-распылитель и широкая конусная форсунка -->
  <g id="bx_nozzle">
    <!-- Нижняя опорная трубка -->
    <rect x="310" y="256" width="114" height="18" rx="4"
          fill="url(#bx_darkMetal)" stroke="#121519" stroke-width="9" />

    <!-- Основная труба распылителя -->
    <rect x="306" y="218" width="120" height="36" rx="6"
          fill="url(#bx_steel)" stroke="#121519" stroke-width="10" />
    <line x1="316" y1="227" x2="416" y2="227" stroke="#d5e1eb" stroke-width="4" opacity="0.6" stroke-linecap="round" />

    <!-- Латунные уплотнительные кольца на трубке -->
    <rect x="336" y="212" width="18" height="48" rx="4"
          fill="url(#bx_brass)" stroke="#121519" stroke-width="7" />
    <rect x="378" y="212" width="18" height="48" rx="4"
          fill="url(#bx_brass)" stroke="#121519" stroke-width="7" />

    <!-- Широкая промышленная конусная форсунка -->
    <path d="M410,214 L462,190 L470,204 L470,268 L462,282 L410,258 Z"
          fill="url(#bx_steel)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <ellipse cx="464" cy="236" rx="10" ry="38" fill="#182229" stroke="#121519" stroke-width="7" />
    <ellipse cx="464" cy="236" rx="5" ry="26" fill="#d9f9ff" />
  </g>

  <!-- Задний упор и вентиль давления -->
  <g id="bx_rear">
    <path d="M36,214 L84,210 L84,284 L40,290 L32,276 L32,226 Z"
          fill="url(#bx_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Красный вентиль сброса давления сверху-слева -->
    <rect x="52" y="180" width="14" height="32" rx="3" fill="url(#bx_brass)" stroke="#121519" stroke-width="6" />
    <rect x="36" y="170" width="46" height="14" rx="5" fill="url(#bx_valveRed)" stroke="#121519" stroke-width="7" />
  </g>

  <!-- Главный баллон с раствором тетрабората натрия (буры) -->
  <g id="bx_tank">
    <!-- Верхняя ручка переноски над баллоном -->
    <path d="M120,180 L134,148 L260,148 L274,180 L252,180 L242,164 L152,164 L142,180 Z"
          fill="url(#bx_darkMetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />

    <!-- Нижняя стальная рама-ложемент баллона -->
    <path d="M78,242 L316,242 L308,284 L84,284 Z"
          fill="url(#bx_steel)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Белый эмалированный цилиндрический резервуар буры -->
    <rect x="86" y="178" width="228" height="90" rx="28"
          fill="url(#bx_enamel)" stroke="#121519" stroke-width="12" />

    <!-- Светлый блик по верхней части баллона -->
    <line x1="114" y1="192" x2="284" y2="192" stroke="#ffffff" stroke-width="6" opacity="0.82" stroke-linecap="round" />

    <!-- Голубые химические полосы маркировки буры -->
    <rect x="118" y="182" width="22" height="82" fill="url(#bx_chemBand)" stroke="#121519" stroke-width="5" />
    <rect x="260" y="182" width="22" height="82" fill="url(#bx_chemBand)" stroke="#121519" stroke-width="5" />

    <!-- Смотровое окно уровня кипящего раствора буры -->
    <rect x="156" y="200" width="88" height="46" rx="10"
          fill="#102530" stroke="#121519" stroke-width="7" />
    <rect x="162" y="212" width="76" height="28" rx="6" fill="url(#bx_chemBand)" />
    <!-- Белая пена и пузырьки буры внутри окна -->
    <rect x="162" y="206" width="76" height="10" rx="4" fill="#f0fdff" opacity="0.92" />
    <circle cx="182" cy="226" r="4.5" fill="#ffffff" opacity="0.85" />
    <circle cx="206" cy="230" r="3.5" fill="#ffffff" opacity="0.8" />
    <circle cx="222" cy="222" r="5" fill="#ffffff" opacity="0.85" />

    <!-- Манометр давления на передней стяжке -->
    <circle cx="294" cy="224" r="18" fill="url(#bx_brass)" stroke="#121519" stroke-width="6" />
    <circle cx="294" cy="224" r="10" fill="#f5fbff" stroke="#121519" stroke-width="3" />
    <line x1="294" y1="224" x2="300" y2="217" stroke="#c93022" stroke-width="3.5" stroke-linecap="round" />
  </g>

  <!-- Вылетающие капли и облачка едкого белого тумана -->
  <g id="bx_mist_plumes">
    <circle cx="484" cy="216" r="9" fill="#f2fdff" stroke="#5ce1ff" stroke-width="3" opacity="0.9" />
    <circle cx="490" cy="238" r="12" fill="#ffffff" stroke="#8ff0ff" stroke-width="3" opacity="0.92" />
    <circle cx="482" cy="260" r="8" fill="#e6fbff" stroke="#5ce1ff" stroke-width="3" opacity="0.85" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Mod_HolyWaterTank.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="hw_brass" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#7a5718" />
      <stop offset="28%" stop-color="#f7dc8b" />
      <stop offset="62%" stop-color="#c99534" />
      <stop offset="100%" stop-color="#61420f" />
    </linearGradient>

    <linearGradient id="hw_silverCap" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#4e5863" />
      <stop offset="30%" stop-color="#e3edf5" />
      <stop offset="68%" stop-color="#9aa7b5" />
      <stop offset="100%" stop-color="#3c454f" />
    </linearGradient>

    <linearGradient id="hw_darkMount" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#58626e" />
      <stop offset="50%" stop-color="#303740" />
      <stop offset="100%" stop-color="#171b21" />
    </linearGradient>

    <linearGradient id="hw_water" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#e3fbff" />
      <stop offset="32%" stop-color="#75e1ff" />
      <stop offset="72%" stop-color="#2292e6" />
      <stop offset="100%" stop-color="#0e4c8c" />
    </linearGradient>

    <radialGradient id="hw_sacredHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#c4f6ff" stop-opacity="0.72" />
      <stop offset="46%" stop-color="#5cc8ff" stop-opacity="0.30" />
      <stop offset="78%" stop-color="#ffe485" stop-opacity="0.14" />
      <stop offset="100%" stop-color="#ffe485" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Мягкое освящённое голубовато-золотое сияние -->
  <g id="hw_halo">
    <circle cx="256" cy="272" r="168" fill="url(#hw_sacredHalo)" />
  </g>

  <!-- Подствольное крепление-рейка и подающий патрубок (слева и справа) -->
  <g id="hw_mounts">
    <!-- Левая крепёжная планка на оружие -->
    <path d="M114,196 L158,196 L158,348 L114,348 L104,332 L104,212 Z"
          fill="url(#hw_darkMount)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Зажимные винты крепления -->
    <rect x="94" y="220" width="22" height="24" rx="4" fill="url(#hw_brass)" stroke="#121519" stroke-width="7" />
    <rect x="94" y="300" width="22" height="24" rx="4" fill="url(#hw_brass)" stroke="#121519" stroke-width="7" />

    <!-- Правый изогнутый штуцер-патрубок подачи святой воды в ствол -->
    <path d="M352,208 C398,208 408,184 408,148"
          fill="none" stroke="#121519" stroke-width="24" stroke-linecap="round" />
    <path d="M352,208 C398,208 408,184 408,148"
          fill="none" stroke="url(#hw_brass)" stroke-width="10" stroke-linecap="round" />
    <rect x="392" y="128" width="32" height="22" rx="5"
          fill="url(#hw_silverCap)" stroke="#121519" stroke-width="8" />
  </g>

  <!-- Верхний латунный вентиль и клапан давления -->
  <g id="hw_valve">
    <!-- Горловина клапана -->
    <rect x="228" y="88" width="56" height="44" rx="6"
          fill="url(#hw_brass)" stroke="#121519" stroke-width="10" />
    <!-- Маховик вентиля сверху -->
    <rect x="192" y="64" width="128" height="26" rx="10"
          fill="url(#hw_brass)" stroke="#121519" stroke-width="10" />
    <line x1="208" y1="74" x2="304" y2="74" stroke="#fff4cc" stroke-width="4" opacity="0.65" stroke-linecap="round" />

    <!-- Компактный манометр сбоку горловины -->
    <circle cx="304" cy="118" r="22" fill="url(#hw_brass)" stroke="#121519" stroke-width="8" />
    <circle cx="304" cy="118" r="12" fill="#f5fcff" stroke="#121519" stroke-width="3.5" />
    <line x1="304" y1="118" x2="311" y2="111" stroke="#1c7ed6" stroke-width="4" stroke-linecap="round" />
  </g>

  <!-- Основной цилиндр со святой водой -->
  <g id="hw_tank_body">
    <!-- Прозрачная колба с сияющей святой водой -->
    <rect x="164" y="156" width="184" height="248" rx="16"
          fill="#0c2640" stroke="#121519" stroke-width="12" />

    <!-- Уровень освящённой воды внутри -->
    <rect x="172" y="182" width="168" height="214" rx="10" fill="url(#hw_water)" />
    <!-- Поверхностный мениск воды -->
    <ellipse cx="256" cy="186" rx="82" ry="12" fill="#d4f9ff" opacity="0.88" />

    <!-- Поднимающиеся сияющие пузырьки -->
    <circle cx="204" cy="348" r="10" fill="#e8fbff" opacity="0.85" />
    <circle cx="298" cy="322" r="12" fill="#e8fbff" opacity="0.8" />
    <circle cx="222" cy="242" r="8" fill="#ffffff" opacity="0.85" />
    <circle cx="306" cy="232" r="7" fill="#ffffff" opacity="0.8" />
    <circle cx="280" cy="364" r="6" fill="#d4f9ff" opacity="0.85" />

    <!-- Вертикальный стеклянный блик слева -->
    <rect x="184" y="196" width="18" height="178" rx="9" fill="#ffffff" opacity="0.48" />

    <!-- Защитные стальные дуги вдоль стекла -->
    <rect x="166" y="160" width="16" height="240" fill="url(#hw_silverCap)" stroke="#121519" stroke-width="6" />
    <rect x="330" y="160" width="16" height="240" fill="url(#hw_silverCap)" stroke="#121519" stroke-width="6" />

    <!-- Верхний бронированный купол-крышка бака -->
    <path d="M150,176 C150,136 186,120 256,120 C326,120 362,136 362,176 Z"
          fill="url(#hw_silverCap)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <rect x="148" y="162" width="216" height="24" rx="6"
          fill="url(#hw_brass)" stroke="#121519" stroke-width="9" />

    <!-- Нижняя бронированная крышка-основание бака -->
    <path d="M150,384 L362,384 C362,424 326,444 256,444 C186,444 150,424 150,384 Z"
          fill="url(#hw_silverCap)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <rect x="148" y="374" width="216" height="24" rx="6"
          fill="url(#hw_brass)" stroke="#121519" stroke-width="9" />

    <!-- Болты на верхнем и нижнем кольцах -->
    <circle cx="176" cy="174" r="5" fill="#121519" />
    <circle cx="256" cy="174" r="5" fill="#121519" />
    <circle cx="336" cy="174" r="5" fill="#121519" />
    <circle cx="176" cy="386" r="5" fill="#121519" />
    <circle cx="256" cy="386" r="5" fill="#121519" />
    <circle cx="336" cy="386" r="5" fill="#121519" />

    <!-- Центральная золотая охранная печать-крест на колбе -->
    <g id="hw_emblem">
      <circle cx="256" cy="284" r="46" fill="#0d3154" fill-opacity="0.72" stroke="url(#hw_brass)" stroke-width="9" />
      <circle cx="256" cy="284" r="46" fill="none" stroke="#121519" stroke-width="4" />
      <!-- Крест охотников -->
      <path d="M247,250 L265,250 L265,275 L290,275 L290,293 L265,293 L265,318 L247,318 L247,293 L222,293 L222,275 L247,275 Z"
            fill="url(#hw_brass)" stroke="#121519" stroke-width="6" stroke-linejoin="round" />
      <circle cx="256" cy="284" r="7" fill="#e8fbff" stroke="#121519" stroke-width="3.5" />
    </g>
  </g>

  <!-- Искры благословения вокруг бака -->
  <g id="hw_sparks">
    <polygon points="122,126 126,140 140,144 126,148 122,162 118,148 104,144 118,140"
             fill="#ffffff" stroke="#6bdcff" stroke-width="3" stroke-linejoin="round" />
    <polygon points="414,274 417,284 427,287 417,290 414,300 411,290 401,287 411,284"
             fill="#ffffff" stroke="#ffe485" stroke-width="2.5" stroke-linejoin="round" />
    <circle cx="394" cy="348" r="5" fill="#e3fbff" stroke="#2292e6" stroke-width="3" />
  </g>
</svg>
```

`Art/Weapons/TMM_Legendary_AtomsJudgement.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="atom_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#9ca7b2" />
      <stop offset="45%" stop-color="#646f7a" />
      <stop offset="100%" stop-color="#343b42" />
    </linearGradient>

    <linearGradient id="atom_steelDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#58636e" />
      <stop offset="50%" stop-color="#353d45" />
      <stop offset="100%" stop-color="#1b2026" />
    </linearGradient>

    <linearGradient id="atom_hazard" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f2cd5c" />
      <stop offset="55%" stop-color="#c49428" />
      <stop offset="100%" stop-color="#755210" />
    </linearGradient>

    <linearGradient id="atom_leather" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7c4e32" />
      <stop offset="50%" stop-color="#4e2e1b" />
      <stop offset="100%" stop-color="#29160b" />
    </linearGradient>

    <linearGradient id="atom_radCore" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ebffe0" />
      <stop offset="35%" stop-color="#76ff6b" />
      <stop offset="75%" stop-color="#23bf3c" />
      <stop offset="100%" stop-color="#0f5c1d" />
    </linearGradient>

    <radialGradient id="atom_radHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#85ff75" stop-opacity="0.70" />
      <stop offset="46%" stop-color="#2ee049" stop-opacity="0.30" />
      <stop offset="78%" stop-color="#178c2c" stop-opacity="0.10" />
      <stop offset="100%" stop-color="#178c2c" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Радиационный ореол вокруг 4 ядерных блоков -->
  <g id="atom_halo">
    <circle cx="342" cy="170" r="118" fill="url(#atom_radHalo)" />
  </g>

  <!-- Суперкувалда повернута по диагонали (рукоять слева-снизу, боёк справа-сверху) -->
  <g id="atom_sledge" transform="rotate(-45 256 256)">
    <!-- Стальное древко суперкувалды -->
    <g id="atom_shaft">
      <!-- Боковая гидравлическая трубка вдоль древка -->
      <path d="M138,274 L306,274 L318,266"
            fill="none" stroke="#121519" stroke-width="14" stroke-linecap="round" stroke-linejoin="round" />
      <path d="M138,274 L306,274 L318,266"
            fill="none" stroke="#9ea9b4" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />

      <!-- Основная труба древка -->
      <rect x="66" y="242" width="276" height="28" rx="6"
            fill="url(#atom_steel)" stroke="#121519" stroke-width="11" />
      <line x1="168" y1="248" x2="314" y2="248" stroke="#d5e0ea" stroke-width="4" opacity="0.55" stroke-linecap="round" />

      <!-- Обмотка рукояти -->
      <rect x="74" y="238" width="98" height="36" rx="7"
            fill="url(#atom_leather)" stroke="#121519" stroke-width="10" />
      <line x1="94" y1="242" x2="94" y2="270" stroke="#1f1008" stroke-width="5" />
      <line x1="114" y1="242" x2="114" y2="270" stroke="#1f1008" stroke-width="5" />
      <line x1="134" y1="242" x2="134" y2="270" stroke="#1f1008" stroke-width="5" />
      <line x1="154" y1="242" x2="154" y2="270" stroke="#1f1008" stroke-width="5" />

      <!-- Усиленный стальной тыльник -->
      <rect x="44" y="232" width="32" height="48" rx="8"
            fill="url(#atom_steelDark)" stroke="#121519" stroke-width="11" />
      <circle cx="60" cy="256" r="6" fill="url(#atom_hazard)" stroke="#121519" stroke-width="4" />

      <!-- Усиливающий хомут у основания бойка -->
      <rect x="284" y="234" width="34" height="44" rx="5"
            fill="url(#atom_hazard)" stroke="#121519" stroke-width="9" />
    </g>

    <!-- Массивный боёк с 4 приваренными фонящими ядерными блоками -->
    <g id="atom_head">
      <!-- Центральный стальной корпус бойка -->
      <path d="M316,174 L416,166 L432,184 L432,328 L416,346 L316,338 L306,320 L306,192 Z"
            fill="url(#atom_steelDark)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

      <!-- Промышленная жёлтая полоса-обойма -->
      <rect x="336" y="172" width="22" height="168" fill="url(#atom_hazard)" stroke="#121519" stroke-width="6" />
      <line x1="338" y1="210" x2="356" y2="224" stroke="#121519" stroke-width="6" />
      <line x1="338" y1="252" x2="356" y2="266" stroke="#121519" stroke-width="6" />
      <line x1="338" y1="294" x2="356" y2="308" stroke="#121519" stroke-width="6" />

      <!-- 4 приваренных протекающих ядерных блока (2 на верхнем торце, 2 на нижнем/ударном) -->
      <!-- Блок 1 (верхний передний) -->
      <rect x="366" y="144" width="46" height="48" rx="8"
            fill="url(#atom_steel)" stroke="#121519" stroke-width="9" />
      <rect x="375" y="152" width="28" height="32" rx="4"
            fill="url(#atom_radCore)" stroke="#121519" stroke-width="4.5" />

      <!-- Блок 2 (верхний средний) -->
      <rect x="372" y="198" width="52" height="48" rx="8"
            fill="url(#atom_steel)" stroke="#121519" stroke-width="9" />
      <rect x="382" y="206" width="32" height="32" rx="4"
            fill="url(#atom_radCore)" stroke="#121519" stroke-width="4.5" />

      <!-- Блок 3 (нижний средний) -->
      <rect x="372" y="266" width="52" height="48" rx="8"
            fill="url(#atom_steel)" stroke="#121519" stroke-width="9" />
      <rect x="382" y="274" width="32" height="32" rx="4"
            fill="url(#atom_radCore)" stroke="#121519" stroke-width="4.5" />

      <!-- Блок 4 (нижний передний) -->
      <rect x="366" y="320" width="46" height="48" rx="8"
            fill="url(#atom_steel)" stroke="#121519" stroke-width="9" />
      <rect x="375" y="328" width="28" height="32" rx="4"
            fill="url(#atom_radCore)" stroke="#121519" stroke-width="4.5" />

      <!-- Стальные стяжки сварки поверх блоков -->
      <path d="M362,190 L428,190 M362,256 L432,256 M362,322 L428,322"
            stroke="#121519" stroke-width="11" stroke-linecap="round" />
      <path d="M364,190 L426,190 M364,256 L430,256 M364,322 L426,322"
            stroke="#8e9aa6" stroke-width="4" stroke-linecap="round" />

      <!-- Протекающие зелёные трещины радиации -->
      <path d="M402,164 L418,154 M412,222 L434,218 M412,290 L434,294 M402,348 L418,358"
            fill="none" stroke="#c2ffba" stroke-width="5" stroke-linecap="round" />
    </g>
  </g>

  <!-- Радиоактивные искры вокруг ударной части -->
  <g id="atom_sparks">
    <circle cx="294" cy="96" r="6" fill="#d4ffca" stroke="#1f9e34" stroke-width="3" />
    <circle cx="426" cy="144" r="7" fill="#d4ffca" stroke="#1f9e34" stroke-width="3" />
    <circle cx="436" cy="230" r="5" fill="#d4ffca" stroke="#1f9e34" stroke-width="3" />
    <circle cx="246" cy="158" r="5" fill="#d4ffca" stroke="#1f9e34" stroke-width="3" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_TeslaCannon.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="tesla_armor" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#6b7682" />
      <stop offset="42%" stop-color="#3f4750" />
      <stop offset="85%" stop-color="#252b31" />
      <stop offset="100%" stop-color="#171b1f" />
    </linearGradient>

    <linearGradient id="tesla_plate" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b96a3" />
      <stop offset="50%" stop-color="#525c66" />
      <stop offset="100%" stop-color="#2e353d" />
    </linearGradient>

    <linearGradient id="tesla_copper" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f0bd73" />
      <stop offset="50%" stop-color="#bd7d36" />
      <stop offset="100%" stop-color="#6e4214" />
    </linearGradient>

    <linearGradient id="tesla_hazard" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe066" />
      <stop offset="55%" stop-color="#d49e22" />
      <stop offset="100%" stop-color="#7d580c" />
    </linearGradient>

    <linearGradient id="tesla_arcCore" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" />
      <stop offset="42%" stop-color="#7aeaff" />
      <stop offset="100%" stop-color="#1a85db" />
    </linearGradient>

    <radialGradient id="tesla_halo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#9cf2ff" stop-opacity="0.72" />
      <stop offset="45%" stop-color="#2cb6ff" stop-opacity="0.30" />
      <stop offset="80%" stop-color="#1368cc" stop-opacity="0.10" />
      <stop offset="100%" stop-color="#1368cc" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Электрическое свечение катушек и дульного излучателя -->
  <g id="tesla_halos">
    <circle cx="372" cy="236" r="82" fill="url(#tesla_halo)" />
    <circle cx="468" cy="236" r="52" fill="url(#tesla_halo)" />
  </g>

  <!-- Рукояти, плечевой упор и силовой кабель -->
  <g id="tesla_lower">
    <!-- Толстый жёлтый высоковольтный кабель -->
    <path d="M98,284 C132,338 238,342 308,282"
          fill="none" stroke="#121519" stroke-width="20" stroke-linecap="round" />
    <path d="M98,284 C132,338 238,342 308,282"
          fill="none" stroke="url(#tesla_hazard)" stroke-width="8" stroke-linecap="round" />

    <!-- Задняя рукоять управления -->
    <path d="M168,280 L210,280 L196,356 L158,352 Z"
          fill="url(#tesla_armor)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M206,282 L242,282 L236,314 L204,314"
          fill="none" stroke="#121519" stroke-width="9" stroke-linejoin="round" />

    <!-- Передняя рукоять поддержки -->
    <path d="M272,276 L308,276 L316,336 L282,336 Z"
          fill="url(#tesla_armor)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
  </g>

  <!-- Верхний прицельный блок и ручка переноски -->
  <g id="tesla_top">
    <path d="M138,186 L154,152 L262,152 L276,186 L252,186 L242,170 L172,170 L162,186 Z"
          fill="url(#tesla_plate)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Оптический прицел Анклава -->
    <rect x="182" y="156" width="52" height="18" rx="4"
          fill="url(#tesla_arcCore)" stroke="#121519" stroke-width="5" />
  </g>

  <!-- Открытый ствол-ускоритель с кольцами Теслы -->
  <g id="tesla_barrel">
    <!-- Центральный высоковольтный стержень -->
    <rect x="292" y="218" width="166" height="36" rx="6"
          fill="url(#tesla_armor)" stroke="#121519" stroke-width="10" />
    <rect x="296" y="227" width="158" height="18" rx="4"
          fill="url(#tesla_arcCore)" stroke="#121519" stroke-width="4.5" />

    <!-- Верхний и нижний изоляторные штыри на дуле -->
    <path d="M440,204 L482,212 L482,226 L444,226 Z"
          fill="url(#tesla_plate)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <path d="M440,268 L482,260 L482,246 L444,246 Z"
          fill="url(#tesla_plate)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />

    <!-- Три массивных медных индукционных кольца Теслы -->
    <rect x="318" y="192" width="26" height="88" rx="8"
          fill="url(#tesla_copper)" stroke="#121519" stroke-width="10" />
    <rect x="364" y="194" width="26" height="84" rx="8"
          fill="url(#tesla_copper)" stroke="#121519" stroke-width="10" />
    <rect x="408" y="198" width="24" height="76" rx="8"
          fill="url(#tesla_copper)" stroke="#121519" stroke-width="10" />

    <!-- Керамические ободки и блики на кольцах -->
    <line x1="331" y1="202" x2="331" y2="270" stroke="#fff3c4" stroke-width="4.5" opacity="0.65" stroke-linecap="round" />
    <line x1="377" y1="204" x2="377" y2="268" stroke="#fff3c4" stroke-width="4.5" opacity="0.65" stroke-linecap="round" />
    <line x1="420" y1="208" x2="420" y2="264" stroke="#fff3c4" stroke-width="4.5" opacity="0.65" stroke-linecap="round" />
  </g>

  <!-- Бронированный плечевой корпус Анклава -->
  <g id="tesla_body">
    <!-- Задний плечевой энергоблок -->
    <path d="M32,194 L118,188 L118,292 L38,302 L30,286 L30,208 Z"
          fill="url(#tesla_armor)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <!-- Жёлтые полосы высокого напряжения -->
    <rect x="46" y="204" width="16" height="82" rx="3" fill="url(#tesla_hazard)" stroke="#121519" stroke-width="4.5" />
    <rect x="74" y="214" width="28" height="58" rx="5" fill="#101a24" stroke="#121519" stroke-width="5" />
    <rect x="80" y="220" width="16" height="46" rx="3" fill="url(#tesla_arcCore)" />

    <!-- Центральный корпус-конденсатор -->
    <path d="M110,186 L288,182 L306,202 L306,274 L288,288 L110,288 Z"
          fill="url(#tesla_armor)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Светлая верхняя грань корпуса -->
    <line x1="122" y1="194" x2="282" y2="191" stroke="#9aa6b3" stroke-width="5" opacity="0.55" stroke-linecap="round" />

    <!-- Боковая бронированная пластина с отметкой Анклава -->
    <path d="M128,208 L268,208 L280,222 L280,256 L268,268 L128,268 Z"
          fill="url(#tesla_plate)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />

    <!-- Светящийся голубой индикатор заряда -->
    <rect x="144" y="224" width="76" height="28" rx="6" fill="#0d1824" stroke="#121519" stroke-width="6" />
    <rect x="150" y="230" width="64" height="16" rx="3" fill="url(#tesla_arcCore)" />

    <!-- Эмблема Анклава (золотая звезда в кольце) -->
    <circle cx="248" cy="238" r="16" fill="#1d2329" stroke="url(#tesla_hazard)" stroke-width="5" />
    <polygon points="248,226 251,234 260,234 253,240 256,248 248,243 240,248 243,240 236,234 245,234"
             fill="#ffe066" />
  </g>

  <!-- Разряды цепной молнии между кольцами и у дула -->
  <g id="tesla_lightning">
    <path d="M344,210 L354,196 L358,214 L364,204"
          fill="none" stroke="#ffffff" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
    <path d="M390,262 L399,276 L402,256 L408,266"
          fill="none" stroke="#ffffff" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" />
    <path d="M432,226 L448,214 L454,238 L474,236 L486,222"
          fill="none" stroke="#e0faff" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" />
    <path d="M460,236 L476,252 L486,246"
          fill="none" stroke="#7aeaff" stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_OverseersGuardian.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="og_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b95a0" />
      <stop offset="40%" stop-color="#58616b" />
      <stop offset="85%" stop-color="#333940" />
      <stop offset="100%" stop-color="#1e2227" />
    </linearGradient>

    <linearGradient id="og_darkSteel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#545d66" />
      <stop offset="50%" stop-color="#31373d" />
      <stop offset="100%" stop-color="#191d21" />
    </linearGradient>

    <linearGradient id="og_wood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#8a5938" />
      <stop offset="48%" stop-color="#5c361e" />
      <stop offset="100%" stop-color="#301a0d" />
    </linearGradient>

    <linearGradient id="og_vaultBlue" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#3c73b8" />
      <stop offset="55%" stop-color="#224a7d" />
      <stop offset="100%" stop-color="#142d4f" />
    </linearGradient>

    <linearGradient id="og_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f5d77a" />
      <stop offset="55%" stop-color="#c49735" />
      <stop offset="100%" stop-color="#735316" />
    </linearGradient>

    <radialGradient id="og_glint" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" stop-opacity="0.85" />
      <stop offset="42%" stop-color="#ffe799" stop-opacity="0.38" />
      <stop offset="100%" stop-color="#ffe799" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Лёгкий двойной отблеск у сдвоенного дульного среза -->
  <g id="og_muzzle_glow">
    <circle cx="470" cy="212" r="24" fill="url(#og_glint)" />
    <circle cx="470" cy="238" r="24" fill="url(#og_glint)" />
  </g>

  <!-- Рукоять, спусковая скоба и изогнутый магазин -->
  <g id="og_lower">
    <!-- Спусковая скоба -->
    <path d="M168,266 L216,266 L208,298 L166,298 Z"
          fill="none" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <path d="M190,268 L184,288" fill="none" stroke="#121519" stroke-width="6" stroke-linecap="round" />

    <!-- Изогнутый магазин повышенной ёмкости -->
    <path d="M226,266 L272,266 C274,300 282,328 294,350 L252,358 C238,330 230,300 226,266 Z"
          fill="url(#og_darkSteel)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Рёбра жёсткости и синяя лента Волт-Тек на магазине -->
    <polygon points="234,300 276,294 281,314 239,320" fill="url(#og_vaultBlue)" stroke="#121519" stroke-width="4" />
    <line x1="238" y1="312" x2="278" y2="306" stroke="#f5d77a" stroke-width="4" />
  </g>

  <!-- Деревянный приклад боевого карабина -->
  <g id="og_stock">
    <path d="M32,226 L124,218 L156,222 L156,268 L128,274 L46,320 L32,306 Z"
          fill="url(#og_wood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <!-- Стальной затыльник приклада -->
    <path d="M32,226 L50,224 L50,316 L38,318 L32,306 Z"
          fill="url(#og_darkSteel)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <!-- Блик на ореховом дереве -->
    <path d="M62,238 L136,232" fill="none" stroke="#b37b52" stroke-width="5" opacity="0.55" stroke-linecap="round" />
    <!-- Антабка -->
    <circle cx="82" cy="296" r="7" fill="url(#og_brass)" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Сдвоенные стволы (фирменная особенность «Стража Смотрителя») -->
  <g id="og_barrels">
    <!-- Верхний ствол -->
    <rect x="376" y="204" width="98" height="16" rx="3"
          fill="url(#og_steel)" stroke="#121519" stroke-width="9" />
    <!-- Нижний синхронный ствол -->
    <rect x="376" y="228" width="98" height="16" rx="3"
          fill="url(#og_darkSteel)" stroke="#121519" stroke-width="9" />

    <!-- Соединительный дульный хомут и сдвоенный компенсатор -->
    <rect x="448" y="198" width="32" height="52" rx="5"
          fill="url(#og_steel)" stroke="#121519" stroke-width="9" />
    <line x1="464" y1="206" x2="464" y2="242" stroke="#121519" stroke-width="5" />
    <!-- Мушка -->
    <polygon points="452,198 468,198 464,178 456,180"
             fill="url(#og_brass)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
  </g>

  <!-- Вентилируемое цевьё -->
  <g id="og_handguard">
    <!-- Верхний перфорированный кожух -->
    <path d="M294,202 L392,204 L388,248 L294,250 Z"
          fill="url(#og_darkSteel)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Вентиляционные отверстия охлаждения -->
    <rect x="312" y="212" width="14" height="12" rx="3" fill="#121519" />
    <rect x="334" y="212" width="14" height="12" rx="3" fill="#121519" />
    <rect x="356" y="212" width="14" height="12" rx="3" fill="#121519" />

    <!-- Нижняя деревянная накладка цевья -->
    <path d="M296,238 L384,238 L376,262 L296,262 Z"
          fill="url(#og_wood)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
  </g>

  <!-- Коллиматорный прицел сверху -->
  <g id="og_optic">
    <path d="M174,196 L186,168 L244,168 L254,196 Z"
          fill="url(#og_darkSteel)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <rect x="196" y="176" width="38" height="12" rx="3" fill="#70e0ff" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Сдвоенный ресивер карабина -->
  <g id="og_receiver">
    <path d="M144,196 L298,196 L306,208 L306,264 L294,272 L144,272 L136,256 L136,210 Z"
          fill="url(#og_steel)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхний блик -->
    <line x1="152" y1="204" x2="292" y2="204" stroke="#c8d3de" stroke-width="5" opacity="0.6" stroke-linecap="round" />

    <!-- Двойной затворный механизм (два параллельных окна выброса и латунных затвора) -->
    <rect x="208" y="208" width="68" height="15" rx="4" fill="#1b2026" stroke="#121519" stroke-width="5" />
    <rect x="216" y="212" width="38" height="7" rx="2" fill="url(#og_brass)" />

    <rect x="208" y="232" width="68" height="15" rx="4" fill="#1b2026" stroke="#121519" stroke-width="5" />
    <rect x="224" y="236" width="38" height="7" rx="2" fill="url(#og_brass)" />

    <!-- Памятный значок Смотрителя Убежища 81 (сине-золотой щиток) -->
    <circle cx="172" cy="234" r="17" fill="url(#og_vaultBlue)" stroke="url(#og_brass)" stroke-width="5" />
    <line x1="162" y1="234" x2="182" y2="234" stroke="#f5d77a" stroke-width="5" stroke-linecap="round" />
    <circle cx="172" cy="234" r="5" fill="#f5d77a" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_RailwayRifle.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="rail_iron" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b949c" />
      <stop offset="42%" stop-color="#586068" />
      <stop offset="85%" stop-color="#353b42" />
      <stop offset="100%" stop-color="#1f2328" />
    </linearGradient>

    <linearGradient id="rail_rust" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#9e6240" />
      <stop offset="50%" stop-color="#693b23" />
      <stop offset="100%" stop-color="#381d10" />
    </linearGradient>

    <linearGradient id="rail_copper" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f0b67d" />
      <stop offset="50%" stop-color="#bd773e" />
      <stop offset="100%" stop-color="#6e3e18" />
    </linearGradient>

    <linearGradient id="rail_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f0d382" />
      <stop offset="55%" stop-color="#bd9037" />
      <stop offset="100%" stop-color="#6e4e16" />
    </linearGradient>

    <linearGradient id="rail_wood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7c4e30" />
      <stop offset="50%" stop-color="#4d2c19" />
      <stop offset="100%" stop-color="#29150a" />
    </linearGradient>

    <linearGradient id="rail_hotSpike" x1="0%" y1="50%" x2="100%" y2="50%">
      <stop offset="0%" stop-color="#5c2818" />
      <stop offset="35%" stop-color="#d94518" />
      <stop offset="72%" stop-color="#ff9924" />
      <stop offset="100%" stop-color="#fff4b8" />
    </linearGradient>

    <radialGradient id="rail_spikeHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffbb4d" stop-opacity="0.72" />
      <stop offset="48%" stop-color="#ff5e1a" stop-opacity="0.28" />
      <stop offset="100%" stop-color="#ff5e1a" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Жаркое свечение раскалённого железнодорожного костыля -->
  <g id="rail_heat_halo">
    <circle cx="454" cy="228" r="56" fill="url(#rail_spikeHalo)" />
  </g>

  <!-- Облачко пара из верхнего свистка-клапана -->
  <g id="rail_steam" opacity="0.82">
    <circle cx="268" cy="144" r="14" fill="#edf4f8" />
    <circle cx="286" cy="134" r="18" fill="#dce7ee" />
    <circle cx="308" cy="126" r="12" fill="#edf4f8" />
  </g>

  <!-- Рукоять, курок и медный паропровод снизу -->
  <g id="rail_lower">
    <!-- Медная паропроводная трубка -->
    <path d="M174,284 C202,334 274,336 312,282"
          fill="none" stroke="#121519" stroke-width="18" stroke-linecap="round" />
    <path d="M174,284 C202,334 274,336 312,282"
          fill="none" stroke="url(#rail_copper)" stroke-width="7" stroke-linecap="round" />

    <!-- Спусковая скоба -->
    <path d="M150,274 L198,274 L190,310 L148,310 Z"
          fill="none" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <path d="M172,276 L166,298" fill="none" stroke="#121519" stroke-width="6" stroke-linecap="round" />

    <!-- Костыльная кассета-магазин -->
    <path d="M254,276 L310,276 L306,338 L258,338 Z"
          fill="url(#rail_iron)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <line x1="270" y1="286" x2="270" y2="328" stroke="#1b2026" stroke-width="6" />
    <line x1="284" y1="286" x2="284" y2="328" stroke="#ff7a29" stroke-width="5" />
    <line x1="296" y1="286" x2="296" y2="328" stroke="#1b2026" stroke-width="6" />
  </g>

  <!-- Самодельный деревянный приклад с железной скобой -->
  <g id="rail_stock">
    <path d="M32,226 L122,220 L150,224 L150,276 L118,284 L48,334 L32,320 Z"
          fill="url(#rail_wood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <path d="M32,226 L52,224 L52,330 L40,332 L32,320 Z"
          fill="url(#rail_rust)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <path d="M64,242 L134,236" fill="none" stroke="#a66f49" stroke-width="5" opacity="0.5" stroke-linecap="round" />
  </g>

  <!-- Открытый рельсовый ствол и раскалённый костыль -->
  <g id="rail_barrel">
    <!-- Раскалённый железнодорожный костыль, торчащий из ствола -->
    <polygon points="402,216 462,218 486,228 462,238 402,240"
             fill="url(#rail_hotSpike)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <!-- Шляпка костыля -->
    <rect x="404" y="210" width="14" height="36" rx="4"
          fill="#d94518" stroke="#121519" stroke-width="6" />

    <!-- Рельсовые направляющие ствола -->
    <rect x="304" y="198" width="142" height="18" rx="4"
          fill="url(#rail_iron)" stroke="#121519" stroke-width="10" />
    <rect x="304" y="242" width="142" height="18" rx="4"
          fill="url(#rail_iron)" stroke="#121519" stroke-width="10" />

    <!-- Стягивающие хомуты ствола -->
    <rect x="336" y="192" width="22" height="74" rx="5"
          fill="url(#rail_brass)" stroke="#121519" stroke-width="9" />
    <rect x="394" y="192" width="22" height="74" rx="5"
          fill="url(#rail_brass)" stroke="#121519" stroke-width="9" />
  </g>

  <!-- Паровой котёл-ресивер с манометром и паровым свистком -->
  <g id="rail_boiler">
    <!-- Верхний паровой свисток Подземки -->
    <rect x="248" y="158" width="22" height="44" rx="5"
          fill="url(#rail_brass)" stroke="#121519" stroke-width="9" />
    <rect x="242" y="154" width="34" height="12" rx="4"
          fill="url(#rail_copper)" stroke="#121519" stroke-width="7" />

    <!-- Массивный цилиндрический паровой котёл -->
    <path d="M138,204 C138,192 150,188 164,188 L294,188 C306,188 314,196 314,208 L314,272 C314,284 306,290 294,290 L164,290 C150,290 138,284 138,272 Z"
          fill="url(#rail_rust)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхний металлический блик котла -->
    <line x1="158" y1="200" x2="294" y2="200" stroke="#cf906b" stroke-width="6" opacity="0.55" stroke-linecap="round" />

    <!-- Стальные клёпаные обручи котла -->
    <rect x="150" y="190" width="16" height="98" fill="url(#rail_iron)" stroke="#121519" stroke-width="6" />
    <rect x="284" y="190" width="16" height="98" fill="url(#rail_iron)" stroke="#121519" stroke-width="6" />

    <!-- Крупный читаемый манометр давления пара -->
    <circle cx="224" cy="239" r="32" fill="url(#rail_brass)" stroke="#121519" stroke-width="9" />
    <circle cx="224" cy="239" r="21" fill="#f4f0e4" stroke="#121519" stroke-width="4" />
    <!-- Красная зона высокого давления на циферблате -->
    <path d="M232,222 A19,19 0 0,1 242,244" fill="none" stroke="#d93226" stroke-width="6" stroke-linecap="round" />
    <!-- Стрелка манометра -->
    <line x1="224" y1="239" x2="238" y2="228" stroke="#121519" stroke-width="5" stroke-linecap="round" />
    <circle cx="224" cy="239" r="5" fill="#121519" />

    <!-- Болты крепления -->
    <circle cx="158" cy="212" r="4" fill="#121519" />
    <circle cx="158" cy="268" r="4" fill="#121519" />
    <circle cx="292" cy="212" r="4" fill="#121519" />
    <circle cx="292" cy="268" r="4" fill="#121519" />
  </g>
</svg>
```

`Art/Weapons/TMM_Legendary_BigBoy.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="bb_olive" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b9668" />
      <stop offset="48%" stop-color="#5d6840" />
      <stop offset="100%" stop-color="#333b22" />
    </linearGradient>

    <linearGradient id="bb_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#9ca6b0" />
      <stop offset="45%" stop-color="#646e78" />
      <stop offset="100%" stop-color="#31373e" />
    </linearGradient>

    <linearGradient id="bb_darkMetal" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#59626b" />
      <stop offset="50%" stop-color="#343b42" />
      <stop offset="100%" stop-color="#1a1f24" />
    </linearGradient>

    <linearGradient id="bb_hazard" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe169" />
      <stop offset="55%" stop-color="#d49e24" />
      <stop offset="100%" stop-color="#7a560e" />
    </linearGradient>

    <linearGradient id="bb_nukeBody" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#92a867" />
      <stop offset="48%" stop-color="#5a7038" />
      <stop offset="100%" stop-color="#2c3a19" />
    </linearGradient>

    <linearGradient id="bb_nukeTip" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ff6b57" />
      <stop offset="55%" stop-color="#c93022" />
      <stop offset="100%" stop-color="#70160e" />
    </linearGradient>

    <radialGradient id="bb_radHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#b8ff7a" stop-opacity="0.62" />
      <stop offset="48%" stop-color="#ffd24d" stop-opacity="0.26" />
      <stop offset="82%" stop-color="#ff7a29" stop-opacity="0.08" />
      <stop offset="100%" stop-color="#ff7a29" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Радиоактивно-янтарный ореол вокруг сдвоенных мини-ядерных зарядов -->
  <g id="bb_halo">
    <circle cx="386" cy="212" r="98" fill="url(#bb_radHalo)" />
  </g>

  <!-- Рукояти удержания и спусковой механизм снизу -->
  <g id="bb_grips">
    <!-- Задняя рукоять управления -->
    <path d="M132,306 L174,306 L160,378 L122,374 Z"
          fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M170,308 L206,308 L200,340 L168,340"
          fill="none" stroke="#121519" stroke-width="9" stroke-linejoin="round" />

    <!-- Передняя опора-рукоять -->
    <path d="M276,302 L314,302 L322,366 L286,366 Z"
          fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Усиленный трос-шнур взвода катапульты -->
    <path d="M108,314 C166,356 238,356 286,318"
          fill="none" stroke="#121519" stroke-width="14" stroke-linecap="round" />
    <path d="M108,314 C166,356 238,356 286,318"
          fill="none" stroke="#a6b0ba" stroke-width="5" stroke-linecap="round" />
  </g>

  <!-- Сдвоенные мини-ядерные боеголовки на направляющих «Большого мальчика» -->
  <g id="bb_nukes">
    <!-- Верхний мини-ядерный снаряд (задний план) -->
    <g id="bb_nuke_top">
      <!-- Стабилизаторы -->
      <path d="M294,142 L328,130 L328,194 L294,182 Z"
            fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
      <!-- Овальный корпус бомбы -->
      <path d="M322,138 C362,126 420,132 452,162 C420,192 362,198 322,186 Z"
            fill="url(#bb_nukeBody)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
      <!-- Жёлтая предупреждающая полоса -->
      <path d="M388,136 C402,138 414,142 422,146 L422,178 C414,182 402,186 388,188 Z"
            fill="url(#bb_hazard)" stroke="#121519" stroke-width="5" />
      <!-- Красный взрыватель -->
      <path d="M432,148 L460,162 L432,176 Z"
            fill="url(#bb_nukeTip)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
      <line x1="338" y1="150" x2="378" y2="146" stroke="#d5ebb0" stroke-width="4" opacity="0.55" stroke-linecap="round" />
    </g>

    <!-- Нижний (передний) мини-ядерный снаряд -->
    <g id="bb_nuke_front">
      <!-- Стабилизаторы -->
      <path d="M306,192 L342,180 L342,248 L306,236 Z"
            fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
      <!-- Овальный корпус бомбы -->
      <path d="M336,188 C378,176 438,182 472,214 C438,246 378,252 336,240 Z"
            fill="url(#bb_nukeBody)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
      <!-- Жёлтая предупреждающая полоса -->
      <path d="M404,186 C418,188 430,192 438,196 L438,232 C430,236 418,240 404,242 Z"
            fill="url(#bb_hazard)" stroke="#121519" stroke-width="5" />
      <!-- Красный взрыватель -->
      <path d="M448,198 L480,214 L448,230 Z"
            fill="url(#bb_nukeTip)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
      <!-- Блик и знак радиации -->
      <line x1="352" y1="200" x2="394" y2="196" stroke="#d5ebb0" stroke-width="4.5" opacity="0.6" stroke-linecap="round" />
      <circle cx="378" cy="216" r="7" fill="url(#bb_hazard)" stroke="#121519" stroke-width="3.5" />
    </g>
  </g>

  <!-- Направляющие рельсы катапульты и пневмопоршни -->
  <g id="bb_rails">
    <!-- Пневматический толкатель -->
    <rect x="242" y="246" width="212" height="22" rx="5"
          fill="url(#bb_steel)" stroke="#121519" stroke-width="10" />
    <!-- Нижняя несущая рельса катапульты -->
    <path d="M236,268 L474,268 L466,304 L236,304 Z"
          fill="url(#bb_olive)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Опорные хомуты и рёбра жёсткости -->
    <rect x="292" y="240" width="24" height="66" rx="5"
          fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="8" />
    <rect x="372" y="240" width="24" height="66" rx="5"
          fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="8" />
    <rect x="442" y="244" width="22" height="62" rx="5"
          fill="url(#bb_hazard)" stroke="#121519" stroke-width="8" />
  </g>

  <!-- Массивная плечевая рама и блок запуска (слева) -->
  <g id="bb_chassis">
    <!-- Задний плечевой упор-демпфер -->
    <path d="M32,226 L98,218 L98,314 L40,324 L30,308 L30,240 Z"
          fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Главный корпус катапульты -->
    <path d="M88,214 L256,204 L274,224 L274,298 L256,312 L88,312 Z"
          fill="url(#bb_olive)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Светлая верхняя кромка корпуса -->
    <line x1="100" y1="222" x2="248" y2="214" stroke="#bdca97" stroke-width="5" opacity="0.55" stroke-linecap="round" />

    <!-- Предупреждающая жёлтая полоса на корпусе -->
    <rect x="108" y="220" width="24" height="84" rx="3" fill="url(#bb_hazard)" stroke="#121519" stroke-width="5" />
    <line x1="110" y1="242" x2="130" y2="256" stroke="#121519" stroke-width="6" />
    <line x1="110" y1="270" x2="130" y2="284" stroke="#121519" stroke-width="6" />

    <!-- Бронированная накладка супермутанта с заклёпками -->
    <path d="M148,232 L248,228 L258,242 L258,284 L246,294 L148,294 Z"
          fill="url(#bb_steel)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />

    <!-- Крупный манометр давления катапульты -->
    <circle cx="192" cy="262" r="22" fill="url(#bb_darkMetal)" stroke="#121519" stroke-width="7" />
    <circle cx="192" cy="262" r="13" fill="#f5f0dc" stroke="#121519" stroke-width="3.5" />
    <line x1="192" y1="262" x2="200" y2="254" stroke="#c93022" stroke-width="4.5" stroke-linecap="round" />
    <circle cx="192" cy="262" r="3.5" fill="#121519" />

    <!-- Болты -->
    <circle cx="236" cy="246" r="4.5" fill="#121519" />
    <circle cx="236" cy="278" r="4.5" fill="#121519" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_TheEqualizer.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="eq_steel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b96a6" />
      <stop offset="42%" stop-color="#535d6b" />
      <stop offset="85%" stop-color="#2c333d" />
      <stop offset="100%" stop-color="#191d24" />
    </linearGradient>

    <linearGradient id="eq_darkFrame" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#48505c" />
      <stop offset="50%" stop-color="#262c34" />
      <stop offset="100%" stop-color="#13171c" />
    </linearGradient>

    <linearGradient id="eq_gold" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffe799" />
      <stop offset="50%" stop-color="#cc9a38" />
      <stop offset="100%" stop-color="#704e14" />
    </linearGradient>

    <linearGradient id="eq_quantumTube" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#61c8ff" />
      <stop offset="50%" stop-color="#ffffff" />
      <stop offset="100%" stop-color="#c48aff" />
    </linearGradient>

    <radialGradient id="eq_core" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ffffff" />
      <stop offset="35%" stop-color="#9ce8ff" />
      <stop offset="72%" stop-color="#5c7cff" />
      <stop offset="100%" stop-color="#2e1c6b" />
    </radialGradient>

    <radialGradient id="eq_halo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#a8ecff" stop-opacity="0.72" />
      <stop offset="45%" stop-color="#7384ff" stop-opacity="0.30" />
      <stop offset="78%" stop-color="#9e5cff" stop-opacity="0.10" />
      <stop offset="100%" stop-color="#9e5cff" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Межпространственное сияние квантового ядра и дула -->
  <g id="eq_halos">
    <circle cx="242" cy="218" r="84" fill="url(#eq_halo)" />
    <circle cx="464" cy="206" r="46" fill="url(#eq_halo)" />
  </g>

  <!-- Спусковая скоба и тяжёлая тактическая рукоять -->
  <g id="eq_grip">
    <!-- Угловатая защитная скоба -->
    <path d="M180,264 L250,264 L238,312 L176,312 Z"
          fill="none" stroke="#121519" stroke-width="12" stroke-linejoin="round" />
    <path d="M214,266 L206,298" fill="none" stroke="#cc9a38" stroke-width="7" stroke-linecap="round" />

    <!-- Рукоять пистолета -->
    <path d="M108,246 L186,256 L154,394 L76,386 Z"
          fill="url(#eq_darkFrame)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Золотая боковая накладка с оккультным ромбом на рукояти -->
    <path d="M114,274 L164,280 L144,366 L94,360 Z"
          fill="url(#eq_gold)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
    <polygon points="129,296 144,322 125,348 110,322"
             fill="url(#eq_darkFrame)" stroke="#121519" stroke-width="5" stroke-linejoin="round" />
    <circle cx="127" cy="322" r="5" fill="#9ce8ff" />

    <!-- Пятка рукояти -->
    <path d="M72,380 L158,388 L154,408 L68,398 Z"
          fill="url(#eq_steel)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
  </g>

  <!-- Нижний стабилизатор ствола -->
  <g id="eq_underbarrel">
    <path d="M242,248 L434,244 L424,272 L246,274 Z"
          fill="url(#eq_darkFrame)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <!-- Светящаяся квантовая полоса снизу -->
    <rect x="272" y="253" width="132" height="10" rx="4"
          fill="url(#eq_quantumTube)" stroke="#121519" stroke-width="3.5" />
  </g>

  <!-- Основной затвор и межпространственный ствол «Уравнителя» -->
  <g id="eq_slide">
    <!-- Задний выступ затвора и мушка -->
    <path d="M74,178 L112,164 L444,164 L466,184 L466,238 L440,252 L112,262 L84,242 L74,196 Z"
          fill="url(#eq_steel)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхняя золотая планка и прицельные приспособления -->
    <path d="M118,164 L436,164 L436,182 L118,182 Z"
          fill="url(#eq_gold)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />
    <polygon points="420,164 438,164 434,146 424,148"
             fill="url(#eq_gold)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />

    <!-- Светлый верхний блик -->
    <line x1="128" y1="173" x2="416" y2="173" stroke="#fff6d1" stroke-width="4" opacity="0.7" stroke-linecap="round" />

    <!-- Вентиляционные окна квантового ускорителя на стволе -->
    <rect x="296" y="194" width="138" height="26" rx="6"
          fill="#121626" stroke="#121519" stroke-width="6" />
    <rect x="304" y="200" width="122" height="14" rx="4"
          fill="url(#eq_quantumTube)" />
    <!-- Золотые рёбра-перемычки поверх светового канала -->
    <line x1="336" y1="194" x2="336" y2="220" stroke="#cc9a38" stroke-width="7" />
    <line x1="368" y1="194" x2="368" y2="220" stroke="#cc9a38" stroke-width="7" />
    <line x1="400" y1="194" x2="400" y2="220" stroke="#cc9a38" stroke-width="7" />

    <!-- Дульный срез со вспышкой искажения -->
    <rect x="448" y="186" width="24" height="44" rx="5"
          fill="url(#eq_gold)" stroke="#121519" stroke-width="8" />
    <circle cx="468" cy="208" r="10" fill="url(#eq_core)" stroke="#121519" stroke-width="4" />

    <!-- Енохианская геометрическая гравировка на задней части затвора -->
    <path d="M114,202 L148,202 L160,214 L148,226 L114,226 Z"
          fill="none" stroke="url(#eq_gold)" stroke-width="5" stroke-linejoin="round" />
    <line x1="126" y1="214" x2="146" y2="214" stroke="#9ce8ff" stroke-width="4" stroke-linecap="round" />

    <!-- Круглая квантово-оккультная камера (сердце «Уравнителя») -->
    <g id="eq_quantum_core">
      <circle cx="242" cy="218" r="36" fill="url(#eq_gold)" stroke="#121519" stroke-width="9" />
      <circle cx="242" cy="218" r="24" fill="url(#eq_core)" stroke="#121519" stroke-width="5" />
      <!-- Внутренние перекрещенные орбиты -->
      <ellipse cx="242" cy="218" rx="18" ry="8" transform="rotate(-35 242 218)"
               fill="none" stroke="#ffffff" stroke-width="3.5" opacity="0.85" />
      <ellipse cx="242" cy="218" rx="18" ry="8" transform="rotate(35 242 218)"
               fill="none" stroke="#ffe799" stroke-width="3.5" opacity="0.85" />
      <circle cx="242" cy="218" r="6" fill="#ffffff" />
    </g>
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_FirstBlade.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="fb_bone" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#f2e9d5" />
      <stop offset="45%" stop-color="#d6c5a3" />
      <stop offset="85%" stop-color="#9e8765" />
      <stop offset="100%" stop-color="#69553c" />
    </linearGradient>

    <linearGradient id="fb_boneDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#bda986" />
      <stop offset="100%" stop-color="#5e4a33" />
    </linearGradient>

    <linearGradient id="fb_teeth" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fff8e3" />
      <stop offset="65%" stop-color="#dbc697" />
      <stop offset="100%" stop-color="#8c7249" />
    </linearGradient>

    <linearGradient id="fb_leather" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#6e2c26" />
      <stop offset="50%" stop-color="#451714" />
      <stop offset="100%" stop-color="#210908" />
    </linearGradient>

    <radialGradient id="fb_markHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#ff2e2e" stop-opacity="0.72" />
      <stop offset="46%" stop-color="#b80f18" stop-opacity="0.32" />
      <stop offset="80%" stop-color="#6e050c" stop-opacity="0.10" />
      <stop offset="100%" stop-color="#6e050c" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Зловещий багровый ореол Метки Каина -->
  <g id="fb_halo">
    <circle cx="294" cy="218" r="134" fill="url(#fb_markHalo)" />
  </g>

  <!-- Первый Клинок повернут по диагонали (рукоять слева-снизу, челюстная кость справа-сверху) -->
  <g id="fb_blade_group" transform="rotate(-45 256 256)">
    <!-- Острые зубы вдоль нижней режущей кромки челюстной кости -->
    <g id="fb_teeth_row">
      <polygon points="222,284 236,316 254,284" fill="url(#fb_teeth)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="256,282 272,318 290,282" fill="url(#fb_teeth)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="292,278 308,314 326,276" fill="url(#fb_teeth)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="328,274 344,306 360,270" fill="url(#fb_teeth)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
      <polygon points="362,268 378,296 394,262" fill="url(#fb_teeth)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
    </g>

    <!-- Первородная изогнутая челюстная кость (основное лезвие) -->
    <g id="fb_jawbone">
      <path d="M52,232 C76,222 126,224 186,216 C258,206 336,192 414,202 C446,206 472,222 486,238
               C458,262 414,274 358,280 C292,288 230,294 176,294 C132,294 82,296 50,284
               C38,276 38,242 52,232 Z"
            fill="url(#fb_bone)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

      <!-- Нижняя теневая грань кости -->
      <path d="M188,268 C264,266 346,258 424,244 C448,240 468,236 476,238
               C446,260 404,270 352,276 C288,284 228,288 184,288 Z"
            fill="url(#fb_boneDark)" opacity="0.65" />

      <!-- Светлый блик по верхнему обуху кости -->
      <path d="M192,226 C264,216 338,204 410,212 C432,214 448,220 460,228"
            fill="none" stroke="#fffbf0" stroke-width="5.5" opacity="0.65" stroke-linecap="round" />

      <!-- Костяные трещины и древние выбоины -->
      <path d="M236,214 L244,234 M314,204 L308,226 M382,204 L374,224"
            fill="none" stroke="#594631" stroke-width="4.5" stroke-linecap="round" />

      <!-- Пылающие алые жилы силы и Метка Каина у основания клинка -->
      <path d="M214,252 C264,248 326,244 388,236 C418,232 444,232 464,238"
            fill="none" stroke="#ff3838" stroke-width="6" stroke-linecap="round" opacity="0.92" />
      <path d="M214,252 C264,248 326,244 388,236"
            fill="none" stroke="#ffc2c2" stroke-width="2.5" stroke-linecap="round" opacity="0.85" />

      <!-- Пылающая Метка Каина на широкой части челюсти -->
      <g id="fb_mark_of_cain">
        <path d="M206,232 C226,232 238,242 238,254 C238,266 226,274 206,274"
              fill="none" stroke="#ff2626" stroke-width="7" stroke-linecap="round" />
        <path d="M254,232 C234,232 222,242 222,254 C222,266 234,274 254,274"
              fill="none" stroke="#ff2626" stroke-width="7" stroke-linecap="round" />
        <line x1="230" y1="226" x2="230" y2="280" stroke="#ff9999" stroke-width="4" stroke-linecap="round" />
      </g>
    </g>

    <!-- Кожаная багрово-тёмная обмотка рукояти -->
    <g id="fb_handle">
      <path d="M58,226 L184,218 L184,296 L58,290 C44,276 44,240 58,226 Z"
            fill="url(#fb_leather)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

      <!-- Перекрестная грубая перевязь -->
      <path d="M78,226 L104,290 M106,224 L132,292 M134,222 L160,294 M160,220 L182,276"
            fill="none" stroke="#170606" stroke-width="7" stroke-linecap="round" />
      <path d="M104,226 L78,290 M132,224 L106,292 M160,222 L134,294"
            fill="none" stroke="#8c362e" stroke-width="4" stroke-linecap="round" opacity="0.7" />

      <!-- Костяной утолщённый сустав-навершие -->
      <ellipse cx="48" cy="258" rx="16" ry="30"
               fill="url(#fb_bone)" stroke="#121519" stroke-width="10" />
    </g>
  </g>

  <!-- Алые искры жажды крови вокруг клинка -->
  <g id="fb_sparks">
    <circle cx="348" cy="134" r="6" fill="#ff9c9c" stroke="#cc1414" stroke-width="3" />
    <circle cx="404" cy="186" r="5" fill="#ff9c9c" stroke="#cc1414" stroke-width="3" />
    <circle cx="242" cy="176" r="5" fill="#ff9c9c" stroke="#cc1414" stroke-width="3" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Legendary_ArchangelBlade.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="ab_goldBlade" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#fff8d6" />
      <stop offset="32%" stop-color="#ffd966" />
      <stop offset="68%" stop-color="#c99324" />
      <stop offset="100%" stop-color="#704b0b" />
    </linearGradient>

    <linearGradient id="ab_goldHighlight" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#fff5bf" />
      <stop offset="50%" stop-color="#ffffff" />
      <stop offset="100%" stop-color="#ffd24d" />
    </linearGradient>

    <linearGradient id="ab_goldDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#b8841f" />
      <stop offset="100%" stop-color="#593a07" />
    </linearGradient>

    <linearGradient id="ab_ivoryGrip" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#ffffff" />
      <stop offset="50%" stop-color="#d8e3ed" />
      <stop offset="100%" stop-color="#8b9aa8" />
    </linearGradient>

    <radialGradient id="ab_graceCore" cx="45%" cy="45%" r="55%">
      <stop offset="0%" stop-color="#ffffff" />
      <stop offset="40%" stop-color="#b8f2ff" />
      <stop offset="80%" stop-color="#52b8ff" />
      <stop offset="100%" stop-color="#1c5ca8" />
    </radialGradient>

    <radialGradient id="ab_holyHalo" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#fff9cc" stop-opacity="0.78" />
      <stop offset="45%" stop-color="#ffd24d" stop-opacity="0.34" />
      <stop offset="78%" stop-color="#85d6ff" stop-opacity="0.14" />
      <stop offset="100%" stop-color="#85d6ff" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Сияющий небесный ореол благодати и святого огня -->
  <g id="ab_halo">
    <circle cx="294" cy="218" r="144" fill="url(#ab_holyHalo)" />
  </g>

  <!-- Клинок Архангела повернут по диагонали (рукоять слева-снизу, витое золотое лезвие справа-сверху) -->
  <g id="ab_blade_group" transform="rotate(-45 256 256)">
    <!-- Витое трёхгранное золотое лезвие -->
    <g id="ab_blade">
      <!-- Основной силуэт длинного стилета -->
      <path d="M172,226 L446,242 L494,256 L446,270 L172,286 Z"
            fill="url(#ab_goldBlade)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

      <!-- Нижняя теневая грань клинка -->
      <polygon points="176,256 484,256 444,268 176,282"
               fill="url(#ab_goldDark)" opacity="0.65" />

      <!-- Фирменные спиральные (витые) рёбра Клинка Архангела -->
      <path d="M188,232 C214,256 226,276 248,280"
            fill="none" stroke="#634109" stroke-width="7" stroke-linecap="round" />
      <path d="M236,232 C262,256 274,274 296,278"
            fill="none" stroke="#634109" stroke-width="7" stroke-linecap="round" />
      <path d="M286,235 C310,256 322,272 344,275"
            fill="none" stroke="#634109" stroke-width="6.5" stroke-linecap="round" />
      <path d="M336,238 C358,256 370,268 390,271"
            fill="none" stroke="#634109" stroke-width="6" stroke-linecap="round" />
      <path d="M386,241 C406,256 416,265 434,267"
            fill="none" stroke="#634109" stroke-width="5.5" stroke-linecap="round" />

      <!-- Ослепительные бело-золотые блики на витках спирали -->
      <path d="M196,232 C220,252 232,270 250,274"
            fill="none" stroke="url(#ab_goldHighlight)" stroke-width="4.5" stroke-linecap="round" />
      <path d="M244,232 C268,252 280,268 298,272"
            fill="none" stroke="url(#ab_goldHighlight)" stroke-width="4.5" stroke-linecap="round" />
      <path d="M294,235 C316,252 328,266 346,269"
            fill="none" stroke="url(#ab_goldHighlight)" stroke-width="4" stroke-linecap="round" />
      <path d="M344,238 C364,252 376,263 392,265"
            fill="none" stroke="url(#ab_goldHighlight)" stroke-width="4" stroke-linecap="round" />
      <path d="M394,242 C412,253 422,260 438,262"
            fill="none" stroke="url(#ab_goldHighlight)" stroke-width="3.5" stroke-linecap="round" />

      <!-- Осевой луч небесного света до самого острия -->
      <line x1="184" y1="256" x2="480" y2="256" stroke="#ffffff" stroke-width="3.5" opacity="0.82" stroke-linecap="round" />
    </g>

    <!-- Крылатая золотая гарда -->
    <g id="ab_guard">
      <path d="M150,210 C168,184 196,178 214,186 C198,202 194,224 196,256
               C194,288 198,310 214,326 C196,334 168,328 150,302 Z"
            fill="url(#ab_goldBlade)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
      <!-- Перьевые насечки на крыльях гарды -->
      <path d="M168,208 L194,196 M166,226 L190,218 M166,286 L190,294 M168,304 L194,316"
            fill="none" stroke="#6b460a" stroke-width="4.5" stroke-linecap="round" />
      <!-- Сияющий центр гарды -->
      <polygon points="174,240 190,256 174,272 158,256"
               fill="url(#ab_graceCore)" stroke="#121519" stroke-width="5" stroke-linejoin="round" />
    </g>

    <!-- Изящная рукоять из слоновой кости и золота -->
    <g id="ab_hilt">
      <rect x="64" y="238" width="94" height="36" rx="8"
            fill="url(#ab_ivoryGrip)" stroke="#121519" stroke-width="10" />
      <!-- Золотые витые кольца на рукояти -->
      <line x1="88" y1="241" x2="88" y2="271" stroke="#c99324" stroke-width="7" />
      <line x1="112" y1="241" x2="112" y2="271" stroke="#c99324" stroke-width="7" />
      <line x1="136" y1="241" x2="136" y2="271" stroke="#c99324" stroke-width="7" />

      <!-- Золотое навершие со сферой благодати -->
      <path d="M32,256 L56,230 L74,236 L74,276 L56,282 Z"
            fill="url(#ab_goldBlade)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
      <circle cx="56" cy="256" r="10" fill="url(#ab_graceCore)" stroke="#121519" stroke-width="4.5" />
    </g>
  </g>

  <!-- Искры святого света вокруг клинка -->
  <g id="ab_sparks">
    <polygon points="366,112 370,126 384,130 370,134 366,148 362,134 348,130 362,126"
             fill="#ffffff" stroke="#ffd24d" stroke-width="3" stroke-linejoin="round" />
    <polygon points="418,196 421,206 431,209 421,212 418,222 415,212 405,209 415,206"
             fill="#ffffff" stroke="#85d6ff" stroke-width="2.5" stroke-linejoin="round" />
    <circle cx="242" cy="148" r="5" fill="#fff9cc" stroke="#d9a426" stroke-width="3" />
  </g>
</svg>
```