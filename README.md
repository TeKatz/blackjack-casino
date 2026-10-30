# Blackjack Casino

A free, offline-first single-player casino game for Windows. It combines Blackjack with four distinct slot modes, an independent idle casino, shared play-money progression and a unified in-app casino lobby. There is no real-money gambling, advertising or account requirement.

The repository now contains the work from Phases 1–6. Current development is focused on presentation polish, content depth, regression protection and keeping every game mode inside one coherent desktop experience.

## Current gameplay

### Blackjack

- play-money bankroll and denomination-labelled wager chips;
- Hit, Stand, Double, Split, Late Surrender and Insurance;
- configurable table rules and presets, including S17/H17, 3:2/6:5, ENHC, deck count and split/double rules;
- persistent shoe and cut-card reshuffle;
- Solo mode or Full Table with 1–5 deterministic AI seats sharing the same shoe;
- three table themes with layered felt/table presentation;
- optional card, chip, settlement and menu animations/sounds;
- F11 fullscreen mode.

### Casino lobby and embedded games

The application opens into a casino lobby. Blackjack, Kniffel, Midnight Reels, Midnight Manor, Neon Vault, Nine Lives and Casino Empire all run inside the same main application window rather than opening separate visible Windows windows. Returning from a game restores the lobby and its navigation state.

The table and slot games share the same play-money bankroll, player level, Prestige, Coins and Season progression. Game-specific statistics are tracked separately for Blackjack, Midnight Reels, Midnight Manor, Neon Vault and Nine Lives. Casino Empire has its own construction credits and idle progression; owner dividends are its connection to the shared play-money bankroll.

## Kniffel

A complete casual solo dice table with five animated vector dice, hold/release,
up to three rolls per turn and a 13-box interactive paper score sheet. All legal
scores are previewed together with candidate glows; entries require confirmation,
including explicit zero scratches. Upper and further-Kniffel bonuses are included.
The current turn is saved automatically, including held dice and remaining rolls.
This foundation has no entry fees, payouts or AI opponents yet.

See [Kniffel rules, controls and validation](docs/kniffel.md).

## Casino Empire

A furnished 2D idle casino with five unlockable rooms, automatic production,
room upgrades and ten-level production milestones. Earn Empire credits to build
your business, including up to eight hours of offline catch-up, and claim a
separate bounded owner dividend as casino cash. The page creates no separate window.

See [Casino Empire foundation and economy](docs/casino-empire.md).

## Midnight Reels

A compact three-reel slot with custom vector textures, animated reels/lights and original sounds. It is the simpler, faster slot experience and shares the common bankroll/progression systems without changing Blackjack rules.

See [slot rules and payouts](docs/slots.md).

## Midnight Manor

A 5×3 / 20-line Gothic video slot with original artwork and synthesized audio, Normal/Fast/Turbo speeds, stoppable Auto Spin, Haunted Night Free Spins and the optional route-based **Escape the Manor** Risk/Reward feature.

See the [design and rules](docs/midnight-manor-design.md), [executable math report](docs/midnight-manor-math.md) and [Windows acceptance checklist](docs/midnight-manor-acceptance.md). Reference-policy RTP is 95.4539%; manual Escape decisions can change risk/return. Pending features survive closing and restart; autoplay never resumes merely from saved settings.

## Neon Vault

Neon Vault is the high-roller presentation slot: a 5×4, **1,024 Ways** machine with bets from $1 to $250,000. Its feature set includes Expanding Wilds, BLACKOUT / OVERLOAD / TOTAL BLACKOUT events, Vault Break Free Spins, Vault Energy / Security Override, Access-Key jackpots and presentation-only Near Miss sequences.

The slot uses custom generated symbol artwork, cached WPF image assets, candidate highlighting while reels settle, dimming of irrelevant cells, confirmed-win highlighting and neon connection paths. Spin results are determined before presentation; Near Miss and anticipation effects never alter the outcome.

See [Neon Vault design and implementation](docs/neon-vault.md).

## Nine Lives

Nine Lives is the compact high-risk/high-reward slot: a **3×3** machine with five fixed paylines, bets from $1 to $25,000, eight custom cat-themed symbols, Pounce Wilds, nine-spin bonus rounds and a rare **5,000× Full Grid Frenzy**.

Its presentation follows the moonlit rooftop concept art directly: a fixed 1536×1024 design surface scales uniformly inside the embedded game host, while the nine reel cells, bankroll, level, bet, win amount and controls remain real WPF elements layered over the artwork. The generated cabinet and symbol art is embedded locally and decoded into frozen image sources, so the mode works offline and does not rely on external URLs.

Nine Lives uses its own persistent statistics bucket and never writes into Midnight Reels or Neon Vault tracking. Version `0.10.0` is the first development line containing the new machine.

## Profile, progression and cosmetics

- player levels 0–100 with voluntary Prestige;
- free Season progression;
- persistent Coins and cosmetics;
- custom profile identity, portrait, title and frame;
- searchable/paged Challenges with **Claim All**;
- searchable/paged cosmetic shop;
- casino-wide totals plus separate statistics for every game mode;
- compatibility migration for older saves whose Reels/Neon slot history could not be attributed safely.

Profile and progression screens are hosted inside the casino window with an in-app back stack instead of separate visible windows.

See [Phase 6](docs/phase-6.md), [table/profile/progression refresh](docs/table-profile-progression.md) and [tracking migration](docs/progression-tracking-migration.md).

## Audio and presentation

The desktop client uses custom dark casino controls, dedicated music identities for Blackjack / slot play / Midnight Manor, synthesized interaction sounds and Windows presentation regression tests. Dynamic reel presentation is designed to reuse controls and cached assets instead of recreating heavy WPF visual trees every frame.

See [audio overhaul](docs/audio-overhaul.md) and [presentation regression](docs/presentation-regression.md).

## Updates

Stable and Development updater channels use versioned manifests, package-size validation and SHA-256 verification. Player saves live outside the installation directory and are preserved across application replacement. The Nine Lives branch advances the development line to `0.10.0-dev.*`, ensuring the Development updater sees it as newer than the previous `0.9.0-dev.*` builds.

See [development update delivery](docs/development-update-delivery.md).

## Projects

| Project | Purpose |
| --- | --- |
| `BlackjackCasino.Core` | Blackjack rules, slot engines, progression/challenges, statistics and other UI-independent domain logic |
| `BlackjackCasino.Update` | Manifest protocol, semantic versions, download and SHA-256 validation |
| `BlackjackCasino.Updater` | Out-of-process staged installation replacement |
| `BlackjackCasino.Desktop` | WPF casino lobby, table/slot presentation, embedded navigation, profile/progression, audio and preferences |
| `BlackjackCasino.Tests` | Core, progression, slot, persistence and updater tests |
| `tools/*` | Presentation/runtime smoke tests, wager regression checks, Manor math and audio rendering helpers |

## Development

Requires the .NET 10 SDK.

```powershell
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

The CI pipeline additionally exercises shared-control contracts, table wager presentation, embedded casino navigation, Midnight Manor runtime behavior and Windows presentation regression captures.

For the overall structure see [`docs/architecture.md`](docs/architecture.md). Historical implementation phases are documented in [`docs/phase-1.md`](docs/phase-1.md) through [`docs/phase-6.md`](docs/phase-6.md).
