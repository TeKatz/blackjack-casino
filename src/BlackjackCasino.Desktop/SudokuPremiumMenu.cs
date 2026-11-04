using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Image-led, resolution-independent Sudoku hub. The artwork comes from real, offline
/// illustrated game assets; WPF only lays out live text and accessible, interactive controls.
/// No on-the-fly DrawingVisual portraits, icon geometry or generated UI illustrations.
/// </summary>
internal static class SudokuPremiumMenu
{
    internal sealed record DifficultyEntry(
        SudokuDifficulty Difficulty,
        string Label,
        bool CanResume,
        string Status,
        Action Resume,
        Action NewPuzzle);

    private const string Gold = "#E5BE75";
    private const string Cream = "#F6E8D0";
    private const string Teal = "#52E0CE";
    private const string Muted = "#9AB4B0";

    private static readonly Lazy<BitmapSource> AmbientArt = new(() => LoadArtwork("sky-bar.png", 1400));
    private static readonly Lazy<BitmapSource> InteriorArt = new(() => LoadArtwork("fine-dining.png", 1250));
    private static readonly ControlTemplate ButtonTemplate = CreateButtonTemplate();

    internal static UIElement Build(
        IReadOnlyList<DifficultyEntry> difficulties,
        int normalCompleted,
        int normalTotal,
        int dailyCompleted,
        bool showTimer,
        Action toggleTimer,
        DateOnly today,
        SudokuPuzzle daily,
        bool dailyFinished,
        Action openDaily)
    {
        // Fixed design coordinates are scaled uniformly as a unit, preserving proportions
        // at 1920x1080 and avoiding overflow at smaller window sizes.
        var stage = new Grid { Width = 1760, Height = 938, ClipToBounds = true };
        stage.Children.Add(new Image
        {
            Source = AmbientArt.Value,
            Stretch = Stretch.UniformToFill,
            IsHitTestVisible = false,
            CacheMode = new BitmapCache(),
        });
        stage.Children.Add(new Border
        {
            Background = new LinearGradientBrush(
                Color.FromArgb(222, 3, 13, 17),
                Color.FromArgb(185, 5, 20, 22),
                new Point(0, 0), new Point(1, 1)),
            IsHitTestVisible = false,
        });

        var layout = new Grid { Margin = new Thickness(95, 16, 95, 26) };
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(155) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        stage.Children.Add(layout);

        var hero = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        hero.Children.Add(Label("A QUIETER CORNER", 14, Teal, true, TextAlignment.Center));
        hero.Children.Add(new TextBlock
        {
            Text = "Take your time.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 62,
            Foreground = Brush(Cream),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 11, ShadowDepth = 3, Opacity = .7 },
        });
        hero.Children.Add(Label(
            "No wagers, no pressure. Pick something small, settle in and leave whenever you want — progress is saved locally.",
            16, "#D9E0D8", false, TextAlignment.Center));
        layout.Children.Add(hero);

        var cardShadow = new Border
        {
            Background = Brush("#E8071D20"),
            CornerRadius = new CornerRadius(24),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 33, ShadowDepth = 12, Opacity = .73 },
        };
        Grid.SetRow(cardShadow, 1);
        layout.Children.Add(cardShadow);
        var card = new Border
        {
            CornerRadius = new CornerRadius(24),
            BorderThickness = new Thickness(2),
            BorderBrush = Brush("#C7A25F"),
            Background = Brush("#E8071D20"),
            ClipToBounds = true,
        };
        var inside = new Grid();
        inside.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.47, GridUnitType.Star) });
        inside.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.53, GridUnitType.Star) });
        // A Border's CornerRadius does not clip child images by itself.
        // Clip the image/content grid so both left corners stay rounded.
        inside.SizeChanged += (_, e) =>
        {
            inside.Clip = new RectangleGeometry(
                new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 22, 22);
        };
        card.Child = inside;
        Grid.SetRow(card, 1);
        layout.Children.Add(card);

        // The left-side illustration is an actual bundled game-art image, not WPF vector art.
        // Keep a single full-resolution painting instead of magnifying a tiny
        // 140x180 Sudoku JPEG and pasting it over the room illustration.
        var scene = new Grid
        {
            ClipToBounds = true,
            Background = new ImageBrush(InteriorArt.Value)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center,
                Opacity = .94,
            },
        };
        scene.Children.Add(new Border
        {
            Background = new LinearGradientBrush(
                Color.FromArgb(211, 5, 25, 27),
                Color.FromArgb(68, 2, 19, 23),
                new Point(0, 0), new Point(1, 0)),
            IsHitTestVisible = false,
        });
        scene.Children.Add(new Border
        {
            Background = new LinearGradientBrush(
                Color.FromArgb(0, 1, 13, 19),
                Color.FromArgb(148, 2, 17, 22),
                new Point(.3, 0), new Point(1, 1)),
            IsHitTestVisible = false,
        });
        Grid.SetColumn(scene, 0);
        inside.Children.Add(scene);

        var leftCopy = new StackPanel
        {
            Margin = new Thickness(52, 39, 40, 26),
            VerticalAlignment = VerticalAlignment.Top,
        };
        var sudokuTitle = Label("S U D O K U", 17, Gold, true, TextAlignment.Center);
        sudokuTitle.HorizontalAlignment = HorizontalAlignment.Stretch;
        sudokuTitle.Margin = new Thickness(0, 0, 0, 9);
        leftCopy.Children.Add(sudokuTitle);
        leftCopy.Children.Add(new TextBlock
        {
            Text = "One grid. No clock to beat.",
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 520,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 42,
            Foreground = Brush(Cream),
            LineHeight = 51,
            Margin = new Thickness(0, 14, 0, 20),
        });
        leftCopy.Children.Add(new TextBlock
        {
            Text = "Pencil marks, undo/redo, optional hints and autosave. Each Free Play difficulty keeps its own paused puzzle, while Daily puzzles remain in a completely separate library.",
            FontSize = 16,
            Foreground = Brush("#D8E6DE"),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 25,
            MaxWidth = 520,
        });

        var progressStrip = new Border
        {
            BorderThickness = new Thickness(0, 1, 0, 1),
            BorderBrush = Brush("#79664B"),
            Padding = new Thickness(0, 17, 0, 17),
            Margin = new Thickness(0, 27, 0, 15),
        };
        var progressGrid = new Grid();
        progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var freeStats = Label($"▦  FREE PLAY   {normalCompleted:N0}/{normalTotal:N0}", 15, Gold, true);
        var dailyStats = Label($"▣  DAILY   {dailyCompleted:N0} completed", 15, Gold, true);
        progressGrid.Children.Add(freeStats);
        Grid.SetColumn(dailyStats, 1);
        progressGrid.Children.Add(dailyStats);
        progressStrip.Child = progressGrid;
        leftCopy.Children.Add(progressStrip);
        leftCopy.Children.Add(Label(
            "NEW PUZZLE replaces only the paused puzzle on that difficulty. Other difficulties remain untouched.",
            14, Muted, false));
        scene.Children.Add(leftCopy);

        var right = new Grid { Margin = new Thickness(24, 28, 35, 24) };
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(87) });
        Grid.SetColumn(right, 1);
        inside.Children.Add(right);

        right.Children.Add(Label("FREE PLAY", 15, Gold, true));
        var rows = new StackPanel { Margin = new Thickness(0, 8, 0, 6) };
        foreach (DifficultyEntry entry in difficulties)
        {
            rows.Children.Add(BuildDifficulty(entry));
        }
        Grid.SetRow(rows, 1);
        right.Children.Add(rows);

        var timerSection = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        var timerButton = ActionButton(showTimer ? "TIMER · VISIBLE" : "TIMER · HIDDEN", false, 750, 43);
        timerButton.Name = "SudokuTimerToggle";
        timerButton.Click += (_, _) => toggleTimer();
        timerSection.Children.Add(timerButton);
        timerSection.Children.Add(new TextBlock
        {
            Text = "Your elapsed time is always preserved in the save.",
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Foreground = Brush(Muted),
            Margin = new Thickness(0, 6, 0, 0),
        });
        Grid.SetRow(timerSection, 2);
        right.Children.Add(timerSection);

        var dailySection = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        var dailyButton = ActionButton(
            dailyFinished
                ? "TODAY'S DAILY · COMPLETE"
                : $"TODAY'S DAILY · {daily.Difficulty.ToString().ToUpperInvariant()}",
            true, 750, 46, isGold: true);
        dailyButton.Name = "SudokuDailyButton";
        dailyButton.Click += (_, _) => openDaily();
        dailySection.Children.Add(dailyButton);
        dailySection.Children.Add(new TextBlock
        {
            Text = today.ToString("dddd, dd MMMM", CultureInfo.CurrentCulture),
            Foreground = Brush("#C7D0C4"),
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        });
        Grid.SetRow(dailySection, 3);
        right.Children.Add(dailySection);

        var footer = Label(
            "The bundled catalog is data-driven and validated before release. Free Play and Daily content never share the same puzzle pool.",
            12, "#AFBBB5", false, TextAlignment.Center);
        footer.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = stage,
        };
        CrispViewboxText.SetEnabled(viewbox, true);
        return new Grid { Background = Brush("#071214"), Children = { viewbox } };
    }

    private static Border BuildDifficulty(DifficultyEntry entry)
    {
        var background = Brush(entry.CanResume ? "#D20C3436" : "#BC0B2227");
        var line = Brush(entry.CanResume ? "#4FE0CC" : "#4D6768");
        var row = new Border
        {
            Name = $"SudokuDifficulty_{entry.Difficulty}",
            Height = 67,
            Margin = new Thickness(0, 0, 0, 7),
            BorderThickness = new Thickness(entry.CanResume ? 1.5 : 1),
            BorderBrush = line,
            CornerRadius = new CornerRadius(11),
            Background = background,
            Padding = new Thickness(16, 7, 12, 7),
        };
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(111) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(141) });

        content.Children.Add(new TextBlock
        {
            Text = "◇",
            FontSize = 26,
            Foreground = Brush(entry.CanResume ? Teal : Gold),
            VerticalAlignment = VerticalAlignment.Center,
        });

        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(Label(entry.Label.ToUpperInvariant(), 15, Cream, true));
        var status = Label(entry.Status, 11, entry.CanResume ? Teal : Muted, false);
        status.Name = $"SudokuStatus_{entry.Difficulty}";
        status.Margin = new Thickness(0, 3, 0, 0);
        titles.Children.Add(status);
        Grid.SetColumn(titles, 1);
        content.Children.Add(titles);

        var resume = ActionButton("RESUME", false, 100, 35);
        resume.Name = $"SudokuResume_{entry.Difficulty}";
        resume.IsEnabled = entry.CanResume;
        resume.Opacity = entry.CanResume ? 1 : .28;
        resume.Click += (_, _) => entry.Resume();
        Grid.SetColumn(resume, 2);
        content.Children.Add(resume);

        var fresh = ActionButton("NEW PUZZLE", true, 131, 35);
        fresh.Name = $"SudokuNew_{entry.Difficulty}";
        fresh.Click += (_, _) => entry.NewPuzzle();
        Grid.SetColumn(fresh, 3);
        content.Children.Add(fresh);

        row.Child = content;
        if (entry.CanResume) row.BorderThickness = new Thickness(2);
        row.MouseEnter += (_, _) =>
        {
            FadeLine(line, entry.CanResume ? "#86FFF1" : "#C9A56B");
            // The animated outline is the hover highlight; the text stays vector-rendered.
        };
        row.MouseLeave += (_, _) =>
        {
            FadeLine(line, entry.CanResume ? "#4FE0CC" : "#4D6768");
            // Do not apply a bitmap effect to the entire row.
        };
        return row;
    }

    internal static Button NavigationButton(string text) =>
        ActionButton(text, false, text.Contains("CLUB", StringComparison.Ordinal) ? 188 : 142, 42);

    private static Button ActionButton(string text, bool primary, double width, double height, bool isGold = false)
    {
        string baseColor = isGold ? "#5A4324" : primary ? "#17403F" : "#172D31";
        string overColor = isGold ? "#A57936" : "#206F66";
        string rim = isGold ? "#E0B96B" : primary ? "#BC9558" : "#507B7B";
        var fill = Brush(baseColor);
        var outline = Brush(rim);
        var button = new Button
        {
            Content = text,
            Width = width,
            Height = height,
            Background = fill,
            BorderBrush = outline,
            BorderThickness = new Thickness(1.3),
            Foreground = Brush(Cream),
            FontSize = isGold ? 15 : 12,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Template = ButtonTemplate,
            RenderTransform = new ScaleTransform(1, 1),
            RenderTransformOrigin = new Point(.5, .5),
        };
        button.GotKeyboardFocus += (_, _) =>
        {
            FadeLine(outline, "#FFF0B5");
            AnimateScale(button, 1.025);
        };
        button.LostKeyboardFocus += (_, _) =>
        {
            FadeLine(outline, rim);
            AnimateScale(button, 1);
        };
        button.MouseEnter += (_, _) =>
        {
            if (!button.IsEnabled) return;
            FadeLine(fill, overColor);
            FadeLine(outline, isGold ? "#FFE0A4" : "#7CFFF0");
            AnimateScale(button, 1.025);
            // The color and scale changes provide hover feedback without caption blur.
        };
        button.MouseLeave += (_, _) =>
        {
            FadeLine(fill, baseColor);
            FadeLine(outline, rim);
            AnimateScale(button, 1);
            // No bitmap effect to clear.
        };
        return button;
    }

    private static ControlTemplate CreateButtonTemplate()
    {
        var frame = new FrameworkElementFactory(typeof(Border));
        frame.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        frame.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        frame.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Control.BorderThickness))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        frame.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        presenter.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(ContentControl.Content))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        presenter.SetBinding(TextElement.ForegroundProperty, new Binding(nameof(Control.Foreground))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        frame.AppendChild(presenter);
        return new ControlTemplate(typeof(Button)) { VisualTree = frame };
    }

    private static TextBlock Label(string text, double size, string color, bool bold, TextAlignment align = TextAlignment.Left)
        => new()
        {
            Text = text,
            FontSize = size,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = Brush(color),
            TextAlignment = align,
            TextWrapping = TextWrapping.Wrap,
        };

    private static void AnimateScale(Button button, double to)
    {
        if (button.RenderTransform is not ScaleTransform transform) return;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(to, TimeSpan.FromMilliseconds(150)) { EasingFunction = easing });
        transform.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(to, TimeSpan.FromMilliseconds(150)) { EasingFunction = easing });
    }

    private static void FadeLine(SolidColorBrush brush, string to)
    {
        brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(
            (Color)ColorConverter.ConvertFromString(to), TimeSpan.FromMilliseconds(160))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private static BitmapSource LoadArtwork(string file, int decodedWidth)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = decodedWidth;
        string folder = file.StartsWith("lounge:", StringComparison.Ordinal) ? "Lounge" : "Empire";
        string asset = folder == "Lounge" ? file["lounge:".Length..] : file;
        bitmap.UriSource = new Uri(
            $"pack://application:,,,/BlackjackCasino.Desktop;component/Assets/{folder}/{asset}",
            UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
