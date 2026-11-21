# 49_1 — Оружие героев, постройка, модули, рюкзаки и снаряды (SVG 1–4)

## Полный список файлов (29 новых SVG)

### A. Оружие героев (`Art/Weapons`)
1. `Art/Weapons/TMM_Sig_dean_sawed_off_shotgun.svg` — обрез Дина Винчестера
2. `Art/Weapons/TMM_Sig_kellogg_revolver.svg` — револьвер Келлогга (.44)
3. `Art/Weapons/TMM_Sig_ruby_demon_knife.svg` — нож Руби
4. `Art/Weapons/TMM_Sig_swan_anchor_club.svg` — дубина Свона (корабельный якорь)
5. `Art/Weapons/TMM_Sig_castiel_angel_blade.svg` — ангельский клинок Кастиэля
6. `Art/Weapons/TMM_Gun_BoraxTurret.svg` — ствол турели буры (вид сверху, на север)

### B. Постройка (`Art/Buildings`)
7. `Art/Buildings/TMM_Turret_Borax.svg` — основание турели буры (вид сверху)

### C. Модули, боеприпасы и снаряжение (`Art/Items`)
8. `Art/Items/TMM_Mod_BoraxTank.svg` — бак буры для оружия
9. `Art/Items/TMM_Mod_RockSaltPack.svg` — пачка патронов с каменной солью
10. `Art/Items/TMM_Mod_SilverEngraving.svg` — набор серебряной гравировки
11. `Art/Items/TMM_Mod_RailroadBallisticWeave.svg` — рулон баллистической ткани «Подземки»
12. `Art/Items/TMM_Ammo_ColtSilverBullet.svg` — серебряные пули для Кольта
13. `Art/Items/TMM_Backpack_Canvas.svg` — холщовый рюкзак
14. `Art/Items/TMM_Backpack_Military.svg` — военный рюкзак
15. `Art/Items/TMM_Backpack_Power.svg` — силовой рюкзак-ранец
16. `Art/Items/TMM_Legendary_GoldenPipBoy.svg` — золотой Пип-бой

### D. Снаряды (`Art/Projectiles`)
17. `Art/Projectiles/TMM_Bullet_Experiment18A.svg` — зелёный плазменный сгусток
18. `Art/Projectiles/TMM_Bullet_FinalJudgment.svg` — красный лазерный луч-штрих
19. `Art/Projectiles/TMM_Bullet_TheColt.svg` — серебряная пуля со светящимся следом
20. `Art/Projectiles/TMM_Projectile_LorenzoWave.svg` — золотистое кольцо-волна
21. `Art/Projectiles/TMM_Bullet_TeslaCannon.svg` — голубая электрическая дуга-шар
22. `Art/Projectiles/TMM_Bullet_OverseersGuardian.svg` — две параллельные латунные пули
23. `Art/Projectiles/TMM_Bullet_RailwayRifle.svg` — раскалённый железнодорожный костыль
24. `Art/Projectiles/TMM_Projectile_BigBoy.svg` — мини-ядерный заряд
25. `Art/Projectiles/TMM_Bullet_TheEqualizer.svg` — бело-синяя «квантовая» пуля
26. `Art/Projectiles/TMM_Bullet_Borax.svg` — белое облачко порошка буры
27. `Art/Projectiles/TMM_Bullet_BoraxTurret.svg` — крупное облако порошка буры
28. `Art/Projectiles/TMM_Bullet_DeanShotgun.svg` — сноп дроби
29. `Art/Projectiles/TMM_Bullet_KelloggRevolver.svg` — тяжёлая пуля .44

---

`Art/Weapons/TMM_Sig_dean_sawed_off_shotgun.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="ds_wood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#96613d" />
      <stop offset="46%" stop-color="#663c22" />
      <stop offset="100%" stop-color="#361d0e" />
    </linearGradient>

    <linearGradient id="ds_woodDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#5e361e" />
      <stop offset="100%" stop-color="#291409" />
    </linearGradient>

    <linearGradient id="ds_steelTop" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8b96a1" />
      <stop offset="42%" stop-color="#525b63" />
      <stop offset="100%" stop-color="#282d33" />
    </linearGradient>

    <linearGradient id="ds_steelBot" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#66707a" />
      <stop offset="50%" stop-color="#3b424a" />
      <stop offset="100%" stop-color="#1b1f24" />
    </linearGradient>

    <linearGradient id="ds_receiver" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7c858f" />
      <stop offset="48%" stop-color="#4b535c" />
      <stop offset="100%" stop-color="#24292e" />
    </linearGradient>

    <linearGradient id="ds_tape" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#3d434a" />
      <stop offset="48%" stop-color="#212529" />
      <stop offset="100%" stop-color="#0f1114" />
    </linearGradient>

    <linearGradient id="ds_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#edd080" />
      <stop offset="55%" stop-color="#b58932" />
      <stop offset="100%" stop-color="#694b15" />
    </linearGradient>
  </defs>

  <!-- Открытые курки и спусковая скоба с двойным спуском -->
  <g id="ds_triggers">
    <!-- Пара классических внешних курков -->
    <path d="M168,200 C160,174 178,162 198,170 C188,180 198,188 210,198 Z"
          fill="url(#ds_steelTop)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />

    <!-- Спусковая скоба -->
    <path d="M148,268 L226,268 C220,308 188,320 156,306 L142,286 Z"
          fill="none" stroke="#121519" stroke-width="14" stroke-linejoin="round" />
    <path d="M148,268 L226,268 C220,308 188,320 156,306 L142,286 Z"
          fill="none" stroke="url(#ds_steelTop)" stroke-width="5" stroke-linejoin="round" />

    <!-- Два спусковых крючка двустволки -->
    <path d="M178,270 C178,286 171,294 164,298" fill="none" stroke="#121519" stroke-width="7" stroke-linecap="round" />
    <path d="M198,270 C198,286 191,294 184,298" fill="none" stroke="#121519" stroke-width="7" stroke-linecap="round" />
  </g>

  <!-- Обрезанная деревянная рукоять-приклад с потёртостями -->
  <g id="ds_stock">
    <path d="M162,216 L186,256 C144,274 114,300 84,336 C64,344 40,330 34,308
             C62,268 104,232 162,216 Z"
          fill="url(#ds_wood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Нижняя тень и спиленный торец -->
    <path d="M40,304 C66,272 104,242 152,226" fill="none" stroke="#ba8257" stroke-width="5" opacity="0.55" stroke-linecap="round" />
    <path d="M34,308 C42,330 64,344 84,336 L94,322 C74,328 54,316 44,298 Z"
          fill="url(#ds_woodDark)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />

    <!-- Потёртости и царапины на дереве -->
    <path d="M76,294 L98,284 M102,268 L128,256 M94,306 L112,298"
          fill="none" stroke="#361c0e" stroke-width="4" opacity="0.7" stroke-linecap="round" />
  </g>

  <!-- Укороченные спаренные стволы двустволки -->
  <g id="ds_barrels">
    <!-- Второй ствол (слегка виден сверху для чтения двустволки) -->
    <path d="M242,192 L462,192 L462,214 L242,216 Z"
          fill="url(#ds_steelTop)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Основной ближний ствол -->
    <path d="M242,208 L474,208 L474,244 L242,248 Z"
          fill="url(#ds_steelBot)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Верхний блик на стволе -->
    <line x1="254" y1="216" x2="462" y2="216" stroke="#b0bcc7" stroke-width="4.5" opacity="0.6" stroke-linecap="round" />

    <!-- Двойной дульный срез -->
    <ellipse cx="472" cy="226" rx="7" ry="15" fill="#121519" />
    <ellipse cx="461" cy="202" rx="5" ry="9" fill="#121519" />

    <!-- Латунная мушка-бусина на срезе -->
    <circle cx="458" cy="188" r="6.5" fill="url(#ds_brass)" stroke="#121519" stroke-width="4" />

    <!-- Металлические царапины на стволе -->
    <path d="M396,222 L420,228 M430,234 L448,232" stroke="#aab6c2" stroke-width="3" opacity="0.55" stroke-linecap="round" />
  </g>

  <!-- Деревянное цевьё, обмотанное чёрной изолентой -->
  <g id="ds_forend">
    <!-- Деревянная основа цевья -->
    <path d="M244,244 L388,242 L378,274 L244,274 Z"
          fill="url(#ds_wood)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Потёртый край дерева впереди -->
    <path d="M360,250 L376,250 L370,266 L356,266 Z" fill="#4a2916" opacity="0.65" />

    <!-- Плотная намотка чёрной изоленты вокруг цевья и стволов -->
    <g id="ds_electrical_tape">
      <path d="M276,204 L356,204 L352,278 L272,278 Z"
            fill="url(#ds_tape)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
      <!-- Диагональные витки изоленты -->
      <path d="M286,206 L298,276 M306,206 L318,276 M326,206 L338,276"
            fill="none" stroke="#101214" stroke-width="6" stroke-linecap="round" />
      <path d="M292,210 L302,270 M312,210 L322,270 M332,210 L342,270"
            fill="none" stroke="#57606b" stroke-width="3.5" opacity="0.6" stroke-linecap="round" />
      <!-- Слегка отклеившийся кончик изоленты снизу -->
      <path d="M336,276 L354,284 L356,272" fill="url(#ds_tape)" stroke="#121519" stroke-width="5" stroke-linejoin="round" />
    </g>
  </g>

  <!-- Стальная колодка переломного механизма -->
  <g id="ds_receiver">
    <path d="M156,208 L252,196 L252,274 L164,274 L148,252 Z"
          fill="url(#ds_receiver)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхний рычаг отпирания стволов -->
    <path d="M150,206 L176,196 L184,208 L158,218 Z"
          fill="url(#ds_steelTop)" stroke="#121519" stroke-width="7" stroke-linejoin="round" />

    <!-- Латунный шарнирный штифт перелома и болты -->
    <circle cx="234" cy="252" r="9" fill="url(#ds_brass)" stroke="#121519" stroke-width="4.5" />
    <circle cx="192" cy="236" r="5" fill="#23282e" stroke="#121519" stroke-width="3.5" />
    <circle cx="216" cy="222" r="4" fill="#23282e" stroke="#121519" stroke-width="3" />
    <!-- Гравировочный завиток на колодке -->
    <path d="M174,254 C188,244 202,258 216,248" fill="none" stroke="#9aa5b0" stroke-width="3.5" opacity="0.6" stroke-linecap="round" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Sig_kellogg_revolver.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="kr_bluedSteel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#697c94" />
      <stop offset="35%" stop-color="#3c4a5c" />
      <stop offset="78%" stop-color="#222a36" />
      <stop offset="100%" stop-color="#141921" />
    </linearGradient>

    <linearGradient id="kr_bluedLight" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#96abc4" />
      <stop offset="60%" stop-color="#5d728c" />
      <stop offset="100%" stop-color="#3c4b5e" />
    </linearGradient>

    <linearGradient id="kr_bluedDark" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#364252" />
      <stop offset="100%" stop-color="#141921" />
    </linearGradient>

    <linearGradient id="kr_darkWood" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#5e3a28" />
      <stop offset="48%" stop-color="#382015" />
      <stop offset="100%" stop-color="#1c0f09" />
    </linearGradient>

    <linearGradient id="kr_chrome" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#e3eef7" />
      <stop offset="50%" stop-color="#9fb1c2" />
      <stop offset="100%" stop-color="#566575" />
    </linearGradient>

    <radialGradient id="kr_cyberGlow" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#e3fbff" />
      <stop offset="45%" stop-color="#59d4ff" />
      <stop offset="100%" stop-color="#135c8a" />
    </radialGradient>
  </defs>

  <!-- Курок и массивная спусковая скоба .44 Magnum -->
  <g id="kr_mechanism">
    <!-- Спортивный курок с насечкой -->
    <path d="M124,186 C112,158 130,144 152,154 C144,166 154,176 166,186 Z"
          fill="url(#kr_chrome)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Спусковая скоба -->
    <path d="M158,266 L236,266 L226,314 L156,314 Z"
          fill="none" stroke="#121519" stroke-width="15" stroke-linejoin="round" />
    <path d="M158,266 L236,266 L226,314 L156,314 Z"
          fill="none" stroke="url(#kr_bluedSteel)" stroke-width="6" stroke-linejoin="round" />

    <!-- Хромированный облегчённый спусковой крючок -->
    <path d="M196,268 C196,288 188,298 180,302" fill="none" stroke="#121519" stroke-width="11" stroke-linecap="round" />
    <path d="M196,268 C196,288 188,298 180,302" fill="none" stroke="url(#kr_chrome)" stroke-width="4.5" stroke-linecap="round" />
  </g>

  <!-- Рукоять из тёмного дерева с кибернетическими насечками -->
  <g id="kr_grip">
    <path d="M126,232 L176,246 C160,298 140,342 114,382 C88,392 50,382 38,364
             C66,318 88,274 102,236 Z"
          fill="url(#kr_darkWood)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Кибернетическая металлическая накладка-гребень по спинке рукояти -->
    <path d="M102,236 C88,276 66,316 38,364 L54,370 C82,324 102,282 116,240 Z"
          fill="url(#kr_bluedSteel)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />

    <!-- Стальная пятка с кибер-индикатором -->
    <path d="M38,364 C50,382 88,392 114,382 L122,366 C94,374 66,368 48,350 Z"
          fill="url(#kr_bluedDark)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />

    <!-- Глубокие кибернетические насечки-пазы на тёмном дереве -->
    <g id="kr_grip_notches">
      <line x1="94" y1="274" x2="146" y2="286" stroke="#121519" stroke-width="7" stroke-linecap="round" />
      <line x1="86" y1="296" x2="138" y2="308" stroke="#121519" stroke-width="7" stroke-linecap="round" />
      <line x1="78" y1="318" x2="130" y2="330" stroke="#121519" stroke-width="7" stroke-linecap="round" />
      <line x1="70" y1="340" x2="120" y2="352" stroke="#121519" stroke-width="7" stroke-linecap="round" />
      <!-- Тонкий стальной отблеск по краям насечек -->
      <line x1="96" y1="270" x2="144" y2="281" stroke="#7d95b0" stroke-width="2.5" opacity="0.65" stroke-linecap="round" />
      <line x1="88" y1="292" x2="136" y2="303" stroke="#7d95b0" stroke-width="2.5" opacity="0.65" stroke-linecap="round" />
      <line x1="80" y1="314" x2="128" y2="325" stroke="#7d95b0" stroke-width="2.5" opacity="0.65" stroke-linecap="round" />
      <!-- Медальон кибер-интерфейса Института в рукояти -->
      <circle cx="116" cy="260" r="8" fill="url(#kr_chrome)" stroke="#121519" stroke-width="4" />
      <circle cx="116" cy="260" r="3.5" fill="url(#kr_cyberGlow)" />
    </g>
  </g>

  <!-- Тяжёлый воронёный ствол .44 Magnum с вентилируемой планкой -->
  <g id="kr_barrel">
    <!-- Высокая прицельная мушка с контрастной вставкой -->
    <polygon points="444,178 472,178 472,158 456,160"
             fill="url(#kr_bluedDark)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
    <line x1="464" y1="164" x2="464" y2="174" stroke="#ff5c4d" stroke-width="5" stroke-linecap="round" />

    <!-- Основной массивный ствол с полным подствольным приливом (full underlug) -->
    <path d="M262,178 L478,178 L478,244 L262,248 Z"
          fill="url(#kr_bluedSteel)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Светлая верхняя прицельная планка -->
    <rect x="266" y="182" width="206" height="12" fill="url(#kr_bluedLight)" />

    <!-- Вентиляционные окна вдоль планки ствола -->
    <rect x="292" y="190" width="26" height="10" rx="3" fill="#121519" />
    <rect x="332" y="190" width="26" height="10" rx="3" fill="#121519" />
    <rect x="372" y="190" width="26" height="10" rx="3" fill="#121519" />
    <rect x="412" y="190" width="26" height="10" rx="3" fill="#121519" />

    <!-- Глубокий подствольный прилив и кибернетические насечки компенсатора -->
    <rect x="266" y="226" width="206" height="16" fill="url(#kr_bluedDark)" />
    <!-- Хромированный шомпол-экстрактор внутри подствольного прилива -->
    <rect x="278" y="230" width="118" height="8" rx="3" fill="url(#kr_chrome)" stroke="#121519" stroke-width="3" />

    <!-- Кибернетические диагональные насечки на дульной части ствола -->
    <path d="M418,208 L428,224 M434,208 L444,224 M450,208 L460,224"
          fill="none" stroke="#121519" stroke-width="6" stroke-linecap="round" />
    <path d="M422,208 L432,224 M438,208 L448,224 M454,208 L464,224"
          fill="none" stroke="#85a4c4" stroke-width="2.5" opacity="0.7" stroke-linecap="round" />
  </g>

  <!-- Мощная воронёная рама и барабан .44 -->
  <g id="kr_frame_cylinder">
    <!-- Верхняя и задняя часть рамы -->
    <path d="M118,198 L156,178 L272,178 L272,272 L138,272 L108,236 Z"
          fill="url(#kr_bluedSteel)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Целик сверху рамы -->
    <rect x="148" y="168" width="26" height="12" rx="3" fill="url(#kr_bluedDark)" stroke="#121519" stroke-width="6" />

    <!-- Массивный 6-зарядный барабан .44 Magnum -->
    <rect x="178" y="188" width="84" height="74" rx="10"
          fill="url(#kr_bluedSteel)" stroke="#121519" stroke-width="11" />

    <!-- Глубокие долы барабана -->
    <rect x="192" y="198" width="54" height="12" rx="4" fill="#1b222c" stroke="#121519" stroke-width="4" />
    <rect x="192" y="218" width="54" height="14" rx="4" fill="#2d3947" stroke="#121519" stroke-width="4" />
    <rect x="192" y="240" width="54" height="12" rx="4" fill="#171d26" stroke="#121519" stroke-width="4" />

    <!-- Блестящие вертикальные грани барабана -->
    <line x1="186" y1="196" x2="186" y2="254" stroke="#8ca3bd" stroke-width="3.5" opacity="0.65" />
    <line x1="254" y1="196" x2="254" y2="254" stroke="#8ca3bd" stroke-width="3.5" opacity="0.65" />

    <!-- Кибернетическая технологическая насечка и микродатчик на раме -->
    <path d="M128,214 L164,214 L164,244 L128,244 Z" fill="url(#kr_bluedDark)" stroke="#121519" stroke-width="5" stroke-linejoin="round" />
    <line x1="136" y1="224" x2="156" y2="224" stroke="#59d4ff" stroke-width="4" stroke-linecap="round" />
    <line x1="136" y1="234" x2="150" y2="234" stroke="#96abc4" stroke-width="3.5" stroke-linecap="round" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Sig_ruby_demon_knife.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="rk_bladeSteel" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#c9d2db" />
      <stop offset="38%" stop-color="#7c8791" />
      <stop offset="76%" stop-color="#434b54" />
      <stop offset="100%" stop-color="#22272e" />
    </linearGradient>

    <linearGradient id="rk_bladeEdge" x1="0%" y1="0%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#9ca8b5" />
      <stop offset="50%" stop-color="#f2f7fc" />
      <stop offset="100%" stop-color="#cbd6e0" />
    </linearGradient>

    <linearGradient id="rk_boneGrip" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7a6855" />
      <stop offset="45%" stop-color="#4d3f32" />
      <stop offset="85%" stop-color="#2e241b" />
      <stop offset="100%" stop-color="#1a130e" />
    </linearGradient>

    <linearGradient id="rk_brass" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#c9a55c" />
      <stop offset="55%" stop-color="#856328" />
      <stop offset="100%" stop-color="#473210" />
    </linearGradient>

    <radialGradient id="rk_aura" cx="50%" cy="50%" r="50%">
      <stop offset="0%" stop-color="#d92b3a" stop-opacity="0.46" />
      <stop offset="50%" stop-color="#7a111c" stop-opacity="0.18" />
      <stop offset="100%" stop-color="#7a111c" stop-opacity="0" />
    </radialGradient>
  </defs>

  <!-- Едва заметная тёмно-багровая оккультная аура вдоль клинка -->
  <g id="rk_halo">
    <ellipse cx="322" cy="248" rx="146" ry="72" fill="url(#rk_aura)" />
  </g>

  <!-- Тонкий изогнутый древний клинок (лежит горизонтально, остриё вправо) -->
  <g id="rk_blade">
    <!-- Основное изогнутое полотно клинка -->
    <path d="M188,228 C258,230 334,226 402,216 C438,210 468,204 484,214
             C472,240 436,266 382,276 C318,288 250,286 188,278 Z"
          fill="url(#rk_bladeSteel)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Светлая режущая кромка снизу до острия -->
    <path d="M196,272 C256,280 318,282 378,270 C426,260 458,238 474,218"
          fill="none" stroke="url(#rk_bladeEdge)" stroke-width="5.5" stroke-linecap="round" />

    <!-- Тёмный дол вдоль обуха -->
    <path d="M204,238 C268,240 338,236 402,226"
          fill="none" stroke="#1e2329" stroke-width="7" stroke-linecap="round" />

    <!-- Демонические оккультные символы-глифы, выгравированные на клинке (только графика, без текста) -->
    <g id="rk_runes" fill="none" stroke="#ff4d5a" stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round">
      <!-- Глиф 1 -->
      <path d="M222,250 L236,264 M236,250 L222,264 M229,246 L229,268" />
      <!-- Глиф 2 -->
      <path d="M258,258 C264,246 276,246 280,258 C274,266 264,266 258,258 Z M269,246 L269,268" />
      <!-- Глиф 3 -->
      <path d="M300,264 L310,248 L320,264 M304,258 L316,258" />
      <!-- Глиф 4 -->
      <path d="M342,248 L358,248 L346,264 L362,262" />
      <!-- Глиф 5 -->
      <path d="M382,244 L394,254 L384,260 M394,244 L406,248" />
    </g>

    <!-- Тонкий светлый блик внутри глифов -->
    <g id="rk_runes_glow" fill="none" stroke="#ffd1d6" stroke-width="1.8" opacity="0.8" stroke-linecap="round">
      <path d="M229,248 L229,266 M269,248 L269,266 M310,250 L318,262 M348,250 L358,260" />
    </g>
  </g>

  <!-- Изящная старинная гарда-больстер -->
  <g id="rk_guard">
    <path d="M172,206 C188,202 200,214 198,252 C200,290 188,302 172,298
             C176,278 176,226 172,206 Z"
          fill="url(#rk_brass)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
    <circle cx="185" cy="252" r="6" fill="#ff3849" stroke="#121519" stroke-width="3.5" />
  </g>

  <!-- Тёмная костяная/роговая рукоять с рельефными бороздками -->
  <g id="rk_handle">
    <!-- Изогнутая форма рукояти из потемневшего оленьего рога -->
    <path d="M40,244 C64,234 118,228 176,232 L176,274 C122,276 74,284 44,296
             C28,288 26,256 40,244 Z"
          fill="url(#rk_boneGrip)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

    <!-- Светлый костяной блик по верхней грани -->
    <path d="M52,248 C84,240 124,238 164,240" fill="none" stroke="#a69179" stroke-width="4.5" opacity="0.6" stroke-linecap="round" />

    <!-- Органическая фактура рога и потёртые борозды -->
    <path d="M74,242 C72,256 76,268 82,280 M104,238 C102,252 106,264 110,274 M134,236 C132,250 134,262 138,272"
          fill="none" stroke="#1c150f" stroke-width="5.5" stroke-linecap="round" />
    <path d="M88,244 L92,274 M118,242 L122,270 M148,240 L150,268"
          fill="none" stroke="#8c7862" stroke-width="2.5" opacity="0.45" stroke-linecap="round" />

    <!-- Бронзовая заклёпка и колпачок навершия -->
    <path d="M36,246 L56,240 L60,290 L42,296 C28,288 26,256 36,246 Z"
          fill="url(#rk_brass)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
    <circle cx="116" cy="256" r="5" fill="url(#rk_brass)" stroke="#121519" stroke-width="3.5" />
  </g>
</svg>
```

---

`Art/Weapons/TMM_Sig_swan_anchor_club.svg`
```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <defs>
    <linearGradient id="sa_iron" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#7a8085" />
      <stop offset="42%" stop-color="#4d5257" />
      <stop offset="85%" stop-color="#2c3034" />
      <stop offset="100%" stop-color="#191c1f" />
    </linearGradient>

    <linearGradient id="sa_rust" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#ba6234" />
      <stop offset="48%" stop-color="#853d1c" />
      <stop offset="100%" stop-color="#471d0b" />
    </linearGradient>

    <linearGradient id="sa_rustBright" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#d97d43" />
      <stop offset="55%" stop-color="#9e4921" />
      <stop offset="100%" stop-color="#54230c" />
    </linearGradient>

    <linearGradient id="sa_chain" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#8c7a6b" />
      <stop offset="50%" stop-color="#59473a" />
      <stop offset="100%" stop-color="#2b2019" />
    </linearGradient>

    <linearGradient id="sa_algae" x1="0%" y1="0%" x2="0%" y2="100%">
      <stop offset="0%" stop-color="#5d7348" />
      <stop offset="100%" stop-color="#2b381f" />
    </linearGradient>
  </defs>

  <!-- Тяжёлая ржавая якорная цепь слева и обвитая вокруг веретена -->
  <g id="sa_chain_back">
    <!-- Свободный хвост цепи слева снизу -->
    <ellipse cx="48" cy="306" rx="18" ry="12" transform="rotate(-25 48 306)"
             fill="none" stroke="#121519" stroke-width="14" />
    <ellipse cx="48" cy="306" rx="18" ry="12" transform="rotate(-25 48 306)"
             fill="none" stroke="url(#sa_chain)" stroke-width="6" />

    <ellipse cx="70" cy="282" rx="18" ry="12" transform="rotate(-40 70 282)"
             fill="none" stroke="#121519" stroke-width="14" />
    <ellipse cx="70" cy="282" rx="18" ry="12" transform="rotate(-40 70 282)"
             fill="none" stroke="url(#sa_rust)" stroke-width="6" />

    <!-- Большое якорное кольцо (рым) на левом конце -->
    <circle cx="90" cy="256" r="30" fill="none" stroke="#121519" stroke-width="20" />
    <circle cx="90" cy="256" r="30" fill="none" stroke="url(#sa_iron)" stroke-width="9" />
  </g>

  <!-- Поперечный шток якоря у левого конца (вертикальный брус) -->
  <g id="sa_stock_bar">
    <rect x="132" y="168" width="28" height="176" rx="8"
          fill="url(#sa_iron)" stroke="#121519" stroke-width="11" />
    <!-- Шарообразные законцовки штока -->
    <circle cx="146" cy="166" r="18" fill="url(#sa_rust)" stroke="#121519" stroke-width="10" />
    <circle cx="146" cy="346" r="18" fill="url(#sa_rust)" stroke="#121519" stroke-width="10" />
  </g>

  <!-- Исполинские рога и лапы корабельного якоря (справа) -->
  <g id="sa_arms">
    <!-- Мощные изогнутые рога, расходящиеся кверху и книзу -->
    <path d="M404,224 C422,190 402,140 364,116 L342,138 C368,164 380,204 374,256
             C380,308 368,348 342,374 L364,396 C402,372 422,322 404,288
             L452,276 C462,264 462,248 452,236 Z"
          fill="url(#sa_iron)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Верхняя треугольная лапа якоря -->
    <path d="M322,94 L394,108 L372,158 L334,136 Z"
          fill="url(#sa_rust)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M336,104 L380,114 L366,140" fill="none" stroke="#e08952" stroke-width="4" opacity="0.55" stroke-linecap="round" />

    <!-- Нижняя треугольная лапа якоря -->
    <path d="M322,418 L394,404 L372,354 L334,376 Z"
          fill="url(#sa_rust)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <path d="M336,408 L380,398 L366,372" fill="none" stroke="#5c240c" stroke-width="4" opacity="0.65" stroke-linecap="round" />

    <!-- Тяжёлый ударный тренд (вершина короны якоря справа) -->
    <path d="M424,222 L470,236 C482,248 482,264 470,276 L424,290 Z"
          fill="url(#sa_rustBright)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />
    <line x1="436" y1="234" x2="464" y2="244" stroke="#f0a875" stroke-width="4" opacity="0.55" stroke-linecap="round" />
  </g>

  <!-- Центральное массивное веретено (древко дубины) -->
  <g id="sa_shank">
    <path d="M108,236 L432,226 L432,286 L108,276 Z"
          fill="url(#sa_iron)" stroke="#121519" stroke-width="12" stroke-linejoin="round" />

    <!-- Светлая верхняя грань и пятна глубокой ржавчины -->
    <line x1="120" y1="244" x2="416" y2="236" stroke="#9aa2a8" stroke-width="5" opacity="0.5" stroke-linecap="round" />
    <path d="M278,234 L344,232 L334,264 L286,262 Z" fill="url(#sa_rust)" opacity="0.72" />
    <path d="M362,252 L420,250 L418,278 L354,276 Z" fill="url(#sa_rust)" opacity="0.78" />
    <!-- Следы тины из затопленного пруда Лебедя -->
    <path d="M388,284 C394,302 384,316 392,326" fill="none" stroke="url(#sa_algae)" stroke-width="7" stroke-linecap="round" />
    <path d="M304,232 C310,218 320,216 326,228" fill="none" stroke="url(#sa_algae)" stroke-width="6" stroke-linecap="round" />

    <!-- Клёпаные железные усиливающие хомуты -->
    <circle cx="146" cy="256" r="8" fill="#1b1f24" stroke="#121519" stroke-width="4" />
    <circle cx="406" cy="256" r="9" fill="#1b1f24" stroke="#121519" stroke-width="4" />
  </g>

  <!-- Обмотка цепи вокруг веретена (рукоять для супермутанта) -->
  <g id="sa_chain_wrap">
    <path d="M180,224 L196,288 M208,224 L224,288 M236,224 L252,288 M264,224 L280,288"
          fill="none" stroke="#121519" stroke-width="20" stroke-linecap="round" />
    <path d="M180,224 L196,288 M208,224 L224,288 M236,224 L252,288 M264,224 L280,288"
          fill="none" stroke="url(#sa_chain)" stroke-width="9" stroke-linecap="round" />

    <!-- Свисающее звено цепи под рукоятью -->
    <ellipse cx="222" cy="304" rx="14" ry="20" transform="rotate(15 222 304)"
             fill="none" stroke="#121519" stroke-width="14" />
    <ellipse cx="222" cy="304" rx="14" ry="20" transform="rotate(15 222 304)"
             fill="none" stroke="url(#sa_rustBright)" stroke-width="6" />

    <!-- Блик на звеньях -->
    <path d="M184,232 L192,262 M212,232 L220,262 M240,232 L248,262 M268,232 L276,262"
          fill="none" stroke="#b8a798" stroke-width="3" opacity="0.55" stroke-linecap="round" />
  </g>
</svg>
```