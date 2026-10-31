using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class VaultSmoke
{
    internal static async Task Run(Window window, string output)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            // Reproduce the longer currency strings from the user's German desktop. This is the
            // layout that previously clipped the leading V in the full-vault bonus line.
            CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = german;
            CultureInfo.CurrentUICulture = german;

            int visibleWindowsBefore = Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible);
            Button vault = Find<Button>(window).Single(button => button.Name == "CasinoVaultButton" && button.IsVisible);
            vault.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(90);
            window.UpdateLayout();

            Grid overlay = Find<Grid>(window).Single(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible);
            Border modal = Find<Border>(overlay).Single(border => border.Name == "CasinoVaultModalFrame" && border.IsVisible);
            Require(modal.ActualWidth > 900 && modal.ActualHeight > 600,
                "Casino Vault in-app modal did not render at its authored size.");
            Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == visibleWindowsBefore,
                "Casino Vault regressed to a second visible native Window.");
            Require(!Application.Current.Windows.Cast<Window>().Any(w => w.IsVisible && w != window && w.Title.Contains("Vault", StringComparison.OrdinalIgnoreCase)),
                "Casino Vault exposed native Windows chrome instead of the in-app modal.");

            Capture(window, Path.Combine(output, "casino-vault-in-app.png"));

            Button open = Find<Button>(overlay).Single(button => button.Name == "CasinoVaultOpenButton" && button.IsVisible);
            Require(open.IsEnabled && open.Content?.ToString()?.StartsWith("BREAK OPEN", StringComparison.Ordinal) == true,
                "Full Gold Vault is not ready to open in the in-app modal.");
            open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(100);
            window.UpdateLayout();

            TextBlock bonus = Find<TextBlock>(overlay).Single(text => text.Name == "VaultRewardBonus" && text.IsVisible);
            TextBlock total = Find<TextBlock>(overlay).Single(text => text.Name == "VaultRewardTotal" && text.IsVisible);
            Require(bonus.Text.StartsWith("FULL VAULT BONUS", StringComparison.Ordinal),
                "Vault reward bonus label lost its leading text.");
            Require(bonus.Text.Contains("10.000,00", StringComparison.Ordinal),
                "Vault reward smoke did not exercise the longer German-formatted bonus amount.");
            Require(bonus.TextWrapping == TextWrapping.Wrap && bonus.TextAlignment == TextAlignment.Center &&
                    bonus.HorizontalAlignment == HorizontalAlignment.Stretch,
                "Vault reward bonus line is not protected against localized text clipping.");
            Require(bonus.ActualWidth >= 250 && bonus.ActualHeight >= 20,
                $"Vault reward bonus layout is too small: {bonus.ActualWidth:0.#}x{bonus.ActualHeight:0.#}.");
            Require(total.Text.Contains("60.000,00", StringComparison.Ordinal),
                "Gold Vault did not present the expected $60,000 total payout in German formatting.");

            Capture(window, Path.Combine(output, "casino-vault-opened.png"));

            Button close = Find<Button>(overlay).Single(button => button.Name == "CasinoVaultCloseButton" && button.IsVisible);
            close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
            window.UpdateLayout();
            Require(!Find<Grid>(window).Any(grid => grid.Name == "CasinoVaultOverlay" && grid.IsVisible),
                "Casino Vault overlay remained visible after closing it.");
            Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == visibleWindowsBefore,
                "Closing the in-app Vault changed the native window count.");

            File.WriteAllText(Path.Combine(output, "vault-result.txt"),
                "PASS: Casino Vault stayed inside MainWindow, rendered custom Midnight Club chrome, opened a full Gold Vault, and displayed the German-formatted reward without clipping.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        int width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
