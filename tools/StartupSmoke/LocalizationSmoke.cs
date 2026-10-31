using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;

internal static class LocalizationSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        ComboBox language = Find<ComboBox>(window).FirstOrDefault(combo => combo.Name == "LanguageComboBox")
            ?? throw new InvalidOperationException("Presentation settings do not expose the Language selector.");

        Require(language.Items.Count == 2, $"Language selector should expose English and Deutsch; found {language.Items.Count} items.");
        Require(language.SelectedIndex == 0, "Existing/default preferences should start in English.");

        language.SelectedIndex = 1;
        await Task.Delay(100);
        window.UpdateLayout();

        Require(CultureInfo.CurrentUICulture.Name == "de-DE",
            $"German selection did not switch CurrentUICulture to de-DE (was {CultureInfo.CurrentUICulture.Name}).");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "DARSTELLUNG"),
            "Presentation header did not switch to German without a restart.");
        Require(Find<CheckBox>(window).Any(check => check.Name == "SoundCheckBox" && Equals(check.Content, "Casino-Soundeffekte")),
            "Sound setting did not switch to its German resource value.");
        Require(Find<Button>(window).Any(button => button.Name == "HitButton" && Equals(button.Content, "KARTE  [H]")),
            "Blackjack action text did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.Text == "Sprache"),
            "Language selector label did not switch to German.");

        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Eine andere Art von Nacht."),
            "Casino lobby headline did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "DIE SAMTTISCHE"),
            "Blackjack lobby subtitle did not switch to German.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "PLATZ NEHMEN   →"),
            "Casino lobby call-to-action did not switch to German.");
        Require(Find<Button>(window).Any(button => button.IsVisible && Equals(button.Content, "Kosmetik-Shop")),
            "Casino lobby footer did not switch to German.");

        SaveScreenshot(window, Path.Combine(output, "localization-german.png"));

        language.SelectedIndex = 0;
        await Task.Delay(100);
        window.UpdateLayout();

        Require(CultureInfo.CurrentUICulture.Name == "en-US",
            $"English selection did not restore CurrentUICulture to en-US (was {CultureInfo.CurrentUICulture.Name}).");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "PRESENTATION"),
            "Presentation header did not switch back to English without a restart.");
        Require(Find<CheckBox>(window).Any(check => check.Name == "SoundCheckBox" && Equals(check.Content, "Casino sound effects")),
            "Sound setting did not restore its English resource value.");
        Require(Find<Button>(window).Any(button => button.Name == "HitButton" && Equals(button.Content, "HIT  [H]")),
            "Blackjack action text did not restore to English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "A different kind of night."),
            "Casino lobby headline did not switch back to English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "THE VELVET TABLES"),
            "Blackjack lobby subtitle did not switch back to English.");
    }

    private static void SaveScreenshot(FrameworkElement element, string path)
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
