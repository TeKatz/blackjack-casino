namespace BlackjackCasino.Core;

public enum ChallengeCategory
{
    Blackjack,
    MidnightReels,
    MidnightManor,
    NeonVault,
    NineLives,
    Lifetime,
    Prestige,
    MidnightDice,
    Sudoku,
}

public enum ChallengeMetric
{
    BlackjackRounds,
    BlackjackHands,
    BlackjackWins,
    BlackjackBlackjacks,
    BlackjackDoubles,
    BlackjackDoubleWins,
    BlackjackSplits,
    BlackjackSurrenders,
    BlackjackInsurance,
    BlackjackWagered,
    BlackjackNetProfit,
    BlackjackBiggestWin,
    BlackjackHighestBankroll,
    BlackjackBestWinStreak,

    ReelsSpins,
    ReelsWins,
    ReelsWagered,
    ReelsReturned,
    ReelsBiggestPayout,
    ReelsNetProfit,

    ManorPaidSpins,
    ManorFreeSpins,
    ManorWagered,
    ManorReturned,
    ManorBigWins,
    ManorFreeSessions,
    ManorBonuses,
    ManorExits,
    ManorMaximumExits,
    ManorCatches,
    ManorCashOuts,
    ManorBestMultiplier,

    NeonSpins,
    NeonPaidSpins,
    NeonFreeSpins,
    NeonWins,
    NeonWagered,
    NeonReturned,
    NeonBiggestPayout,
    NeonFreeSpinSessions,
    NeonJackpots,
    NeonGrandJackpots,
    NeonBlackouts,
    NeonTotalBlackouts,
    NeonSecurityOverrides,
    NeonBestMultiplier,

    NineLivesSpins,
    NineLivesPaidSpins,
    NineLivesBonusSpins,
    NineLivesWins,
    NineLivesWagered,
    NineLivesReturned,
    NineLivesBiggestPayout,
    NineLivesBonusSessions,
    NineLivesBonusSpinsAwarded,
    NineLivesPounceWilds,
    NineLivesFullGridFrenzies,
    NineLivesBestMultiplier,

    LifetimeExperience,
    LifetimePlaytimeSeconds,
    LifetimeWagered,
    LifetimePlays,
    PrestigeCount,
    DiceGames, DiceBestScore, DiceUpperBonuses, DiceFiveOfAKinds, DiceFullHouses, DiceLargeStraights,
    DiceCasinoGames, DiceCasinoWins, DicePodiums, DiceWagered, DiceReturned, DiceBiggestPayout,
    SudokuCompleted, SudokuDailyCompleted, SudokuNoHintCompleted, SudokuExpertCompleted, SudokuMasterCompleted,
}

public sealed record ChallengeDefinition(
    string Id,
    ChallengeCategory Category,
    string Name,
    string Description,
    ChallengeMetric Metric,
    decimal Goal,
    long CoinReward,
    bool IsEndgame = false);

public static class ChallengeCatalog
{
    private static readonly IReadOnlyList<ChallengeDefinition> Challenges = Array.AsReadOnly(new[]
    {
        // Sudoku deliberately rewards persistence rather than speed. The Chill Lounge should
        // never turn its timer into pressure or punish players for using accessibility aids.
        C("sudoku-first", ChallengeCategory.Sudoku, "First Grid", "Complete your first Sudoku puzzle.", ChallengeMetric.SudokuCompleted, 1, 50),
        C("sudoku-10", ChallengeCategory.Sudoku, "Settling In", "Complete 10 Sudoku puzzles.", ChallengeMetric.SudokuCompleted, 10, 150),
        C("sudoku-50", ChallengeCategory.Sudoku, "Quiet Regular", "Complete 50 Sudoku puzzles.", ChallengeMetric.SudokuCompleted, 50, 400),
        E("sudoku-250", ChallengeCategory.Sudoku, "A Thousand Little Squares", "Complete 250 Sudoku puzzles.", ChallengeMetric.SudokuCompleted, 250, 1500),
        C("sudoku-daily-1", ChallengeCategory.Sudoku, "Today's Grid", "Complete your first Daily Sudoku.", ChallengeMetric.SudokuDailyCompleted, 1, 75),
        C("sudoku-daily-30", ChallengeCategory.Sudoku, "A Month of Dailies", "Complete 30 Daily Sudokus. They do not need to be consecutive.", ChallengeMetric.SudokuDailyCompleted, 30, 350),
        C("sudoku-clean-1", ChallengeCategory.Sudoku, "No Nudge Needed", "Complete a Sudoku without using a hint.", ChallengeMetric.SudokuNoHintCompleted, 1, 75),
        C("sudoku-clean-25", ChallengeCategory.Sudoku, "Pencil and Patience", "Complete 25 Sudokus without using a hint.", ChallengeMetric.SudokuNoHintCompleted, 25, 350),
        C("sudoku-expert-10", ChallengeCategory.Sudoku, "Deep Focus", "Complete 10 Expert Sudokus.", ChallengeMetric.SudokuExpertCompleted, 10, 500),
        C("sudoku-master-1", ChallengeCategory.Sudoku, "Master Key", "Complete your first Master Sudoku.", ChallengeMetric.SudokuMasterCompleted, 1, 250),
        E("sudoku-master-25", ChallengeCategory.Sudoku, "Stillness", "Complete 25 Master Sudokus.", ChallengeMetric.SudokuMasterCompleted, 25, 1200),

        C("sudoku-25", ChallengeCategory.Sudoku, "Quiet Evenings", "Complete 25 Sudoku puzzles.", ChallengeMetric.SudokuCompleted, 25, 250),
        C("sudoku-100", ChallengeCategory.Sudoku, "A Shelf of Grids", "Complete 100 Sudoku puzzles.", ChallengeMetric.SudokuCompleted, 100, 750),
        E("sudoku-500", ChallengeCategory.Sudoku, "The Patient Mind", "Complete 500 Sudoku puzzles.", ChallengeMetric.SudokuCompleted, 500, 2500),
        C("sudoku-daily-7", ChallengeCategory.Sudoku, "Seven Quiet Days", "Complete 7 Daily Sudokus; no streak required.", ChallengeMetric.SudokuDailyCompleted, 7, 175),
        E("sudoku-daily-100", ChallengeCategory.Sudoku, "Daily Ritual", "Complete 100 Daily Sudokus; no streak required.", ChallengeMetric.SudokuDailyCompleted, 100, 1000),
        C("sudoku-clean-10", ChallengeCategory.Sudoku, "Trust Your Pencil", "Complete 10 Sudokus without hints.", ChallengeMetric.SudokuNoHintCompleted, 10, 200),
        E("sudoku-clean-100", ChallengeCategory.Sudoku, "Clear Thinking", "Complete 100 Sudokus without hints.", ChallengeMetric.SudokuNoHintCompleted, 100, 900),
        C("sudoku-expert-1", ChallengeCategory.Sudoku, "A Deeper Grid", "Complete your first Expert Sudoku.", ChallengeMetric.SudokuExpertCompleted, 1, 175),
        E("sudoku-expert-50", ChallengeCategory.Sudoku, "Expert's Corner", "Complete 50 Expert Sudokus.", ChallengeMetric.SudokuExpertCompleted, 50, 1500),
        C("sudoku-master-10", ChallengeCategory.Sudoku, "Mindful Master", "Complete 10 Master Sudokus.", ChallengeMetric.SudokuMasterCompleted, 10, 750),

        C("dice-game-1", ChallengeCategory.MidnightDice, "First Sheet", "Complete a Midnight Dice game in either mode.", ChallengeMetric.DiceGames, 1, 50),
        C("dice-games-10", ChallengeCategory.MidnightDice, "Dice Lounge Regular", "Complete 10 Midnight Dice games.", ChallengeMetric.DiceGames, 10, 150),
        C("dice-games-50", ChallengeCategory.MidnightDice, "A Stack of Sheets", "Complete 50 Midnight Dice games.", ChallengeMetric.DiceGames, 50, 400),
        E("dice-games-250", ChallengeCategory.MidnightDice, "The Long Roll", "Complete 250 Midnight Dice games.", ChallengeMetric.DiceGames, 250, 1500),
        C("dice-score-200", ChallengeCategory.MidnightDice, "Finding Your Rhythm", "Score at least 200 points in one game.", ChallengeMetric.DiceBestScore, 200, 100),
        C("dice-score-250", ChallengeCategory.MidnightDice, "A Beautiful Sheet", "Score at least 250 points in one game.", ChallengeMetric.DiceBestScore, 250, 200),
        E("dice-score-300", ChallengeCategory.MidnightDice, "Three Hundred Club", "Score at least 300 points in one game.", ChallengeMetric.DiceBestScore, 300, 500),
        C("dice-bonus-1", ChallengeCategory.MidnightDice, "Above the Line", "Complete a game with the upper-section bonus.", ChallengeMetric.DiceUpperBonuses, 1, 75),
        C("dice-bonus-25", ChallengeCategory.MidnightDice, "Upper Management", "Earn the upper bonus in 25 completed games.", ChallengeMetric.DiceUpperBonuses, 25, 350),
        C("dice-five-1", ChallengeCategory.MidnightDice, "All Together", "Record a five of a kind and complete the game.", ChallengeMetric.DiceFiveOfAKinds, 1, 100),
        E("dice-five-50", ChallengeCategory.MidnightDice, "Five-Star Regular", "Record 50 five-of-a-kind rolls in completed games.", ChallengeMetric.DiceFiveOfAKinds, 50, 1000),
        C("dice-full-10", ChallengeCategory.MidnightDice, "Full House Guest", "Fill Full House in 10 completed games.", ChallengeMetric.DiceFullHouses, 10, 150),
        C("dice-straight-10", ChallengeCategory.MidnightDice, "Straight to the Point", "Fill Large Straight in 10 completed games.", ChallengeMetric.DiceLargeStraights, 10, 200),
        C("dice-table-1", ChallengeCategory.MidnightDice, "Pull Up a Chair", "Finish your first casino table against three AI opponents.", ChallengeMetric.DiceCasinoGames, 1, 100),
        C("dice-win-1", ChallengeCategory.MidnightDice, "Table Champion", "Take first place at a casino dice table; shared first counts.", ChallengeMetric.DiceCasinoWins, 1, 150),
        C("dice-wins-10", ChallengeCategory.MidnightDice, "House Favourite", "Take first place at 10 casino dice tables.", ChallengeMetric.DiceCasinoWins, 10, 500),
        E("dice-wins-50", ChallengeCategory.MidnightDice, "Velvet Crown", "Take first place at 50 casino dice tables.", ChallengeMetric.DiceCasinoWins, 50, 1500),
        C("dice-podium-10", ChallengeCategory.MidnightDice, "Always in Contention", "Finish in the top two at 10 casino dice tables.", ChallengeMetric.DicePodiums, 10, 300),
        C("dice-wager-100000", ChallengeCategory.MidnightDice, "Serious Seat", "Pay $100,000 in casino dice entry fees.", ChallengeMetric.DiceWagered, 100000, 300),
        E("dice-payout-100000", ChallengeCategory.MidnightDice, "A Golden Sheet", "Receive at least $100,000 from one casino dice table.", ChallengeMetric.DiceBiggestPayout, 100000, 1000),
        C("dice-games-25", ChallengeCategory.MidnightDice, "Evening Ritual", "Complete 25 Midnight Dice games.", ChallengeMetric.DiceGames, 25, 250),
        E("dice-games-100", ChallengeCategory.MidnightDice, "Hundred Sheets", "Complete 100 Midnight Dice games.", ChallengeMetric.DiceGames, 100, 800),
        C("dice-score-275", ChallengeCategory.MidnightDice, "Almost Perfect", "Score at least 275 points in one game.", ChallengeMetric.DiceBestScore, 275, 350),
        C("dice-bonus-10", ChallengeCategory.MidnightDice, "Above Average", "Earn the upper bonus in 10 completed games.", ChallengeMetric.DiceUpperBonuses, 10, 200),
        C("dice-five-10", ChallengeCategory.MidnightDice, "Five in Style", "Record 10 five of a kinds in completed games.", ChallengeMetric.DiceFiveOfAKinds, 10, 350),
        E("dice-full-50", ChallengeCategory.MidnightDice, "House Collector", "Fill Full House in 50 completed games.", ChallengeMetric.DiceFullHouses, 50, 600),
        E("dice-straight-50", ChallengeCategory.MidnightDice, "Straight Talk", "Fill Large Straight in 50 completed games.", ChallengeMetric.DiceLargeStraights, 50, 650),
        C("dice-table-10", ChallengeCategory.MidnightDice, "The Regular Table", "Finish 10 casino dice tables.", ChallengeMetric.DiceCasinoGames, 10, 300),
        E("dice-podium-50", ChallengeCategory.MidnightDice, "Podium Resident", "Finish in the top two at 50 casino dice tables.", ChallengeMetric.DicePodiums, 50, 900),
        E("dice-returned-1000000", ChallengeCategory.MidnightDice, "Velvet Fortune", "Receive $1,000,000 in total casino dice prizes.", ChallengeMetric.DiceReturned, 1000000, 1500),

        // Blackjack — 25 challenges. Milestones intentionally overlap so ordinary laid-back play
        // always has something moving without forcing exotic rules or high stakes.
        C("bj-rounds-10", ChallengeCategory.Blackjack, "Take a Seat", "Complete 10 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 10, 60),
        C("bj-rounds-50", ChallengeCategory.Blackjack, "Settled In", "Complete 50 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 50, 120),
        C("bj-rounds-250", ChallengeCategory.Blackjack, "Table Regular", "Complete 250 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 250, 300),
        C("bj-rounds-1000", ChallengeCategory.Blackjack, "Night Shift", "Complete 1,000 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 1000, 800),
        C("bj-hands-500", ChallengeCategory.Blackjack, "Many Hands", "Play 500 Blackjack hands, including split hands.", ChallengeMetric.BlackjackHands, 500, 350),
        C("bj-win-1", ChallengeCategory.Blackjack, "First Victory", "Win your first Blackjack hand.", ChallengeMetric.BlackjackWins, 1, 25),
        C("bj-win-25", ChallengeCategory.Blackjack, "Winning Ways", "Win 25 Blackjack hands.", ChallengeMetric.BlackjackWins, 25, 100),
        C("bj-win-100", ChallengeCategory.Blackjack, "Table Veteran", "Win 100 Blackjack hands.", ChallengeMetric.BlackjackWins, 100, 250),
        C("bj-win-500", ChallengeCategory.Blackjack, "House Nemesis", "Win 500 Blackjack hands.", ChallengeMetric.BlackjackWins, 500, 700),
        C("bj-blackjack-5", ChallengeCategory.Blackjack, "Natural Talent", "Hit 5 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 5, 100),
        C("bj-blackjack-25", ChallengeCategory.Blackjack, "Blackjack Hunter", "Hit 25 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 25, 250),
        C("bj-blackjack-100", ChallengeCategory.Blackjack, "Natural Born", "Hit 100 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 100, 700),
        C("bj-double-10", ChallengeCategory.Blackjack, "Double Down", "Double 10 hands.", ChallengeMetric.BlackjackDoubles, 10, 100),
        C("bj-double-win-10", ChallengeCategory.Blackjack, "Double Trouble", "Win 10 doubled hands.", ChallengeMetric.BlackjackDoubleWins, 10, 150),
        C("bj-double-win-50", ChallengeCategory.Blackjack, "Committed", "Win 50 doubled hands.", ChallengeMetric.BlackjackDoubleWins, 50, 450),
        C("bj-split-10", ChallengeCategory.Blackjack, "Split Decision", "Split 10 pairs.", ChallengeMetric.BlackjackSplits, 10, 120),
        C("bj-split-50", ChallengeCategory.Blackjack, "Split Personality", "Split 50 pairs.", ChallengeMetric.BlackjackSplits, 50, 400),
        C("bj-surrender-10", ChallengeCategory.Blackjack, "Know When to Fold", "Use surrender 10 times.", ChallengeMetric.BlackjackSurrenders, 10, 100),
        C("bj-insurance-10", ChallengeCategory.Blackjack, "Covered", "Take insurance 10 times.", ChallengeMetric.BlackjackInsurance, 10, 100),
        C("bj-wager-5000", ChallengeCategory.Blackjack, "Chips in Motion", "Wager $5,000 at Blackjack in total.", ChallengeMetric.BlackjackWagered, 5000, 150),
        C("bj-wager-25000", ChallengeCategory.Blackjack, "Serious Action", "Wager $25,000 at Blackjack in total.", ChallengeMetric.BlackjackWagered, 25000, 400),
        C("bj-profit-1000", ChallengeCategory.Blackjack, "Ahead of the House", "Reach $1,000 lifetime Blackjack net profit.", ChallengeMetric.BlackjackNetProfit, 1000, 300),
        C("bj-biggest-500", ChallengeCategory.Blackjack, "One Big Hand", "Record a $500+ Blackjack round win.", ChallengeMetric.BlackjackBiggestWin, 500, 250),
        C("bj-streak-5", ChallengeCategory.Blackjack, "Hot Streak", "Reach a 5-round win streak.", ChallengeMetric.BlackjackBestWinStreak, 5, 200),
        C("bj-streak-10", ChallengeCategory.Blackjack, "Unbroken", "Reach a 10-round win streak.", ChallengeMetric.BlackjackBestWinStreak, 10, 600),

        // Midnight Reels — 10 challenges.
        C("reels-spins-25", ChallengeCategory.MidnightReels, "Warm Up the Reels", "Play 25 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 25, 60),
        C("reels-spins-100", ChallengeCategory.MidnightReels, "One More Spin", "Play 100 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 100, 150),
        C("reels-spins-500", ChallengeCategory.MidnightReels, "Reel Regular", "Play 500 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 500, 450),
        C("reels-wins-25", ChallengeCategory.MidnightReels, "Lights on the Line", "Win 25 Midnight Reels spins.", ChallengeMetric.ReelsWins, 25, 120),
        C("reels-wins-100", ChallengeCategory.MidnightReels, "Classic Luck", "Win 100 Midnight Reels spins.", ChallengeMetric.ReelsWins, 100, 350),
        C("reels-wager-5000", ChallengeCategory.MidnightReels, "Coin Churn", "Wager $5,000 on Midnight Reels.", ChallengeMetric.ReelsWagered, 5000, 180),
        C("reels-return-10000", ChallengeCategory.MidnightReels, "Back Through the Tray", "Return $10,000 from Midnight Reels spins.", ChallengeMetric.ReelsReturned, 10000, 250),
        C("reels-payout-500", ChallengeCategory.MidnightReels, "Bell Ringer", "Land a $500+ Midnight Reels payout.", ChallengeMetric.ReelsBiggestPayout, 500, 250),
        C("reels-profit-1000", ChallengeCategory.MidnightReels, "Old-School Edge", "Reach $1,000 Midnight Reels net profit.", ChallengeMetric.ReelsNetProfit, 1000, 350),
        C("reels-spins-1500", ChallengeCategory.MidnightReels, "Mechanical Memory", "Play 1,500 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 1500, 900),

        // Midnight Manor — 10 challenges.
        C("manor-spins-25", ChallengeCategory.MidnightManor, "Invitation Accepted", "Play 25 Midnight Manor paid spins.", ChallengeMetric.ManorPaidSpins, 25, 60),
        C("manor-spins-100", ChallengeCategory.MidnightManor, "The House Knows You", "Play 100 Midnight Manor paid spins.", ChallengeMetric.ManorPaidSpins, 100, 160),
        C("manor-free-50", ChallengeCategory.MidnightManor, "Haunted Night", "Play 50 Manor Free Spins.", ChallengeMetric.ManorFreeSpins, 50, 180),
        C("manor-free-sessions-5", ChallengeCategory.MidnightManor, "Moonlit", "Trigger 5 Free Spin sessions.", ChallengeMetric.ManorFreeSessions, 5, 150),
        C("manor-bonus-5", ChallengeCategory.MidnightManor, "Open the Door", "Trigger Escape the Manor 5 times.", ChallengeMetric.ManorBonuses, 5, 180),
        C("manor-exit-1", ChallengeCategory.MidnightManor, "Made It Out", "Reach the Moonlit Gate once.", ChallengeMetric.ManorExits, 1, 250),
        C("manor-exit-10", ChallengeCategory.MidnightManor, "Escape Artist", "Reach the Moonlit Gate 10 times.", ChallengeMetric.ManorExits, 10, 700),
        C("manor-max-exit-1", ChallengeCategory.MidnightManor, "Impossible Door", "Escape through a 1,000× Moonlit Gate.", ChallengeMetric.ManorMaximumExits, 1, 1000),
        C("manor-caught-5", ChallengeCategory.MidnightManor, "The Manor Found You", "Get caught by the ghost 5 times.", ChallengeMetric.ManorCatches, 5, 100),
        C("manor-best-250", ChallengeCategory.MidnightManor, "Deep into the Manor", "Finish a Manor run at 250× or more.", ChallengeMetric.ManorBestMultiplier, 250, 500),

        // Neon Vault starts from verified play only; the old pooled slot totals have no source tag.
        C("neon-spins-25", ChallengeCategory.NeonVault, "Enter the Vault", "Play 25 Neon Vault spins.", ChallengeMetric.NeonSpins, 25, 60),
        C("neon-spins-100", ChallengeCategory.NeonVault, "Access Granted", "Play 100 Neon Vault spins.", ChallengeMetric.NeonSpins, 100, 150),
        C("neon-spins-500", ChallengeCategory.NeonVault, "Vault Regular", "Play 500 Neon Vault spins.", ChallengeMetric.NeonSpins, 500, 450),
        C("neon-wins-100", ChallengeCategory.NeonVault, "Electric Luck", "Win 100 Neon Vault spins.", ChallengeMetric.NeonWins, 100, 350),
        C("neon-free-100", ChallengeCategory.NeonVault, "After Hours Access", "Play 100 Neon Vault free spins.", ChallengeMetric.NeonFreeSpins, 100, 250),
        C("neon-sessions-10", ChallengeCategory.NeonVault, "Keys to the Vault", "Trigger 10 Neon Vault free spin sessions.", ChallengeMetric.NeonFreeSpinSessions, 10, 300),
        C("neon-jackpots-5", ChallengeCategory.NeonVault, "Vault Breaker", "Land 5 Neon Vault jackpots.", ChallengeMetric.NeonJackpots, 5, 400),
        C("neon-blackouts-25", ChallengeCategory.NeonVault, "Lights Out", "Encounter 25 Neon Vault blackouts of any kind.", ChallengeMetric.NeonBlackouts, 25, 250),
        C("neon-wager-25000", ChallengeCategory.NeonVault, "Vault Circulation", "Wager $25,000 on Neon Vault paid spins.", ChallengeMetric.NeonWagered, 25000, 350),
        C("neon-multiplier-250", ChallengeCategory.NeonVault, "Power Surge", "Land a Neon Vault payout of 250× the spin bet.", ChallengeMetric.NeonBestMultiplier, 250, 500),

        // Nine Lives — feature-driven goals use the already separately tracked Nine Lives statistics.
        C("nine-spins-25", ChallengeCategory.NineLives, "Curiosity", "Play 25 Nine Lives spins.", ChallengeMetric.NineLivesSpins, 25, 60),
        C("nine-spins-100", ChallengeCategory.NineLives, "Cat Nap Cancelled", "Play 100 Nine Lives spins.", ChallengeMetric.NineLivesSpins, 100, 150),
        C("nine-spins-500", ChallengeCategory.NineLives, "Rooftop Regular", "Play 500 Nine Lives spins.", ChallengeMetric.NineLivesSpins, 500, 450),
        C("nine-wins-100", ChallengeCategory.NineLives, "Lucky Paws", "Win 100 Nine Lives spins.", ChallengeMetric.NineLivesWins, 100, 350),
        C("nine-bonus-50", ChallengeCategory.NineLives, "Nine More Lives", "Play 50 Nine Lives bonus spins.", ChallengeMetric.NineLivesBonusSpins, 50, 220),
        C("nine-sessions-10", ChallengeCategory.NineLives, "Lives to Spare", "Trigger 10 Nine Lives bonus sessions.", ChallengeMetric.NineLivesBonusSessions, 10, 300),
        C("nine-pounce-25", ChallengeCategory.NineLives, "Pounce!", "Trigger 25 Pounce Wild transformations.", ChallengeMetric.NineLivesPounceWilds, 25, 300),
        C("nine-wager-25000", ChallengeCategory.NineLives, "Yarn in Motion", "Wager $25,000 on paid Nine Lives spins.", ChallengeMetric.NineLivesWagered, 25000, 350),
        C("nine-multiplier-250", ChallengeCategory.NineLives, "Moonlit Payday", "Land a Nine Lives payout of 250× the spin bet.", ChallengeMetric.NineLivesBestMultiplier, 250, 550),
        C("nine-frenzy-1", ChallengeCategory.NineLives, "All Nine", "Land a Full Grid Frenzy.", ChallengeMetric.NineLivesFullGridFrenzies, 1, 1000),

        // Long-term goals: all are beyond the existing introductory milestones.
        E("bj-rounds-10000", ChallengeCategory.Blackjack, "Keeper of the Table", "Complete 10,000 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 10000, 2500),
        E("bj-wins-5000", ChallengeCategory.Blackjack, "House Legend", "Win 5,000 Blackjack hands.", ChallengeMetric.BlackjackWins, 5000, 2500),
        E("bj-blackjacks-1000", ChallengeCategory.Blackjack, "A Thousand Naturals", "Hit 1,000 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 1000, 3000),
        E("bj-double-wins-1000", ChallengeCategory.Blackjack, "Double Dynasty", "Win 1,000 doubled Blackjack hands.", ChallengeMetric.BlackjackDoubleWins, 1000, 3000),
        E("bj-wager-1000000", ChallengeCategory.Blackjack, "Million Dollar Table", "Wager $1,000,000 at Blackjack in total.", ChallengeMetric.BlackjackWagered, 1000000, 2500),
        E("reels-spins-10000", ChallengeCategory.MidnightReels, "Clockwork Legend", "Play 10,000 separately tracked Midnight Reels spins.", ChallengeMetric.ReelsSpins, 10000, 2500),
        E("reels-wins-2500", ChallengeCategory.MidnightReels, "Mechanical Master", "Win 2,500 separately tracked Midnight Reels spins.", ChallengeMetric.ReelsWins, 2500, 3000),
        E("reels-return-1000000", ChallengeCategory.MidnightReels, "Golden Tray", "Return $1,000,000 from separately tracked Midnight Reels spins.", ChallengeMetric.ReelsReturned, 1000000, 2500),
        E("reels-wager-1000000", ChallengeCategory.MidnightReels, "Reel Institution", "Wager $1,000,000 on separately tracked Midnight Reels spins.", ChallengeMetric.ReelsWagered, 1000000, 2500),
        E("manor-spins-10000", ChallengeCategory.MidnightManor, "Permanent Resident", "Play 10,000 Midnight Manor paid spins.", ChallengeMetric.ManorPaidSpins, 10000, 2500),
        E("manor-free-5000", ChallengeCategory.MidnightManor, "Endless Haunted Night", "Play 5,000 Manor free spins.", ChallengeMetric.ManorFreeSpins, 5000, 2500),
        E("manor-exits-100", ChallengeCategory.MidnightManor, "Master of the Gate", "Reach the Moonlit Gate 100 times.", ChallengeMetric.ManorExits, 100, 3000),
        E("manor-max-exits-10", ChallengeCategory.MidnightManor, "Impossible Ten", "Escape through a 1,000× Moonlit Gate 10 times.", ChallengeMetric.ManorMaximumExits, 10, 4000),
        E("neon-paid-10000", ChallengeCategory.NeonVault, "Vault Institution", "Play 10,000 separately tracked Neon Vault paid spins.", ChallengeMetric.NeonPaidSpins, 10000, 2500),
        E("neon-free-5000", ChallengeCategory.NeonVault, "Unlimited Access", "Play 5,000 separately tracked Neon Vault free spins.", ChallengeMetric.NeonFreeSpins, 5000, 2500),
        E("neon-grand-10", ChallengeCategory.NeonVault, "Grand Vault Legend", "Land 10 separately tracked Neon Vault Grand jackpots.", ChallengeMetric.NeonGrandJackpots, 10, 4000),
        E("neon-total-blackouts-100", ChallengeCategory.NeonVault, "Beyond the Blackout", "Encounter 100 separately tracked Neon Vault Total Blackouts.", ChallengeMetric.NeonTotalBlackouts, 100, 3500),
        E("neon-overrides-250", ChallengeCategory.NeonVault, "System Architect", "Activate 250 separately tracked Neon Vault security overrides.", ChallengeMetric.NeonSecurityOverrides, 250, 3000),
        E("nine-paid-10000", ChallengeCategory.NineLives, "Nine Lives Institution", "Play 10,000 separately tracked Nine Lives paid spins.", ChallengeMetric.NineLivesPaidSpins, 10000, 2500),
        E("nine-bonus-5000", ChallengeCategory.NineLives, "Endless Rooftop", "Play 5,000 Nine Lives bonus spins.", ChallengeMetric.NineLivesBonusSpins, 5000, 3000),
        E("nine-pounce-500", ChallengeCategory.NineLives, "Predator Instinct", "Trigger 500 Pounce Wild transformations.", ChallengeMetric.NineLivesPounceWilds, 500, 3000),
        E("nine-frenzy-10", ChallengeCategory.NineLives, "Nine Times Nine", "Land 10 Full Grid Frenzies.", ChallengeMetric.NineLivesFullGridFrenzies, 10, 5000),
        E("lifetime-xp-1000000", ChallengeCategory.Lifetime, "Casino Institution", "Earn 1,000,000 lifetime player XP across all games.", ChallengeMetric.LifetimeExperience, 1000000, 4000),
        E("lifetime-hours-250", ChallengeCategory.Lifetime, "The Long Night", "Record 250 hours of total casino playtime.", ChallengeMetric.LifetimePlaytimeSeconds, 900000, 3000),
        E("lifetime-wager-10000000", ChallengeCategory.Lifetime, "Ten Million in Motion", "Wager $10,000,000 across all games, including preserved pooled slot history.", ChallengeMetric.LifetimeWagered, 10000000, 4000),
        E("lifetime-plays-100000", ChallengeCategory.Lifetime, "A Hundred Thousand Nights", "Complete 100,000 Blackjack rounds, dice games, Sudoku puzzles and slot spins combined, including preserved pooled slot history.", ChallengeMetric.LifetimePlays, 100000, 5000),
        E("prestige-3", ChallengeCategory.Prestige, "Third Time Crowned", "Reach Prestige 3.", ChallengeMetric.PrestigeCount, 3, 2500),
        E("prestige-5", ChallengeCategory.Prestige, "Fivefold Legend", "Reach Prestige 5.", ChallengeMetric.PrestigeCount, 5, 4000),
        E("prestige-10", ChallengeCategory.Prestige, "Eternal High Roller", "Reach Prestige 10.", ChallengeMetric.PrestigeCount, 10, 7500),
    });

    public static IReadOnlyList<ChallengeDefinition> All => Challenges;

    public static IReadOnlyList<ChallengeDefinition> For(ChallengeCategory category) =>
        Challenges.Where(x => x.Category == category).ToArray();

    public static ChallengeDefinition? TryGet(string id) =>
        Challenges.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    public static decimal Progress(PlayerProfileState profile, ChallengeDefinition challenge)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(challenge);
        StatisticsSnapshot blackjack = profile.LifetimeStatistics;
        SlotStatistics reels = profile.Slots;
        Manor.ManorStatistics manor = profile.Manor;
        NeonVaultStatistics neon = profile.NeonVault;
        NineLivesStatistics nine = profile.NineLives;
        SlotStatistics legacy = profile.LegacyPooledSlots;

        return challenge.Metric switch
        {
            ChallengeMetric.BlackjackRounds => blackjack.RoundsPlayed,
            ChallengeMetric.BlackjackHands => blackjack.HandsPlayed,
            ChallengeMetric.BlackjackWins => blackjack.HandsWon,
            ChallengeMetric.BlackjackBlackjacks => blackjack.Blackjacks,
            ChallengeMetric.BlackjackDoubles => blackjack.Doubles,
            ChallengeMetric.BlackjackDoubleWins => blackjack.DoubleWins,
            ChallengeMetric.BlackjackSplits => blackjack.Splits,
            ChallengeMetric.BlackjackSurrenders => blackjack.Surrenders,
            ChallengeMetric.BlackjackInsurance => blackjack.InsuranceTaken,
            ChallengeMetric.BlackjackWagered => blackjack.TotalWagered,
            ChallengeMetric.BlackjackNetProfit => Math.Max(0, blackjack.NetProfit),
            ChallengeMetric.BlackjackBiggestWin => blackjack.BiggestWin,
            ChallengeMetric.BlackjackHighestBankroll => blackjack.HighestBankroll,
            ChallengeMetric.BlackjackBestWinStreak => blackjack.BestWinStreak,

            ChallengeMetric.ReelsSpins => reels.Spins,
            ChallengeMetric.ReelsWins => reels.Wins,
            ChallengeMetric.ReelsWagered => reels.TotalWagered,
            ChallengeMetric.ReelsReturned => reels.TotalReturned,
            ChallengeMetric.ReelsBiggestPayout => reels.BiggestPayout,
            ChallengeMetric.ReelsNetProfit => Math.Max(0, reels.NetProfit),

            ChallengeMetric.ManorPaidSpins => manor.PaidSpins,
            ChallengeMetric.ManorFreeSpins => manor.FreeSpins,
            ChallengeMetric.ManorWagered => manor.TotalWagered,
            ChallengeMetric.ManorReturned => manor.TotalReturned,
            ChallengeMetric.ManorBigWins => manor.BigWins,
            ChallengeMetric.ManorFreeSessions => manor.FreeSpinsTriggered,
            ChallengeMetric.ManorBonuses => manor.ManorBonuses,
            ChallengeMetric.ManorExits => manor.Exits,
            ChallengeMetric.ManorMaximumExits => manor.MaximumExits,
            ChallengeMetric.ManorCatches => manor.Catches,
            ChallengeMetric.ManorCashOuts => manor.CashOuts,
            ChallengeMetric.ManorBestMultiplier => manor.BestManorMultiplier,

            ChallengeMetric.NeonSpins => neon.Spins,
            ChallengeMetric.NeonPaidSpins => neon.PaidSpins,
            ChallengeMetric.NeonFreeSpins => neon.FreeSpins,
            ChallengeMetric.NeonWins => neon.Wins,
            ChallengeMetric.NeonWagered => neon.TotalWagered,
            ChallengeMetric.NeonReturned => neon.TotalReturned,
            ChallengeMetric.NeonBiggestPayout => neon.BiggestPayout,
            ChallengeMetric.NeonFreeSpinSessions => neon.FreeSpinSessions,
            ChallengeMetric.NeonJackpots => neon.Jackpots,
            ChallengeMetric.NeonGrandJackpots => neon.GrandJackpots,
            ChallengeMetric.NeonBlackouts => neon.Blackouts,
            ChallengeMetric.NeonTotalBlackouts => neon.TotalBlackouts,
            ChallengeMetric.NeonSecurityOverrides => neon.SecurityOverrides,
            ChallengeMetric.NeonBestMultiplier => neon.BestPayoutMultiplier,

            ChallengeMetric.NineLivesSpins => nine.Spins,
            ChallengeMetric.NineLivesPaidSpins => nine.PaidSpins,
            ChallengeMetric.NineLivesBonusSpins => nine.BonusSpins,
            ChallengeMetric.NineLivesWins => nine.Wins,
            ChallengeMetric.NineLivesWagered => nine.TotalWagered,
            ChallengeMetric.NineLivesReturned => nine.TotalReturned,
            ChallengeMetric.NineLivesBiggestPayout => nine.BiggestPayout,
            ChallengeMetric.NineLivesBonusSessions => nine.BonusSessions,
            ChallengeMetric.NineLivesBonusSpinsAwarded => nine.BonusSpinsAwarded,
            ChallengeMetric.NineLivesPounceWilds => nine.PounceWilds,
            ChallengeMetric.NineLivesFullGridFrenzies => nine.FullGridFrenzies,
            ChallengeMetric.NineLivesBestMultiplier => nine.BestPayoutMultiplier,

            ChallengeMetric.DiceGames => profile.Kniffel.CompletedGames,
            ChallengeMetric.DiceBestScore => profile.Kniffel.BestScore,
            ChallengeMetric.DiceUpperBonuses => profile.Kniffel.UpperBonuses,
            ChallengeMetric.DiceFiveOfAKinds => profile.Kniffel.FiveOfAKinds,
            ChallengeMetric.DiceFullHouses => profile.Kniffel.FullHouses,
            ChallengeMetric.DiceLargeStraights => profile.Kniffel.LargeStraights,
            ChallengeMetric.DiceCasinoGames => profile.Kniffel.CasinoGamesCompleted,
            ChallengeMetric.DiceCasinoWins => profile.Kniffel.CasinoWins,
            ChallengeMetric.DicePodiums => profile.Kniffel.PodiumFinishes,
            ChallengeMetric.DiceWagered => profile.Kniffel.TotalWagered,
            ChallengeMetric.DiceReturned => profile.Kniffel.TotalReturned,
            ChallengeMetric.DiceBiggestPayout => profile.Kniffel.BiggestPayout,

            ChallengeMetric.SudokuCompleted => profile.Sudoku.CompletedPuzzles,
            ChallengeMetric.SudokuDailyCompleted => profile.Sudoku.DailyCompleted,
            ChallengeMetric.SudokuNoHintCompleted => profile.Sudoku.NoHintCompletions,
            ChallengeMetric.SudokuExpertCompleted => profile.Sudoku.ExpertCompleted,
            ChallengeMetric.SudokuMasterCompleted => profile.Sudoku.MasterCompleted,

            ChallengeMetric.LifetimeExperience => profile.LifetimeExperience,
            ChallengeMetric.LifetimePlaytimeSeconds => profile.TotalPlaytimeSeconds,
            ChallengeMetric.LifetimeWagered => blackjack.TotalWagered + reels.TotalWagered + neon.TotalWagered + nine.TotalWagered
                + manor.TotalWagered + legacy.TotalWagered + profile.Kniffel.TotalWagered,
            ChallengeMetric.LifetimePlays => (decimal)blackjack.RoundsPlayed + reels.Spins + neon.Spins + nine.Spins
                + manor.PaidSpins + manor.FreeSpins + legacy.Spins + profile.Kniffel.CompletedGames + profile.Sudoku.CompletedPuzzles,
            ChallengeMetric.PrestigeCount => profile.Prestige,
            _ => 0,
        };
    }

    public static bool Complete(PlayerProfileState profile, ChallengeDefinition challenge) =>
        Progress(profile, challenge) >= challenge.Goal;

    private static ChallengeDefinition C(
        string id,
        ChallengeCategory category,
        string name,
        string description,
        ChallengeMetric metric,
        decimal goal,
        long coins) => new(id, category, name, description, metric, goal, coins);

    private static ChallengeDefinition E(
        string id,
        ChallengeCategory category,
        string name,
        string description,
        ChallengeMetric metric,
        decimal goal,
        long coins) => new(id, category, name, description, metric, goal, coins, IsEndgame: true);
}
