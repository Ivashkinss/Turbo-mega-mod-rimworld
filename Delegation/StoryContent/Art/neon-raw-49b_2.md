`Art/Items/TMM_Mod_BoraxTank.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="mbt_steel" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#374048"/>
      <stop offset="26%" stop-color="#768591"/>
      <stop offset="48%" stop-color="#98a7b3"/>
      <stop offset="76%" stop-color="#515d69"/>
      <stop offset="100%" stop-color="#2a3138"/>
    </linearGradient>
    <linearGradient id="mbt_cap" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#2f373f"/>
      <stop offset="35%" stop-color="#6b7985"/>
      <stop offset="70%" stop-color="#48535e"/>
      <stop offset="100%" stop-color="#242a30"/>
    </linearGradient>
    <linearGradient id="mbt_clamp" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#262c33"/>
      <stop offset="32%" stop-color="#545f69"/>
      <stop offset="65%" stop-color="#3c454d"/>
      <stop offset="100%" stop-color="#1f2429"/>
    </linearGradient>
    <linearGradient id="mbt_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#d9bc75"/>
      <stop offset="45%" stop-color="#9e7f40"/>
      <stop offset="100%" stop-color="#5c461d"/>
    </linearGradient>
    <linearGradient id="mbt_powder" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#c4d0d6"/>
      <stop offset="36%" stop-color="#f2f7f9"/>
      <stop offset="70%" stop-color="#e1eaee"/>
      <stop offset="100%" stop-color="#aebcc4"/>
    </linearGradient>
    <linearGradient id="mbt_glass_void" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#232c33"/>
      <stop offset="100%" stop-color="#3f4d57"/>
    </linearGradient>
    <linearGradient id="mbt_hazard" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#8a6c22"/>
      <stop offset="42%" stop-color="#cfa63e"/>
      <stop offset="85%" stop-color="#997726"/>
      <stop offset="100%" stop-color="#695017"/>
    </linearGradient>
    <clipPath id="mbt_win_clip">
      <rect x="218" y="186" width="76" height="156" rx="22"/>
    </clipPath>
    <clipPath id="mbt_haz_clip">
      <rect x="174" y="362" width="164" height="24"/>
    </clipPath>
  </defs>

  <g transform="rotate(-14 256 256)">
    <!-- Side brass pressure release gauge assembly (left side) -->
    <path d="M 184,158 L 146,158" stroke="#121519" stroke-width="24" stroke-linecap="round"/>
    <path d="M 184,158 L 146,158" stroke="url(#mbt_brass)" stroke-width="10" stroke-linecap="round"/>
    <circle cx="138" cy="158" r="24" fill="url(#mbt_brass)" stroke="#121519" stroke-width="10"/>
    <circle cx="138" cy="158" r="14" fill="#dce4e8" stroke="#121519" stroke-width="4"/>
    <path d="M 138,158 L 145,150" stroke="#9e2f27" stroke-width="3.5" stroke-linecap="round"/>
    <circle cx="138" cy="158" r="3" fill="#121519"/>

    <!-- Top threaded weapon coupling nozzle & valve collar -->
    <rect x="234" y="74" width="44" height="36" rx="7"
          fill="url(#mbt_brass)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <line x1="238" y1="86" x2="274" y2="86" stroke="#121519" stroke-width="4.5"/>
    <line x1="238" y1="96" x2="274" y2="96" stroke="#121519" stroke-width="4.5"/>
    <!-- Hex valve base nut -->
    <rect x="216" y="102" width="80" height="26" rx="6"
          fill="url(#mbt_brass)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <line x1="238" y1="104" x2="238" y2="126" stroke="#121519" stroke-width="4.5"/>
    <line x1="274" y1="104" x2="274" y2="126" stroke="#121519" stroke-width="4.5"/>

    <!-- Bottom drain plug -->
    <rect x="236" y="408" width="40" height="20" rx="6"
          fill="url(#mbt_cap)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>

    <!-- Main pressurized steel cylindrical tank body -->
    <rect x="174" y="122" width="164" height="292" rx="64"
          fill="url(#mbt_steel)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

    <!-- Welded dome seam lines -->
    <path d="M 178,166 Q 256,176 334,166" fill="none" stroke="#252c33" stroke-width="4.5" opacity="0.8"/>
    <path d="M 178,360 Q 256,370 334,360" fill="none" stroke="#252c33" stroke-width="4.5" opacity="0.8"/>

    <!-- Subtle hazard band near bottom of tank -->
    <g clip-path="url(#mbt_haz_clip)">
      <rect x="172" y="360" width="168" height="28" fill="url(#mbt_hazard)"/>
      <g stroke="#1b2026" stroke-width="12">
        <line x1="184" y1="390" x2="202" y2="358"/>
        <line x1="214" y1="390" x2="232" y2="358"/>
        <line x1="244" y1="390" x2="262" y2="358"/>
        <line x1="274" y1="390" x2="292" y2="358"/>
        <line x1="304" y1="390" x2="322" y2="358"/>
      </g>
    </g>
    <path d="M 175,362 L 337,362 M 176,386 L 336,386" stroke="#121519" stroke-width="5"/>

    <!-- Metallic vertical specular sheen on tank left side -->
    <path d="M 196,174 L 196,350" stroke="#c3d2de" stroke-width="8" stroke-linecap="round" opacity="0.35"/>

    <!-- Recessed sight glass frame -->
    <rect x="212" y="180" width="88" height="168" rx="26"
          fill="#2c343d" stroke="#121519" stroke-width="8"/>

    <!-- Sight glass interior with white borax powder -->
    <g clip-path="url(#mbt_win_clip)">
      <!-- Dark headspace above powder -->
      <rect x="218" y="186" width="76" height="44" fill="url(#mbt_glass_void)"/>
      <!-- White granular borax powder fill -->
      <path d="M 216,214 Q 238,204 258,213 Q 278,221 296,212 L 296,344 L 216,344 Z"
            fill="url(#mbt_powder)"/>
      <!-- Powder shadow contour layers -->
      <path d="M 216,248 Q 246,238 272,249 Q 286,254 296,246 L 296,344 L 216,344 Z"
            fill="#d4dfe6" opacity="0.45"/>
      <path d="M 216,294 Q 250,286 296,298 L 296,344 L 216,344 Z"
            fill="#bccad4" opacity="0.4"/>
      <!-- Granular bright specks -->
      <circle cx="234" cy="228" r="4" fill="#ffffff"/>
      <circle cx="256" cy="238" r="3" fill="#ffffff"/>
      <circle cx="272" cy="224" r="3.5" fill="#ffffff"/>
      <circle cx="242" cy="264" r="4.5" fill="#ffffff"/>
      <circle cx="268" cy="278" r="3.5" fill="#ffffff"/>
      <circle cx="236" cy="306" r="3.5" fill="#ffffff"/>
      <circle cx="264" cy="318" r="4" fill="#ffffff"/>
      <circle cx="252" cy="252" r="2.5" fill="#9fb0ba"/>
      <circle cx="278" cy="296" r="2.8" fill="#9fb0ba"/>
      <circle cx="246" cy="326" r="2.5" fill="#9fb0ba"/>
      <!-- Measurement gauge ticks on right side of glass -->
      <g stroke="#3e4b54" stroke-width="3.5" stroke-linecap="round" opacity="0.7">
        <line x1="274" y1="220" x2="292" y2="220"/>
        <line x1="280" y1="244" x2="292" y2="244"/>
        <line x1="274" y1="268" x2="292" y2="268"/>
        <line x1="280" y1="292" x2="292" y2="292"/>
        <line x1="274" y1="316" x2="292" y2="316"/>
      </g>
      <!-- Glass reflection highlight -->
      <path d="M 230,198 L 230,330" stroke="#ffffff" stroke-width="6" stroke-linecap="round" opacity="0.72"/>
      <path d="M 241,200 L 241,244" stroke="#ffffff" stroke-width="2.5" stroke-linecap="round" opacity="0.5"/>
    </g>
    <!-- Crisp inner glass rim outline -->
    <rect x="218" y="186" width="76" height="156" rx="22"
          fill="none" stroke="#121519" stroke-width="6.5"/>

    <!-- Heavy upper mounting clamp band (хомут) with side bolt lugs -->
    <rect x="150" y="200" width="24" height="32" rx="6"
          fill="url(#mbt_clamp)" stroke="#121519" stroke-width="10"/>
    <rect x="338" y="200" width="24" height="32" rx="6"
          fill="url(#mbt_clamp)" stroke="#121519" stroke-width="10"/>
    <rect x="164" y="204" width="184" height="24" rx="6"
          fill="url(#mbt_clamp)" stroke="#121519" stroke-width="9"/>
    <!-- Upper clamp center protective bar cutout highlight -->
    <line x1="176" y1="211" x2="336" y2="211" stroke="#7d8a96" stroke-width="3" stroke-linecap="round" opacity="0.5"/>

    <!-- Heavy lower mounting clamp band (хомут) with side bolt lugs -->
    <rect x="150" y="294" width="24" height="32" rx="6"
          fill="url(#mbt_clamp)" stroke="#121519" stroke-width="10"/>
    <rect x="338" y="294" width="24" height="32" rx="6"
          fill="url(#mbt_clamp)" stroke="#121519" stroke-width="10"/>
    <rect x="164" y="298" width="184" height="24" rx="6"
          fill="url(#mbt_clamp)" stroke="#121519" stroke-width="9"/>
    <line x1="176" y1="305" x2="336" y2="305" stroke="#7d8a96" stroke-width="3" stroke-linecap="round" opacity="0.5"/>

    <!-- Clamp bolt heads -->
    <g fill="#9aa8b5" stroke="#121519" stroke-width="4">
      <circle cx="186" cy="216" r="6"/>
      <circle cx="326" cy="216" r="6"/>
      <circle cx="186" cy="310" r="6"/>
      <circle cx="326" cy="310" r="6"/>
    </g>

    <!-- Re-stroke main silhouette edge gently where needed for clean bold rim -->
    <rect x="174" y="122" width="164" height="292" rx="64"
          fill="none" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  </g>
</svg>
```

`Art/Items/TMM_Mod_RockSaltPack.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="rsp_box_front" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#b89d76"/>
      <stop offset="52%" stop-color="#967a56"/>
      <stop offset="100%" stop-color="#6e563a"/>
    </linearGradient>
    <linearGradient id="rsp_box_side" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7a6244"/>
      <stop offset="100%" stop-color="#4d3b27"/>
    </linearGradient>
    <linearGradient id="rsp_box_lid" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#a88c65"/>
      <stop offset="100%" stop-color="#6e573b"/>
    </linearGradient>
    <linearGradient id="rsp_box_in" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#211912"/>
      <stop offset="100%" stop-color="#423324"/>
    </linearGradient>
    <linearGradient id="rsp_shell_red" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#bd4037"/>
      <stop offset="40%" stop-color="#962a23"/>
      <stop offset="100%" stop-color="#5e1713"/>
    </linearGradient>
    <linearGradient id="rsp_shell_red_v" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#6e1b16"/>
      <stop offset="35%" stop-color="#bd4138"/>
      <stop offset="72%" stop-color="#8c251f"/>
      <stop offset="100%" stop-color="#571511"/>
    </linearGradient>
    <linearGradient id="rsp_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3c77d"/>
      <stop offset="45%" stop-color="#b59246"/>
      <stop offset="100%" stop-color="#6e5422"/>
    </linearGradient>
    <linearGradient id="rsp_brass_v" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#7a5e27"/>
      <stop offset="35%" stop-color="#e6ca81"/>
      <stop offset="75%" stop-color="#ad8a40"/>
      <stop offset="100%" stop-color="#634b1d"/>
    </linearGradient>
    <linearGradient id="rsp_salt_light" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="55%" stop-color="#e3ecef"/>
      <stop offset="100%" stop-color="#b8c7cf"/>
    </linearGradient>
    <linearGradient id="rsp_salt_shade" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#cad7de"/>
      <stop offset="100%" stop-color="#8b9da8"/>
    </linearGradient>
  </defs>

  <!-- Open rear top cardboard lid flap -->
  <path d="M 150,152 L 194,88 L 376,94 L 352,162 Z"
        fill="url(#rsp_box_lid)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>
  <!-- Cardboard crease fold shadow -->
  <line x1="164" y1="146" x2="344" y2="154" stroke="#3b2d1f" stroke-width="5" stroke-linecap="round"/>

  <!-- Small side dust flap sticking up -->
  <path d="M 350,160 L 406,136 L 418,176 L 354,196 Z"
        fill="url(#rsp_box_side)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>

  <!-- Dark open box interior cavity -->
  <polygon points="104,192 150,152 354,160 326,204"
           fill="url(#rsp_box_in)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>

  <!-- Row of red shotgun shells peeking out from inside the open box -->
  <g stroke="#121519" stroke-width="7" stroke-linejoin="round">
    <!-- Shell inside left -->
    <rect x="150" y="128" width="42" height="72" rx="8" fill="url(#rsp_shell_red_v)"/>
    <ellipse cx="171" cy="132" rx="19" ry="7" fill="url(#rsp_brass_v)"/>
    <circle cx="171" cy="132" r="5" fill="#9e7f3a" stroke-width="3"/>

    <!-- Shell inside center -->
    <rect x="200" y="122" width="44" height="80" rx="8" fill="url(#rsp_shell_red_v)"/>
    <ellipse cx="222" cy="126" rx="20" ry="7" fill="url(#rsp_brass_v)"/>
    <circle cx="222" cy="126" r="5" fill="#9e7f3a" stroke-width="3"/>

    <!-- Shell inside right (tilted slightly) -->
    <g transform="rotate(14 282 162)">
      <rect x="260" y="124" width="44" height="82" rx="8" fill="url(#rsp_shell_red_v)"/>
      <ellipse cx="282" cy="128" rx="20" ry="7" fill="url(#rsp_brass_v)"/>
      <circle cx="282" cy="128" r="5" fill="#9e7f3a" stroke-width="3"/>
    </g>
  </g>

  <!-- Cardboard box right side panel -->
  <polygon points="326,204 388,164 384,324 320,372"
           fill="url(#rsp_box_side)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

  <!-- Cardboard box main front panel -->
  <polygon points="104,192 326,204 320,372 108,354"
           fill="url(#rsp_box_front)" stroke="#121519" stroke-width="12" stroke-linejoin="round"/>

  <!-- Worn cardboard wear marks & tape strip -->
  <path d="M 116,206 L 314,216" stroke="#d4bd98" stroke-width="4" stroke-linecap="round" opacity="0.45"/>
  <path d="M 118,342 L 308,358" stroke="#473623" stroke-width="5" stroke-linecap="round" opacity="0.45"/>

  <!-- Decorative printed geometric emblem on box front (no text) -->
  <g transform="translate(216 280) rotate(3)" opacity="0.82">
    <rect x="-74" y="-44" width="148" height="88" rx="8"
          fill="#c4ab82" fill-opacity="0.35" stroke="#4d3925" stroke-width="5"/>
    <rect x="-64" y="-34" width="128" height="68" rx="4"
          fill="none" stroke="#4d3925" stroke-width="2.5" stroke-dasharray="10 6"/>
    <!-- Salt crystal / shotgun diamond icon -->
    <polygon points="0,-24 22,0 0,24 -22,0" fill="#e6eef0" stroke="#4d3925" stroke-width="4.5" stroke-linejoin="round"/>
    <polygon points="0,-24 10,-4 0,4 -10,-4" fill="#ffffff"/>
    <line x1="-50" y1="0" x2="-32" y2="0" stroke="#7d2520" stroke-width="6" stroke-linecap="round"/>
    <line x1="32" y1="0" x2="50" y2="0" stroke="#7d2520" stroke-width="6" stroke-linecap="round"/>
  </g>

  <!-- Spilled red shotgun shell #1 (front-left, rolling diagonally forward) -->
  <g transform="translate(152 384) rotate(22)">
    <!-- Red hull -->
    <rect x="-66" y="-23" width="104" height="46" rx="10"
          fill="url(#rsp_shell_red)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <!-- Hull longitudinal ribbing highlight -->
    <line x1="-52" y1="-10" x2="26" y2="-10" stroke="#df6b62" stroke-width="4" stroke-linecap="round" opacity="0.65"/>
    <line x1="-52" y1="4" x2="26" y2="4" stroke="#4f120f" stroke-width="3.5" stroke-linecap="round" opacity="0.5"/>
    <!-- Brass head -->
    <rect x="26" y="-24" width="38" height="48" rx="6"
          fill="url(#rsp_brass)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <!-- Brass rim flange -->
    <rect x="58" y="-27" width="12" height="54" rx="5"
          fill="url(#rsp_brass)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
    <line x1="36" y1="-12" x2="54" y2="-12" stroke="#fff2c2" stroke-width="3.5" stroke-linecap="round" opacity="0.7"/>
  </g>

  <!-- Spilled red shotgun shell #2 (front-right, angled toward camera showing salt-packed mouth) -->
  <g transform="translate(346 364) rotate(-26)">
    <!-- Brass base at back -->
    <rect x="24" y="-24" width="40" height="48" rx="6"
          fill="url(#rsp_brass)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <rect x="58" y="-27" width="12" height="54" rx="5"
          fill="url(#rsp_brass)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
    <!-- Red hull -->
    <rect x="-64" y="-23" width="96" height="46" rx="10"
          fill="url(#rsp_shell_red)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <!-- Open crimped mouth packed with white rock salt -->
    <ellipse cx="-60" cy="0" rx="11" ry="20" fill="#471310" stroke="#121519" stroke-width="7"/>
    <ellipse cx="-61" cy="0" rx="7" ry="13" fill="#e6eff2"/>
    <line x1="-42" y1="-11" x2="22" y2="-11" stroke="#e06e65" stroke-width="4" stroke-linecap="round" opacity="0.65"/>
  </g>

  <!-- Coarse faceted rock salt crystals (крупная соль) scattered in foreground -->
  <g stroke="#121519" stroke-width="7.5" stroke-linejoin="round">
    <!-- Large crystal chunk center-front -->
    <polygon points="236,374 264,364 282,386 266,412 238,406 226,388" fill="url(#rsp_salt_light)"/>
    <polygon points="264,364 282,386 266,412 254,388" fill="url(#rsp_salt_shade)" stroke-width="3.5"/>
    <polygon points="236,374 264,364 254,388 226,388" fill="#ffffff" stroke-width="3"/>

    <!-- Medium crystal bottom-center -->
    <polygon points="282,404 306,396 318,414 300,428 280,420" fill="url(#rsp_salt_light)"/>
    <polygon points="306,396 318,414 300,428 296,410" fill="url(#rsp_salt_shade)" stroke-width="3"/>

    <!-- Crystal near left shell -->
    <polygon points="104,394 128,388 136,408 118,422 98,412" fill="url(#rsp_salt_light)"/>
    <polygon points="128,388 136,408 118,422 116,404" fill="url(#rsp_salt_shade)" stroke-width="3"/>

    <!-- Crystal tucked between shells -->
    <polygon points="196,404 218,398 228,416 212,428 192,420" fill="url(#rsp_salt_light)"/>
    <polygon points="218,398 228,416 212,428 210,412" fill="url(#rsp_salt_shade)" stroke-width="3"/>

    <!-- Crystal right side near shell mouth -->
    <polygon points="384,378 408,370 420,390 402,406 382,396" fill="url(#rsp_salt_light)"/>
    <polygon points="408,370 420,390 402,406 398,388" fill="url(#rsp_salt_shade)" stroke-width="3"/>

    <!-- Small crystal shards -->
    <polygon points="262,344 278,338 286,352 270,358" fill="#f0f7fa" stroke-width="6.5"/>
    <polygon points="332,408 348,402 356,416 340,424" fill="#e6eff2" stroke-width="6.5"/>
    <polygon points="164,418 178,412 184,424 170,430" fill="#f0f7fa" stroke-width="6.5"/>
  </g>
</svg>
```

`Art/Items/TMM_Mod_SilverEngraving.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="seg_leather_main" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#7d583e"/>
      <stop offset="48%" stop-color="#5c3d29"/>
      <stop offset="100%" stop-color="#362216"/>
    </linearGradient>
    <linearGradient id="seg_leather_roll" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#3b2518"/>
      <stop offset="35%" stop-color="#876045"/>
      <stop offset="68%" stop-color="#5e3f2b"/>
      <stop offset="100%" stop-color="#2e1c12"/>
    </linearGradient>
    <linearGradient id="seg_leather_strap" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#4d3322"/>
      <stop offset="50%" stop-color="#332014"/>
      <stop offset="100%" stop-color="#21140c"/>
    </linearGradient>
    <linearGradient id="seg_wood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#b08156"/>
      <stop offset="50%" stop-color="#7d5333"/>
      <stop offset="100%" stop-color="#472d1a"/>
    </linearGradient>
    <linearGradient id="seg_bone" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#dfd5be"/>
      <stop offset="55%" stop-color="#b5a68c"/>
      <stop offset="100%" stop-color="#7a6d59"/>
    </linearGradient>
    <linearGradient id="seg_brass" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#dec178"/>
      <stop offset="50%" stop-color="#a3823d"/>
      <stop offset="100%" stop-color="#5e471b"/>
    </linearGradient>
    <linearGradient id="seg_steel" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#e3eef5"/>
      <stop offset="45%" stop-color="#96a7b5"/>
      <stop offset="100%" stop-color="#515f6b"/>
    </linearGradient>
    <linearGradient id="seg_silver_plate" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#e8f1f7"/>
      <stop offset="28%" stop-color="#c3d3de"/>
      <stop offset="52%" stop-color="#f2f8fc"/>
      <stop offset="80%" stop-color="#94a5b3"/>
      <stop offset="100%" stop-color="#667685"/>
    </linearGradient>
    <linearGradient id="seg_silver_inner" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#cfdce6"/>
      <stop offset="45%" stop-color="#e8f2f7"/>
      <stop offset="100%" stop-color="#8799a8"/>
    </linearGradient>
  </defs>

  <!-- Leather tool roll (partially unfurled, positioned left-to-center) -->
  <g transform="rotate(-12 216 250)">
    <!-- Unfurled leather flap behind tools -->
    <path d="M 118,128 L 308,132 C 322,132 332,144 330,158 L 318,344 C 316,358 304,368 290,368 L 116,362 Z"
          fill="url(#seg_leather_main)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

    <!-- Engraving chisels (резцы) tucked inside leather pockets -->
    <!-- Chisel 1: Wood handle & angled graver -->
    <g transform="translate(204 166) rotate(-4)">
      <path d="M -5,-78 L 5,-78 L 8,-14 L -8,-14 Z" fill="url(#seg_steel)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>
      <polygon points="-5,-78 5,-78 0,-94" fill="#eef6fa" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>
      <rect x="-11" y="-18" width="22" height="16" rx="4" fill="url(#seg_brass)" stroke="#121519" stroke-width="7"/>
      <path d="M -14,-4 C -18,18 -16,52 0,58 C 16,52 18,18 14,-4 Z" fill="url(#seg_wood)" stroke="#121519" stroke-width="9"/>
    </g>

    <!-- Chisel 2: Bone handle & flat chisel tip -->
    <g transform="translate(262 174) rotate(5)">
      <path d="M -7,-74 L 7,-74 L 7,-12 L -7,-12 Z" fill="url(#seg_steel)" stroke="#121519" stroke-width="8" stroke-linejoin="round"/>
      <polygon points="-9,-88 9,-84 7,-72 -7,-72" fill="#f0f7fc" stroke="#121519" stroke-width="7" stroke-linejoin="round"/>
      <rect x="-11" y="-16" width="22" height="16" rx="4" fill="url(#seg_brass)" stroke="#121519" stroke-width="7"/>
      <path d="M -14,-2 C -17,18 -15,50 0,56 C 15,50 17,18 14,-2 Z" fill="url(#seg_bone)" stroke="#121519" stroke-width="9"/>
    </g>

    <!-- Lower leather tool pocket overlay -->
    <path d="M 142,202 L 324,206 L 316,348 C 314,360 304,368 290,368 L 142,362 Z"
          fill="url(#seg_leather_main)" stroke="#121519" stroke-width="11" stroke-linejoin="round"/>
    <!-- Pocket divider stitching -->
    <line x1="230" y1="208" x2="226" y2="360" stroke="#24160d" stroke-width="4.5" stroke-dasharray="9 7" stroke-linecap="round"/>
    <line x1="154" y1="216" x2="314" y2="220" stroke="#9e7657" stroke-width="3" stroke-dasharray="8 7" stroke-linecap="round" opacity="0.65"/>

    <!-- Rolled leather cylinder edge on the left -->
    <rect x="86" y="122" width="76" height="248" rx="34"
          fill="url(#seg_leather_roll)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>
    <!-- Spiral roll end detail at top -->
    <ellipse cx="124" cy="140" rx="26" ry="12" fill="#2e1c12" stroke="#121519" stroke-width="6"/>
    <path d="M 110,140 C 110,134 136,134 136,141 C 136,146 118,146 120,140"
          fill="none" stroke="#8c6448" stroke-width="4" stroke-linecap="round"/>

    <!-- Leather cinch strap & brass buckle wrapping the roll -->
    <rect x="80" y="258" width="124" height="28" rx="6"
          fill="url(#seg_leather_strap)" stroke="#121519" stroke-width="9" stroke-linejoin="round"/>
    <rect x="136" y="252" width="32" height="40" rx="6"
          fill="none" stroke="url(#seg_brass)" stroke-width="10"/>
    <rect x="136" y="252" width="32" height="40" rx="6"
          fill="none" stroke="#121519" stroke-width="4"/>
    <line x1="146" y1="272" x2="164" y2="272" stroke="#d9bc75" stroke-width="4.5" stroke-linecap="round"/>
  </g>

  <!-- Ornate engraved silver plate (серебряная пластина с узором) in right foreground -->
  <g transform="translate(316 292) rotate(8)">
    <!-- Beveled octagonal/notched silver plaque -->
    <path d="M -88,-112 L 88,-112 L 106,-94 L 106,94 L 88,112 L -88,112 L -106,94 L -106,-94 Z"
          fill="url(#seg_silver_plate)" stroke="#121519" stroke-width="13" stroke-linejoin="round"/>

    <!-- Inner recessed silver field -->
    <path d="M -76,-96 L 76,-96 L 90,-82 L 90,82 L 76,96 L -76,96 L -90,82 L -90,-82 Z"
          fill="url(#seg_silver_inner)" stroke="#4c5c6b" stroke-width="4.5" stroke-linejoin="round"/>

    <!-- Diagonal bright metallic sheen streak across plate -->
    <polygon points="-86,-42 18,-96 62,-96 -86,2" fill="#ffffff" opacity="0.42"/>
    <polygon points="-24,96 90,14 90,46 18,96" fill="#ffffff" opacity="0.32"/>

    <!-- Corner decorative rivets -->
    <g fill="#677887" stroke="#2c3640" stroke-width="3">
      <circle cx="-72" cy="-76" r="5"/>
      <circle cx="72" cy="-76" r="5"/>
      <circle cx="-72" cy="76" r="5"/>
      <circle cx="72" cy="76" r="5"/>
    </g>

    <!-- Engraved supernatural filigree & protective arcane ward pattern (no text) -->
    <g fill="none" stroke="#2e3945" stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round">
      <!-- Outer protective diamond & circle -->
      <circle cx="0" cy="0" r="54"/>
      <circle cx="0" cy="0" r="44" stroke-width="2.5" stroke-dasharray="6 5"/>
      <polygon points="0,-64 46,0 0,64 -46,0" stroke-width="4"/>
      <!-- Central four-pointed supernatural star -->
      <path d="M 0,-34 C 4,-12 12,-4 34,0 C 12,4 4,12 0,34 C -4,12 -12,4 -34,0 C -12,-4 -4,-12 0,-34 Z"
            fill="#495969" fill-opacity="0.32" stroke-width="4"/>
      <circle cx="0" cy="0" r="8" fill="#f0f7fc" stroke-width="3.5"/>
      <!-- Top and bottom baroque scrollwork flourishes -->
      <path d="M -52,-62 C -34,-82 -14,-78 0,-64 C 14,-78 34,-82 52,-62"/>
      <path d="M -52,62 C -34,82 -14,78 0,64 C 14,78 34,82 52,62"/>
      <!-- Side curling vine filigree -->
      <path d="M -64,-28 C -78,-14 -78,14 -64,28"/>
      <path d="M 64,-28 C 78,-14 78,14 64,28"/>
      <circle cx="-54" cy="-54" r="3" fill="#2e3945"/>
      <circle cx="54" cy="-54" r="3" fill="#2e3945"/>
      <circle cx="-54" cy="54" r="3" fill="#2e3945"/>
      <circle cx="54" cy="54" r="3" fill="#2e3945"/>
    </g>

    <!-- Subtle bright etched highlight along the engraved lines -->
    <path d="M -1,-33 C 3,-11 11,-3 33,-1" fill="none" stroke="#ffffff" stroke-width="1.8" opacity="0.75"/>
  </g>

  <!-- Loose detail engraving burin resting diagonally across lower foreground -->
  <g transform="translate(262 384) rotate(56)">
    <!-- Fine steel shaft & beveled engraving tip -->
    <path d="M -4,-98 L 4,-98 L 7,-22 L -7,-22 Z"
          fill="url(#seg_steel)" stroke="#121519" stroke-width="8.5" stroke-linejoin="round"/>
    <polygon points="-4,-98 4,-98 0,-116"
             fill="#ffffff" stroke="#121519" stroke-width="7.5" stroke-linejoin="round"/>
    <!-- Brass ferrule -->
    <rect x="-10" y="-26" width="20" height="16" rx="4"
          fill="url(#seg_brass)" stroke="#121519" stroke-width="7.5"/>
    <!-- Rounded hardwood graver palm handle -->
    <path d="M -14,-12 C -24,10 -22,50 0,58 C 22,50 24,10 14,-12 Z"
          fill="url(#seg_wood)" stroke="#121519" stroke-width="10" stroke-linejoin="round"/>
    <path d="M -8,0 C -13,18 -11,38 -3,46" fill="none" stroke="#d4a67d" stroke-width="3.5" stroke-linecap="round" opacity="0.5"/>
  </g>

  <!-- Tiny curled silver shaving near the active graver tip -->
  <path d="M 358,314 C 368,306 382,314 376,326 C 372,334 360,332 363,324"
        fill="none" stroke="#121519" stroke-width="9" stroke-linecap="round"/>
  <path d="M 358,314 C 368,306 382,314 376,326 C 372,334 360,332 363,324"
        fill="none" stroke="#e8f3fa" stroke-width="3.8" stroke-linecap="round"/>
</svg>
```