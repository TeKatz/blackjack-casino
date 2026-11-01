using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>Focused celebration shown only after a theme reward has been durably claimed.</summary>
internal sealed class CollectionThemeRewardWindow : Window
{
    private readonly bool animationsEnabled;
    private readonly Border shell;
    private readonly DropShadowEffect glow;
    private readonly ScaleTransform scale = new(.94, .94);

    internal CollectionThemeRewardWindow(
        CollectionTheme theme,
        CollectionThemeRewardClaimResult result,
        bool animationsEnabled)
    {
        this.animationsEnabled = animationsEnabled;
        Color accent = (Color)ColorConverter.ConvertFromString(theme.Accent);
        Color packColor = CollectionArtwork.RarityColor(result.PackRarity ?? CollectionRarity.Rare);

        Title = theme.Name + " — Collection Complete";
        Width = 560;
        Height = 410;
        MinWidth = 560;
        MinHeight = 410;
        MaxWidth = 560;
        MaxHeight = 410;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        FontFamily = new FontFamily("Segoe UI");
        Foreground = new SolidColorBrush(Color.FromRgb(242, 238, 228));

        glow = new DropShadowEffect
        {
            Color = accent,
            ShadowDepth = 0,
            BlurRadius = 18,
            Opacity = .54,
        };
        shell = new Border
        {
            Margin = new Thickness(28),
            Padding = new Thickness(28, 24, 28, 22),
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1.5),
            BorderBrush = new SolidColorBrush(accent),
            Background = new LinearGradientBrush(Color.FromRgb(8, 25, 24), Color.FromRgb(10, 16, 26), 35),
            Effect = glow,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = scale,
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        heading.Children.Add(new TextBlock
        {
            Text = "COLLECTION COMPLETE",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(accent),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        heading.Children.Add(new TextBlock
        {
            Text = theme.Symbol,
            FontSize = 42,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(accent),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
            Effect = new DropShadowEffect { Color = accent, ShadowDepth = 0, BlurRadius = 14, Opacity = .7 },
        });
        heading.Children.Add(new TextBlock
        {
            Text = theme.Name,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(247, 232, 207)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0),
        });
        grid.Children.Add(heading);

        var congratulations = new TextBlock
        {
            Text = $"Congratulations — every card in {theme.Name} is now part of your album.",
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(178, 195, 187)),
            FontSize = 12,
            Margin = new Thickness(24, 12, 24, 15),
        };
        Grid.SetRow(congratulations, 1);
        grid.Children.Add(congratulations);

        var rewards = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        rewards.Children.Add(RewardCard(
            "◆",
            $"+{result.Coins:N0}",
            "COSMETIC COINS",
            Color.FromRgb(84, 225, 183)));
        rewards.Children.Add(RewardCard(
            "✦",
            $"+{result.PackCount} {result.PackRarity}",
            result.PackCount == 1 ? "CARD PACK" : "CARD PACKS",
            packColor));
        Grid.SetRow(rewards, 2);
        grid.Children.Add(rewards);

        var close = new Button
        {
            Name = "ThemeRewardContinue",
            Content = "CONTINUE",
            Width = 150,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 18, 0, 0),
            Background = new SolidColorBrush(Color.FromArgb(80, accent.R, accent.G, accent.B)),
            BorderBrush = new SolidColorBrush(accent),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Color.FromRgb(246, 241, 226)),
            FontWeight = FontWeights.Bold,
            Cursor = CasinoCursors.Hand,
        };
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 3);
        grid.Children.Add(close);

        shell.Child = grid;
        Content = shell;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            Close();
        };
        Loaded += (_, _) =>
        {
            close.Focus();
            StartPresentation();
        };
        Closed += (_, _) => StopPresentation();
    }

    private static Border RewardCard(string symbol, string value, string caption, Color accent)
    {
        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(new TextBlock
        {
            Text = symbol,
            FontSize = 23,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(accent),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        content.Children.Add(new TextBlock
        {
            Text = value.ToUpperInvariant(),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(245, 240, 226)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        });
        content.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(151, 171, 162)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        });
        return new Border
        {
            Width = 205,
            Height = 106,
            Margin = new Thickness(7, 0, 7, 0),
            Padding = new Thickness(12, 10, 12, 10),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(180, accent.R, accent.G, accent.B)),
            Background = new LinearGradientBrush(Color.FromRgb(10, 33, 28), Color.FromRgb(10, 23, 28), 20),
            Child = content,
        };
    }

    private void StartPresentation()
    {
        if (!animationsEnabled)
        {
            shell.Opacity = 1;
            scale.ScaleX = scale.ScaleY = 1;
            return;
        }

        shell.Opacity = 0;
        shell.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });
        var grow = new DoubleAnimation(.94, 1, TimeSpan.FromMilliseconds(330))
        {
            EasingFunction = new BackEase { Amplitude = .22, EasingMode = EasingMode.EaseOut },
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(15, 31, TimeSpan.FromSeconds(1.25))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
        glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(.34, .82, TimeSpan.FromSeconds(1.25))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
    }

    private void StopPresentation()
    {
        shell.BeginAnimation(UIElement.OpacityProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
        glow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
    }
}
