using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core;

internal static class LocalizationSmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        Localization.InstallAutomaticTranslation();

        ModernLobbyView react = Find<ModernLobbyView>(window).Single(view => view.Ready && view.IsVisible);
        Require(react.Browser.CoreWebView2 is not null,
            "React lobby is not ready for the visible language switch.");
        Require(window.FindName("SoundCheckBox") is null &&
            window.FindName("GameModeComboBox") is null,
            "Obsolete WPF Blackjack settings were not removed.");
        Require(await react.Browser.CoreWebView2!.ExecuteScriptAsync(
            "Boolean(document.querySelector('.lobby-language'))") == "true",
            "React lobby does not expose the language switch.");

        await react.Browser.CoreWebView2!.ExecuteScriptAsync(
            "document.querySelector('.lobby-language')?.click()");
        // An async smoke test carries its own captured ExecutionContext. CurrentUICulture
        // inside this continuation may therefore remain English even after the WPF host
        // changes the application locale in WebView2's message callback.
        for (int i = 0; i < 60 && Localization.Language != UiLanguagePreference.German; i++)
            await Task.Delay(50);
        window.UpdateLayout();

        // The normal Blackjack route is now React. Its German UI must respond
        // to the same native language preference as the React casino lobby.
        window.Modes.Open("blackjack");
        ModernBlackjackView? modernBlackjack = null;
        for (int i = 0; i < 160; i++)
        {
            await Task.Delay(75);
            modernBlackjack = Find<ModernBlackjackView>(window).FirstOrDefault();
            if (modernBlackjack is { Ready: true, Failed: false }) break;
        }
        Require(modernBlackjack is { Ready: true, Failed: false, IsVisible: true },
            "German Blackjack did not open its primary React table.");
        string translatedBlackjack = await modernBlackjack!.Browser.CoreWebView2!.ExecuteScriptAsync(
            "Boolean(document.querySelector('.bj-app[data-language=de]')"
            + "&&document.querySelector('.bj-player')?.textContent?.includes('DEIN PLATZ')"
            + "&&[...document.querySelectorAll('.bj-actions button')].some(x=>x.textContent?.includes('KARTE'))"
            + "&&document.querySelector('.bj-sidebar')?.textContent?.includes('TISCHEINSTELLUNGEN'))");
        Require(translatedBlackjack == "true",
            "The primary React Blackjack table is not localized to German.");
        await modernBlackjack.Browser.CoreWebView2.ExecuteScriptAsync(
            "document.querySelector('.bj-back')?.click()");
        for (int i=0; i<100 && Find<ModernBlackjackView>(window).Any(v=>v.IsVisible); i++)
            await Task.Delay(60);
        Require(!Find<ModernBlackjackView>(window).Any(v=>v.IsVisible),
            "Localized React Blackjack did not return to the lobby.");

        Require(Localization.Language == UiLanguagePreference.German
            && Localization.Culture.Name == "de-DE"
            && CultureInfo.DefaultThreadCurrentUICulture?.Name == "de-DE",
            $"German selection did not update application culture (active: {Localization.Language}).");
        Require(await react.Browser.CoreWebView2!.ExecuteScriptAsync(
            "Boolean(document.documentElement.lang==='de' && document.querySelector('.lobby-hero h1')?.textContent?.includes('Eine Nacht'))") == "true",
            "The React lobby did not retain German presentation after Blackjack.");
        CheckGermanShellWindows(window);
        CheckPhase2Translations();
        SaveScreenshot(window, Path.Combine(output, "localization-german-lobby.png"));

        NavigationSmoke.OpenCovered(window, "chill-lounge");
        await Task.Delay(140);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Finde dein eigenes Tempo."),
            "Chill Lounge game menu remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Ein wenig Raum zum Denken."),
            "Sudoku Lounge tile remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-lounge.png"));

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGame_sudoku" && button.IsVisible));
        await Task.Delay(80);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Ein Raster. Keine Uhr, die du schlagen musst."),
            "Sudoku hub headline remained English.");
        Require(Find<Button>(window).Any(button => button.Name == "SudokuTimerToggle" && Equals(button.Content, "TIMER · SICHTBAR")),
            "Sudoku timer control remained English.");
        Require(Find<Button>(window).Any(button => button.Name == "SudokuNew_Medium" && Equals(button.Content, "NEUES RÄTSEL")),
            "Sudoku New Puzzle action remained English.");
        Require(Find<Button>(window).Any(button => IsVisibleGamesButton(button, "SPIELE")),
            "Sudoku Games header navigation did not localize to German.");

        Click(Find<Button>(window).Single(button => button.Name == "SudokuNew_Expert"));
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ZAHL"),
            "Sudoku game number-pad label remained English.");
        Require(Find<Button>(window).Any(button => button.IsVisible && Equals(button.Content, "EIN FELD AUFDECKEN")),
            "Sudoku hint action remained English.");
        Require(Find<System.Windows.Controls.Primitives.ToggleButton>(window).Any(button => button.IsVisible && Equals(button.Content, "NOTIZEN  [LEERTASTE]")),
            "Sudoku pencil action remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-sudoku.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← LOUNGE")));
        await Task.Delay(70);
        window.UpdateLayout();
        Click(Find<Button>(window).Single(button => IsVisibleGamesButton(button, "SPIELE")));
        await Task.Delay(70);
        window.UpdateLayout();

        Click(Find<Button>(window).Single(button => button.Name == "LoungeGame_solitaire" && button.IsVisible));
        await Task.Delay(90);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "Lass die Karten zur Ruhe kommen."),
            "Midnight Solitaire hub headline remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ERFOLGE"),
            "Midnight Solitaire achievement section remained English.");
        Require(Find<Button>(window).Any(button => IsVisibleGamesButton(button, "SPIELE")),
            "Midnight Solitaire Games header navigation did not localize to German.");
        Require(!Find<Button>(window).Any(button => button.IsVisible && button.Content is string text && text.Contains("LOUNGE-SPIELE", StringComparison.OrdinalIgnoreCase)),
            "Midnight Solitaire still exposes the old Lounge-Spiele navigation.");
        Button newDeal = Find<Button>(window).First(button => button.IsVisible && Equals(button.Content, "NEUES SPIEL"));
        Click(newDeal);
        await Task.Delay(100);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ZIEHEN"),
            "Klondike draw-zone label remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "ABLAGE"),
            "Klondike waste-zone label remained English.");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "FUNDAMENTE"),
            "Klondike foundation-zone label remained English.");
        Border[] cards = Find<Border>(window).Where(IsPlayingCard).ToArray();
        Require(cards.Length > 0, "Localized Klondike did not render playing cards.");
        MouseClick(cards[^1]);
        await Task.Delay(50);
        window.UpdateLayout();
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text.StartsWith("AUSGEWÄHLT", StringComparison.Ordinal)),
            "Solitaire selected-card feedback remained English.");
        SaveScreenshot(window, Path.Combine(output, "localization-german-solitaire.png"));

        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "← SOLITAIRE")));
        await Task.Delay(50);
        Click(Find<Button>(window).Single(button => IsVisibleGamesButton(button, "SPIELE")));
        await Task.Delay(50);
        Click(Find<Button>(window).Single(button => button.IsVisible && Equals(button.Content, "ZURÜCK ZUM CLUB")));
        await Task.Delay(100);
        window.UpdateLayout();

        ModernLobbyView? languageLobby = Find<ModernLobbyView>(window).FirstOrDefault(view => view.Ready && view.IsVisible);
        Require(languageLobby?.Browser.CoreWebView2 is not null, "Returning from Lounge lost the modern lobby.");
        await languageLobby!.Browser.CoreWebView2!.ExecuteScriptAsync(
            "document.querySelector('.lobby-language')?.click()");
        for (int i = 0; i < 60 && Localization.Language != UiLanguagePreference.English; i++)
            await Task.Delay(50);
        window.UpdateLayout();

        Require(Localization.Language == UiLanguagePreference.English
            && Localization.Culture.Name == "en-US"
            && CultureInfo.DefaultThreadCurrentUICulture?.Name == "en-US",
            $"English selection did not restore application culture (active: {Localization.Language}, default: {CultureInfo.DefaultThreadCurrentUICulture?.Name}).");
        Require(Find<TextBlock>(window).Any(text => text.IsVisible && text.Text == "A different kind of night."),
            "Casino lobby headline did not switch back to English without a restart.");
        // The visible header belongs to React while the WPF header remains the
        // fallback. Its localized text still has to be correct, but not IsVisible.
        Require(await languageLobby!.Browser.CoreWebView2!.ExecuteScriptAsync(
            "Boolean(document.querySelector('.vault-widget')?.textContent?.includes('VAULT'))") == "true",
            "React Vault widget did not restore English navigation.");
        ModernLobbyView? modernLobby = Find<ModernLobbyView>(window)
            .FirstOrDefault(view => view.Ready && view.IsVisible);
        if (modernLobby is not null)
        {
            string reactEnglish = string.Empty;
            for (int i = 0; i < 20; i++)
            {
                reactEnglish = await modernLobby.Browser.CoreWebView2!.ExecuteScriptAsync(
                    "document.documentElement.lang==='en' &&"
                    + "Boolean([...document.querySelectorAll('.lobby-nav-item')]"
                    + ".find(x=>x.textContent?.includes('Vault')))");
                if (reactEnglish.Equals("true", StringComparison.OrdinalIgnoreCase)) break;
                await Task.Delay(120);
            }
            Require(reactEnglish.Equals("true", StringComparison.OrdinalIgnoreCase),
                "React lobby did not restore English labels and Vault navigation.");
        }
    }

    private static void CheckPhase2Translations()
    {
        (string Source, string German)[] samples =
        [
            ("YOUR DICE TABLE", "DEIN WÜRFELTISCH"),
            ("THREE REELS · ONE PAYLINE · PLAY MONEY", "DREI WALZEN · EINE GEWINNLINIE · SPIELGELD"),
            ("THE HOUSE REMEMBERS", "DAS HAUS ERINNERT SICH"),
            ("ACCESS LEVEL I", "ZUGANGSSTUFE I"),
            ("HIGH VOLATILITY · 3×3 · FIVE LINES", "HOHE VOLATILITÄT · 3×3 · FÜNF LINIEN"),
            ("WORDS TO DISCOVER", "WÖRTER ZU FINDEN"),
            ("PAIRS FOUND", "PAARE GEFUNDEN"),
            ("Slot Gallery", "Spielautomatengalerie"),
            ("Warm lights, brass cabinets and the first heartbeat of your casino.",
                "Warmes Licht, Messinggehäuse und der erste Herzschlag deines Casinos."),
            ("Turn 3 of 13", "Runde 3 von 13"),
            ("Roll 2 / 3", "Wurf 2 / 3"),
            ("Record 13 points", "13 Punkte eintragen"),
            ("Upper: 35 points · Bonus: 0 / 35 (at 63)",
                "Oben: 35 Punkte · Bonus: 0 / 35 (bei 63)"),
            ("NINE LIVES · 4 SPINS LEFT", "NINE LIVES · 4 FREISPIELE ÜBRIG"),
            ("Found FOREST. Saved.", "FOREST gefunden. Gespeichert."),
            ("Hint: FOREST begins at the glowing letter.",
                "Hinweis: FOREST beginnt beim leuchtenden Buchstaben."),
            ("7 / 12 words found · 03:12 · 2 hints",
                "7 / 12 Wörter gefunden · 03:12 · 2 Hinweise"),
            ("Active managers: 1/3", "Aktive Manager: 1/3"),
        ];
        foreach ((string source, string german) in samples)
        {
            string actual = Localization.TranslateSource(source);
            Require(string.Equals(actual, german, StringComparison.Ordinal),
                $"Game translation mismatch for '{source}': expected '{german}', got '{actual}'.");
        }
    }

    private static void CheckGermanShellWindows(MainWindow owner)
    {
        var profile = new PlayerProfileState { DisplayName = "Testspieler", Level = 75, Prestige = 3, Coins = 1500 };
        // The retired WPF startup welcome is no longer shown or built. The
        // German React lobby, its profile and language selector are checked above,
        // while the native profile fallback is still covered below.
        using var controller = new PlayerProgressionController(profile);
        var playerWindow = new PlayerProfileWindow(controller) { Owner = owner };
        try
        {
            playerWindow.Show();
            playerWindow.UpdateLayout();
            Require(Find<TextBlock>(playerWindow).Any(x => x.Text == "Deine Casino-Identität"),
                "Player profile identity heading remained English.");
            Require(Find<TextBlock>(playerWindow).Any(x => x.Text.Contains("Stufe 75", StringComparison.Ordinal)),
                "Player profile level summary remained English.");
            Require(Find<Button>(playerWindow).Any(x => Equals(x.Content, "Fortschritt & Shop →")),
                "Player profile progression action remained English.");
        }
        finally { playerWindow.Close(); }

    }

    private static bool IsVisibleGamesButton(Button button, string label)
    {
        if (!button.IsVisible || button.Content is not string text) return false;
        return text.Contains(label, StringComparison.OrdinalIgnoreCase) &&
               !text.Contains("LOUNGE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlayingCard(Border border) =>
        border.IsVisible && Math.Abs(border.Width - 76d) < 0.1 && Math.Abs(border.Height - 108d) < 0.1 && border.Child is Grid;

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void MouseClick(Border card)
    {
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            Source = card,
        };
        card.RaiseEvent(args);
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
