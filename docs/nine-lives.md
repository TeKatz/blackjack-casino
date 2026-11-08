# Nine Lives — Moonlit Rooftop cabinet

The playable Nine Lives cabinet is React/WebView2. PixiJS renders the ambient night sky; the
C# `NineLivesSession` remains authoritative for every spin, stake, result, bonus and payout.

## Cabinet presentation (November 2026)

- The existing eight packaged 1024px JPG symbol illustrations are preserved. Soft alpha-edge
  masking, dark-pixel screen blending and symbol-themed full-cell light fields prevent their
  square black photographic backgrounds from appearing as hard rectangular boxes. The images
  retain their existing on-screen scale; animated ambient artwork fills the unused side space.
- The top-right moon now has crater texture, rim lighting and a restrained halo/hover.
- Reels render as **three independent compositor-transform tapes** with deterministic visual
  filler and final rows from the settled C# spin. No 85ms symbol replacement loop or motion
  blur is involved. Staggered easing and recoil are cosmetic only.
- **Normal / Fast / Turbo** alters reel/feature timing, never RNG or accounting, and persists
  locally per cabinet. Reduced-motion and the existing animation setting skip cosmetic movement.
- **Autospins:** 10, 20, 50, 100 or unlimited. Every spin is an individual native receipt;
  the loop stops for insufficient balance, failed receipts or confirmation timeouts.
- A verified line win or bonus trigger adds a pulsing gold/violet glow to the outer cabinet
  rim. Payline overlays continue to use the C# winning-lines result.
- Nine Lives owns **eight new offline-synthesized sound cues** in `NineLivesAudio` for spin,
  reel stop, loss, win, big win, pounce, bonus and frenzy. Generic `SlotAudio` sounds are
  not played by this cabinet.
- Triple Nine / three Bonus icons: native strip weights are expressed per **1000**, with a
  slight increase in per-cell Bonus probability from 3.0% to 3.1% (30 to 31 weight points),
  offset by Yarn (280 to 279). Free-spin length and payout multipliers are unchanged.

## Regression coverage

- TypeScript tests verify deterministic reel destinations, speed ordering and idle 3×3 image
  topology; WebView2 startup smoke verifies that the packaged artwork, three reels and new
  controls render in Windows.
- C# tests check the exact strip weights, bonus trigger, receipt recovery and all eight
  standalone PCM cues, including amplitude bounds and fade-out.
