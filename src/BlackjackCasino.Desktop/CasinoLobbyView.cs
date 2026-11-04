using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoLobbyView : UserControl
{
    private readonly CasinoModeRegistry modes;
    private readonly Func<bool> animations;
    private readonly ProgressBar seasonProgress = new() { Name = "LobbySeasonProgress", Minimum = 0, Maximum = 1 };
    private readonly ProgressBar challengeProgress = new() { Name = "LobbyChallengeProgress", Minimum = 0, Maximum = 1 };
    private readonly TextBlock seasonStatus = new() { Name = "LobbySeasonStatus" };
    private readonly TextBlock challengeStatus = new() { Name = "LobbyChallengeStatus" };
    private LobbyProgressSnapshot? lastProgress;
    private bool compactSeasonStatus;
    private readonly UniformGrid cards = new() { Columns = 4, Width = 1280 };
    private Border? albumPackBadge;
    private TextBlock? albumPackBadgeText;
    private DropShadowEffect? albumPackBadgeGlow;
    private static readonly string[] Order = ["blackjack", "kniffel", "chill-lounge", "casino-empire", "midnight-reels", "neon-vault", "nine-lives", "midnight-manor"];

    internal CasinoLobbyView(CasinoModeRegistry modes, Action<string> progression, Func<bool>? animations = null)
    {
        this.modes = modes;
        this.animations = animations ?? (() => true);
        Background = new LinearGradientBrush(Color.FromRgb(10, 17, 22), Color.FromRgb(23, 32, 28), 80);

        var root = new Grid { Margin = new Thickness(30, 14, 30, 14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var heading = new StackPanel { Margin = new Thickness(10, 0, 10, 12) };
        heading.Children.Add(new TextBlock { Text = "THE MIDNIGHT CLUB", Foreground = Ink("#D6B478"), FontSize = 11 });
        heading.Children.Add(new TextBlock
        {
            Text = "A different kind of night.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 30,
            Foreground = Ink("#F7EBD5"),
            Margin = new Thickness(0, 4, 0, 4),
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Choose your table. Find your rhythm. Make yourself at home.",
            Foreground = Ink("#B8C7BE"),
            FontSize = 13,
        });
        root.Children.Add(heading);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(264) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var gamesViewport = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = cards,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        body.Children.Add(gamesViewport);

        Border hub = BuildProgressionHub(progression);
        Grid.SetColumn(hub, 2);
        body.Children.Add(hub);
        // Expand only into the unused right-hand letterbox gutter of the games Viewbox.
        // The games column, scale, centering and tile hit targets remain exactly as before.
        void FitHub()
        {
            if (cards.ActualWidth <= 0 || !cards.IsArrangeValid || !body.IsArrangeValid) return;
            Rect games = cards.TransformToAncestor(body).TransformBounds(new Rect(0, 0, cards.ActualWidth, cards.ActualHeight));
            double columnStart = body.ColumnDefinitions[0].ActualWidth + 22;
            double extra = Math.Max(0, columnStart - games.Right - 18);
            Thickness next = new(-extra, 8, 0, 8);
            if (Math.Abs(hub.Margin.Left - next.Left) > .5) hub.Margin = next;
        }
        bool fitQueued = false;
        void QueueFit()
        {
            if (fitQueued) return;
            fitQueued = true;
            Dispatcher.BeginInvoke(() => { fitQueued = false; if (IsLoaded) FitHub(); });
        }
        body.SizeChanged += (_, _) => QueueFit();
        gamesViewport.SizeChanged += (_, _) => QueueFit();
        Loaded += (_, _) => QueueFit();

        Content = root;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Refresh();
    }

    internal void SetAlbumPackCount(int count)
    {
        count = Math.Max(0, count);
        if (albumPackBadge is null || albumPackBadgeText is null) return;

        albumPackBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        albumPackBadgeText.Text = count > 99 ? "+99" : $"+{count}";
        albumPackBadge.ToolTip = count > 0
            ? $"{count} unopened collection pack{(count == 1 ? string.Empty : "s")}" 
            : null;

        PackBadgeMotion.Sync(albumPackBadge);
    }

    internal void SetProgress(LobbyProgressSnapshot snapshot)
    {
        if (snapshot == lastProgress) return;
        lastProgress = snapshot;
        seasonProgress.Value = snapshot.SeasonFraction;
        seasonStatus.Text = SeasonCaption(snapshot, compactSeasonStatus);
        seasonStatus.ToolTip = SeasonCaption(snapshot, false);
        challengeProgress.Value = snapshot.ChallengeFraction;
        challengeStatus.Text = snapshot.ChallengeId is null ? snapshot.ChallengeName : $"{snapshot.ChallengeName} · {snapshot.ChallengeFraction:P0}";
        challengeStatus.ToolTip = snapshot.ChallengeId is null ? snapshot.ChallengeName : $"{snapshot.ChallengeName}: {snapshot.ChallengeProgress:N0} / {snapshot.ChallengeGoal:N0}";
    }

    private static string SeasonCaption(LobbyProgressSnapshot snapshot, bool compact) => snapshot.SeasonComplete
        ? "SEASON COMPLETE"
        : compact ? $"T{snapshot.SeasonTier} · {snapshot.SeasonExperience:N0}/{snapshot.SeasonRequired:N0} XP"
        : $"Tier {snapshot.SeasonTier} → {snapshot.SeasonNextTier}\n{snapshot.SeasonExperience:N0} / {snapshot.SeasonRequired:N0} XP";

    private Border BuildProgressionHub(Action<string> progression)
    {
        var shell = new Border
        {
            Name = "LobbyOverviewHub",
            Background = new LinearGradientBrush(Ink("#10231F").Color, Ink("#0C1918").Color, 90),
            BorderBrush = Ink("#38564D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(15, 16, 15, 15),
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 8),
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition());
        var stack = new StackPanel();
        layout.Children.Add(stack);
        var destinations = new UniformGrid { Rows = 5, Columns = 1 };
        Grid.SetRow(destinations, 1); layout.Children.Add(destinations);
        stack.Children.Add(new TextBlock
        {
            Text = "YOUR MIDNIGHT CLUB",
            Foreground = Ink("#D0B36F"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "At a glance.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 24,
            Foreground = Ink("#F3E6D1"),
            Margin = new Thickness(0, 4, 0, 4),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Rewards, style, goals and your casino career — all in one corner.",
            FontSize = 13,
            Foreground = Ink("#91A69D"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
        });

        destinations.Children.Add(ProgressionTile(
            "LobbyProgression_SeasonPass",
            "Season Pass",
            "SEASON REWARDS",
            "Follow your current season track and claim unlocked rewards.",
            "#D2B363",
            progression));
        destinations.Children.Add(ProgressionTile(
            "LobbyProgression_CosmeticShop",
            "Cosmetic Shop",
            "PERSONAL STYLE",
            "Browse card sets, portraits and other ways to make the club yours.",
            "#A78AC8",
            progression));
        destinations.Children.Add(ProgressionTile(
            "LobbyProgression_Album",
            "Collector's Album",
            "CARD COLLECTION",
            "Open packs, complete themed collections and inspect discovered editions.",
            "#D7B568",
            progression,
            showPackBadge: true));
        destinations.Children.Add(ProgressionTile(
            "LobbyProgression_Challenges",
            "Challenges",
            "GOALS & REWARDS",
            "See active challenges, completed goals and rewards ready to claim.",
            "#78B69A",
            progression));
        destinations.Children.Add(ProgressionTile(
            "LobbyProgression_Overview",
            "Statistics",
            "PLAYER STATISTICS",
            "Review lifetime results, game-by-game performance and your casino playtime.",
            "#7FA9C2",
            progression));

        shell.Child = layout;
        return shell;
    }

    private Button ProgressionTile(
        string name,
        string section,
        string eyebrow,
        string description,
        string accentHex,
        Action<string> progression,
        bool showPackBadge = false)
    {
        Brush accent = Ink(accentHex);
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // The icon itself is bundled raster artwork, never a WPF-drawn glyph.
        // Give every destination a shared jewel-case treatment in its own accent.
        var accentColor = ((SolidColorBrush)accent).Color;
        var iconScale = new ScaleTransform(1, 1);
        var emblemFrame = new Grid
        {
            Width = 70,
            Height = 70,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = iconScale,
        };
        emblemFrame.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(14),
            Background = new LinearGradientBrush(
                Color.FromArgb(210, accentColor.R, accentColor.G, accentColor.B),
                Color.FromRgb(8, 22, 24), 135),
            BorderBrush = accent,
            BorderThickness = new Thickness(1.5),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                Color = accentColor,
                BlurRadius = 12,
                ShadowDepth = 1,
                Opacity = .23,
            },
        });
        emblemFrame.Children.Add(new Border
        {
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(10),
            Background = Ink("#0A1C20"),
            BorderBrush = Ink("#63796D"),
            BorderThickness = new Thickness(.8),
            Padding = new Thickness(3),
            Child = new Viewbox
            {
                Child = ProgressionArtwork.Create(section, 64),
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
            },
        });
        content.Children.Add(emblemFrame);

        var copy = new StackPanel { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = eyebrow,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = accent,
        });
        copy.Children.Add(new TextBlock
        {
            Text = section,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 21,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Ink("#F1E4CF"),
            Margin = new Thickness(0, 2, 0, 3),
        });
        copy.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Foreground = Ink("#A7BDB3"),
            TextWrapping = TextWrapping.Wrap,
        });
        if (section is "Season Pass" or "Challenges")
        {
            ProgressBar progress = section == "Season Pass" ? seasonProgress : challengeProgress;
            TextBlock status = section == "Season Pass" ? seasonStatus : challengeStatus;
            progress.Height = 5; progress.Margin = new Thickness(0, 8, 0, 5);
            progress.Foreground = accent; progress.Background = Ink("#243B35");
            status.Foreground = Ink("#D5E5D9"); status.FontSize = 13;
            status.FontWeight = FontWeights.SemiBold;
            status.TextWrapping = TextWrapping.Wrap;
            status.MaxHeight = 34;
            status.TextTrimming = TextTrimming.CharacterEllipsis;
            copy.Children.Add(progress); copy.Children.Add(status);
        }
        Grid.SetColumn(copy, 1);
        content.Children.Add(copy);

        if (showPackBadge)
        {
            albumPackBadgeText = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Ink("#14251F"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            albumPackBadgeGlow = new DropShadowEffect
            {
                Color = Color.FromRgb(112, 226, 181),
                BlurRadius = 11,
                ShadowDepth = 0,
                Opacity = .38,
            };
            albumPackBadge = new Border
            {
                MinWidth = 46,
                Height = 34,
                Padding = new Thickness(6, 0, 6, 0),
                Margin = new Thickness(7, 0, 0, 0),
                CornerRadius = new CornerRadius(17),
                Background = Ink("#70DDB0"),
                BorderBrush = Ink("#B6F5D8"),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Top,
                Child = albumPackBadgeText,
                Effect = albumPackBadgeGlow,
                Visibility = Visibility.Collapsed,
            };
            albumPackBadge.Name = "LobbyAlbumPackBadge";
            PackBadgeMotion.Attach(albumPackBadge, this.animations);
            Grid.SetColumn(albumPackBadge, 2);
            content.Children.Add(albumPackBadge);
        }

        var button = new Button
        {
            Name = name,
            Content = content,
            ToolTip = description,
            Tag = section,
            Background = Ink("#112420"),
            BorderBrush = Ink("#34544A"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 10, 10, 10),
            Margin = new Thickness(0, 0, 0, 7),
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Cursor = CasinoCursors.Hand,
        };
        var eyebrowText = (TextBlock)copy.Children[0];
        var titleText = (TextBlock)copy.Children[1];
        var descriptionText = (TextBlock)copy.Children[2];
        button.SizeChanged += (_, _) =>
        {
            bool compact = button.ActualHeight < 115 || button.ActualWidth < 280;
            eyebrowText.Visibility = !compact && button.ActualHeight >= 158 ? Visibility.Visible : Visibility.Collapsed;
            descriptionText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            descriptionText.MaxHeight = 32;
            descriptionText.TextTrimming = TextTrimming.CharacterEllipsis;
            button.Padding = compact ? new Thickness(7, 5, 7, 5) : new Thickness(10);
            titleText.FontSize = compact ? 18 : 21;
            titleText.MaxHeight = compact ? 44 : 52;
            titleText.Margin = compact ? new Thickness(0, 0, 0, 2) : new Thickness(0, 2, 0, 3);
            emblemFrame.Width = emblemFrame.Height = compact ? 40 : 70;
            content.ColumnDefinitions[0].Width = new GridLength(compact ? 50 : 76);
            if (section is "Season Pass" or "Challenges")
            {
                TextBlock status = section == "Season Pass" ? seasonStatus : challengeStatus;
                status.FontSize = compact ? 12 : 13;
                status.TextWrapping = compact ? TextWrapping.NoWrap : TextWrapping.Wrap;
                status.MaxHeight = compact ? 18 : 34;
                ProgressBar progress = section == "Season Pass" ? seasonProgress : challengeProgress;
                progress.Height = compact ? 4 : 5;
                progress.Margin = compact ? new Thickness(0, 2, 0, 3) : new Thickness(0, 8, 0, 5);
                if (section == "Season Pass")
                {
                    compactSeasonStatus = compact;
                    if (lastProgress is { } snapshot) seasonStatus.Text = SeasonCaption(snapshot, compact);
                }
            }
        };
        button.MouseEnter += (_, _) =>
        {
            if (!animations()) return;
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            iconScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(1.045, TimeSpan.FromMilliseconds(140)) { EasingFunction = easing });
            iconScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1.045, TimeSpan.FromMilliseconds(140)) { EasingFunction = easing });
        };
        button.MouseLeave += (_, _) =>
        {
            iconScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(140)));
            iconScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(140)));
        };
        button.Click += (_, _) => progression(section);
        return button;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        modes.Changed -= Refresh;
        modes.Changed += Refresh;
        Localization.LanguageChanged -= Localization_LanguageChanged;
        Localization.LanguageChanged += Localization_LanguageChanged;
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        modes.Changed -= Refresh;
        Localization.LanguageChanged -= Localization_LanguageChanged;
        albumPackBadgeGlow?.BeginAnimation(DropShadowEffect.OpacityProperty, null);
        albumPackBadgeGlow?.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
    }

    private void Localization_LanguageChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        cards.Children.Clear();
        cards.Rows = Math.Max(1, (modes.Modes.Count + 3) / 4);
        cards.Height = cards.Rows * 266;
        foreach (CasinoMode mode in modes.Modes.OrderBy(m => Array.IndexOf(Order, m.Id) is int index && index >= 0 ? index : int.MaxValue))
        {
            string Label(string source) => mode.UseResourceKeys ? Localization.T(source) : Localized(source);
            Brush accent = Ink(mode.Accent);
            var panel = new Grid { Height = 246 };
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(94) });
            panel.RowDefinitions.Add(new RowDefinition());
            var art = new Grid
            {
                ClipToBounds = true,
                Background = new LinearGradientBrush(((SolidColorBrush)accent).Color, Color.FromRgb(9, 17, 22), 65),
            };
            if (mode.Artwork is not null) art.Children.Add(mode.Artwork());
            else art.Children.Add(new TextBlock
            {
                Text = mode.Emblem,
                FontSize = 48,
                Foreground = Ink("#F4DFC0"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
            art.Children.Add(new Border
            {
                Background = Ink("#990B1718"),
                VerticalAlignment = VerticalAlignment.Bottom,
                Child = new TextBlock
                {
                    Text = Label(mode.Category).ToUpperInvariant(),
                    FontSize = 10,
                    Foreground = Ink("#F4DFC0"),
                    Margin = new Thickness(14, 3, 14, 3),
                },
            });
            panel.Children.Add(art);

            var text = new StackPanel { Margin = new Thickness(16, 9, 16, 9) };
            text.Children.Add(new TextBlock { Text = Label(mode.Subtitle).ToUpperInvariant(), FontSize = 10, Foreground = accent });
            text.Children.Add(new TextBlock
            {
                Text = Label(mode.Title),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 25,
                Foreground = Ink("#F3E6D1"),
                Margin = new Thickness(0, 3, 0, 6),
            });
            text.Children.Add(new TextBlock
            {
                Text = Label(mode.Description),
                FontSize = 12,
                Foreground = Ink("#ADBCB6"),
                TextWrapping = TextWrapping.Wrap,
                Height = 50,
            });
            text.Children.Add(new TextBlock
            {
                Text = modes.CanOpen(mode.Id) ? Localization.T("Lobby.TakeSeat") : Localization.T("Lobby.CurrentlyOpen"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Ink("#E7CCA0"),
                Margin = new Thickness(0, 7, 0, 0),
            });
            Grid.SetRow(text, 1);
            panel.Children.Add(text);

            var primary = new Button
            {
                Name = "Mode_" + mode.Id.Replace('-', '_'),
                Tag = mode.Id,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Content = panel,
                Background = Ink("#111F20"),
                BorderBrush = accent,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = modes.CanOpen(mode.Id),
            };
            primary.Click += (_, _) => modes.Open(mode.Id);
            var wrapper = new Grid { Margin = new Thickness(8) };
            wrapper.Children.Add(primary);
            if (modes.HasSecondary(mode.Id))
            {
                var secondary = new Button
                {
                    Name = "ModeSecondary_" + mode.Id.Replace('-', '_'),
                    Tag = mode.Id + "-secondary",
                    Content = Localization.T("Lobby.NewWindow"),
                    ToolTip = modes.SecondaryLabel(mode.Id),
                    FontSize = 10,
                    Padding = new Thickness(9, 5, 9, 5),
                    Margin = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    IsEnabled = modes.CanOpenSecondary(mode.Id),
                };
                secondary.Click += (_, _) => modes.OpenSecondary(mode.Id);
                wrapper.Children.Add(secondary);
            }
            cards.Children.Add(wrapper);
        }
    }

    private static string Localized(string source) =>
        Localization.KeyForEnglishSource(source) is string key ? Localization.T(key) : source;

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
