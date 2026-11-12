using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

/// <summary>
/// The exit confirmation belongs to the WPF owner, never to a separate
/// WebView2 window. No profile or game state is mutated until Close succeeds.
/// </summary>
internal sealed class CasinoExitDialog : Window
{
    internal static bool Confirm(Window owner, bool german) =>
        new CasinoExitDialog(owner, german).ShowDialog() == true;

    private CasinoExitDialog(Window owner, bool german)
    {
        Owner = owner;
        Title = german ? "The Midnight Club verlassen" : "Leave The Midnight Club";
        Width = 488;
        Height = 300;
        MinWidth = 440;
        MaxWidth = 530;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ShowInTaskbar = false;
        Background = Brushes.Transparent;
        FontFamily = new FontFamily("Segoe UI");
        UseLayoutRounding = true;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);

        var outer = new Border
        {
            CornerRadius = new CornerRadius(18),
            BorderBrush = Brush("#8C8D7E66"),
            BorderThickness = new Thickness(1),
            Background = new LinearGradientBrush(
                ColorFrom("#192D2C"), ColorFrom("#101A25"), 118),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black, BlurRadius = 38,
                ShadowDepth = 16, Opacity = .69
            }
        };

        var contents = new Grid { Margin = new Thickness(29, 25, 29, 27) };
        contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contents.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var kicker = new TextBlock
        {
            Text = "THE MIDNIGHT CLUB  /  " + (german ? "SITZUNG" : "SESSION"),
            Foreground = Brush("#D8C395"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 9)
        };
        Grid.SetRow(kicker, 0);
        contents.Children.Add(kicker);

        var title = new TextBlock
        {
            Text = german ? "Casino wirklich verlassen?" : "Leave the casino?",
            FontSize = 25,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#FFF2DB"),
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(title, 1);
        contents.Children.Add(title);

        var message = new TextBlock
        {
            Text = german
                ? "Die Anwendung und die aktuelle Sitzung werden beendet. "
                  + "Schließe laufende Aktionen vor dem Verlassen ab."
                : "The application and current session will close. "
                  + "Finish any active actions before leaving.",
            Foreground = Brush("#C0D0C7"),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 20,
            MaxWidth = 420,
            Margin = new Thickness(0, 0, 0, 22)
        };
        Grid.SetRow(message, 2);
        contents.Children.Add(message);

        var line = new Border
        {
            Background = Brush("#71887957"),
            Height = 1,
            Margin = new Thickness(0, 0, 0, 18)
        };
        Grid.SetRow(line, 3);
        contents.Children.Add(line);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var stay = CreateButton(
            german ? "Im Casino bleiben" : "Stay in the casino",
            Brush("#24463B"), Brush("#7EBA9A"), Brush("#F6E9CE"));
        var leave = CreateButton(
            german ? "Beenden" : "Exit",
            Brush("#362B33"), Brush("#A98783"), Brush("#EBCAC7"));
        stay.IsDefault = true;
        stay.Margin = new Thickness(0, 0, 11, 0);
        stay.Click += (_, _) => DialogResult = false;
        leave.Click += (_, _) => DialogResult = true;
        AutomationProperties.SetName(stay, german ? "Abbruch, Casino geöffnet lassen" : "Cancel exit");
        AutomationProperties.SetName(leave, german ? "Casino schließen" : "Confirm exit");
        actions.Children.Add(stay);
        actions.Children.Add(leave);
        Grid.SetRow(actions, 4);
        contents.Children.Add(actions);

        outer.Child = contents;
        Content = new Grid
        {
            Margin = new Thickness(22),
            Children = { outer }
        };
        KeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape) return;
            DialogResult = false;
            args.Handled = true;
        };
        Loaded += (_, _) => stay.Focus();
    }

    private static Button CreateButton(string title, Brush fill, Brush border, Brush text)
    {
        var button = new Button
        {
            Content = title,
            MinHeight = 43,
            MinWidth = 132,
            Padding = new Thickness(17, 10, 17, 10),
            Background = fill,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Foreground = text,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 12.5,
            Cursor = Cursors.Hand,
            FocusVisualStyle = null
        };

        var frame = new FrameworkElementFactory(typeof(Border));
        frame.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        frame.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        frame.SetBinding(Border.BackgroundProperty,
            new Binding(nameof(Button.Background))
            { RelativeSource = RelativeSource.TemplatedParent });
        frame.SetBinding(Border.BorderBrushProperty,
            new Binding(nameof(Button.BorderBrush))
            { RelativeSource = RelativeSource.TemplatedParent });
        frame.SetBinding(Border.PaddingProperty,
            new Binding(nameof(Button.Padding))
            { RelativeSource = RelativeSource.TemplatedParent });

        var textPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        textPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        textPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        textPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        frame.AppendChild(textPresenter);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = frame };
        var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(BorderBrushProperty, Brush("#E9D8AC")));
        template.Triggers.Add(hover);
        var pressed = new Trigger { Property = IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(OpacityProperty, .79));
        template.Triggers.Add(pressed);
        button.Template = template;
        return button;
    }

    private static Color ColorFrom(string color) =>
        (Color)ColorConverter.ConvertFromString(color)!;
    private static Brush Brush(string color) =>
        new SolidColorBrush(ColorFrom(color));
}
