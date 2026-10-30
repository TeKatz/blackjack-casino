using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow : Window
{
    private readonly PlayerProgressionController controller;

    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(166, 187, 174));
    private static readonly Brush PositiveBrush = new SolidColorBrush(Color.FromRgb(142, 199, 129));
    private static readonly Brush NegativeBrush = new SolidColorBrush(Color.FromRgb(217, 126, 126));
    private static readonly Brush CoinBrush = new SolidColorBrush(Color.FromRgb(84, 225, 183));
    private static readonly Brush CoinMutedBrush = new SolidColorBrush(Color.FromRgb(72, 177, 151));

    public ProgressionWindow(PlayerProgressionController controller)
    {
        this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        InitializeComponent();
        MenuAudio.Attach(this);
        InitializeCollections();
        StateChanged += (_, _) => RefreshMaximizeGlyph();
        RefreshAll();
        RefreshMaximizeGlyph();
    }

    internal void SelectTab(string header)
    {
        ProgressionTabs.SelectedItem = ProgressionTabs.Items.OfType<TabItem>()
            .FirstOrDefault(tab => string.Equals(tab.Header?.ToString(), header, StringComparison.OrdinalIgnoreCase))
            ?? ProgressionTabs.Items[0];
    }

    private void RefreshAll()
    {
        controller.AccruePlaytime();
        RefreshProfileHeader();
        RefreshStatistics();
        RefreshHistory();
        RefreshSeason();
        RefreshShop();
        RefreshChallenges();
    }

    private void RefreshProfileHeader()
    {
        PlayerProfileState profile = controller.Profile;
        CosmeticDefinition title = CosmeticCatalog.Get(profile.EquippedTitleId);
        TitleText.Text = $"{profile.DisplayName} · {title.Name}";
        TitleText.Foreground = CosmeticAppearance.TitleBrush(title);
        LevelText.Text = $"Prestige {profile.Prestige} · Level {profile.Level} / {ProgressionCurve.MaximumLevel} · Lifetime XP {profile.LifetimeExperience:N0}";
        CoinsText.Text = $"◆ {profile.Coins:N0} COSMETIC COINS";
        CoinsText.Foreground = CoinBrush;
        ApplyFramePresentation(ProfileFrame, CosmeticCatalog.Get(profile.EquippedFrameId));
        PrestigeButton.IsEnabled = controller.Service.CanPrestige;

        if (profile.Level >= ProgressionCurve.MaximumLevel)
        {
            LevelProgress.Maximum = 1;
            LevelProgress.Value = 1;
            LevelProgressText.Text = "LEVEL 100 — PRESTIGE AVAILABLE";
        }
        else
        {
            long required = controller.Service.ExperienceToNextLevel;
            LevelProgress.Maximum = Math.Max(1, required);
            LevelProgress.Value = Math.Min(profile.LevelExperience, required);
            LevelProgressText.Text = $"{profile.LevelExperience:N0} / {required:N0} XP";
        }
    }

    private void RefreshHistory()
    {
        HistoryPanel.Children.Clear();
        RoundHistoryEntry[] entries = controller.History.OrderByDescending(x => x.CompletedAt).Take(150).ToArray();
        if (entries.Length == 0)
        {
            HistoryPanel.Children.Add(MutedText("No hand history yet. Finish a hand and it will appear here."));
            return;
        }

        foreach (RoundHistoryEntry entry in entries)
        {
            string player = string.Join(" | ", entry.PlayerHands.Select((hand, index) => $"H{index + 1} {Cards(hand.Cards)} → {Outcome(hand.Outcome)}"));
            var border = PanelBorder();
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"{entry.CompletedAt.ToLocalTime():g}    {SignedMoney(entry.TotalNet)}",
                FontWeight = FontWeights.Bold,
                Foreground = entry.TotalNet >= 0 ? PositiveBrush : NegativeBrush,
            });
            stack.Children.Add(new TextBlock { Text = $"Bet {Money(entry.InitialBet)} · Dealer {Cards(entry.DealerCards)}", Margin = new Thickness(0, 5, 0, 0), Foreground = MutedBrush });
            stack.Children.Add(new TextBlock { Text = player, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            border.Child = stack;
            HistoryPanel.Children.Add(border);
        }
    }

    private void RefreshSeason()
    {
        SeasonDefinition season = SeasonCatalog.Get(controller.Profile.ActiveSeasonId);
        SeasonProgressState progress = controller.Service.GetOrCreateSeasonProgress(season.Id);
        SeasonTitleText.Text = season.Name;
        SeasonDescriptionText.Text = season.Description;
        SeasonTierText.Text = $"Tier {progress.Tier} / {season.Tiers.Count} · {progress.Experience:N0} Season XP";
        SeasonProgress.Maximum = season.Tiers[^1].RequiredExperience;
        SeasonProgress.Value = Math.Min(progress.Experience, season.Tiers[^1].RequiredExperience);

        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(tier => tier.Tier > progress.Tier);
        SeasonNextRewardText.Inlines.Clear();
        if (next is null)
        {
            SeasonNextRewardText.Text = "Season complete";
            SeasonNextRewardXpText.Text = $"All {season.Tiers.Count} rewards unlocked.";
        }
        else
        {
            SeasonNextRewardText.Inlines.Add(new Run($"Tier {next.Tier} · "));
            SeasonNextRewardText.Inlines.Add(new Run($"◆ {next.Coins:N0} coins") { Foreground = CoinBrush, FontWeight = FontWeights.Bold });
            if (next.CosmeticId is not null)
            {
                SeasonNextRewardText.Inlines.Add(new Run($" + {CosmeticCatalog.Get(next.CosmeticId).Name}"));
            }
            SeasonNextRewardXpText.Text = $"{Math.Max(0, next.RequiredExperience - progress.Experience):N0} XP to unlock";
        }

        SeasonMilestonesPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers.Where(tier => tier.CosmeticId is not null || tier.Tier % 10 == 0))
        {
            bool unlocked = progress.Tier >= tier.Tier;
            var milestone = new Border
            {
                Width = 160,
                MinHeight = 72,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = unlocked ? PositiveBrush : new SolidColorBrush(Color.FromRgb(53, 78, 65)),
                Background = unlocked
                    ? new SolidColorBrush(Color.FromRgb(18, 54, 37))
                    : new SolidColorBrush(Color.FromRgb(9, 29, 20)),
                Opacity = unlocked ? 1 : 0.78,
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"TIER {tier.Tier}", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = unlocked ? PositiveBrush : MutedBrush });
            if (tier.CosmeticId is not null)
            {
                CosmeticDefinition cosmetic = CosmeticCatalog.Get(tier.CosmeticId);
                stack.Children.Add(new TextBlock { Text = cosmetic.Name, Margin = new Thickness(0, 4, 0, 0), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                stack.Children.Add(new TextBlock { Text = cosmetic.Kind.ToString().ToUpperInvariant(), FontSize = 9, Foreground = MutedBrush, Margin = new Thickness(0, 2, 0, 0) });
            }
            stack.Children.Add(CoinText($"◆ {tier.Coins:N0}", 11, FontWeights.SemiBold));
            milestone.Child = stack;
            SeasonMilestonesPanel.Children.Add(milestone);
        }

        SeasonRewardsPanel.Children.Clear();
        foreach (SeasonTierDefinition tier in season.Tiers)
        {
            bool unlocked = progress.Tier >= tier.Tier;
            bool isNext = next?.Tier == tier.Tier;
            CosmeticDefinition? cosmetic = tier.CosmeticId is null ? null : CosmeticCatalog.Get(tier.CosmeticId);

            var border = PanelBorder();
            border.Padding = new Thickness(12, 9, 12, 9);
            border.Margin = new Thickness(2, 3, 2, 3);
            border.Opacity = unlocked || isNext ? 1 : 0.62;
            if (isNext)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(116, 126, 184));
                border.Background = new SolidColorBrush(Color.FromRgb(14, 31, 31));
            }
            else if (unlocked)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 100, 73));
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(126) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            string state = unlocked ? "UNLOCKED" : isNext ? "NEXT" : "LOCKED";
            var badge = new Border
            {
                Background = unlocked
                    ? new SolidColorBrush(Color.FromRgb(22, 66, 44))
                    : isNext
                        ? new SolidColorBrush(Color.FromRgb(39, 48, 82))
                        : new SolidColorBrush(Color.FromRgb(13, 38, 26)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = $"T{tier.Tier}",
                    FontWeight = FontWeights.Bold,
                    Foreground = unlocked ? PositiveBrush : isNext ? new SolidColorBrush(Color.FromRgb(166, 177, 232)) : MutedBrush,
                },
            };
            grid.Children.Add(badge);

            var reward = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = cosmetic is null ? FontWeights.Normal : FontWeights.SemiBold,
            };
            reward.Inlines.Add(new Run($"◆ {tier.Coins:N0} coins") { Foreground = CoinBrush, FontWeight = FontWeights.SemiBold });
            if (cosmetic is not null)
            {
                reward.Inlines.Add(new Run($" · {cosmetic.Name} ({cosmetic.Kind})"));
            }
            Grid.SetColumn(reward, 1);
            grid.Children.Add(reward);

            var xp = new TextBlock
            {
                Text = $"{tier.RequiredExperience:N0} XP",
                Foreground = MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(xp, 2);
            grid.Children.Add(xp);

            var status = new TextBlock
            {
                Text = state,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = unlocked ? PositiveBrush : isNext ? new SolidColorBrush(Color.FromRgb(166, 177, 232)) : MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(status, 3);
            grid.Children.Add(status);

            border.Child = grid;
            SeasonRewardsPanel.Children.Add(border);
        }
    }

    private FrameworkElement CreateCosmeticPreview(CosmeticDefinition cosmetic)
    {
        return cosmetic.Kind switch
        {
            CosmeticKind.Portrait => CreatePortraitVisual(cosmetic, 78),
            CosmeticKind.DiceSet => new KniffelDie { Width = 72, Height = 72, Value = 5, VisualVariant = cosmetic.VisualVariant },
            CosmeticKind.Frame => CreateFramePreview(cosmetic),
            _ => new Border
            {
                Width = 104,
                Height = 58,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(14, 43, 29)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 100, 73)),
                BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = cosmetic.Name, Foreground = CosmeticAppearance.TitleBrush(cosmetic), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, FontSize = 11 },
            },
        };
    }

    private FrameworkElement CreateFramePreview(CosmeticDefinition cosmetic)
    {
        var frame = new Border
        {
            Width = 104,
            Height = 64,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromRgb(8, 27, 18)),
            Padding = new Thickness(7),
            Child = new TextBlock
            {
                Text = "PROFILE",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = MutedBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        ApplyFramePresentation(frame, cosmetic, compact: true);
        return frame;
    }

    internal static FrameworkElement CreatePortraitVisual(CosmeticDefinition portrait, double size)
    {
        if (PortraitArtwork.Get(portrait.Id) is { } image)
        {
            // ImageBrush + Ellipse produces a real circular crop; CornerRadius on a
            // Border alone would not clip its child image. Keep the original square
            // composition centred so its crown, ears and ornamental ring stay intact.
            var picture = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new ImageBrush(image) { Stretch = Stretch.UniformToFill, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center },
                Stroke = new SolidColorBrush(Color.FromArgb(190, 225, 211, 170)),
                StrokeThickness = Math.Max(1, size / 80),
                ToolTip = portrait.Name,
                SnapsToDevicePixels = true,
            };
            System.Windows.Automation.AutomationProperties.SetName(picture, portrait.Name);
            RenderOptions.SetBitmapScalingMode(picture, BitmapScalingMode.HighQuality);
            return picture;
        }
        (Color a, Color b, string glyph) = PortraitTheme(portrait.VisualVariant);
        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.5),
            BorderThickness = new Thickness(Math.Max(1, size / 42)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(190, 225, 211, 170)),
            Background = new LinearGradientBrush(a, b, new Point(0, 0), new Point(1, 1)),
            ClipToBounds = true,
        };
        var grid = new Grid();
        grid.Children.Add(new Ellipse
        {
            Width = size * 0.38,
            Height = size * 0.38,
            Fill = new SolidColorBrush(Color.FromArgb(215, 235, 229, 215)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, size * 0.17, 0, 0),
        });
        grid.Children.Add(new Ellipse
        {
            Width = size * 0.72,
            Height = size * 0.50,
            Fill = new SolidColorBrush(Color.FromArgb(225, 22, 25, 31)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, -size * 0.16),
        });
        grid.Children.Add(new TextBlock
        {
            Text = glyph,
            FontSize = size * 0.22,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(225, 255, 222, 145)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, size * 0.05, size * 0.08, 0),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 5, ShadowDepth = 1, Opacity = 0.5 },
        });
        border.Child = grid;
        border.Clip = new EllipseGeometry(new Point(size/2,size/2),size/2,size/2);
        return border;
    }

    private static (Color A, Color B, string Glyph) PortraitTheme(string variant) => variant switch
    {
        "portrait-night-dealer" => (Rgb(28, 56, 48), Rgb(10, 18, 25), "♠"),
        "portrait-card-shark" => (Rgb(18, 74, 81), Rgb(14, 24, 44), "A"),
        "portrait-moon-cat" => (Rgb(77, 63, 107), Rgb(25, 24, 46), "☾"),
        "portrait-neon-ace" => (Rgb(23, 95, 100), Rgb(95, 31, 94), "A"),
        "portrait-lucky-raven" => (Rgb(45, 48, 57), Rgb(8, 12, 18), "◆"),
        "portrait-midnight-lady" => (Rgb(74, 48, 79), Rgb(20, 24, 48), "☽"),
        "portrait-golden-joker" => (Rgb(134, 91, 34), Rgb(54, 24, 37), "✦"),
        "portrait-velvet-king" => (Rgb(103, 36, 52), Rgb(35, 19, 30), "♛"),
        "portrait-eclipse-raven" => (Rgb(41, 68, 82), Rgb(52, 27, 68), "R"),
        "portrait-eclipse-lady" => (Rgb(90, 48, 87), Rgb(25, 39, 62), "☾"),
        "portrait-eclipse-moon" => (Rgb(41, 89, 104), Rgb(31, 28, 65), "●"),
        "portrait-eclipse-sovereign" => (Rgb(117, 78, 38), Rgb(62, 30, 81), "♛"),
        _ => (Rgb(39, 91, 64), Rgb(14, 37, 27), "♠"),
    };

    private static void ApplyFramePresentation(Border target, CosmeticDefinition frame, bool compact = false)
    {
        if (frame.VisualVariant.StartsWith("static-", StringComparison.Ordinal))
        {
            target.BorderBrush = new SolidColorBrush(CosmeticAppearance.StandardFrame(frame.VisualVariant));
            target.Effect = null;
            return;
        }
        (Color a, Color b, string motion, double seconds) = FrameTheme(frame.VisualVariant);
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        var first = new GradientStop(a, 0);
        var middle = new GradientStop(b, 0.52);
        var last = new GradientStop(a, 1);
        brush.GradientStops.Add(first);
        brush.GradientStops.Add(middle);
        brush.GradientStops.Add(last);
        target.BorderBrush = brush;

        var glow = new DropShadowEffect
        {
            Color = b,
            BlurRadius = compact ? 8 : 12,
            ShadowDepth = 0,
            Opacity = compact ? 0.28 : 0.34,
        };
        target.Effect = glow;

        switch (motion)
        {
            case "sweep":
                middle.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(0.08, 0.92, TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                });
                break;
            case "orbit":
                var rotate = new RotateTransform(0, 0.5, 0.5);
                brush.RelativeTransform = rotate;
                rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(seconds))
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                });
                break;
            case "ember":
                middle.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(b, Lighten(b, 42), TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.20, compact ? 0.38 : 0.52, TimeSpan.FromSeconds(seconds * 0.72))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
                break;
            case "pulse":
                glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(compact ? 6 : 9, compact ? 12 : 18, TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                });
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.18, compact ? 0.42 : 0.56, TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
                break;
            case "prism":
                first.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(a, b, TimeSpan.FromSeconds(seconds)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
                last.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(b, a, TimeSpan.FromSeconds(seconds * 1.2)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
                break;
            default:
                glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.18, compact ? 0.32 : 0.42, TimeSpan.FromSeconds(seconds))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
                break;
        }
    }

    private static (Color A, Color B, string Motion, double Seconds) FrameTheme(string variant) => variant switch
    {
        "emerald-breathe" => (Rgb(65, 150, 105), Rgb(112, 220, 156), "pulse", 2.8),
        "midnight-sweep" => (Rgb(60, 86, 142), Rgb(108, 153, 225), "sweep", 3.6),
        "burgundy-ember" => (Rgb(127, 59, 72), Rgb(211, 87, 104), "ember", 2.4),
        "gold-shimmer" => (Rgb(139, 100, 39), Rgb(238, 198, 91), "sweep", 2.7),
        "aurora-drift" => (Rgb(65, 208, 177), Rgb(152, 87, 218), "prism", 4.2),
        "crimson-heartbeat" => (Rgb(117, 34, 49), Rgb(235, 82, 104), "pulse", 1.65),
        "starlight-orbit" => (Rgb(125, 159, 207), Rgb(223, 235, 245), "orbit", 8.0),
        "royal-violet" => (Rgb(118, 73, 164), Rgb(224, 184, 91), "sweep", 4.6),
        "obsidian-pulse" => (Rgb(24, 31, 37), Rgb(54, 224, 205), "pulse", 3.2),
        "prestige-bronze" => (Rgb(136, 78, 42), Rgb(208, 142, 82), "ember", 3.2),
        "prestige-silver" => (Rgb(125, 137, 147), Rgb(225, 234, 240), "orbit", 9.0),
        "prestige-gold" => (Rgb(151, 113, 36), Rgb(255, 220, 102), "orbit", 6.5),
        "midnight-line" => (Rgb(67, 87, 157), Rgb(118, 146, 229), "sweep", 4.0),
        "neon-noir" => (Rgb(59, 210, 188), Rgb(197, 78, 219), "prism", 3.4),
        "eclipse-thread" => (Rgb(74, 96, 135), Rgb(170, 96, 187), "sweep", 4.5),
        "eclipse-halo" => (Rgb(55, 155, 168), Rgb(206, 107, 178), "orbit", 7.0),
        "eclipse-crown" => (Rgb(191, 137, 55), Rgb(136, 79, 190), "prism", 4.8),
        _ => (Rgb(82, 105, 90), Rgb(122, 151, 129), "breathe", 4.0),
    };

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

    private static Color Lighten(Color color, byte amount) => Color.FromRgb(
        (byte)Math.Min(255, color.R + amount),
        (byte)Math.Min(255, color.G + amount),
        (byte)Math.Min(255, color.B + amount));

    private static TextBlock CoinText(string text, double size, FontWeight weight)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = CoinBrush,
            FontSize = size,
            FontWeight = weight,
            Margin = new Thickness(0, 4, 0, 0),
        };
    }

    private static string FormatChallengeValue(ChallengeDefinition challenge, decimal value)
    {
        if (challenge.Metric == ChallengeMetric.LifetimePlaytimeSeconds) return $"{value / 3600m:N1} h";
        string metric = challenge.Metric.ToString();
        if (metric.EndsWith("Wagered", StringComparison.Ordinal) || metric.EndsWith("Returned", StringComparison.Ordinal)
            || metric.EndsWith("NetProfit", StringComparison.Ordinal) || metric.EndsWith("BiggestWin", StringComparison.Ordinal)
            || metric.EndsWith("BiggestPayout", StringComparison.Ordinal) || metric.EndsWith("HighestBankroll", StringComparison.Ordinal))
        {
            return $"${value:N0}";
        }
        return metric.EndsWith("Multiplier", StringComparison.Ordinal) ? $"{value:N0}×" : $"{value:N0}";
    }

    private void PrestigeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!controller.Service.CanPrestige)
        {
            return;
        }

        MessageBoxResult answer = MessageBox.Show(
            Window.GetWindow(ProgressionRoot) ?? this,
            "Enter Prestige? Your level and current level XP reset to 0. Coins, cosmetics, statistics, bankroll and Season Pass progress stay exactly as they are.",
            "Enter Prestige",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        PrestigeResult result = controller.Prestige();
        ShopStatusText.Text = result.Message;
        RefreshAll();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (embeddedClose is not null) return;

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (embeddedClose is null) WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (embeddedClose is { } close) close(); else Close();
    }

    private void ToggleMaximize()
    {
        if (embeddedClose is not null) return;
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        RefreshMaximizeGlyph();
    }

    private void RefreshMaximizeGlyph()
    {
        if (MaximizeButton is not null)
        {
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }
    }

    private string BuildStatisticsText(StatisticsSnapshot stats, bool includePlaytime)
    {
        decimal winRate = stats.HandsPlayed == 0 ? 0m : (decimal)stats.HandsWon / stats.HandsPlayed;
        string text =
            $"Rounds: {stats.RoundsPlayed:N0}\n" +
            $"Hands: {stats.HandsPlayed:N0}\n" +
            $"W / L / Push: {stats.HandsWon:N0} / {stats.HandsLost:N0} / {stats.Pushes:N0}\n" +
            $"Win rate: {winRate:P1}\n" +
            $"Blackjacks: {stats.Blackjacks:N0}\n" +
            $"Doubles: {stats.Doubles:N0} ({stats.DoubleWins:N0} won)\n" +
            $"Splits: {stats.Splits:N0}\n" +
            $"Surrenders: {stats.Surrenders:N0}\n" +
            $"Insurance taken: {stats.InsuranceTaken:N0}\n" +
            $"Total wagered: {Money(stats.TotalWagered)}\n" +
            $"Net: {SignedMoney(stats.NetProfit)}\n" +
            $"Biggest win: {Money(stats.BiggestWin)}\n" +
            $"Biggest loss: {Money(stats.BiggestLoss)}\n" +
            $"Best win streak: {stats.BestWinStreak:N0}\n" +
            $"Worst loss streak: {stats.WorstLossStreak:N0}";

        if (includePlaytime)
        {
            text += $"\nPlaytime: {FormatDuration(controller.TotalPlaytime)}";
        }

        return text;
    }

    private static Border PanelBorder() => new()
    {
        Background = new SolidColorBrush(Color.FromArgb(180, 11, 33, 23)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(49, 90, 67)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12),
        Margin = new Thickness(2, 4, 2, 4),
    };

    private static TextBlock MutedText(string text) => new() { Text = text, Foreground = MutedBrush, Margin = new Thickness(4) };

    private static string Cards(IEnumerable<Card> cards) => string.Join(" ", cards.Select(card => $"{Rank(card.Rank)}{Suit(card.Suit)}"));

    private static string Rank(Rank rank) => rank switch
    {
        BlackjackCasino.Core.Rank.Ace => "A",
        BlackjackCasino.Core.Rank.Jack => "J",
        BlackjackCasino.Core.Rank.Queen => "Q",
        BlackjackCasino.Core.Rank.King => "K",
        _ => ((int)rank).ToString(CultureInfo.InvariantCulture),
    };

    private static string Suit(Suit suit) => suit switch
    {
        BlackjackCasino.Core.Suit.Clubs => "♣",
        BlackjackCasino.Core.Suit.Diamonds => "♦",
        BlackjackCasino.Core.Suit.Hearts => "♥",
        BlackjackCasino.Core.Suit.Spades => "♠",
        _ => "?",
    };

    private static string Outcome(HandOutcome outcome) => outcome switch
    {
        HandOutcome.Blackjack => "Blackjack",
        HandOutcome.Win => "Win",
        HandOutcome.Loss => "Loss",
        HandOutcome.Push => "Push",
        HandOutcome.Surrender => "Surrender",
        _ => outcome.ToString(),
    };

    private static string Money(decimal amount) => $"${amount:N2}";
    private static string SignedMoney(decimal amount) => amount switch
    {
        > 0 => $"+${amount:N2}",
        < 0 => $"-${Math.Abs(amount):N2}",
        _ => "$0.00",
    };

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }

        return $"{Math.Max(0, duration.Minutes)}m";
    }
}
