using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoVaultWindow : Window
{
    private readonly Func<CasinoVaultSnapshot> snapshotProvider;
    private readonly Func<CasinoVaultOpenResult> openVault;
    private readonly Func<bool> animationsEnabled;
    private readonly Func<bool> soundEnabled;
    private readonly object audioOwner = new();

    private readonly TextBlock tierText = new();
    private readonly TextBlock amountText = new();
    private readonly TextBlock percentText = new();
    private readonly TextBlock helperText = new();
    private readonly TextBlock lifetimeText = new();
    private readonly ProgressBar progress = new();
    private readonly Button openButton = new();
    private readonly Border safeDoor = new();
    private readonly Border safeGlow = new();
    private readonly Border rewardPanel = new();
    private readonly TextBlock rewardBase = new();
    private readonly TextBlock rewardBonus = new();
    private readonly TextBlock rewardTotal = new();
    private readonly RotateTransform wheelRotation = new();
    private readonly ScaleTransform doorScale = new(1, 1);
    private readonly ScaleTransform rewardScale = new(0.92, 0.92);

    internal CasinoVaultWindow(
        Func<CasinoVaultSnapshot> snapshotProvider,
        Func<CasinoVaultOpenResult> openVault,
        Func<bool> animationsEnabled,
        Func<bool> soundEnabled)
    {
        this.snapshotProvider = snapshotProvider;
        this.openVault = openVault;
        this.animationsEnabled = animationsEnabled;
        this.soundEnabled = soundEnabled;

        Title = "Casino Vault · Blackjack Casino";
        Width = 1_040;
        Height = 720;
        MinWidth = 900;
        MinHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush("#071014");
        Foreground = Brush("#F5EEDC");
        FontFamily = new FontFamily("Segoe UI");
        Content = BuildLayout();
        Closed += (_, _) => CasinoAudio.Mixer.Stop(audioOwner);
        Refresh();
    }

    private UIElement BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(28) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.92, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.08, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel { Margin = new Thickness(4, 0, 4, 18) };
        heading.Children.Add(new TextBlock
        {
            Text = "THE MIDNIGHT CLUB",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#B89B68"),
            CharacterSpacing = 180,
        });
        heading.Children.Add(new TextBlock
        {
            Text = "CASINO VAULT",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 34,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#F8E8C9"),
            Margin = new Thickness(0, 4, 0, 3),
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Every real wager moves something forward — even on a cold night.",
            FontSize = 13,
            Foreground = Brush("#9DB2AD"),
        });
        Grid.SetColumnSpan(heading, 2);
        root.Children.Add(heading);

        var safeHost = new Grid { Margin = new Thickness(10, 12, 28, 12) };
        safeHost.Children.Add(BuildSafe());
        Grid.SetRow(safeHost, 1);
        root.Children.Add(safeHost);

        var info = BuildInfoPanel();
        Grid.SetRow(info, 1);
        Grid.SetColumn(info, 1);
        root.Children.Add(info);

        var footer = new Grid { Margin = new Thickness(4, 18, 4, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        helperText.FontSize = 11;
        helperText.Foreground = Brush("#7F9791");
        helperText.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(helperText);
        var close = new Button
        {
            Content = "CLOSE",
            Padding = new Thickness(24, 9, 24, 9),
            MinWidth = 110,
            Background = Brush("#102328"),
            BorderBrush = Brush("#385057"),
            Foreground = Brush("#DDE7E3"),
        };
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 2);
        Grid.SetColumnSpan(footer, 2);
        root.Children.Add(footer);

        return root;
    }

    private UIElement BuildSafe()
    {
        var frame = new Grid
        {
            Width = 390,
            Height = 390,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        safeGlow.CornerRadius = new CornerRadius(54);
        safeGlow.Margin = new Thickness(14);
        safeGlow.Background = new RadialGradientBrush(
            Color.FromArgb(115, 221, 176, 75),
            Color.FromArgb(0, 221, 176, 75));
        safeGlow.Opacity = 0.32;
        safeGlow.Effect = new BlurEffect { Radius = 28 };
        frame.Children.Add(safeGlow);

        var body = new Border
        {
            Margin = new Thickness(24),
            CornerRadius = new CornerRadius(42),
            BorderThickness = new Thickness(4),
            BorderBrush = new LinearGradientBrush(BrushColor("#6E7477"), BrushColor("#252D31"), 45),
            Background = new LinearGradientBrush(BrushColor("#1C2529"), BrushColor("#091216"), 125),
            Effect = new DropShadowEffect { BlurRadius = 26, ShadowDepth = 10, Opacity = 0.65, Color = Colors.Black },
        };
        frame.Children.Add(body);

        safeDoor.Margin = new Thickness(48);
        safeDoor.CornerRadius = new CornerRadius(135);
        safeDoor.BorderThickness = new Thickness(5);
        safeDoor.BorderBrush = new LinearGradientBrush(BrushColor("#E2BC68"), BrushColor("#66502A"), 35);
        safeDoor.Background = new RadialGradientBrush(BrushColor("#303A3D"), BrushColor("#11191D"));
        safeDoor.RenderTransformOrigin = new Point(0.5, 0.5);
        safeDoor.RenderTransform = doorScale;

        var doorGrid = new Grid();
        var ring = new Ellipse
        {
            Width = 168,
            Height = 168,
            Stroke = Brush("#B99A5D"),
            StrokeThickness = 8,
            Fill = Brush("#172125"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        doorGrid.Children.Add(ring);

        var wheel = new Grid
        {
            Width = 150,
            Height = 150,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = wheelRotation,
        };
        wheel.Children.Add(new Ellipse { Stroke = Brush("#D5BA7B"), StrokeThickness = 3, Margin = new Thickness(26) });
        for (int i = 0; i < 6; i++)
        {
            var spoke = new Border
            {
                Width = 8,
                Height = 66,
                CornerRadius = new CornerRadius(4),
                Background = Brush("#D5BA7B"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 0, 0),
                RenderTransformOrigin = new Point(0.5, 1),
                RenderTransform = new RotateTransform(i * 60),
            };
            wheel.Children.Add(spoke);
        }
        wheel.Children.Add(new Ellipse
        {
            Width = 38,
            Height = 38,
            Fill = new RadialGradientBrush(BrushColor("#F4D68D"), BrushColor("#755928")),
            Stroke = Brush("#34270F"),
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        doorGrid.Children.Add(wheel);

        var plate = new Border
        {
            Padding = new Thickness(16, 6, 16, 6),
            CornerRadius = new CornerRadius(7),
            Background = Brush("#CC090F12"),
            BorderBrush = Brush("#7C6840"),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 25),
            Child = new TextBlock
            {
                Text = "MIDNIGHT RESERVE",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brush("#E6D09A"),
                CharacterSpacing = 100,
            },
        };
        doorGrid.Children.Add(plate);
        safeDoor.Child = doorGrid;
        frame.Children.Add(safeDoor);

        rewardPanel.Background = Brush("#EE111B1E");
        rewardPanel.BorderBrush = Brush("#E6BF62");
        rewardPanel.BorderThickness = new Thickness(2);
        rewardPanel.CornerRadius = new CornerRadius(18);
        rewardPanel.Padding = new Thickness(22);
        rewardPanel.Margin = new Thickness(52);
        rewardPanel.Visibility = Visibility.Collapsed;
        rewardPanel.RenderTransformOrigin = new Point(0.5, 0.5);
        rewardPanel.RenderTransform = rewardScale;
        rewardPanel.Effect = new DropShadowEffect { Color = BrushColor("#E0AC45"), BlurRadius = 28, ShadowDepth = 0, Opacity = 0.45 };
        var rewardStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        rewardStack.Children.Add(new TextBlock
        {
            Text = "VAULT OPENED",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#E8C974"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        rewardBase.FontSize = 15;
        rewardBase.Foreground = Brush("#C5D3CF");
        rewardBase.HorizontalAlignment = HorizontalAlignment.Center;
        rewardBase.Margin = new Thickness(0, 20, 0, 5);
        rewardStack.Children.Add(rewardBase);
        rewardBonus.FontSize = 18;
        rewardBonus.FontWeight = FontWeights.Bold;
        rewardBonus.Foreground = Brush("#78E0B9");
        rewardBonus.HorizontalAlignment = HorizontalAlignment.Center;
        rewardStack.Children.Add(rewardBonus);
        rewardTotal.FontFamily = new FontFamily("Georgia");
        rewardTotal.FontSize = 34;
        rewardTotal.FontWeight = FontWeights.Bold;
        rewardTotal.Foreground = Brush("#FFE29A");
        rewardTotal.HorizontalAlignment = HorizontalAlignment.Center;
        rewardTotal.Margin = new Thickness(0, 10, 0, 0);
        rewardStack.Children.Add(rewardTotal);
        rewardPanel.Child = rewardStack;
        frame.Children.Add(rewardPanel);

        return frame;
    }

    private UIElement BuildInfoPanel()
    {
        var panel = new Border
        {
            Margin = new Thickness(8, 12, 4, 12),
            Padding = new Thickness(26),
            CornerRadius = new CornerRadius(18),
            Background = Brush("#B5101C20"),
            BorderBrush = Brush("#324A50"),
            BorderThickness = new Thickness(1),
        };
        var stack = new StackPanel();
        tierText.FontFamily = new FontFamily("Georgia");
        tierText.FontSize = 27;
        tierText.FontWeight = FontWeights.Bold;
        tierText.Foreground = Brush("#F5DB9A");
        stack.Children.Add(tierText);

        amountText.FontSize = 26;
        amountText.FontWeight = FontWeights.Bold;
        amountText.Foreground = Brush("#F9F3E7");
        amountText.Margin = new Thickness(0, 18, 0, 3);
        stack.Children.Add(amountText);

        percentText.FontSize = 12;
        percentText.Foreground = Brush("#8EB6AA");
        stack.Children.Add(percentText);

        progress.Height = 16;
        progress.Margin = new Thickness(0, 12, 0, 18);
        progress.Minimum = 0;
        progress.Maximum = 1;
        progress.Background = Brush("#19292D");
        progress.Foreground = Brush("#DDB04D");
        stack.Children.Add(progress);

        stack.Children.Add(InfoLine("CONTRIBUTION", "2% of every eligible play-money wager"));
        stack.Children.Add(InfoLine("FULL VAULT", "Open only at 100% · bonus is added on top"));
        stack.Children.Add(InfoLine("NO CATCH", "Free spins, rewards and Empire income never refill it"));

        lifetimeText.Margin = new Thickness(0, 22, 0, 0);
        lifetimeText.Padding = new Thickness(14);
        lifetimeText.Background = Brush("#10191D");
        lifetimeText.Foreground = Brush("#AFC1BC");
        lifetimeText.FontSize = 12;
        lifetimeText.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(lifetimeText);

        openButton.Height = 54;
        openButton.Margin = new Thickness(0, 22, 0, 0);
        openButton.FontSize = 15;
        openButton.FontWeight = FontWeights.Bold;
        openButton.Background = Brush("#A67D2D");
        openButton.Foreground = Brush("#160F05");
        openButton.BorderBrush = Brush("#F0CE78");
        openButton.BorderThickness = new Thickness(1.5);
        openButton.Click += Open_Click;
        stack.Children.Add(openButton);

        panel.Child = stack;
        return panel;
    }

    private static Border InfoLine(string label, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(126) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#B69862"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        var text = new TextBlock
        {
            Text = value,
            FontSize = 12,
            Foreground = Brush("#B8C8C3"),
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return new Border { Padding = new Thickness(0, 8, 0, 8), BorderBrush = Brush("#22353A"), BorderThickness = new Thickness(0, 0, 0, 1), Child = grid };
    }

    private void Refresh()
    {
        CasinoVaultSnapshot snapshot = snapshotProvider();
        Color accent = BrushColor(snapshot.Tier.Accent);
        tierText.Text = snapshot.Tier.Name.ToUpperInvariant();
        tierText.Foreground = new SolidColorBrush(accent);
        amountText.Text = $"{Money(snapshot.CurrentAmount)}  /  {Money(snapshot.Tier.Capacity)}";
        percentText.Text = $"{snapshot.Progress:P1} full · {snapshot.Tier.FullBonusRate:P0} full-vault bonus";
        progress.Value = (double)snapshot.Progress;
        progress.Foreground = new SolidColorBrush(accent);

        string next = snapshot.NextTier is null
            ? "Maximum Vault tier unlocked."
            : $"Next tier: {snapshot.NextTier.Name} at {Money(snapshot.NextTier.RequiredLifetimeWager)} lifetime wager.";
        if (!string.Equals(snapshot.UnlockedTier.Id, snapshot.Tier.Id, StringComparison.Ordinal))
        {
            next = $"{snapshot.UnlockedTier.Name} is unlocked and will apply to the next fresh Vault.";
        }
        helperText.Text = next;
        lifetimeText.Text = $"Vaults opened  {snapshot.VaultsOpened:N0}   ·   Contributions  {Money(snapshot.LifetimeContributions)}\n"
            + $"Paid back  {Money(snapshot.LifetimePayouts)}   ·   Full bonuses  {Money(snapshot.LifetimeBonuses)}   ·   Best  {Money(snapshot.BiggestPayout)}";

        if (snapshot.PayoutPending)
        {
            openButton.Content = "PAYOUT RECOVERY IN PROGRESS";
            openButton.IsEnabled = false;
        }
        else if (snapshot.IsFull)
        {
            openButton.Content = $"BREAK OPEN  ·  +{snapshot.Tier.FullBonusRate:P0} BONUS";
            openButton.IsEnabled = true;
            safeGlow.Opacity = 0.72;
            if (animationsEnabled())
            {
                safeGlow.BeginAnimation(OpacityProperty, new DoubleAnimation(0.42, 0.88, TimeSpan.FromMilliseconds(850))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                });
            }
        }
        else
        {
            openButton.Content = snapshot.Progress >= 0.90m ? "ALMOST FULL" : "VAULT LOCKED";
            openButton.IsEnabled = false;
            if (snapshot.Progress >= 0.90m && animationsEnabled())
            {
                safeGlow.BeginAnimation(OpacityProperty, new DoubleAnimation(0.25, 0.58, TimeSpan.FromSeconds(1.25))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                });
            }
            else
            {
                safeGlow.BeginAnimation(OpacityProperty, null);
                safeGlow.Opacity = 0.32;
            }
        }
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        openButton.IsEnabled = false;
        CasinoVaultOpenResult result;
        try
        {
            // The financial transition completes before any theatrical presentation begins.
            result = openVault();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or OverflowException)
        {
            helperText.Text = "Vault could not be opened: " + ex.Message;
            Refresh();
            return;
        }

        rewardBase.Text = $"Reserve released  {Money(result.BaseAmount)}";
        rewardBonus.Text = $"FULL VAULT BONUS  +{Money(result.BonusAmount)}  ({result.BonusRate:P0})";
        rewardTotal.Text = "+" + Money(result.TotalPayout);

        if (!animationsEnabled())
        {
            RevealReward();
            return;
        }

        CasinoVaultAudio.Play(CasinoVaultCue.Unlock, audioOwner, soundEnabled());
        wheelRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 760, TimeSpan.FromMilliseconds(980))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.HoldEnd,
        });
        safeGlow.BeginAnimation(OpacityProperty, new DoubleAnimation(0.55, 1, TimeSpan.FromMilliseconds(420))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2),
        });
        await Task.Delay(820);

        CasinoVaultAudio.Play(CasinoVaultCue.Door, audioOwner, soundEnabled());
        doorScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0.84, TimeSpan.FromMilliseconds(620))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        });
        doorScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.84, TimeSpan.FromMilliseconds(620))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        });
        safeDoor.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.10, TimeSpan.FromMilliseconds(680)));
        await Task.Delay(610);
        RevealReward();
    }

    private void RevealReward()
    {
        rewardPanel.Visibility = Visibility.Visible;
        safeDoor.Visibility = Visibility.Hidden;
        rewardScale.ScaleX = rewardScale.ScaleY = animationsEnabled() ? 0.92 : 1;
        rewardPanel.Opacity = animationsEnabled() ? 0 : 1;
        if (animationsEnabled())
        {
            rewardPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420)));
            rewardScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(520))
            {
                EasingFunction = new BackEase { Amplitude = 0.28, EasingMode = EasingMode.EaseOut },
            });
            rewardScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(520))
            {
                EasingFunction = new BackEase { Amplitude = 0.28, EasingMode = EasingMode.EaseOut },
            });
        }
        CasinoVaultAudio.Play(CasinoVaultCue.Reward, audioOwner, soundEnabled());
        helperText.Text = "Payout transferred to your play-money bankroll. A fresh Vault is ready when you close this screen.";
        openButton.Content = "VAULT OPENED";
        openButton.IsEnabled = false;
    }

    private static string Money(decimal amount) => string.Format(CultureInfo.CurrentCulture, "${0:N2}", amount);
    private static SolidColorBrush Brush(string hex) => new(BrushColor(hex));
    private static Color BrushColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
