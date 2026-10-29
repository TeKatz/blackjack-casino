using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlackjackCasino.Desktop;

internal partial class NineLivesWindow
{
    private bool polishedLayoutApplied;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (polishedLayoutApplied)
        {
            return;
        }

        polishedLayoutApplied = true;

        // Let the authored rooftop/moon artwork participate in the composition again.
        BackgroundArt.Opacity = 0.22;

        // The restored concept-art symbols are square compositions. Uniform rendering keeps
        // the full cats/objects visible and high-quality scaling avoids the harsh nearest-neighbour look.
        for (int i = 0; i < symbolImages.Length; i++)
        {
            symbolImages[i].Stretch = Stretch.Uniform;
            RenderOptions.SetBitmapScalingMode(symbolImages[i], BitmapScalingMode.HighQuality);
            cells[i].Background = FrozenBrush("#F10A0712");
        }

        SettingsButton.Content = "⚙";
        SettingsButton.FontFamily = new FontFamily("Segoe UI Symbol");
        SettingsButton.FontSize = 23;

        // Navigation belongs in one predictable place. The duplicate bottom Casino action
        // made the footer noisy and looked like a second back-arrow bug.
        MenuButton.Visibility = Visibility.Collapsed;

        // The shortcut cheat-sheet consumed a large part of the permanent info rail. It is
        // secondary information and no longer lives on the main machine surface.
        HideShortcutRail();

        // A win must read instantly, especially on a high-volatility 3x3 machine.
        WinText.FontSize = 24;
        WinText.FontWeight = FontWeights.Bold;
        WinText.Foreground = FrozenBrush("#FFF0A6");
        WinText.TextAlignment = TextAlignment.Center;

        // The inherited GoldButton style swapped to a saturated purple hover. Use a quiet
        // gold treatment for high-value actions instead of flashing the whole control.
        ApplyCalmGoldButton(MaxButton);
        Button? backToGame = FindVisualChildren<Button>(PaytableFlyout)
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "BACK TO GAME", StringComparison.Ordinal));
        if (backToGame is not null)
        {
            backToGame.Width = 196;
            backToGame.Height = 46;
            backToGame.Padding = new Thickness(14, 8, 14, 8);
            backToGame.FontSize = 14;
            ApplyCalmGoldButton(backToGame);
        }

        SoundToggle.IsChecked = soundsEnabled;
        AnimationsToggle.IsChecked = animationsEnabled;
        AutoButton.Content = "AUTO ×20";
        SpinButton.Content = machine.BonusActive ? "FREE SPIN" : "SPIN";
    }

    private void HideShortcutRail()
    {
        foreach (StackPanel panel in FindVisualChildren<StackPanel>(this))
        {
            int shortcutIndex = -1;
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is TextBlock text
                    && string.Equals(text.Text, "SHORTCUTS", StringComparison.Ordinal))
                {
                    shortcutIndex = i;
                    break;
                }
            }

            if (shortcutIndex < 0)
            {
                continue;
            }

            for (int i = shortcutIndex; i < panel.Children.Count; i++)
            {
                panel.Children[i].Visibility = Visibility.Collapsed;
            }
            break;
        }
    }

    private static void ApplyCalmGoldButton(Button button)
    {
        button.Foreground = FrozenBrush("#24131C");
        button.Background = FrozenBrush("#F2D16A");
        button.BorderBrush = FrozenBrush("#FFF0A7");
        button.BorderThickness = new Thickness(1.5);
        button.FontFamily = new FontFamily("Georgia");
        button.FontWeight = FontWeights.Bold;
        button.Cursor = System.Windows.Input.Cursors.Hand;
        button.Template = CreateCalmButtonTemplate();

        button.MouseEnter += (_, _) => button.Opacity = 0.92;
        button.MouseLeave += (_, _) => button.Opacity = 1.0;
        button.PreviewMouseDown += (_, _) => button.Opacity = 0.78;
        button.PreviewMouseUp += (_, _) => button.Opacity = button.IsMouseOver ? 0.92 : 1.0;
    }

    private static ControlTemplate CreateCalmButtonTemplate()
    {
#pragma warning disable CS0618
        var chrome = new FrameworkElementFactory(typeof(Border));
        chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(11));
        chrome.SetBinding(Border.BackgroundProperty, new Binding(nameof(Button.Background))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        chrome.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Button.BorderBrush))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        chrome.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Button.BorderThickness))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(Button.Content))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding(nameof(Button.ContentTemplate))
        {
            RelativeSource = RelativeSource.TemplatedParent,
        });
        chrome.AppendChild(presenter);

        return new ControlTemplate(typeof(Button)) { VisualTree = chrome };
#pragma warning restore CS0618
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }
            foreach (T descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
