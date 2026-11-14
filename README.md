# The Midnight Club

**An atmospheric, offline single-player casino and game collection for Windows.**

Play classic table games, original slot machines, puzzles, management games and an action roguelite in one connected experience. Everything uses **virtual credits**: no real-money gambling, payments, account registration, ads or online connection are required to play.

## Games

| Experience | What to expect |
| --- | --- |
| **Blackjack** | Classic Blackjack with configurable table rules, solo play and AI opponents, including splitting, doubling, insurance and surrender. |
| **Roulette** | Casino roulette with a shared virtual bankroll. |
| **Midnight Reels** | A classic three-reel slot machine. |
| **Midnight Manor** | A gothic five-reel slot with free spins and an interactive, integrated **3D Escape the Manor** bonus. |
| **Neon Vault** | A futuristic 1,024-ways video slot with expanding Wilds, jackpots and Vault Break features. |
| **Nine Lives** | A cat-themed slot with Pounce Wilds and bonus spins. |
| **Midnight Dice** | A Kniffel-style dice game, casually or at paid tables against AI. |
| **Midnight Merge** | Restore a casino room by room through matching, deliveries and unlockable renovations. |
| **Casino Empire** | Build and manage a casino property portfolio, research upgrades and earn progress while away. |
| **Chill Lounge** | Eight activities: Sudoku, Solitaire, Word Search, Memory, Jigsaw Puzzle, Tales from the Backroom, **Midnight Bastion** tower defense and **Midnight Sweep** minesweeper. |
| **Arcade** | 20 small skill-based games with independent high scores and challenges. |
| **Midnight Descent** | A separate Godot action roguelite with dungeon exploration, combat, character builds, relics and bosses. |

## Beyond the games

- **One virtual casino bankroll** for wagering modes, with a separate management economy in Casino Empire.
- **Long-term progression:** levels, Prestige, seasonal rewards, challenges, daily bonuses and player statistics.
- **Customization and collecting:** cosmetic Coins, unlockable titles and frames, shop items, the **Collector's Album** and Casino Vault.
- **Designed for offline play:** locally bundled game artwork, sound and music; progress is saved on your computer.
- **English and German** interface support, with ongoing translation work for some larger story content.
- **Modern Windows presentation:** responsive casino lobby and game interfaces, animations, audio settings and fullscreen support.

## Download and play

Get a packaged Windows build from **[GitHub Releases](https://github.com/TeKatz/blackjack-casino/releases)**. A GitHub *Source code* archive is **not** a playable build.

1. Download the Windows `blackjack-casino-win-x64.zip` package for your chosen release.
2. Extract it to a normal folder, keeping its contents together.
3. Launch **`BlackjackCasino.NativeShell.exe`**.

The game is distributed as a self-contained Windows application. It uses the Microsoft Edge **WebView2 Runtime** for its local interface. Internet access is not required for ordinary gameplay; checking for and downloading updates does require a connection.

**Release channels:** [Stable releases](https://github.com/TeKatz/blackjack-casino/releases) are intended for regular play. [Development](https://github.com/TeKatz/blackjack-casino/releases/tag/development) offers newer changes for early testing and may contain regressions. The in-game updater checks your selected channel and verifies downloaded packages before installation.

Player profiles are stored separately from the application files under `%LOCALAPPDATA%\BlackjackCasino`. Existing NativeShell profiles and imported saves are preserved during updates. Back up that folder before testing experimental builds or making manual changes to saved data.

## Technology and project documentation

The casino uses **.NET 10 / C#** for game logic, payouts, progression and saved transactions. A native **WinForms/WebView2** host displays locally bundled **React, TypeScript and PixiJS** interfaces; Godot powers Midnight Descent and the Manor 3D bonus. The former WPF interface is no longer shipped.

For contributors and curious players:

- **[Documentation hub](docs/index.md)** — gameplay guides, architecture, save behavior and historical design notes.
- **[Architecture](docs/architecture.md)** — C# authority, local browser security and recovery.
- **[Contributing and building](CONTRIBUTING.md)** — Windows SDK requirements, build steps and tests.
- **[Releases and branches](docs/branching-and-release-policy.md)** — Stable and Development delivery.

**The Midnight Club is a fictional play-money game. Virtual credits have no cash value.**
