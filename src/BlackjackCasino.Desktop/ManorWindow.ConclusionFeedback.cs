using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

internal partial class ManorWindow
{
    private Grid? conclusionOverlay;
    private Border? conclusionCard;
    private Image? conclusionArt;
    private TextBlock? conclusionEyebrow;
    private TextBlock? conclusionTitle;
    private TextBlock? conclusionValue;
    private TextBlock? conclusionNote;
    private bool enhancedChoiceFlowInitialized;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += ConclusionFeedback_Loaded;
    }

    private void ConclusionFeedback_Loaded(object sender, RoutedEventArgs e)
    {
        if (enhancedChoiceFlowInitialized)
        {
            return;
        }

        enhancedChoiceFlowInitialized = true;
        BuildConclusionOverlay();

        RecommendButton.Click -= Recommend_Click;
        RecommendButton.Click += EnhancedRecommend_Click;
        LeaveButton.Click -= Leave_Click;
        LeaveButton.Click += EnhancedLeave_Click;
        CandleButton.Click -= Candle_Click;
        CandleButton.Click += EnhancedCandle_Click;

        PreviewKeyDown -= Window_KeyDown;
        PreviewKeyDown += EnhancedWindow_KeyDown;

        RoutesPanel.LayoutUpdated += (_, _) => RewireRouteButtons();
        RewireRouteButtons();
    }

    private void RewireRouteButtons()
    {
        foreach (Button button in RoutesPanel.Children.OfType<Button>())
        {
            button.Click -= Route_Click;
            button.Click -= EnhancedRoute_Click;
            button.Click += EnhancedRoute_Click;
        }
    }

    private void BuildConclusionOverlay()
    {
        if (conclusionOverlay is not null || BonusPanel.Parent is not Grid root)
        {
            return;
        }

        conclusionEyebrow = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = ManorArtwork.Ink("#D8C8D7"),
            Margin = new Thickness(0, 0, 0, 8),
        };
        conclusionTitle = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 34,
            FontWeight = FontWeights.Bold,
            Foreground = ManorArtwork.Ink("#F4DDA8"),
            Margin = new Thickness(12, 0, 12, 8),
        };
        conclusionValue = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 27,
            FontWeight = FontWeights.Bold,
            Foreground = ManorArtwork.Ink("#FFF0C3"),
            Margin = new Thickness(0, 7, 0, 9),
        };
        conclusionNote = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 20,
            Foreground = ManorArtwork.Ink("#D6C8D9"),
            MaxWidth = 480,
            Margin = new Thickness(22, 0, 22, 6),
        };
        conclusionArt = new Image
        {
            Width = 112,
            Height = 112,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            RenderTransformOrigin = new Point(0.5, 0.5),
            Margin = new Thickness(0, 2, 0, 10),
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stack.Children.Add(conclusionEyebrow);
        stack.Children.Add(conclusionArt);
        stack.Children.Add(conclusionTitle);
        stack.Children.Add(conclusionValue);
        stack.Children.Add(conclusionNote);

        conclusionCard = new Border
        {
            Width = 590,
            MinHeight = 390,
            Padding = new Thickness(28, 24, 28, 22),
            CornerRadius = new CornerRadius(18),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new LinearGradientBrush(
                Color.FromRgb(29, 22, 34),
                Color.FromRgb(12, 12, 22),
                new Point(0, 0),
                new Point(1, 1)),
            Child = stack,
        };

        conclusionOverlay = new Grid
        {
            Visibility = Visibility.Collapsed,
            Opacity = 0,
            Background = new SolidColorBrush(Color.FromArgb(232, 7, 6, 13)),
            IsHitTestVisible = true,
        };
        conclusionOverlay.Children.Add(conclusionCard);
        Panel.SetZIndex(conclusionOverlay, 300);
        root.Children.Add(conclusionOverlay);
    }

    private async Task EnhancedRunChoice(Action<ManorSession> choice)
    {
        if (State.Run is not { Status: ManorRunStatus.Hunting })
        {
            return;
        }

        ClearRoutePreview();
        int oldBank = State.Run.Banked;
        int oldReward = State.Run.Current;
        controller.Execute(choice);
        ShowRun();

        ManorRunState run = State.Run!;
        if (run.Status == ManorRunStatus.Hunting)
        {
            if (run.Banked > oldBank)
            {
                audio.Play(ManorCue.Checkpoint);
            }
            else if (run.Current > oldReward)
            {
                audio.Play(ManorCue.Item);
            }
            else
            {
                audio.Play(new ManorRun(run).Distance <= 2 ? ManorCue.GhostNear : ManorCue.GhostFar);
            }
            return;
        }

        await PresentRunConclusion(run);
        DoorPanel.Visibility = Visibility.Collapsed;
        BonusPanel.Visibility = Visibility.Collapsed;
        play.Resume();
        await ContinueFeatures();
    }

    private async Task PresentRunConclusion(ManorRunState run)
    {
        if (run.Status is not (ManorRunStatus.Escaped or ManorRunStatus.Caught or ManorRunStatus.Exhausted))
        {
            await Wait(Timing(950, 450, 220));
            return;
        }

        BuildConclusionOverlay();
        if (conclusionOverlay is null || conclusionCard is null || conclusionArt is null
            || conclusionEyebrow is null || conclusionTitle is null || conclusionValue is null || conclusionNote is null)
        {
            await Wait(Timing(1200, 600, 300));
            return;
        }

        bool escaped = run.Status == ManorRunStatus.Escaped;
        bool maximum = escaped && run.ExitTarget == 1000;
        Color accent = escaped ? Color.FromRgb(230, 198, 126) : Color.FromRgb(205, 102, 139);
        Color secondary = escaped ? Color.FromRgb(118, 207, 203) : Color.FromRgb(136, 74, 123);

        conclusionEyebrow.Text = escaped
            ? maximum ? "THE FINAL DOOR YIELDS" : "THE NIGHT LETS YOU GO"
            : run.Status == ManorRunStatus.Exhausted ? "THE CLOCK STRIKES" : "THE FOOTSTEPS STOP BEHIND YOU";
        conclusionTitle.Text = escaped
            ? maximum ? "THE MOONLIT GATE" : "YOU ESCAPED THE MANOR"
            : run.Status == ManorRunStatus.Exhausted ? "NO TIME LEFT" : "THE MANOR FOUND YOU";
        conclusionNote.Text = escaped
            ? maximum
                ? "You reached the far side of the Manor and claimed its maximum escape reward."
                : "The gate opens behind you. Everything you carried out is secured."
            : $"The ghost caught you before the next safe room. Your last checkpoint still pays {run.Multiplier(run.Banked):0.##}×.";
        conclusionArt.Source = ManorArtwork.Symbol(escaped ? ManorSymbol.Bonus : ManorSymbol.Lady);
        conclusionValue.Text = "0×";

        conclusionCard.BorderBrush = new SolidColorBrush(accent);
        conclusionCard.Effect = new DropShadowEffect
        {
            Color = accent,
            BlurRadius = escaped ? 32 : 26,
            ShadowDepth = 0,
            Opacity = escaped ? 0.62 : 0.55,
        };
        conclusionTitle.Foreground = new SolidColorBrush(accent);
        conclusionValue.Foreground = new SolidColorBrush(escaped ? Color.FromRgb(255, 235, 180) : Color.FromRgb(255, 188, 207));
        conclusionOverlay.Background = new LinearGradientBrush(
            Color.FromArgb(242, escaped ? (byte)14 : (byte)25, 9, escaped ? (byte)20 : (byte)22),
            Color.FromArgb(245, 7, escaped ? (byte)16 : (byte)7, escaped ? (byte)20 : (byte)15),
            new Point(0, 0), new Point(1, 1));
        conclusionOverlay.Visibility = Visibility.Visible;
        conclusionOverlay.Opacity = Animations ? 0 : 1;

        if (Animations)
        {
            conclusionOverlay.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Timing(420, 240, 150)))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.HoldEnd,
                });

            var scale = new ScaleTransform(escaped ? 0.68 : 1.18, escaped ? 0.68 : 1.18);
            conclusionArt.RenderTransform = scale;
            var scaleAnimation = new DoubleAnimation(escaped ? 0.68 : 1.18, 1, TimeSpan.FromMilliseconds(Timing(780, 430, 260)))
            {
                EasingFunction = new BackEase { Amplitude = escaped ? 0.28 : 0.12, EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            var glow = new DropShadowEffect { Color = secondary, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.3 };
            conclusionArt.Effect = glow;
            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                new DoubleAnimation(12, escaped ? 38 : 30, TimeSpan.FromMilliseconds(650))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                });
            glow.BeginAnimation(DropShadowEffect.OpacityProperty,
                new DoubleAnimation(0.28, escaped ? 0.82 : 0.68, TimeSpan.FromMilliseconds(650))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                });
        }

        decimal finalMultiplier = run.PaidMultiplier;
        decimal finalMoney = finalMultiplier * State.Bet;
        int countDuration = Animations ? Timing(1500, 820, 520) : 0;
        if (countDuration > 0)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < countDuration)
            {
                double raw = Math.Clamp((double)watch.ElapsedMilliseconds / countDuration, 0d, 1d);
                double eased = 1d - Math.Pow(1d - raw, 3d);
                decimal multiplier = finalMultiplier * (decimal)eased;
                decimal money = finalMoney * (decimal)eased;
                conclusionValue.Text = $"{multiplier:0.##}×  ·  {Money(money)}";
                await Wait(24);
            }
        }
        conclusionValue.Text = $"{finalMultiplier:0.##}×  ·  {Money(finalMoney)}";

        int hold = escaped
            ? Timing(maximum ? 3600 : 2900, maximum ? 2300 : 1900, maximum ? 1550 : 1250)
            : Timing(2300, 1550, 1050);
        await Wait(hold);

        if (Animations)
        {
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(Timing(420, 230, 140)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
                FillBehavior = FillBehavior.HoldEnd,
            };
            conclusionOverlay.BeginAnimation(OpacityProperty, fade);
            await Wait(Timing(430, 240, 150));
        }

        conclusionOverlay.BeginAnimation(OpacityProperty, null);
        conclusionOverlay.Opacity = 0;
        conclusionOverlay.Visibility = Visibility.Collapsed;
        conclusionArt.BeginAnimation(OpacityProperty, null);
        conclusionArt.Effect = null;
    }

    private async void EnhancedRoute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int room })
        {
            await RunOperation(() => EnhancedRunChoice(s => s.Move(room)));
        }
    }

    private async void EnhancedRecommend_Click(object sender, RoutedEventArgs e) =>
        await RunOperation(() => EnhancedRunChoice(s => s.FollowPolicy()));

    private async void EnhancedLeave_Click(object sender, RoutedEventArgs e) =>
        await RunOperation(() => EnhancedRunChoice(s => s.CashOut()));

    private async void EnhancedCandle_Click(object sender, RoutedEventArgs e) =>
        await RunOperation(() => EnhancedRunChoice(s => s.UseCandle()));

    private async void EnhancedWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        if (e.IsRepeat)
        {
            e.Handled = true;
            return;
        }
        if (Keyboard.FocusedElement is TextBox or ComboBox)
        {
            return;
        }
        if (e.Key == Key.X)
        {
            Stop_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (BonusPanel.Visibility == Visibility.Visible && State.Run is { Status: ManorRunStatus.Hunting })
        {
            Action<ManorSession>? choice = e.Key switch
            {
                Key.R => s => s.FollowPolicy(),
                Key.L => s => s.CashOut(),
                Key.C when State.Run.HasCandle => s => s.UseCandle(),
                _ => null,
            };
            int number = e.Key >= Key.D1 && e.Key <= Key.D5
                ? e.Key - Key.D1
                : e.Key >= Key.NumPad1 && e.Key <= Key.NumPad5 ? e.Key - Key.NumPad1 : -1;
            if (number >= 0 && RoutesPanel.Children.OfType<Button>().ElementAtOrDefault(number) is { IsEnabled: true, Tag: int room })
            {
                choice = s => s.Move(room);
            }
            if (choice is not null)
            {
                e.Handled = true;
                await RunOperation(() => EnhancedRunChoice(choice));
            }
            return;
        }

        if (e.Key == Key.Space && Keyboard.FocusedElement is not Button and not CheckBox)
        {
            e.Handled = true;
            await RunOperation(() => State.CycleActive ? Resume() : StartPaid(false));
        }
    }
}
