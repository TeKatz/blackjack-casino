# Collector Album: pack reveal UX

The React album is an animation-only client of a durable, native C# pack-opening receipt.

## November 2026 polish

- The opening overlay fits its owning record-page viewport without internal or background
  vertical scrolling. Cards scale inside the available theatrical region; no forced
  viewport-height increase is needed.
- Front/back card faces now rotate in a true preserve-3D layer **without a CSS filter
  flattening their backfaces**. The actual illustrated atlas artwork is visible on reveal.
- A label above each revealed card uses the committed native `isNew` and
  `duplicatePoints` fields: green **NEW** or violet **DUPLICATE +X**.
- A single pack, as well as every pack in an Open All sequence, issues the native
  `complete` command **once per receipt** after all five cards have appeared.
  The native open already credited the collection and duplicate points, so completion
  only clears the pending receipt; it does **not** credit anything again.
- The finished screen offers **Back to collection** and, when inventory allows,
  **Open another pack**. An unsuccessful completion shows an explicit retry instead
  of silently issuing repeated native commands. Leaving before all five cards are
  revealed still preserves the existing recovery workflow.
- Collected Epic and Legendary gallery cards have permanent, restrained ambient
  rarity glows and occasional foil sweeps (motion disabled in accessibility mode).
- Lobby navigation displays distinct softly pulsing claimable reward counters for
  **Collections** (complete, unclaimed theme rewards) and **Challenges** (existing
  challenge-ready count). Labels shrink and ellipsize to keep badges within the
  narrow desktop navigation column.

Validation: frontend bridge checks, WebView2 album startup/collection presentation
smokes and native exactly-once transaction tests. No randomness, atlas assets,
payout amounts or claimed-reward persistence rules are changed.
