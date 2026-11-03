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

namespace BlackjackCasino.Desktop;

/// <summary>
/// Shared image-led Chill Lounge hub for Solitaire, Word Search, Memory and Backroom.
/// Illustrated backdrops are packaged assets; only live text and controls are WPF UI.
/// The 1760x938 design canvas scales as a single 16:9 composition.
/// </summary>
internal static class PremiumLoungeTheme
{
    internal const string Cream = "#F6E8D0";
    internal const string Gold = "#E5BE75";
    internal const string Teal = "#52E0CE";
    internal const string Muted = "#9AB4B0";

    private static readonly Dictionary<string, BitmapSource> ArtworkCache = new(StringComparer.Ordinal);
    private static readonly ControlTemplate FlatButtonTemplate = CreateButtonTemplate();

    internal static UIElement Build(
        string ambientArt,
        string featureArt,
        string eyebrow,
        string heroTitle,
        string heroSubtitle,
        string gameLabel,
        string heading,
        string description,
        UIElement leftExtra,
        UIElement rightContent,
        string footer)
    {
        var stage = new Grid
        {
            Width = 1760,
            Height = 938,
            ClipToBounds = true,
        };
        stage.Children.Add(new Image
        {
            Source = Art(ambientArt, 1550),
            Stretch = Stretch.UniformToFill,
            IsHitTestVisible = false,
            CacheMode = new BitmapCache(),
        });
        stage.Children.Add(new Border
        {
            IsHitTestVisible = false,
            Background = new LinearGradientBrush(
                Color.FromArgb(225, 3, 13, 17),
                Color.FromArgb(192, 5, 20, 22),
                new Point(0, 0), new Point(1, 1)),
        });

        var layout = new Grid { Margin = new Thickness(95, 16, 95, 26) };
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(155) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        stage.Children.Add(layout);

        var hero = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        hero.Children.Add(Label(eyebrow, 14, Teal, true, TextAlignment.Center));
        hero.Children.Add(new TextBlock
        {
            Text = heroTitle,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 59,
            Foreground = Ink(Cream),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 7),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 11,
                ShadowDepth = 3,
                Opacity = .7,
            },
        });
        hero.Children.Add(Label(heroSubtitle, 16, "#D9E0D8", false, TextAlignment.Center));
        layout.Children.Add(hero);

        var plate = new Border
        {
            BorderThickness = new Thickness(2),
            BorderBrush = Ink("#C7A25F"),
            CornerRadius = new CornerRadius(24),
            Background = Ink("#E8071D20"),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 33,
                ShadowDepth = 12,
                Opacity = .73,
            },
        };
        var interior = new Grid();
        interior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(.47, GridUnitType.Star) });
        interior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(.53, GridUnitType.Star) });

        // Clip the image and both content columns to the *rounded* inner card surface.
        // ClipToBounds alone does not clip to the Border's CornerRadius.
        interior.SizeChanged += (_, e) =>
        {
            interior.Clip = new RectangleGeometry(
                new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 22, 22);
        };
        plate.Child = interior;
        Grid.SetRow(plate, 1);
        layout.Children.Add(plate);

        var scene = new Grid { ClipToBounds = true };
        scene.Children.Add(new Image
        {
            Source = Art(featureArt, 950),
            Stretch = Stretch.UniformToFill,
            Opacity = .67,
            IsHitTestVisible = false,
            CacheMode = new BitmapCache(),
        });
        scene.Children.Add(new Border
        {
            IsHitTestVisible = false,
            Background = new LinearGradientBrush(
                Color.FromArgb(246, 5, 25, 27),
                Color.FromArgb(88, 2, 19, 23),
                new Point(0, 0), new Point(.13, 1)),
        });
        scene.Children.Add(new Border
        {
            IsHitTestVisible = false,
            Background = new LinearGradientBrush(
                Color.FromArgb(0, 1, 13, 19),
                Color.FromArgb(214, 2, 17, 22),
                new Point(.3, .5), new Point(1, .5)),
        });
        interior.Children.Add(scene);

        var copy = new StackPanel { Margin = new Thickness(52, 39, 39, 28) };
        copy.Children.Add(Label(gameLabel, 17, Gold, true));
        copy.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 43,
            FontFamily = new FontFamily("Georgia"),
            Foreground = Ink(Cream),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 51,
            MaxWidth = 565,
            Margin = new Thickness(0, 14, 0, 20),
        });
        copy.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 16,
            Foreground = Ink("#D8E6DE"),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 545,
            LineHeight = 25,
        });
        if (leftExtra is not null)
        {
            var extra = new Border
            {
                BorderBrush = Ink("#79664B"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 29, 0, 0),
                Padding = new Thickness(0, 19, 0, 0),
                Child = leftExtra,
            };
            copy.Children.Add(extra);
        }
        scene.Children.Add(copy);

        var right = new Grid { Margin = new Thickness(25, 28, 35, 25) };
        Grid.SetColumn(right, 1);
        interior.Children.Add(right);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.VerticalOnly,
            CanContentScroll = false,
            Focusable = false,
            Content = rightContent,
        };
        right.Children.Add(scroll);

        TextBlock footerText = Label(footer, 12, "#AFBBB5", false, TextAlignment.Center);
        footerText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(footerText, 2);
        layout.Children.Add(footerText);

        return new Grid
        {
            Background = Ink("#071214"),
            Children =
            {
                new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.Both,
                    Child = stage,
                },
            },
        };
    }

    internal static TextBlock Label(string text, double size, string hex, bool bold = false,
        TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = Ink(hex),
        TextAlignment = align,
        TextWrapping = TextWrapping.Wrap,
    };

    internal static TextBlock Heading(string text, double size = 15)
    {
        var label = Label(text, size, Gold, true);
        label.Margin = new Thickness(0, 0, 0, 13);
        return label;
    }

    internal static UIElement Stats(params string[] items)
    {
        var wrap = new StackPanel();
        foreach (string item in items)
        {
            var text = Label(item, 15, Gold, true);
            text.Margin = new Thickness(0, 0, 0, 13);
            wrap.Children.Add(text);
        }
        return wrap;
    }

    internal static Border Panel(UIElement content, double padding = 17)
    {
        return new Border
        {
            Background = Ink("#D20B2428"),
            BorderBrush = Ink("#557873"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(padding),
            Child = content,
        };
    }

    internal static Border Row(string title, string subtitle, bool selected = false, double height = 70)
    {
        var outline = Ink(selected ? "#4FE0CC" : "#4D6768");
        var row = new Border
        {
            MinHeight = height,
            BorderThickness = new Thickness(selected ? 1.5 : 1),
            BorderBrush = outline,
            CornerRadius = new CornerRadius(10),
            Background = Ink(selected ? "#D20C3436" : "#BC0B2227"),
            Margin = new Thickness(0, 0, 0, 7),
            Padding = new Thickness(14, 8, 13, 8),
        };
        if (selected)
        {
            row.Effect = Halo(.27);
        }
        row.MouseEnter += (_, _) =>
        {
            Fade(outline, selected ? "#86FFF1" : "#C9A56B");
            row.Effect = Halo(.44);
        };
        row.MouseLeave += (_, _) =>
        {
            Fade(outline, selected ? "#4FE0CC" : "#4D6768");
            row.Effect = selected ? Halo(.27) : null;
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = "◇",
            FontSize = 24,
            Foreground = Ink(selected ? Teal : Gold),
            VerticalAlignment = VerticalAlignment.Center,
        });
        var labelStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        labelStack.Children.Add(Label(title.ToUpperInvariant(), 15, Cream, true));
        var sub = Label(subtitle, 11, selected ? Teal : Muted);
        sub.Margin = new Thickness(0, 3, 0, 0);
        labelStack.Children.Add(sub);
        Grid.SetColumn(labelStack, 1);
        grid.Children.Add(labelStack);
        row.Child = grid;
        return row;
    }

    internal static Grid RowContent(Border row)
        => row.Child as Grid
            ?? throw new InvalidOperationException("Premium row requires its original Grid.");

    internal static void AddRowAction(Border row, Button button, double width)
    {
        Grid content = RowContent(row);
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width + 8) });
        int index = content.ColumnDefinitions.Count - 1;
        Grid.SetColumn(button, index);
        button.HorizontalAlignment = HorizontalAlignment.Right;
        content.Children.Add(button);
    }

    internal static Button Button(string content, string name, Action click, bool primary = true,
        bool enabled = true, double width = 127, double height = 36, bool gold = false)
    {
        string normal = gold ? "#5A4324" : primary ? "#17403F" : "#172D31";
        string hover = gold ? "#A57936" : "#206F66";
        string border = gold ? "#E0B96B" : primary ? "#BC9558" : "#507B7B";
        SolidColorBrush fill = Ink(normal);
        SolidColorBrush rim = Ink(border);
        var control = new Button
        {
            Name = name,
            Content = content,
            Width = width,
            Height = height,
            IsEnabled = enabled,
            Opacity = enabled ? 1 : .3,
            Background = fill,
            BorderBrush = rim,
            BorderThickness = new Thickness(1.3),
            Foreground = Ink(Cream),
            FontSize = gold ? 14 : 12,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Template = FlatButtonTemplate,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new ScaleTransform(1, 1),
        };
        control.Click += (_, _) => click();
        control.MouseEnter += (_, _) =>
        {
            if (!control.IsEnabled) return;
            Fade(fill, hover);
            Fade(rim, gold ? "#FFE0A4" : "#7CFFF0");
            Scale(control, 1.025);
            control.Effect = Halo(.55, gold);
        };
        control.MouseLeave += (_, _) =>
        {
            Fade(fill, normal);
            Fade(rim, border);
            Scale(control, 1);
            control.Effect = null;
        };
        control.GotKeyboardFocus += (_, _) =>
        {
            Fade(rim, "#FFF0B5");
            Scale(control, 1.025);
        };
        control.LostKeyboardFocus += (_, _) =>
        {
            Fade(rim, border);
            Scale(control, 1);
        };
        return control;
    }

    internal static void StyleInput(Control control)
    {
        control.Background = Ink("#102C30");
        control.Foreground = Ink(Cream);
        control.BorderBrush = Ink("#79654B");
        control.BorderThickness = new Thickness(1);
        control.Padding = new Thickness(12, 8, 12, 8);
        control.FontSize = 14;
    }

    private static DropShadowEffect Halo(double opacity, bool gold = false) => new()
    {
        Color = gold ? Color.FromRgb(249, 191, 91) : Color.FromRgb(45, 225, 199),
        BlurRadius = 15,
        ShadowDepth = 0,
        Opacity = opacity,
    };

    private static ControlTemplate CreateButtonTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetBinding(Border.BackgroundProperty,
            new Binding(nameof(Control.Background)) { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderBrushProperty,
            new Binding(nameof(Control.BorderBrush)) { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderThicknessProperty,
            new Binding(nameof(Control.BorderThickness)) { RelativeSource = RelativeSource.TemplatedParent });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        presenter.SetBinding(ContentPresenter.ContentProperty,
            new Binding(nameof(ContentControl.Content)) { RelativeSource = RelativeSource.TemplatedParent });
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty,
            new Binding(nameof(ContentControl.ContentTemplate)) { RelativeSource = RelativeSource.TemplatedParent });
        presenter.SetBinding(TextElement.ForegroundProperty,
            new Binding(nameof(Control.Foreground)) { RelativeSource = RelativeSource.TemplatedParent });
        border.AppendChild(presenter);
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }

    private static void Scale(Button button, double target)
    {
        if (button.RenderTransform is not ScaleTransform transform) return;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(target, TimeSpan.FromMilliseconds(150)) { EasingFunction = easing });
        transform.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(target, TimeSpan.FromMilliseconds(150)) { EasingFunction = easing });
    }

    private static void Fade(SolidColorBrush brush, string target)
        => brush.BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation((Color)ColorConverter.ConvertFromString(target),
                TimeSpan.FromMilliseconds(160))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });

    internal static SolidColorBrush Ink(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex));

    private static BitmapSource Art(string file, int decodeWidth)
    {
        lock (ArtworkCache)
        {
            if (ArtworkCache.TryGetValue(file, out BitmapSource? bitmap)) return bitmap;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = decodeWidth;
            image.UriSource = new Uri(
                $"pack://application:,,,/BlackjackCasino.Desktop;component/Assets/Empire/{file}",
                UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            ArtworkCache.Add(file, image);
            return image;
        }
    }
}
