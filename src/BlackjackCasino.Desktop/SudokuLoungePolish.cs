using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Presentation-only polish for Sudoku inside the Chill Lounge. It deliberately lives outside
/// SudokuView so the puzzle/input model stays independent from completion effects and navigation.
/// </summary>
internal static class SudokuLoungePolish
{
    private sealed class ViewState
    {
        internal required Grid Host { get; init; }
        internal required DispatcherTimer CompletionWatch { get; init; }
        internal bool CompletionShown { get; set; }
    }

    private static readonly ConditionalWeakTable<SudokuView, ViewState> States = new();
    private static bool installed;

    internal static void EnsureInstalled()
    {
        if (installed)
        {
            return;
        }

        installed = true;
        EventManager.RegisterClassHandler(
            typeof(SudokuView),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnSudokuLoaded));
        EventManager.RegisterClassHandler(
            typeof(SudokuView),
            FrameworkElement.UnloadedEvent,
            new RoutedEventHandler(OnSudokuUnloaded));
    }

    private static void OnSudokuLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not SudokuView view || States.TryGetValue(view, out _))
        {
            return;
        }

        if (view.Content is not UIElement original)
        {
            return;
        }

        view.Content = null;
        var host = new Grid
        {
            ClipToBounds = true,
            Background = Brushes.Transparent,
        };
        host.Children.Add(original);
        view.Content = host;

        ApplyFlatTogglePresentation(host);

        var watch = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120),
        };
        var state = new ViewState
        {
            Host = host,
            CompletionWatch = watch,
            CompletionShown = view.Snapshot.Completed,
        };
        States.Add(view, state);

        watch.Tick += (_, _) => TryShowCompletion(view, state);
        view.AddHandler(
            ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => TryShowCompletion(view, state)),
            handledEventsToo: true);
        view.AddHandler(
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler((_, _) => TryShowCompletion(view, state)),
            handledEventsToo: true);

        if (!state.CompletionShown)
        {
            watch.Start();
        }
    }

    private static void TryShowCompletion(SudokuView view, ViewState state)
    {
        if (state.CompletionShown || !view.Snapshot.Completed)
        {
            return;
        }

        state.CompletionShown = true;
        state.CompletionWatch.Stop();
        ShowCompletion(view, state.Host);
    }

    private static void OnSudokuUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is SudokuView view && States.TryGetValue(view, out ViewState? state))
        {
            state.CompletionWatch.Stop();
        }
    }

    private static void ApplyFlatTogglePresentation(DependencyObject root)
    {
        ControlTemplate template = CreateFlatControlTemplate(typeof(ToggleButton), new CornerRadius(3));
        foreach (ToggleButton toggle in FindVisualChildren<ToggleButton>(root))
        {
            toggle.Template = template;
            toggle.FocusVisualStyle = null;
            toggle.Cursor = Cursors.Hand;
        }
    }

    private static void ShowCompletion(SudokuView view, Grid host)
    {
        SudokuGameState snapshot = view.Snapshot;
        SudokuPuzzle puzzle = SudokuLibrary.Get(view.PuzzleId);

        var overlay = new Grid
        {
            Name = "SudokuCompletionOverlay",
            Background = new SolidColorBrush(Color.FromArgb(226, 5, 12, 14)),
            Opacity = 0,
            Focusable = true,
        };
        Panel.SetZIndex(overlay, 100);

        var halo = new Ellipse
        {
            Width = 760,
            Height = 620,
            IsHitTestVisible = false,
            Opacity = 0.44,
            Fill = new RadialGradientBrush(
                Color.FromArgb(54, 102, 190, 158),
                Color.FromArgb(0, 5, 12, 14)),
        };
        overlay.Children.Add(halo);

        var card = new Border
        {
            Width = 650,
            MaxWidth = 650,
            Padding = new Thickness(38, 34, 38, 32),
            CornerRadius = new CornerRadius(20),
            BorderThickness = new Thickness(1),
            BorderBrush = Ink("#70998B"),
            Background = new LinearGradientBrush(
                InkColor("#16292A"),
                InkColor("#0F1B1D"),
                new Point(0, 0),
                new Point(1, 1)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0,
            Effect = new DropShadowEffect
            {
                BlurRadius = 42,
                ShadowDepth = 0,
                Opacity = 0.58,
                Color = InkColor("#071012"),
            },
            RenderTransformOrigin = new Point(0.5, 0.5),
        };
        var scale = new ScaleTransform(0.94, 0.94);
        card.RenderTransform = scale;
        overlay.Children.Add(card);

        var content = new StackPanel();
        card.Child = content;

        content.Children.Add(new TextBlock
        {
            Text = puzzle.Pool == SudokuPuzzlePool.Daily ? "DAILY COMPLETE" : "PUZZLE COMPLETE",
            Foreground = Ink("#8EC9B0"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
        });
        content.Children.Add(new TextBlock
        {
            Text = puzzle.Pool == SudokuPuzzlePool.Daily ? "A quiet win for today." : "Nicely done.",
            Foreground = Ink("#F4E8D7"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 34,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 7, 0, 7),
        });
        content.Children.Add(new TextBlock
        {
            Text = snapshot.HintsUsed == 0
                ? "Clean solve — no hints used."
                : snapshot.HintsUsed == 1
                    ? "One hint helped along the way."
                    : $"{snapshot.HintsUsed} hints helped along the way.",
            Foreground = Ink("#AFC4BC"),
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 24),
        });

        var stats = new UniformGrid
        {
            Rows = 1,
            Columns = 4,
            Margin = new Thickness(0, 0, 0, 27),
        };
        stats.Children.Add(CompletionStat("TIME", FormatTime(snapshot.ElapsedSeconds)));
        stats.Children.Add(CompletionStat("HINTS", snapshot.HintsUsed.ToString()));
        stats.Children.Add(CompletionStat("DIFFICULTY", DifficultyName(puzzle.Difficulty).ToUpperInvariant()));
        stats.Children.Add(CompletionStat("MODE", puzzle.Pool == SudokuPuzzlePool.Daily ? "DAILY" : "FREE PLAY"));
        content.Children.Add(stats);

        content.Children.Add(new TextBlock
        {
            Name = "SudokuCompletionXp", Text = view.CompletionRewardNotice?.Invoke() ?? "",
            Foreground = Ink("#8EC9B0"), FontSize = 14, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 18),
        });
        var actions = new UniformGrid { Rows = 1, Columns = 3 };
        Button backToPuzzle = CompletionButton("BACK TO PUZZLE", "SudokuCompletionBackToPuzzle", false);
        Button returnToLounge = CompletionButton("RETURN TO LOUNGE", "SudokuCompletionReturnToLounge", true);
        Button returnToClub = CompletionButton("RETURN TO CLUB", "SudokuCompletionReturnToClub", false);
        actions.Children.Add(backToPuzzle);
        actions.Children.Add(returnToLounge);
        actions.Children.Add(returnToClub);
        content.Children.Add(actions);

        backToPuzzle.Click += (_, _) =>
        {
            host.Children.Remove(overlay);
            view.Focus();
        };
        returnToLounge.Click += (_, _) => RaiseButton(view, "← LOUNGE", null);
        returnToClub.Click += (_, _) =>
        {
            Window? window = Window.GetWindow(view) ?? Application.Current?.MainWindow;
            if (window is not null)
            {
                RaiseButton(window, "RETURN TO CLUB", returnToClub);
            }
        };

        host.Children.Add(overlay);
        overlay.Focus();

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        overlay.BeginAnimation(UIElement.OpacityProperty, fade);

        card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(360))
        {
            BeginTime = TimeSpan.FromMilliseconds(65),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        });
        var settle = new CubicEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(460))
        {
            BeginTime = TimeSpan.FromMilliseconds(45),
            EasingFunction = settle,
        });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(460))
        {
            BeginTime = TimeSpan.FromMilliseconds(45),
            EasingFunction = settle,
        });
    }

    private static Border CompletionStat(string label, string value)
    {
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 9,
            Foreground = Ink("#7E9A91"),
            TextAlignment = TextAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#EEE5D6"),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0),
        });
        return new Border
        {
            Background = Ink("#122123"),
            BorderBrush = Ink("#31504B"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 11, 8, 11),
            Margin = new Thickness(4),
            Child = stack,
        };
    }

    private static Button CompletionButton(string text, string name, bool primary)
    {
        var button = new Button
        {
            Name = name,
            Content = text,
            Foreground = Ink(primary ? "#F5EBDD" : "#DDD9CD"),
            Background = Ink(primary ? "#31564D" : "#17282A"),
            BorderBrush = Ink(primary ? "#8EC9B0" : "#4D6B65"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 12, 10, 12),
            Margin = new Thickness(4),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
        };
        button.Template = CreateFlatControlTemplate(typeof(Button), new CornerRadius(6));
        return button;
    }

    private static ControlTemplate CreateFlatControlTemplate(Type targetType, CornerRadius cornerRadius)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, TemplateBinding(nameof(Control.Background)));
        border.SetBinding(Border.BorderBrushProperty, TemplateBinding(nameof(Control.BorderBrush)));
        border.SetBinding(Border.BorderThicknessProperty, TemplateBinding(nameof(Control.BorderThickness)));
        border.SetValue(Border.CornerRadiusProperty, cornerRadius);

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetBinding(ContentPresenter.ContentProperty, TemplateBinding(nameof(ContentControl.Content)));
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, TemplateBinding(nameof(ContentControl.ContentTemplate)));
        presenter.SetBinding(FrameworkElement.MarginProperty, TemplateBinding(nameof(Control.Padding)));
        presenter.SetBinding(TextElement.ForegroundProperty, TemplateBinding(nameof(Control.Foreground)));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        border.AppendChild(presenter);

        return new ControlTemplate(targetType) { VisualTree = border };
    }

    private static Binding TemplateBinding(string path) => new(path)
    {
        RelativeSource = RelativeSource.TemplatedParent,
        Mode = BindingMode.OneWay,
    };

    private static bool RaiseButton(DependencyObject root, string content, Button? excluded)
    {
        Button? target = FindVisualChildren<Button>(root).FirstOrDefault(button =>
            button.IsVisible &&
            !ReferenceEquals(button, excluded) &&
            string.Equals(button.Content?.ToString(), content, StringComparison.Ordinal));
        if (target is null)
        {
            return false;
        }

        target.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, target));
        return true;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }
            foreach (T nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static string DifficultyName(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Relaxed => "Relaxed",
        SudokuDifficulty.Easy => "Easy",
        SudokuDifficulty.Medium => "Medium",
        SudokuDifficulty.Hard => "Hard",
        SudokuDifficulty.Expert => "Expert",
        SudokuDifficulty.Master => "Master",
        _ => difficulty.ToString(),
    };

    private static string FormatTime(long seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0L, seconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private static SolidColorBrush Ink(string hex) => new(InkColor(hex));

    private static Color InkColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
