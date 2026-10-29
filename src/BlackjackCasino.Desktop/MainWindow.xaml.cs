using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

public partial class MainWindow : Window
{
    private sealed record TablePalette(
        string Window,
        string Header,
        string Table,
        string Side,
        string Seat,
        string Border,
        string Action,
        string MutedButton,
        string Accent,
        string AccentLight,
        string MutedText,
        string SoftText,
        string Status,
        string AiWin);

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

    private AppPreferences preferences;
    private CasinoSession session;
    private PlayModePreference playMode;
    private int aiSeatCount;
    private decimal selectedBet;
    private UpdateManifest? pendingManifest;
    private bool suppressPreferenceEvents;
    private bool animateNextRender;
    private int animationSequence;
    private bool isFullscreen;
    private WindowState previousWindowState;
    private WindowStyle previousWindowStyle;
    private ResizeMode previousResizeMode;
    private Rect previousWindowBounds;

    private readonly PlayerProfileState? initialProfile;
    private readonly bool testSession;
    public MainWindow() : this(null,null) { }
    internal MainWindow(AppPreferences? initialPreferences,PlayerProfileState? profile)
    {
        initialProfile=profile; testSession=initialPreferences is not null;
        InitializeComponent();

        string? manorRecoveryNotice = null;
        try { if(!testSession) new ManorPersistence(ManorPersistence.DefaultDirectory).Recover(); }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        { manorRecoveryNotice = "Manor checkpoint could not be recovered: " + ex.Message; }
        preferences = initialPreferences ?? PlayerStateStore.Load();
        MenuAudio.Enabled = preferences.SoundEnabled;
        MenuAudio.Attach(this, () => SoundEnabled);
        playMode = preferences.PlayMode;
        aiSeatCount = preferences.AiSeatCount;
        selectedBet = Math.Min(preferences.SelectedBet, preferences.Bankroll);
        session = CreateSession(preferences.Bankroll);
        VersionText.Text = AppVersion.Current.ToString();

        suppressPreferenceEvents = true;
        Width = preferences.WindowWidth;
        Height = preferences.WindowHeight;
        WindowState = preferences.WindowMaximized ? WindowState.Maximized : WindowState.Normal;
        GameModeComboBox.SelectedIndex = playMode == PlayModePreference.FullTable ? 1 : 0;
        AiSeatCountComboBox.SelectedIndex = aiSeatCount - 1;
        ThemeComboBox.SelectedIndex = (int)preferences.Theme;
        SoundCheckBox.IsChecked = preferences.SoundEnabled;
        AnimationsCheckBox.IsChecked = preferences.AnimationsEnabled;

        UpdateChannel initialChannel = preferences.UpdateChannel
            ?? (AppVersion.Current.PreRelease is null ? UpdateChannel.Stable : UpdateChannel.Development);
        UpdateChannelComboBox.SelectedIndex = initialChannel == UpdateChannel.Development ? 1 : 0;
        preferences = preferences with { UpdateChannel = initialChannel };
        suppressPreferenceEvents = false;

        ApplyTheme(preferences.Theme);

        GameModeComboBox.SelectionChanged += GameModeComboBox_SelectionChanged;
        AiSeatCountComboBox.SelectionChanged += AiSeatCountComboBox_SelectionChanged;
        ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
        UpdateChannelComboBox.SelectionChanged += UpdateChannelComboBox_SelectionChanged;
        SoundCheckBox.Checked += PresentationSettingChanged;
        SoundCheckBox.Unchecked += PresentationSettingChanged;
        AnimationsCheckBox.Checked += PresentationSettingChanged;
        AnimationsCheckBox.Unchecked += PresentationSettingChanged;

        Loaded += async (_, _) =>
        {
            RefreshGameUi();
            if (manorRecoveryNotice is not null) StatusText.Text = manorRecoveryNotice;
            if(!testSession) await CheckForUpdatesAsync(silent: true);
        };
        Closed += (_, _) =>
        {
            SavePreferences();
            httpClient.Dispose();
        };
    }

    private bool SoundEnabled => SoundCheckBox.IsChecked == true;

    private bool AnimationsEnabled => AnimationsCheckBox.IsChecked == true;

    private TableThemePreference SelectedTheme => ThemeComboBox.SelectedIndex switch
    {
        1 => TableThemePreference.MidnightBlue,
        2 => TableThemePreference.Burgundy,
        _ => TableThemePreference.ClassicGreen,
    };

    private UpdateChannel SelectedUpdateChannel => UpdateChannelComboBox.SelectedIndex == 1
        ? UpdateChannel.Development
        : UpdateChannel.Stable;

    private CasinoSession CreateSession(decimal bankroll)
    {
        IReadOnlyList<AiPlayerProfile> aiPlayers = playMode == PlayModePreference.FullTable
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
        if (suppressPreferenceEvents || !IsLoaded || session.RoundInProgress)
        {
            return;
        }

        PlayModePreference requested = GameModeComboBox.SelectedIndex == 1
            ? PlayModePreference.FullTable
            : PlayModePreference.Solo;

        if (requested == playMode)
        {
            return;
        }

        SwitchTable(requested, aiSeatCount);
    }

    private void AiSeatCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressPreferenceEvents || !IsLoaded || session.RoundInProgress || playMode != PlayModePreference.FullTable)
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

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressPreferenceEvents || !IsLoaded)
        {
            return;
        }

        preferences = preferences with { Theme = SelectedTheme };
        ApplyTheme(SelectedTheme);
        RefreshGameUi();
        SavePreferences();
    }

    private void UpdateChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressPreferenceEvents || !IsLoaded)
        {
            return;
        }

        pendingManifest = null;
        InstallUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "Channel changed. Check for updates.";
        SavePreferences();
    }

    private void PresentationSettingChanged(object sender, RoutedEventArgs e)
    {
        MenuAudio.Enabled = SoundEnabled;
        if (!SoundEnabled) CasinoFeedback.Stop();
        if (!suppressPreferenceEvents && IsLoaded)
        {
            SavePreferences();
        }
    }

    private void SwitchTable(PlayModePreference mode, int seats)
    {
        decimal bankroll = session.Bankroll;

        playMode = mode;
        aiSeatCount = seats;
        session = CreateSession(bankroll);
        selectedBet = Math.Min(selectedBet, bankroll);

        StatusText.Text = playMode == PlayModePreference.Solo
            ? "Solo table ready. Place a bet to begin."
            : $"Full table ready with {aiSeatCount} AI seat{(aiSeatCount == 1 ? string.Empty : "s")}. Place a bet to begin.";

        SavePreferences();
        RefreshGameUi();
    }

    private void Chip_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress || sender is not Button { Tag: string raw } || !decimal.TryParse(raw, CultureInfo.InvariantCulture, out decimal chip))
        {
            return;
        }

        selectedBet = Math.Min(session.Bankroll, selectedBet + chip);
        CasinoFeedback.PlayChip(SoundEnabled);
        SavePreferences();
        RefreshGameUi();
    }

    private void ClearBet_Click(object sender, RoutedEventArgs e)
    {
        if (!session.RoundInProgress)
        {
            selectedBet = 0m;
            SavePreferences();
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
            animateNextRender = AnimationsEnabled;
            session.StartRound(selectedBet);
            CasinoFeedback.PlayCard(SoundEnabled);
            HandleRoundStateAfterMutation(playMode == PlayModePreference.FullTable ? "Cards dealt around the table." : "Cards dealt.");
        }
        catch (Exception ex)
        {
            animateNextRender = false;
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
            animateNextRender = AnimationsEnabled && action is PlayerAction.Hit or PlayerAction.DoubleDown or PlayerAction.Split;
            session.Act(action);
            if (action is PlayerAction.Hit or PlayerAction.DoubleDown or PlayerAction.Split)
            {
                CasinoFeedback.PlayCard(SoundEnabled);
            }

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
            animateNextRender = false;
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
        StatusText.Text = "Bankroll reset. Place a bet to begin.";
        SavePreferences();
        RefreshGameUi();
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            previousWindowState = WindowState;
            previousWindowStyle = WindowStyle;
            previousResizeMode = ResizeMode;
            previousWindowBounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth, ActualHeight)
                : RestoreBounds;

            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            isFullscreen = true;
            return;
        }

        WindowState = WindowState.Normal;
        WindowStyle = previousWindowStyle;
        ResizeMode = previousResizeMode;
        if (previousWindowBounds.Width >= MinWidth && previousWindowBounds.Height >= MinHeight)
        {
            Left = previousWindowBounds.Left;
            Top = previousWindowBounds.Top;
            Width = previousWindowBounds.Width;
            Height = previousWindowBounds.Height;
        }
        WindowState = previousWindowState;
        isFullscreen = false;
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
            selectedBet = Math.Min(selectedBet, session.Bankroll);
            CasinoFeedback.PlayOutcome(SoundEnabled, round.Result!.TotalNet);
            SavePreferences();
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
        animationSequence = 0;

        try
        {
            BankrollText.Text = Money(session.Bankroll);
            BetText.Text = Money(selectedBet);
            ShoeText.Text = $"#{session.ShoeNumber} · {session.CardsRemaining} cards";
            TableModeText.Text = lobby?.Visibility == Visibility.Visible ? "Casino lobby" : playMode == PlayModePreference.Solo
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
            AiSettingsPanel.Visibility = playMode == PlayModePreference.FullTable ? Visibility.Visible : Visibility.Collapsed;

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
        finally
        {
            animateNextRender = false;
        }
    }

    private void RenderAiSeats(BlackjackRound? round)
    {
        if (playMode != PlayModePreference.FullTable)
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

    private Border BuildAiPlaceholder(AiPlayerProfile profile)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = profile.Name.ToUpperInvariant(),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = UiBrush("MutedTextBrush"),
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"Bet {Money(profile.Bet)}\nWaiting for deal",
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = UiBrush("SoftTextBrush"),
        });

        return new Border
        {
            Width = 172,
            Height = 108,
            Margin = new Thickness(5),
            Padding = new Thickness(10),
            Background = UiBrush("SeatBackgroundBrush"),
            BorderBrush = UiBrush("PanelBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Child = stack,
        };
    }

    private Border BuildAiSeat(AiSeatState seat, bool settled)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"{seat.Name.ToUpperInvariant()} · {Money(seat.InitialBet)}",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = UiBrush("MutedTextBrush"),
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
            Background = UiBrush("SeatBackgroundBrush"),
            BorderBrush = settled && seat.TotalNet > 0 ? UiBrush("AiWinBrush") : UiBrush("PanelBorderBrush"),
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
                Background = UiBrush("SeatBackgroundBrush"),
                BorderBrush = active ? UiBrush("AccentBrush") : UiBrush("PanelBorderBrush"),
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
                Foreground = active ? UiBrush("AccentLightBrush") : UiBrush("MutedTextBrush"),
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

    private Border BuildEmptySeat()
    {
        return new Border
        {
            Width = 250,
            Height = 150,
            BorderBrush = UiBrush("PanelBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(8),
            Child = new TextBlock
            {
                Text = "YOUR SEAT\n\nPlace a bet and deal",
                TextAlignment = TextAlignment.Center,
                Foreground = UiBrush("SoftTextBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }

    private Border CreateCard(Card card, bool faceDown = false, bool compact = false)
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
            border.Background = UiBrush("ActionBackgroundBrush");
            border.BorderBrush = UiBrush("AccentBrush");
            border.Child = new TextBlock
            {
                Text = "♠",
                FontSize = compact ? 24 : 36,
                FontWeight = FontWeights.Bold,
                Foreground = UiBrush("AccentBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            AnimateEntrance(border, compact);
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
        AnimateEntrance(border, compact);
        return border;
    }

    private void AnimateEntrance(FrameworkElement element, bool compact)
    {
        if (!animateNextRender || !AnimationsEnabled)
        {
            return;
        }

        int sequence = animationSequence++;
        TimeSpan delay = TimeSpan.FromMilliseconds(Math.Min(sequence, 18) * 32d);
        double offset = compact ? -7d : -14d;
        var transform = new TranslateTransform(0d, offset);
        element.RenderTransform = transform;
        element.Opacity = 0d;

        var opacity = new DoubleAnimation(0d, 1d, TimeSpan.FromMilliseconds(150d))
        {
            BeginTime = delay,
            FillBehavior = FillBehavior.HoldEnd,
        };
        var movement = new DoubleAnimation(offset, 0d, TimeSpan.FromMilliseconds(190d))
        {
            BeginTime = delay,
            FillBehavior = FillBehavior.HoldEnd,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };

        element.BeginAnimation(OpacityProperty, opacity);
        transform.BeginAnimation(TranslateTransform.YProperty, movement);
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
        if (e.Key == Key.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

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

            SavePreferences();
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
                UpdateStatusText.Text = $"{channel}: up to date (installed {result.CurrentVersion}, available {result.LatestVersion})";
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            SemanticVersion minimumUpdater = SemanticVersion.Parse(result.Manifest.MinimumUpdaterVersion);
            SemanticVersion updaterVersion = UpdaterLauncher.CurrentVersion ?? AppVersion.Current;
            if (minimumUpdater > updaterVersion)
            {
                pendingManifest = null;
                UpdateStatusText.Text = $"Update {result.LatestVersion} requires updater {minimumUpdater} or newer (installed {updaterVersion}).";
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

    private void SavePreferences()
    {
        if(testSession) return;
        Rect bounds;
        bool maximized;

        if (isFullscreen)
        {
            bounds = previousWindowBounds;
            maximized = previousWindowState == WindowState.Maximized;
        }
        else
        {
            bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, ActualWidth, ActualHeight)
                : RestoreBounds;
            maximized = WindowState == WindowState.Maximized;
        }

        double width = bounds.Width >= MinWidth ? bounds.Width : preferences.WindowWidth;
        double height = bounds.Height >= MinHeight ? bounds.Height : preferences.WindowHeight;

        preferences = preferences with
        {
            Bankroll = session.Bankroll,
            SelectedBet = Math.Min(selectedBet, session.Bankroll),
            PlayMode = playMode,
            AiSeatCount = aiSeatCount,
            Theme = SelectedTheme,
            SoundEnabled = SoundEnabled,
            AnimationsEnabled = AnimationsEnabled,
            UpdateChannel = SelectedUpdateChannel,
            WindowWidth = width,
            WindowHeight = height,
            WindowMaximized = maximized,
        };

        PlayerStateStore.Save(preferences);
    }

    private void ApplyTheme(TableThemePreference theme)
    {
        TablePalette palette = theme switch
        {
            TableThemePreference.MidnightBlue => new TablePalette(
                "#071019", "#0A1722", "#0B2637", "#091923", "#102C3D", "#315A70",
                "#174660", "#102F41", "#D4A84F", "#F0CD7A", "#9EB5C2", "#7392A3", "#071E2C", "#5C8E9E"),
            TableThemePreference.Burgundy => new TablePalette(
                "#180A0C", "#211012", "#351417", "#1F0E10", "#2B1518", "#704047",
                "#5B272D", "#381A1E", "#D4A84F", "#F0CD7A", "#C2A0A4", "#98767B", "#260F12", "#8F625C"),
            _ => new TablePalette(
                "#07150E", "#091B12", "#0A291A", "#091B12", "#0B2117", "#31533F",
                "#163D29", "#102A1D", "#D4A84F", "#F0CD7A", "#9EB1A4", "#789484", "#071D13", "#6A8F55"),
        };

        SetBrushResource("WindowBackgroundBrush", palette.Window);
        SetBrushResource("HeaderBackgroundBrush", palette.Header);
        SetBrushResource("TableBackgroundBrush", palette.Table);
        SetBrushResource("SideBackgroundBrush", palette.Side);
        SetBrushResource("SeatBackgroundBrush", palette.Seat);
        SetBrushResource("PanelBorderBrush", palette.Border);
        SetBrushResource("ActionBackgroundBrush", palette.Action);
        SetBrushResource("MutedButtonBackgroundBrush", palette.MutedButton);
        SetBrushResource("AccentBrush", palette.Accent);
        SetBrushResource("AccentLightBrush", palette.AccentLight);
        SetBrushResource("MutedTextBrush", palette.MutedText);
        SetBrushResource("SoftTextBrush", palette.SoftText);
        SetBrushResource("StatusBackgroundBrush", palette.Status);
        SetBrushResource("AiWinBrush", palette.AiWin);
    }

    private void SetBrushResource(string key, string hex) => Resources[key] = Brush(hex);

    private Brush UiBrush(string key) => (Brush)FindResource(key);

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
