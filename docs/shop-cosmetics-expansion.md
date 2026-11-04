# Shop cosmetics expansion

Nine portraits generated individually with the built-in image generation tool. Bundled as 1024 × 1024 JPEGs; the desktop decodes each once at 512 px and shares the frozen bitmap across previews and equipped portraits. Existing cosmetic IDs, prices and season unlocks are retained.

## New sets and prices

Prices are cosmetic coins (◆). All twelve additions can be bought at Prestige 0; the existing Midnight Sovereign Prestige I gate is retained.

| Tier | Card set | Price | Dice set | Price |
| --- | --- | ---: | --- | ---: |
| Standard | Linen Ivory | 650 | Porcelain Blue | 750 |
| Standard | Harbor Blue | 950 | Forest Resin | 1,200 |
| Mid-range | Rosewood Club | 5,600 | Copper Forge | 5,600 |
| Mid-range | Jade Filigree | 6,800 | Sapphire Inlay | 6,800 |
| Premium | Celestial Opal | 14,500 | Astral Opal | 11,000 |
| Premium | Solar Eclipse | 18,000 | Solar Crown | 14,500 |

Ruby Velvet and Aurora Prism retain their IDs and prices, with refined shading and subtle ruby/aurora halos. Premium cards use metallic edge reflections and a soft inner light; premium dice use layered translucent halo strokes and a reflected-light finish. These effects are static: no perpetual clocks, timers, full-window scans or blur effects are added. Card backs use cached frozen vector brushes. One card renderer and one dice palette serve shop previews and gameplay; equipped changes update existing face-up and face-down cards without revealing hidden cards.

## Validation

- Release solution build: passed with zero warnings and zero errors.
- Full unit suite: 517 passed, zero failed or skipped, including purchase/equip/save-reload and insufficient-funds coverage for all twelve new sets.
- Windows ManorSmoke project: builds with zero warnings and errors. Its portrait and cosmetics galleries exercise circular crops, full/compact card backs, repeated restyling and D20 palette coverage.
- Localization parity, collection artwork integrity, empire artwork integrity, shared controls regression checks and git whitespace check pass.
- The Windows runtime smoke executes in GitHub CI; it cannot run on the Linux editing host.

## Portrait prompts

### portrait-eclipse-raven

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/eclipse-raven.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game profile portrait, highly detailed polished painterly realistic digital artwork. Subject: Eclipse Raven, a majestic black raven in close head and upper chest portrait, glossy layered ebony feathers with violet-blue iridescence, sharp silver beak, intelligent pale amethyst eye, a small silver lunar clasp. Backdrop: deep plum velvet darkness and a restrained warm eclipse corona behind its head, soft smoky moonlight. Composition: single raven centered, head fills middle 60 percent, beak and head completely inside central circular safe area, clean silhouette readable at 48 pixels, luxurious realistic feather detail. No lettering, no borders, no logos, no collage, no watermark.

### portrait-eclipse-sovereign

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/eclipse-sovereign.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Eclipse Sovereign: a mature regal man with silver-black hair, subtle short beard, finely carved antique-gold crown with small amethyst gems, midnight plum velvet ceremonial jacket; quiet commanding gaze. A warm solar eclipse corona in a dark purple smoky sky behind his head, restrained gold edge lighting. Style: highly detailed, polished painterly realistic digital artwork for a premium game, cinematic restrained lighting, tactile fine materials. Composition: SINGLE centered head and upper shoulders, entire face/head and accessories safely inside central 75 percent circle, readable silhouette at 48 pixels, full bleed square background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-house-guest

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/house-guest.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. House Guest: a welcoming young adult man with dark chestnut hair, clean-shaven face, warm expressive brown eyes and a slight confident smile, elegant charcoal dinner suit, ivory shirt, dark emerald tie and tiny gold spade lapel pin. A luxurious softly blurred green and amber Art Deco casino lounge behind him. Approachable, grounded, refined. Style: highly detailed, polished painterly realistic digital artwork for a premium game, cinematic restrained lighting, tactile fine materials. Composition: SINGLE centered head and upper shoulders, entire face/head and accessories safely inside central 75 percent circle, readable silhouette at 48 pixels, full bleed square background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-eclipse-lady

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/eclipse-lady.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Lady in Eclipse: a mysterious elegant adult woman with long black hair, pearl-silver earrings with a crescent shape, dark plum velvet evening dress, pale skin with natural detail and composed warm gaze. Soft silvery moonlight across her face and a muted amber eclipse halo on deep violet velvet darkness behind her. Style: highly detailed, polished painterly realistic digital artwork for a premium game, cinematic restrained lighting, tactile fine materials. Composition: SINGLE centered head and upper shoulders, entire face/head and accessories safely inside central 75 percent circle, readable silhouette at 48 pixels, full bleed square background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-eclipse-moon

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/eclipse-moon.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Moonbound: a striking white arctic wolf with exquisitely detailed silver fur, icy lavender eyes, serene alert gaze, tiny dark silver lunar pendant at lower neck, close head and chest portrait, three-quarter view. A luminous full moon, wisps of dark indigo mist, scattered stars and subtle violet rim light behind its head. Style: highly detailed, polished painterly realistic digital artwork for a premium game, cinematic restrained lighting, tactile fine materials. Composition: SINGLE centered head and upper shoulders, entire face/head and accessories safely inside central 75 percent circle, readable silhouette at 48 pixels, full bleed square background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-dynasty-heiress

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/dynasty-heiress.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Dynasty Heiress: poised young adult East Asian woman, glossy black hair in an elegant updo, a delicate sculpted gold and amethyst diadem, obsidian high-collared silk jacket with intricate imperial gold embroidery. Confident direct gaze. Cyan and magenta architectural casino lights softly blurred behind her, warm flattering light on face. Luxurious future royalty. Style: highly detailed polished painterly realistic digital artwork, cinematic restrained lighting, tactile fine materials, coherent Neon Dynasty casino collection. Composition: SINGLE centered head and upper shoulders, entire face, hair and crown inside central 75 percent circular safe zone, strong silhouette readable at 48 pixels, full bleed square dark background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-neon-overseer

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/neon-overseer.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Neon Overseer: imposing adult Black man, close-cropped hair and a silver streak at temple, composed watchful face, sleek violet-black chrome tailored high-collared coat with understated cyan inlays, a small transparent cybernetic cyan eyepiece beside one eye without obscuring his face. Deep dark futuristic casino with geometric cyan and violet rails behind him. Floor security chief, refined and authoritative. Style: highly detailed polished painterly realistic digital artwork, cinematic restrained lighting, tactile fine materials, coherent Neon Dynasty casino collection. Composition: SINGLE centered head and upper shoulders, entire face, hair and crown inside central 75 percent circular safe zone, strong silhouette readable at 48 pixels, full bleed square dark background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-neon-emperor

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/neon-emperor.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Neon Emperor: regal mature East Asian man with swept-back silver-black hair and a short neatly groomed beard, sophisticated angular imperial gold crown with cyan crystal insets, black ceremonial jacket with intricate restrained gold filigree and a magenta silk collar. Calm proud gaze. Dark futuristic imperial casino with cyan and hot magenta architectural lighting, halo of warm gold behind head. Highest-tier reward, lavish fine detail. Style: highly detailed polished painterly realistic digital artwork, cinematic restrained lighting, tactile fine materials, coherent Neon Dynasty casino collection. Composition: SINGLE centered head and upper shoulders, entire face, hair and crown inside central 75 percent circular safe zone, strong silhouette readable at 48 pixels, full bleed square dark background. No text, no frame, no border, no logo, no watermark, no collage.

### portrait-neon-croupier

Asset: `src/BlackjackCasino.Desktop/Assets/Portraits/neon-croupier.jpg`

Use case: stylized-concept. Asset type: square 1:1 casino game avatar portrait. Neon Croupier: stylish adult female casino dealer with warm brown skin, short swept-back dark hair, confident friendly expression, black glass-textured satin waistcoat, crisp ivory shirt, magenta bow tie, tiny cyan spade lapel pin, subtle futuristic earring. Elegant blurred black-glass casino behind her with narrow cyan and magenta neon strips. Sophisticated professional dealer, not armored. Style: highly detailed polished painterly realistic digital artwork, cinematic restrained lighting, tactile fine materials, coherent Neon Dynasty casino collection. Composition: SINGLE centered head and upper shoulders, entire face, hair and crown inside central 75 percent circular safe zone, strong silhouette readable at 48 pixels, full bleed square dark background. No text, no frame, no border, no logo, no watermark, no collage.
