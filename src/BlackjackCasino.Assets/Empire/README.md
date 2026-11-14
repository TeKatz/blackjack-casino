# Casino Empire room atlas

`rooms-atlas.png` is original generated art for the offline desktop client. It is
kept as a source asset and copied into the local `ModernFrontend/empire-art/` package by the NativeShell build. The current React/WebView2 client uses packaged images; no network requests or image generation happen at runtime.
The six equally sized tiles are Slot Gallery, Card Salon, Midnight Bar, Roulette
Room, VIP Lounge and treasury/lobby cover (left to right, top to bottom).

Created with the image generation tool. The original PNG source is preserved unchanged. Current React/PixiJS presentation keeps text, levels, income, lighting particles, upgrade flashes, controls and milestones separate from the background artwork, preserving readability at different sizes.

## Generation prompt

Use case: stylized-concept. Asset type: production 2D game background atlas for Casino Empire, a luxury midnight casino incremental desktop game. Create ONE landscape image, exactly a uniform 3-column by 2-row grid of SIX independent equal-size rectangular room illustrations, touching edge-to-edge, no gutters or border. Target 1536x1024 pixels, each tile 512x512. Each illustration fills its own square fully, and nothing crosses a tile boundary. Consistent exquisite hand-painted 2D videogame environment art, straight-on interior elevation, decorative art-deco casino furnishings, detailed material textures, midnight teal, antique brass and warm gold lighting, subtle burgundy and plum. NO people, NO lettering, NO text, NO numbers, NO UI, NO icons, NO watermark, no 3D renders, no isometric view. The art is an actual background behind separately rendered game UI. Top-left tile: intimate slot gallery with three jewel-lit vintage brass slot cabinets, emerald velvet wallpaper and warm sconces. Top-middle tile: elegant card salon, emerald felt blackjack table in foreground, playing cards/chips as small details, rich dark wood wall and brass sconces. Top-right tile: midnight cocktail bar, polished walnut bar, softly glowing shelves of colored glass bottles and purple velvet bar stools. Bottom-left tile: roulette room with a beautiful red-black roulette wheel on dark wood table in foreground, wine-colored velvet wall and golden details. Bottom-middle tile: exclusive VIP lounge with plush plum sofas, round marble coffee table, gold champagne accents and a dramatic art-deco chandelier. Bottom-right tile: a treasury room with ornate brass vault door, decorative cabinet and warm gold floor reflections. High craft and cozy cinematic lighting, sharp readable furniture silhouettes even when each tile is shown very small. Keep main focal furniture within center 80% of each square. Each of the six squares is a distinct room, not a continuous panorama.

## Individual upper and resort rooms

Ten original square images now give every Upper Floor and Resort Floor room its
own illustration. `room-prompts.json` records the built-in imagegen prompt set.
The established Main Floor atlas and its treasury illustration stay in use.
Room IDs select their own assets; no new room reuses an atlas tile. The native release copies all eleven Empire PNG source assets to the offline frontend package. React presents the selected room and management status; decorative effects and image caching are handled by the current browser presentation.
