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
        InitializeReelMotionPresentation();
        if (polishedLayoutApplied)
        {
            return;
        }

        polishedLayoutApplied = true;

        // Let the authored rooftop/moon artwork participate in the composition again.
        BackgroundArt.Opacity = 0.36;

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

        // LAST WIN occupies the entire flexible console column.

        // The shortcut cheat-sheet is secondary information and no longer belongs on the
        // permanent machine surface. Search from the hosted canvas instead of the Window:
        // ShowEmbeddedGame reparents the content, so the Window itself is no longer a reliable
        // visual-tree root once Nine Lives is shown inside MainWindow.
        HideShortcutRail();

        // A win must read instantly, especially on a high-volatility 3x3 machine.
        WinText.FontSize = 27;
        WinText.FontWeight = FontWeights.Bold;
        WinText.Foreground = FrozenBrush("#FFF0A6");
        WinText.TextAlignment = TextAlignment.Center;

        // The inherited GoldButton style swapped to a saturated purple hover. Use a quiet
        // gold treatment for high-value actions instead of flashing the whole control.
        MaxButton.Style = (Style)FindResource("GoldButton");
        Button? backToGame = FindVisualChildren<Button>(PaytableFlyout)
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "BACK TO GAME", StringComparison.Ordinal));
        if (backToGame is not null)
        {
            backToGame.Width = 196;
            backToGame.Height = 46;
            backToGame.Padding = new Thickness(14, 8, 14, 8);
            backToGame.FontSize = 14;
            backToGame.Style = (Style)FindResource("GoldButton");
        }

        SoundToggle.IsChecked = soundsEnabled;
        AnimationsToggle.IsChecked = animationsEnabled;
        AutoButton.Content = "AUTO ×20";
        SpinButton.Content = machine.BonusActive ? "FREE SPIN" : "SPIN";
    }

    private void HideShortcutRail()
    {
        DependencyObject root = SettingsFlyout.Parent as DependencyObject
            ?? PaytableFlyout.Parent as DependencyObject
            ?? this;

        TextBlock? shortcutLabel = FindVisualChildren<TextBlock>(root)
            .FirstOrDefault(text => string.Equals(text.Text, "SHORTCUTS", StringComparison.Ordinal));
        if (shortcutLabel is null)
        {
            return;
        }

        shortcutLabel.Visibility = Visibility.Collapsed;

        if (shortcutLabel.Parent is Panel parent)
        {
            int index = parent.Children.IndexOf(shortcutLabel);
            if (index >= 0 && index + 1 < parent.Children.Count)
            {
                parent.Children[index + 1].Visibility = Visibility.Collapsed;
            }

            if (index > 0 && parent.Children[index - 1] is Border separator && separator.Height <= 2)
            {
                separator.Visibility = Visibility.Collapsed;
            }
        }
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
