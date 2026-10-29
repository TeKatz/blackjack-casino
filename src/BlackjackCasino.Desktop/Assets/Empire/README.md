# Casino Empire room atlas

`rooms-atlas.png` is original generated art for the offline desktop client. It is
embedded as a WPF resource; no network requests or image generation happen at runtime.
The six equally sized tiles are Slot Gallery, Card Salon, Midnight Bar, Roulette
Room, VIP Lounge and treasury/lobby cover (left to right, top to bottom).

Created with the built-in image generation tool. The original PNG is bundled
unchanged. Frozen CroppedBitmap resources select the tiles at runtime. Text,
levels, income, lighting particles, upgrade flashes, controls and milestone
indicators are rendered separately, keeping them readable at different sizes.

## Generation prompt

Use case: stylized-concept. Asset type: production 2D game background atlas for Casino Empire, a luxury midnight casino incremental desktop game. Create ONE landscape image, exactly a uniform 3-column by 2-row grid of SIX independent equal-size rectangular room illustrations, touching edge-to-edge, no gutters or border. Target 1536x1024 pixels, each tile 512x512. Each illustration fills its own square fully, and nothing crosses a tile boundary. Consistent exquisite hand-painted 2D videogame environment art, straight-on interior elevation, decorative art-deco casino furnishings, detailed material textures, midnight teal, antique brass and warm gold lighting, subtle burgundy and plum. NO people, NO lettering, NO text, NO numbers, NO UI, NO icons, NO watermark, no 3D renders, no isometric view. The art is an actual background behind separately rendered game UI. Top-left tile: intimate slot gallery with three jewel-lit vintage brass slot cabinets, emerald velvet wallpaper and warm sconces. Top-middle tile: elegant card salon, emerald felt blackjack table in foreground, playing cards/chips as small details, rich dark wood wall and brass sconces. Top-right tile: midnight cocktail bar, polished walnut bar, softly glowing shelves of colored glass bottles and purple velvet bar stools. Bottom-left tile: roulette room with a beautiful red-black roulette wheel on dark wood table in foreground, wine-colored velvet wall and golden details. Bottom-middle tile: exclusive VIP lounge with plush plum sofas, round marble coffee table, gold champagne accents and a dramatic art-deco chandelier. Bottom-right tile: a treasury room with ornate brass vault door, decorative cabinet and warm gold floor reflections. High craft and cozy cinematic lighting, sharp readable furniture silhouettes even when each tile is shown very small. Keep main focal furniture within center 80% of each square. Each of the six squares is a distinct room, not a continuous panorama.
