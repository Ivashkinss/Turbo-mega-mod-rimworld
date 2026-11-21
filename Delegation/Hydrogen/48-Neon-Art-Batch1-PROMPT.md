**Режим: Canvas. Модель: Neon. Уровень рассуждений — высокий.**

## Порядок ответа (важно)
Не готовь всё сразу — иначе упрёшься в лимит длины. Каждый ответ — **ровно 4 SVG-файла** из списка ниже, по порядку, затем жди «дальше». Первый ответ — сразу файлы 1–4. Над каждым блоком — путь файла, сам блок — ```svg.

## Задача 48 — уникальная графика, партия 1: оружие, модули, снаряды
Мы делаем мод RimWorld «Turbo mega mod» (Fallout × «Сверхъестественное»). Правило проекта: у всего нового — своя уникальная картинка. Сейчас предметы используют ванильные заглушки. Ты рисуешь **векторные SVG**, я сам перевожу их в PNG нужного размера (128 px иконка, 64 px на карте).

### Стиль (по образцу твоего шлема T-60 ниже)
- `viewBox="0 0 512 512"`, прозрачный фон, без текста, без `<image>`, без внешних ссылок, без фильтров размытия. Градиенты можно.
- Толстый тёмный контур `#121519`, **10–14 единиц** — так он читается на 64 px. Мелкие детали тоньше 4 единиц пропадут — не рассчитывай на них.
- Плоская подсветка сверху-слева, как в RimWorld; лёгкий износ.
- Узнаваемый силуэт важнее деталей: предмет должен читаться на 64×64.
- Уникальный цветовой акцент у каждой легендарки (свечение, руны, латунь, Enochian-символы и т. п.).

### Ориентация
- **Оружие** (`Art/Weapons/...`): лежит горизонтально, **дуло/острие вправо**, рукоять слева, занимает ~85–90% ширины, по центру по вертикали.
- **Снаряды** (`Art/Projectiles/...`): маленький яркий снаряд **остриём вверх**, занимает ~30% высоты по центру, светящийся.
- **Предметы** (`Art/Items/...`): по центру, ~70% кадра. Для предметов с пометкой «стопка» нарисуй **три файла**: `_a` (одна штука), `_b` (несколько), `_c` (куча).

### Файлы партии 1 (по порядку)
Оружие:
1. `Art/Weapons/TMM_Legendary_TheColt.svg` — Кольт Сэмюэля Кольта: старинный кавалерийский револьвер, серебряная гравировка, пентаграмма на рукояти.
2. `Art/Weapons/TMM_Legendary_Experiment18A.svg` — Эксперимент 18-А: перегруженная плазменная винтовка Института, зелёное свечение катушек.
3. `Art/Weapons/TMM_Legendary_FinalJudgment.svg` — «Последний суд»: тяжёлая лазерная гатлинг-пушка Братства Стали, красные линзы.
4. `Art/Weapons/TMM_Legendary_LorenzosArtifact.svg` — пушка-артефакт Лоренцо: странное устройство с золотым светящимся шаром в каркасе.
5. `Art/Weapons/TMM_Legendary_AtomsJudgement.svg` — «Суд Атома»: огромный гаечный ключ, светящийся радиацией (зелёным).
6. `Art/Weapons/TMM_Legendary_TeslaCannon.svg` — тесла-пушка Анклава: громоздкая, с катушками и голубыми разрядами.
7. `Art/Weapons/TMM_Legendary_OverseersGuardian.svg` — «Страж Смотрителя»: боевое ружьё в сине-жёлтых цветах Убежища, двойной ствол.
8. `Art/Weapons/TMM_Legendary_RailwayRifle.svg` — тяжёлая рельсовая винтовка: паровой котёл, манометр, костыли.
9. `Art/Weapons/TMM_Legendary_BigBoy.svg` — «Большой мальчик»: катапульта мини-ядерных зарядов «Толстяк», с радиационным знаком.
10. `Art/Weapons/TMM_Legendary_TheEqualizer.svg` — «Уравнитель»: футуристическая винтовка с фиолетовой квантовой камерой.
11. `Art/Weapons/TMM_Legendary_FirstBlade.svg` — Первый клинок: костяной кинжал из челюсти, метка Каина.
12. `Art/Weapons/TMM_Legendary_ArchangelBlade.svg` — клинок архангела: трёхгранный серебряный клинок с бело-золотым сиянием.
13. `Art/Weapons/TMM_Legendary_AlphaPredator.svg` — «Альфа-хищник»: зазубренный мачете из когтя/кости с багровым оттенком.
14. `Art/Weapons/TMM_Gun_BoraxSprayer.svg` — распылитель буры: самодельный ранцевый распылитель с баллоном и раструбом.

Предметы:
15. `Art/Items/TMM_Mod_HolyWaterTank.svg` — баллон со святой водой: стальной цилиндр, голубое окошко, крест.
16. `Art/Items/TMM_Mod_BoraxTank.svg` — канистра буры: белая/жёлтая канистра с химическим знаком.
17–19. `Art/Items/TMM_Mod_RockSaltPack_a.svg`, `_b.svg`, `_c.svg` — пачки патронов с каменной солью (стопка).
20–22. `Art/Items/TMM_Mod_SilverEngraving_a.svg`, `_b.svg`, `_c.svg` — набор серебряной гравировки с енохианскими символами (стопка).
23–25. `Art/Items/TMM_Mod_RailroadBallisticWeave_a.svg`, `_b.svg`, `_c.svg` — рулон баллистической ткани «Подземки» с символом фонаря (стопка).
26–28. `Art/Items/TMM_Ammo_ColtSilverBullet_a.svg`, `_b.svg`, `_c.svg` — серебряные пули для Кольта (стопка).

Снаряды:
29. `Art/Projectiles/TMM_Bullet_Experiment18A.svg` — зелёный плазменный сгусток.
30. `Art/Projectiles/TMM_Bullet_FinalJudgment.svg` — красный лазерный луч.
31. `Art/Projectiles/TMM_Bullet_TheColt.svg` — серебряная пуля со следом.
32. `Art/Projectiles/TMM_Projectile_LorenzoWave.svg` — золотая волна-кольцо.
33. `Art/Projectiles/TMM_Bullet_TeslaCannon.svg` — голубая молния-дуга.
34. `Art/Projectiles/TMM_Bullet_OverseersGuardian.svg` — латунная пуля.
35. `Art/Projectiles/TMM_Bullet_RailwayRifle.svg` — раскалённый железнодорожный костыль.
36. `Art/Projectiles/TMM_Projectile_BigBoy.svg` — мини-ядерный заряд.
37. `Art/Projectiles/TMM_Bullet_TheEqualizer.svg` — фиолетовый квантовый заряд.
38. `Art/Projectiles/TMM_Bullet_Borax.svg` — белое облачко-струя буры.

После последнего файла — одна строка «партия 1 готова».

## Образец стиля: твой шлем T-60
```svg
<svg id="vector-asset" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
            <defs>
              <!-- Основной стальной корпус -->
              <linearGradient id="steelMain" x1="18%" y1="10%" x2="82%" y2="90%">
                <stop offset="0%" stop-color="#a8b3be" />
                <stop offset="28%" stop-color="#7c8792" />
                <stop offset="68%" stop-color="#535d67" />
                <stop offset="100%" stop-color="#384048" />
              </linearGradient>

              <!-- Светлый гребень сверху -->
              <linearGradient id="steelTop" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stop-color="#c4cdd6" />
                <stop offset="55%" stop-color="#89949f" />
                <stop offset="100%" stop-color="#59636d" />
              </linearGradient>

              <!-- Тёмная броня (козырёк, щёки, респиратор) -->
              <linearGradient id="steelDark" x1="15%" y1="0%" x2="85%" y2="100%">
                <stop offset="0%" stop-color="#69747e" />
                <stop offset="50%" stop-color="#48515a" />
                <stop offset="100%" stop-color="#2c333a" />
              </linearGradient>

              <!-- Тёмный металл внутри и шланги -->
              <linearGradient id="gunmetal" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stop-color="#22272c" />
                <stop offset="50%" stop-color="#3b434b" />
                <stop offset="100%" stop-color="#1c2025" />
              </linearGradient>

              <!-- Стекло визора -->
              <linearGradient id="visorGlass" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stop-color="#193440" />
                <stop offset="35%" stop-color="#245363" />
                <stop offset="70%" stop-color="#132b36" />
                <stop offset="100%" stop-color="#0b171d" />
              </linearGradient>

              <!-- Свечение фонаря -->
              <radialGradient id="lampGlow" cx="45%" cy="45%" r="55%">
                <stop offset="0%" stop-color="#fffbe0" />
                <stop offset="38%" stop-color="#ffe169" />
                <stop offset="75%" stop-color="#e59a1a" />
                <stop offset="100%" stop-color="#8c5308" />
              </radialGradient>

              <!-- Ореол света вокруг фонаря -->
              <radialGradient id="lampHalo" cx="50%" cy="50%" r="50%">
                <stop offset="0%" stop-color="#ffe885" stop-opacity="0.55" />
                <stop offset="55%" stop-color="#f5b82e" stop-opacity="0.18" />
                <stop offset="100%" stop-color="#f5b82e" stop-opacity="0" />
              </radialGradient>

              <!-- Медный/латунный акцент фильтров -->
              <linearGradient id="brassRing" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stop-color="#d2b47c" />
                <stop offset="50%" stop-color="#9c7e4c" />
                <stop offset="100%" stop-color="#5e4928" />
              </linearGradient>
            </defs>

            <!-- 1. ЗАДНЯЯ ШЕЙНАЯ МАНЖЕТА И НИЖНИЙ ВОРОТНИК -->
            <g id="neck-collar">
              <path d="M168,376 L344,376 L354,424 C320,444 192,444 158,424 Z"
                    fill="url(#gunmetal)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />
              <path d="M174,398 Q256,414 338,398" fill="none" stroke="#14171c" stroke-width="6" />
              <path d="M168,414 Q256,430 344,414" fill="none" stroke="#535d67" stroke-width="3" opacity="0.55" />
            </g>

            <!-- 2. БОКОВЫЕ УЗЛЫ СВЯЗИ И КРЕПЛЕНИЯ ФИЛЬТРОВ -->
            <g id="side-pods">
              <!-- Левый модуль -->
              <rect x="86" y="218" width="44" height="106" rx="12"
                    fill="url(#steelDark)" stroke="#121519" stroke-width="10" />
              <rect x="94" y="232" width="16" height="78" rx="4" fill="#2c333a" />
              <line x1="94" y1="252" x2="112" y2="252" stroke="#121519" stroke-width="4" />
              <line x1="94" y1="271" x2="112" y2="271" stroke="#121519" stroke-width="4" />
              <line x1="94" y1="290" x2="112" y2="290" stroke="#121519" stroke-width="4" />

              <!-- Правый модуль -->
              <rect x="382" y="218" width="44" height="106" rx="12"
                    fill="url(#steelDark)" stroke="#121519" stroke-width="10" />
              <rect x="402" y="232" width="16" height="78" rx="4" fill="#2c333a" />
              <line x1="400" y1="252" x2="418" y2="252" stroke="#121519" stroke-width="4" />
              <line x1="400" y1="271" x2="418" y2="271" stroke="#121519" stroke-width="4" />
              <line x1="400" y1="290" x2="418" y2="290" stroke="#121519" stroke-width="4" />
            </g>

            <!-- 3. КУПОЛ ШЛЕМА (ОСНОВНАЯ БРОНЯ) -->
            <g id="dome">
              <path d="M116,246 C108,144 162,74 256,74 C350,74 404,144 396,246
                       L386,342 L338,386 L174,386 L126,342 Z"
                    fill="url(#steelMain)" stroke="#121519" stroke-width="11" stroke-linejoin="round" />

              <!-- Боковое затемнение купола -->
              <path d="M320,86 C378,112 396,174 390,248 L382,338 L338,380 L316,380
                       L346,320 L356,220 C358,154 344,112 320,86 Z"
                    fill="#1b2026" opacity="0.32" />

              <!-- Блик сверху-слева -->
              <path d="M138,166 C152,114 192,88 238,84 L224,112 C188,120 162,140 152,172 Z"
                    fill="#dce5ee" opacity="0.32" />

              <!-- Центральный бронированный гребень (фирменная деталь T-60) -->
              <path d="M222,78 C240,72 272,72 290,78 L298,176 L256,188 L214,176 Z"
                    fill="url(#steelTop)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
              <line x1="256" y1="78" x2="256" y2="184" stroke="#2f3740" stroke-width="4" opacity="0.6" />
              <!-- Заклёпки на гребне -->
              <circle cx="232" cy="108" r="4.5" fill="#2c333a" stroke="#c8d2dc" stroke-width="1.5" />
              <circle cx="280" cy="108" r="4.5" fill="#2c333a" stroke="#c8d2dc" stroke-width="1.5" />
              <circle cx="230" cy="154" r="4.5" fill="#2c333a" stroke="#c8d2dc" stroke-width="1.5" />
              <circle cx="282" cy="154" r="4.5" fill="#2c333a" stroke="#c8d2dc" stroke-width="1.5" />

              <!-- Панельные швы на лбу -->
              <path d="M144,158 L214,144" fill="none" stroke="#22282f" stroke-width="4" stroke-linecap="round" />
              <path d="M368,158 L298,144" fill="none" stroke="#22282f" stroke-width="4" stroke-linecap="round" />
            </g>

            <!-- 4. ТЯЖЁЛЫЙ НАДБРОВНЫЙ КОЗЫРЁК -->
            <g id="brow-plate">
              <!-- Тень под козырьком -->
              <path d="M122,194 L256,222 L390,194 L384,214 L256,240 L128,214 Z"
                    fill="#0d1014" opacity="0.7" />
              <path d="M112,174 L256,196 L400,174 L394,202 L256,226 L118,202 Z"
                    fill="url(#steelDark)" stroke="#121519" stroke-width="9" stroke-linejoin="round" />
              <!-- Светлая кромка козырька -->
              <path d="M122,181 L256,202 L390,181" fill="none" stroke="#b6c1cc" stroke-width="3" opacity="0.55" />
            </g>

            <!-- 5. УГЛУБЛЁННЫЙ БРОНЕВИЗОР -->
            <g id="visor">
              <!-- Внутренняя чёрная рама -->
              <path d="M132,208 L256,230 L380,208 L368,256 L256,272 L144,256 Z"
                    fill="#111418" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
              <!-- Бронестекло визора -->
              <path d="M144,218 L256,238 L368,218 L358,250 L256,264 L154,250 Z"
                    fill="url(#visorGlass)" stroke="#0d1115" stroke-width="4" stroke-linejoin="round" />
              <!-- Разделительная перемычка по центру визора -->
              <path d="M248,236 L264,236 L262,265 L250,265 Z" fill="#2b333c" stroke="#121519" stroke-width="3" />
              <!-- Острые блики на стекле -->
              <polygon points="164,224 196,229 180,252 158,248" fill="#7be0ff" opacity="0.34" />
              <polygon points="208,231 220,233 206,255 196,254" fill="#a8eeff" opacity="0.24" />
              <polygon points="292,233 320,228 308,252 284,256" fill="#7be0ff" opacity="0.22" />
            </g>

            <!-- 6. ЩЁЧНЫЕ БРОНЕПЛАСТИНЫ -->
            <g id="cheek-plates">
              <!-- Левая щека -->
              <path d="M126,254 L198,274 L188,368 L138,342 Z"
                    fill="url(#steelMain)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
              <!-- Правая щека -->
              <path d="M386,254 L314,274 L324,368 L374,342 Z"
                    fill="url(#steelDark)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
              <!-- Малые вентиляционные прорези на щеках -->
              <line x1="146" y1="286" x2="176" y2="294" stroke="#1a1f24" stroke-width="5" stroke-linecap="round" />
              <line x1="146" y1="302" x2="174" y2="310" stroke="#1a1f24" stroke-width="5" stroke-linecap="round" />
              <line x1="366" y1="286" x2="336" y2="294" stroke="#1a1f24" stroke-width="5" stroke-linecap="round" />
              <line x1="366" y1="302" x2="338" y2="310" stroke="#1a1f24" stroke-width="5" stroke-linecap="round" />
            </g>

            <!-- 7. ЦЕНТРАЛЬНЫЙ РЕСПИРАТОРНЫЙ БЛОК И РЕШЁТКА -->
            <g id="respirator-snout">
              <!-- Корпус «морды» T-60 -->
              <path d="M206,266 L306,266 L326,386 L290,410 L222,410 L186,386 Z"
                    fill="url(#steelTop)" stroke="#121519" stroke-width="10" stroke-linejoin="round" />

              <!-- Внутренний тёмный воздухозаборник -->
              <path d="M220,282 L292,282 L302,354 L210,354 Z"
                    fill="#1b2026" stroke="#121519" stroke-width="6" stroke-linejoin="round" />

              <!-- Вертикальные рёбра решётки -->
              <line x1="234" y1="286" x2="230" y2="350" stroke="#626d78" stroke-width="5" stroke-linecap="round" />
              <line x1="249" y1="286" x2="248" y2="350" stroke="#626d78" stroke-width="5" stroke-linecap="round" />
              <line x1="263" y1="286" x2="264" y2="350" stroke="#626d78" stroke-width="5" stroke-linecap="round" />
              <line x1="278" y1="286" x2="282" y2="350" stroke="#626d78" stroke-width="5" stroke-linecap="round" />

              <!-- Центральный круглый клапан фильтра -->
              <circle cx="256" cy="378" r="24" fill="url(#steelDark)" stroke="#121519" stroke-width="7" />
              <circle cx="256" cy="378" r="13" fill="url(#brassRing)" stroke="#121519" stroke-width="4" />
              <circle cx="256" cy="378" r="5" fill="#181c21" />
            </g>

            <!-- 8. ДЫХАТЕЛЬНЫЕ ГОФРИРОВАННЫЕ ШЛАНГИ -->
            <g id="breathing-hoses">
              <!-- Левый шланг -->
              <path d="M196,356 C144,364 108,348 110,310"
                    fill="none" stroke="#121519" stroke-width="24" stroke-linecap="round" />
              <path d="M196,356 C144,364 108,348 110,310"
                    fill="none" stroke="#3f4750" stroke-width="13" stroke-linecap="round" />
              <path d="M192,356 C144,363 110,347 110,314"
                    fill="none" stroke="#808b96" stroke-width="11" stroke-dasharray="5 9" stroke-linecap="butt" opacity="0.65" />

              <!-- Правый шланг -->
              <path d="M316,356 C368,364 404,348 402,310"
                    fill="none" stroke="#121519" stroke-width="24" stroke-linecap="round" />
              <path d="M316,356 C368,364 404,348 402,310"
                    fill="none" stroke="#343b43" stroke-width="13" stroke-linecap="round" />
              <path d="M320,356 C368,363 402,347 402,314"
                    fill="none" stroke="#6f7a85" stroke-width="11" stroke-dasharray="5 9" stroke-linecap="butt" opacity="0.55" />

              <!-- Фильтры-бочонки на концах шлангов -->
              <circle cx="192" cy="356" r="14" fill="url(#brassRing)" stroke="#121519" stroke-width="6" />
              <circle cx="320" cy="356" r="14" fill="url(#brassRing)" stroke="#121519" stroke-width="6" />
            </g>

            <!-- 9. ТАКТИЧЕСКИЙ ФОНАРЬ СПРАВА (ФИРМЕННАЯ ДЕТАЛЬ T-60) -->
            <g id="headlamp">
              <!-- Ореол свечения -->
              <circle cx="404" cy="166" r="56" fill="url(#lampHalo)" />
              <!-- Кронштейн -->
              <path d="M366,154 L398,148 L398,184 L368,182 Z"
                    fill="url(#steelDark)" stroke="#121519" stroke-width="8" stroke-linejoin="round" />
              <!-- Корпус фонаря -->
              <circle cx="404" cy="166" r="24" fill="url(#steelDark)" stroke="#121519" stroke-width="8" />
              <!-- Светящаяся линза -->
              <circle cx="404" cy="166" r="15" fill="url(#lampGlow)" stroke="#1e2329" stroke-width="3" />
              <circle cx="399" cy="161" r="4.5" fill="#ffffff" opacity="0.9" />
            </g>

            <!-- 10. ЛЁГКИЕ БОЕВЫЕ ЦАРАПИНЫ И ДЕТАЛИЗАЦИЯ -->
            <g id="weathering" opacity="0.55" stroke-linecap="round">
              <path d="M164,132 L182,148" stroke="#dce5ee" stroke-width="2.5" />
              <path d="M166,135 L184,151" stroke="#1b2026" stroke-width="1.5" />
              <path d="M332,126 L348,136" stroke="#dce5ee" stroke-width="2" />
              <path d="M148,322 L166,330" stroke="#dce5ee" stroke-width="2" />
              <circle cx="136" cy="264" r="3" fill="#20262c" />
              <circle cx="376" cy="264" r="3" fill="#20262c" />
            </g>
          </svg>
```
