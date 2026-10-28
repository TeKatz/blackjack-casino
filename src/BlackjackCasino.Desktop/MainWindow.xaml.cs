using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

public partial class MainWindow : Window
{
    private enum PlayMode
    {
        Solo,
        FullTable,
    }

    private static readonly GameRules TableRules = new()
    {
        DeckCount = 6,
        DealerHitsSoft17 = false,
        DealerPeeksForBlackjack = true,
        AllowInsurance = true,
        AllowLateSurrender = true,
        DoubleAfterSplit = true,
        ResplitAces = false,
        HitSplitAces = false,
        MaxSplitHands = 4,
        BlackjackPayout = 1.5m,
        Penetration = 0.75,
    };

    private static readonly (string Name, decimal Bet)[] AiRoster =
    [
        ("Alex", 25m),
        ("Morgan", 50m),
        ("Casey", 10m),
        ("Jordan", 25m),
        ("Riley", 100m),
    ];

    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    private CasinoSession session;
    private PlayMode playMode = PlayMode.Solo;
    private int aiSeatCount = 3;
    private decimal selectedBet = 25m;
    private UpdateManifest? pendingManifest;

    public MainWindow()
    {
        InitializeComponent();
        session = CreateSession(PlayerStateStore.LoadBankroll());
        VersionText.Text = AppVersion.Current.ToString();

        GameModeComboBox.SelectionChanged += GameModeComboBox_SelectionChanged;
        AiSeatCountComboBox.SelectionChanged += AiSeatCountComboBox_SelectionChanged;

        Loaded += async (_, _) =>
        {
            RefreshGameUi();
            await CheckForUpdatesAsync(silent: true);
        };
        Closed += (_, _) =>
        {
            PlayerStateStore.SaveBankroll(session.Bankroll);
            httpClient.Dispose();
        };
    }

    private UpdateChannel SelectedUpdateChannel => UpdateChannelComboBox.SelectedIndex == 1
        ? UpdateChannel.Development
        : UpdateChannel.Stable;

    private CasinoSession CreateSession(decimal bankroll)
    {
        IReadOnlyList<AiPlayerProfile> aiPlayers = playMode == PlayMode.FullTable
            ? BuildAiProfiles(aiSeatCount)
            : [];

        return new CasinoSession(TableRules, bankroll, aiPlayers: aiPlayers);
    }

    private static IReadOnlyList<AiPlayerProfile> BuildAiProfiles(int count)
    {
        return AiRoster.Take(count)
            .Select(entry => new AiPlayerProfile(entry.Name, entry.Bet))
            .ToArray();
    }

    private void GameModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || session.RoundInProgress)
        {
            return;
        }

        PlayMode requested = GameModeComboBox.SelectedIndex == 1
            ? PlayMode.FullTable
            : PlayMode.Solo;

        if (requested == playMode)
        {
            return;
        }

        SwitchTable(requested, aiSeatCount);
    }

    private void AiSeatCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || session.RoundInProgress || playMode != PlayMode.FullTable)
        {
            return;
        }

        int requested = Math.Clamp(AiSeatCountComboBox.SelectedIndex + 1, 1, 5);
        if (requested == aiSeatCount)
        {
            return;
        }

        SwitchTable(playMode, requested);
    }

    private void SwitchTable(PlayMode mode, int seats)
    {
        decimal bankroll = session.Bankroll;
        PlayerStateStore.SaveBankroll(bankroll);

        playMode = mode;
        aiSeatCount = seats;
        session = CreateSession(bankroll);
        selectedBet = Math.Min(selectedBet, bankroll);

        StatusText.Text = playMode == PlayMode.Solo
            ? "Solo table ready. Place a bet to begin."
            : $"Full table ready with {aiSeatCount} AI seat{(aiSeatCount == 1 ? string.Empty : "s")}. Place a bet to begin.";

        RefreshGameUi();
    }

    private void Chip_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress || sender is not Button { Tag: string raw } || !decimal.TryParse(raw, CultureInfo.InvariantCulture, out decimal chip))
        {
            return;
        }

        selectedBet = Math.Min(session.Bankroll, selectedBet + chip);
        RefreshGameUi();
    }

    private void ClearBet_Click(object sender, RoutedEventArgs e)
    {
        if (!session.RoundInProgress)
        {
            selectedBet = 0m;
            RefreshGameUi();
        }
    }

    private void Deal_Click(object sender, RoutedEventArgs e) => Deal();

    private void Deal()
    {
        if (session.RoundInProgress || selectedBet <= 0m || selectedBet > session.Bankroll)
        {
            return;
        }

        try
        {
            session.StartRound(selectedBet);
            HandleRoundStateAfterMutation(playMode == PlayMode.FullTable ? "Cards dealt around the table." : "Cards dealt.");
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void Hit_Click(object sender, RoutedEventArgs e) => PerformAction(PlayerAction.Hit);

    private void Stand_Click(object sender, RoutedEventArgs e) => PerformAction(PlayerAction.Stand);

    private void Double_Click(object sender, RoutedEventArgs e) => PerformAction(PlayerAction.DoubleDown);

    private void Split_Click(object sender, RoutedEventArgs e) => PerformAction(PlayerAction.Split);

    private void Surrender_Click(object sender, RoutedEventArgs e) => PerformAction(PlayerAction.Surrender);

    private void PerformAction(PlayerAction action)
    {
        try
        {
            session.Act(action);
            HandleRoundStateAfterMutation(action switch
            {
                PlayerAction.Hit => "Hit.",
                PlayerAction.Stand => "Stand.",
                PlayerAction.DoubleDown => "Double down.",
                PlayerAction.Split => "Split.",
                PlayerAction.Surrender => "Surrender.",
                _ => string.Empty,
            });
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void TakeInsurance_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            session.TakeInsurance();
            HandleRoundStateAfterMutation("Insurance taken.");
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void DeclineInsurance_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            session.DeclineInsurance();
            HandleRoundStateAfterMutation("No insurance.");
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void ResetBankroll_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            return;
        }

        MessageBoxResult answer = MessageBox.Show(
            this,
            "Reset your local bankroll to $1,000? This only affects play-money chips.",
            "Reset bankroll",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        session.ResetBankroll();
        selectedBet = 25m;
        PlayerStateStore.SaveBankroll(session.Bankroll);
        StatusText.Text = "Bankroll reset. Place a bet to begin.";
        RefreshGameUi();
    }

    private void HandleRoundStateAfterMutation(string fallbackStatus)
    {
        BlackjackRound? round = session.CurrentRound;
        if (round is null)
        {
            StatusText.Text = fallbackStatus;
            RefreshGameUi();
            return;
        }

        StatusText.Text = round.Phase switch
        {
            RoundPhase.InsuranceDecision => "Dealer shows an Ace. Insurance is available for half your original bet.",
            RoundPhase.PlayerTurn when round.PlayerHands.Count > 1 => $"Playing hand {round.ActiveHandIndex + 1} of {round.PlayerHands.Count}.",
            RoundPhase.PlayerTurn => fallbackStatus,
            RoundPhase.Settled => BuildSettlementMessage(round),
            _ => fallbackStatus,
        };

        if (round.Phase == RoundPhase.Settled)
        {
            PlayerStateStore.SaveBankroll(session.Bankroll);
            selectedBet = Math.Min(selectedBet, session.Bankroll);
        }

        RefreshGameUi();
    }

    private string BuildSettlementMessage(BlackjackRound round)
    {
        RoundResult result = round.Result!;
        string headline = result.TotalNet switch
        {
            > 0m => $"You win {Money(result.TotalNet)}",
            < 0m => $"You lose {Money(Math.Abs(result.TotalNet))}",
            _ => "Push",
        };

        if (result.Hands.Count == 1)
        {
            return $"{headline} · {OutcomeText(result.Hands[0].Outcome)}";
        }

        string hands = string.Join(" · ", result.Hands.Select(x => $"H{x.HandIndex + 1}: {OutcomeText(x.Outcome)} {SignedMoney(x.Net)}"));
        return $"{headline} · {hands}";
    }

    private void RefreshGameUi()
    {
        BankrollText.Text = Money(session.Bankroll);
        BetText.Text = Money(selectedBet);
        ShoeText.Text = $"#{session.ShoeNumber} · {session.CardsRemaining} cards";
        TableModeText.Text = playMode == PlayMode.Solo
            ? "Solo table"
            : $"Full table · {aiSeatCount} AI seat{(aiSeatCount == 1 ? string.Empty : "s")}";

        BlackjackRound? round = session.CurrentRound;
        bool betting = !session.RoundInProgress;
        bool canDeal = betting && selectedBet > 0m && selectedBet <= session.Bankroll;

        DealButton.IsEnabled = canDeal;
        ClearBetButton.IsEnabled = betting && selectedBet > 0m;
        ResetBankrollButton.IsEnabled = betting;
        GameModeComboBox.IsEnabled = betting;
        AiSeatCountComboBox.IsEnabled = betting;
        AiSettingsPanel.Visibility = playMode == PlayMode.FullTable ? Visibility.Visible : Visibility.Collapsed;

        DealerCardsPanel.Children.Clear();
        PlayerHandsPanel.Children.Clear();
        AiTableSeatsPanel.Children.Clear();

        RenderAiSeats(round);

        if (round is null)
        {
            DealerValueText.Text = string.Empty;
            PlayerHandsPanel.Children.Add(BuildEmptySeat());
            SetActionAvailability(new HashSet<PlayerAction>());
            InsurancePanel.Visibility = Visibility.Collapsed;
            return;
        }

        RenderDealer(round);
        RenderPlayerHands(round);
        SetActionAvailability(session.GetAvailableActions());
        InsurancePanel.Visibility = round.Phase == RoundPhase.InsuranceDecision
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RenderAiSeats(BlackjackRound? round)
    {
        if (playMode != PlayMode.FullTable)
        {
            AiSeatsScrollViewer.Visibility = Visibility.Collapsed;
            return;
        }

        AiSeatsScrollViewer.Visibility = Visibility.Visible;

        if (round is null)
        {
            foreach (AiPlayerProfile profile in BuildAiProfiles(aiSeatCount))
            {
                AiTableSeatsPanel.Children.Add(BuildAiPlaceholder(profile));
            }
            return;
        }

        foreach (AiSeatState seat in round.AiSeats)
        {
            AiTableSeatsPanel.Children.Add(BuildAiSeat(seat, round.Phase == RoundPhase.Settled));
        }
    }

    private static Border BuildAiPlaceholder(AiPlayerProfile profile)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = profile.Name.ToUpperInvariant(),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brush("#9EB1A4"),
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"Bet {Money(profile.Bet)}\nWaiting for deal",
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = Brush("#789484"),
        });

        return new Border
        {
            Width = 172,
            Height = 108,
            Margin = new Thickness(5),
            Padding = new Thickness(10),
            Background = Brush("#0B2117"),
            BorderBrush = Brush("#31533F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Child = stack,
        };
    }

    private static Border BuildAiSeat(AiSeatState seat, bool settled)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"{seat.Name.ToUpperInvariant()} · {Money(seat.InitialBet)}",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brush("#9EB1A4"),
        });

        for (int i = 0; i < seat.Hands.Count; i++)
        {
            BlackjackHand hand = seat.Hands[i];
            var cards = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 2),
            };

            foreach (Card card in hand.Cards)
            {
                cards.Children.Add(CreateCard(card, compact: true));
            }
            stack.Children.Add(cards);

            string value = hand.IsSurrendered ? "Surrendered" : FormatHandValue(hand);
            if (settled && i < seat.Results.Count)
            {
                HandResult result = seat.Results[i];
                value += $" · {OutcomeText(result.Outcome)} {SignedMoney(result.Net)}";
            }

            stack.Children.Add(new TextBlock
            {
                Text = seat.Hands.Count > 1 ? $"H{i + 1}: {value}" : value,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brush("#C8D5CC"),
            });
        }

        return new Border
        {
            MinWidth = 172,
            Margin = new Thickness(5),
            Padding = new Thickness(9),
            Background = Brush("#0B2117"),
            BorderBrush = settled && seat.TotalNet > 0 ? Brush("#6A8F55") : Brush("#31533F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Child = stack,
        };
    }

    private void RenderDealer(BlackjackRound round)
    {
        bool hideHoleCard = round.Phase is RoundPhase.InsuranceDecision or RoundPhase.PlayerTurn;

        for (int i = 0; i < round.DealerHand.Cards.Count; i++)
        {
            DealerCardsPanel.Children.Add(CreateCard(round.DealerHand.Cards[i], faceDown: hideHoleCard && i == 1));
        }

        DealerValueText.Text = hideHoleCard
            ? $"Showing {VisibleCardValue(round.DealerHand.Cards[0])}"
            : FormatHandValue(round.DealerHand);
    }

    private void RenderPlayerHands(BlackjackRound round)
    {
        for (int i = 0; i < round.PlayerHands.Count; i++)
        {
            BlackjackHand hand = round.PlayerHands[i];
            bool active = round.Phase == RoundPhase.PlayerTurn && i == round.ActiveHandIndex;

            var outer = new Border
            {
                Background = Brush("#0B2117"),
                BorderBrush = active ? Brush("#D4A84F") : Brush("#31533F"),
                BorderThickness = new Thickness(active ? 2 : 1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(8),
                Padding = new Thickness(14),
                MinWidth = 230,
            };

            var stack = new StackPanel();
            string label = round.PlayerHands.Count > 1 ? $"HAND {i + 1}" : "YOUR HAND";
            stack.Children.Add(new TextBlock
            {
                Text = $"{label}  ·  {Money(hand.Bet)}",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = active ? Brush("#F0CD7A") : Brush("#9EB1A4"),
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            var cards = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 5),
            };
            foreach (Card card in hand.Cards)
            {
                cards.Children.Add(CreateCard(card));
            }
            stack.Children.Add(cards);

            string value = hand.IsSurrendered ? "Surrendered" : FormatHandValue(hand);
            if (round.Phase == RoundPhase.Settled && round.Result is not null)
            {
                HandResult result = round.Result.Hands[i];
                value += $"  ·  {OutcomeText(result.Outcome)} {SignedMoney(result.Net)}";
            }

            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 13,
                Foreground = Brush("#D4DED7"),
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            outer.Child = stack;
            PlayerHandsPanel.Children.Add(outer);
        }
    }

    private static Border BuildEmptySeat()
    {
        return new Border
        {
            Width = 250,
            Height = 150,
            BorderBrush = Brush("#31533F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(8),
            Child = new TextBlock
            {
                Text = "YOUR SEAT\n\nPlace a bet and deal",
                TextAlignment = TextAlignment.Center,
                Foreground = Brush("#789484"),
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }

    private static Border CreateCard(Card card, bool faceDown = false, bool compact = false)
    {
        double width = compact ? 48 : 76;
        double height = compact ? 68 : 108;
        double rankSize = compact ? 13 : 20;
        double suitSize = compact ? 22 : 34;

        var border = new Border
        {
            Width = width,
            Height = height,
            Margin = new Thickness(compact ? 2 : 4),
            CornerRadius = new CornerRadius(compact ? 5 : 7),
            BorderThickness = new Thickness(1),
        };

        if (faceDown)
        {
            border.Background = Brush("#173A2A");
            border.BorderBrush = Brush("#D4A84F");
            border.Child = new TextBlock
            {
                Text = "♠",
                FontSize = compact ? 24 : 36,
                FontWeight = FontWeights.Bold,
                Foreground = Brush("#D4A84F"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            return border;
        }

        bool red = card.Suit is Suit.Hearts or Suit.Diamonds;
        Brush ink = red ? Brush("#B82931") : Brush("#171A18");
        border.Background = Brushes.WhiteSmoke;
        border.BorderBrush = Brush("#D8D8D2");

        var grid = new Grid { Margin = new Thickness(compact ? 5 : 8) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.Children.Add(new TextBlock
        {
            Text = RankText(card.Rank),
            FontSize = rankSize,
            FontWeight = FontWeights.Bold,
            Foreground = ink,
        });
        var suit = new TextBlock
        {
            Text = SuitText(card.Suit),
            FontSize = suitSize,
            Foreground = ink,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetRow(suit, 1);
        grid.Children.Add(suit);
        border.Child = grid;
        return border;
    }

    private void SetActionAvailability(IReadOnlySet<PlayerAction> actions)
    {
        HitButton.IsEnabled = actions.Contains(PlayerAction.Hit);
        StandButton.IsEnabled = actions.Contains(PlayerAction.Stand);
        DoubleButton.IsEnabled = actions.Contains(PlayerAction.DoubleDown);
        SplitButton.IsEnabled = actions.Contains(PlayerAction.Split);
        SurrenderButton.IsEnabled = actions.Contains(PlayerAction.Surrender);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is ComboBox)
        {
            return;
        }

        PlayerAction? action = e.Key switch
        {
            Key.H => PlayerAction.Hit,
            Key.S => PlayerAction.Stand,
            Key.D => PlayerAction.DoubleDown,
            Key.P => PlayerAction.Split,
            Key.R => PlayerAction.Surrender,
            _ => null,
        };

        if (action is PlayerAction requested && session.GetAvailableActions().Contains(requested))
        {
            PerformAction(requested);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && DealButton.IsEnabled)
        {
            Deal();
            e.Handled = true;
        }
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(silent: false);
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (pendingManifest is null)
        {
            return;
        }

        try
        {
            SetUpdateBusy(true);
            UpdateStatusText.Text = "Downloading update…";

            string updateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlackjackCasino",
                "Updates");

            var progress = new Progress<double>(value =>
                UpdateStatusText.Text = $"Downloading update… {value:P0}");

            var downloader = new PackageDownloader(httpClient);
            string packagePath = await downloader.DownloadAndVerifyAsync(
                pendingManifest.Package,
                updateDirectory,
                progress);

            PlayerStateStore.SaveBankroll(session.Bankroll);
            UpdateStatusText.Text = "Update verified. Restarting…";
            UpdaterLauncher.Launch(packagePath, pendingManifest.Package.Sha256);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Update failed: {ex.Message}";
            SetUpdateBusy(false);
        }
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        UpdateChannel channel = SelectedUpdateChannel;

        try
        {
            SetUpdateBusy(true);
            if (!silent)
            {
                UpdateStatusText.Text = $"Checking {channel}…";
            }

            var source = new HttpManifestUpdateSource(httpClient, GetManifestUri(channel));
            var client = new UpdateClient(source, channel.ToManifestValue());
            UpdateCheckResult result = await client.CheckAsync(AppVersion.Current);

            if (!result.UpdateAvailable)
            {
                pendingManifest = null;
                UpdateStatusText.Text = $"{channel}: up to date";
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            SemanticVersion minimumUpdater = SemanticVersion.Parse(result.Manifest.MinimumUpdaterVersion);
            if (minimumUpdater > AppVersion.Current)
            {
                pendingManifest = null;
                UpdateStatusText.Text = $"Update {result.LatestVersion} requires a newer updater.";
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            pendingManifest = result.Manifest;
            UpdateStatusText.Text = $"{channel}: version {result.LatestVersion} is available";
            InstallUpdateButton.IsEnabled = UpdaterLauncher.IsAvailable;

            if (!UpdaterLauncher.IsAvailable)
            {
                UpdateStatusText.Text += " (updater helper missing in this development build)";
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            pendingManifest = null;
            UpdateStatusText.Text = $"No {channel} release channel has been published yet";
            InstallUpdateButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            pendingManifest = null;
            InstallUpdateButton.IsEnabled = false;
            UpdateStatusText.Text = silent
                ? $"{channel} update check unavailable"
                : $"Update check failed: {ex.Message}";
        }
        finally
        {
            SetUpdateBusy(false);
        }
    }

    private static Uri GetManifestUri(UpdateChannel channel) => channel switch
    {
        UpdateChannel.Stable => new Uri("https://github.com/TeKatz/blackjack-casino/releases/latest/download/update-manifest.json"),
        UpdateChannel.Development => new Uri("https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json"),
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    private void SetUpdateBusy(bool busy)
    {
        CheckUpdateButton.IsEnabled = !busy;
        UpdateChannelComboBox.IsEnabled = !busy;
        if (busy)
        {
            InstallUpdateButton.IsEnabled = false;
        }
        else if (pendingManifest is not null)
        {
            InstallUpdateButton.IsEnabled = UpdaterLauncher.IsAvailable;
        }
    }

    private static string FormatHandValue(BlackjackHand hand)
    {
        HandValue value = hand.Value;
        if (value.IsBust)
        {
            return $"{value.Total} · Bust";
        }

        if (hand.IsNaturalBlackjack)
        {
            return "21 · Blackjack";
        }

        return value.IsSoft ? $"Soft {value.Total}" : value.Total.ToString(CultureInfo.InvariantCulture);
    }

    private static int VisibleCardValue(Card card) => card.Rank == Rank.Ace ? 11 : card.HardValue;

    private static string OutcomeText(HandOutcome outcome) => outcome switch
    {
        HandOutcome.Win => "Win",
        HandOutcome.Loss => "Loss",
        HandOutcome.Push => "Push",
        HandOutcome.Blackjack => "Blackjack",
        HandOutcome.Surrender => "Surrender",
        _ => outcome.ToString(),
    };

    private static string Money(decimal amount) => $"${amount:N2}";

    private static string SignedMoney(decimal amount) => amount switch
    {
        > 0m => $"+{Money(amount)}",
        < 0m => $"-{Money(Math.Abs(amount))}",
        _ => "$0.00",
    };

    private static string RankText(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(CultureInfo.InvariantCulture),
    };

    private static string SuitText(Suit suit) => suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?",
    };

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
