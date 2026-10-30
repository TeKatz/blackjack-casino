using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/startup-smoke");
        Directory.CreateDirectory(output);

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoControls.xaml", UriKind.Relative),
        });

        int exit = 0;
        app.DispatcherUnhandledException += (_, e) =>
        {
            File.WriteAllText(Path.Combine(output, "dispatcher-failure.txt"), e.Exception.ToString());
            exit = 1;
            e.Handled = true;
            app.Shutdown(exit);
        };

        var profile = new PlayerProfileState
        {
            DisplayName = "Startup Smoke",
            Coins = 10_000,
            SlotTrackingVersion = 1,
        };
        var preferences = new AppPreferences
        {
            Bankroll = 50_000m,
            SoundEnabled = false,
            MusicEnabled = false,
            AnimationsEnabled = false,
            WindowWidth = 1280,
            WindowHeight = 900,
        };

        var window = new MainWindow(preferences, profile);
        window.ContentRendered += async (_, _) =>
        {
            try
            {
                await Task.Delay(150);
                Require(window.IsVisible, "Main window never became visible.");
                Require(window.Modes.Modes.Any(mode => mode.Id == "nine-lives"), "Nine Lives was not registered in the lobby.");

                window.Modes.Open("nine-lives");
                await Task.Delay(260);
                window.UpdateLayout();

                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
                    "Nine Lives opened another visible native window.");
                Require(Find<Image>(window).Any(image => image.IsVisible && image.Source is not null),
                    "Nine Lives did not render any artwork after opening.");

                SaveScreenshot(window, Path.Combine(output, "nine-lives-main.png"));

                Image[] reelImages = Find<Image>(window)
                    .Where(image => image.IsVisible
                        && image.Parent is Border frame
                        && Math.Abs(frame.Width - 257d) < 0.1
                        && Math.Abs(frame.Height - 199d) < 0.1)
                    .ToArray();
                Require(reelImages.Length == 9,
                    $"Nine Lives did not expose exactly nine reel artwork surfaces; found {reelImages.Length}.");
                Require(reelImages.All(image => image.Source is BitmapSource),
                    "Nine Lives regressed to replacement vector symbols instead of the restored concept-art set.");
                Require(reelImages.All(image => image.Source is BitmapSource bitmap
                    && bitmap.PixelWidth >= 112 && bitmap.PixelHeight >= 112),
                    "Nine Lives concept-art symbols decoded below their authored source resolution.");

                Require(Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Nine Lives Spin button is missing from the hosted surface.");
                Button back = Find<Button>(window).FirstOrDefault(button => button.Name == "CasinoButton")
                    ?? throw new InvalidOperationException("Nine Lives does not expose a Casino/back button.");
                Button? duplicateMenu = Find<Button>(window).FirstOrDefault(button => button.Name == "MenuButton");
                Require(back.IsVisible,
                    "Nine Lives Casino/back button exists but is not visible.");
                Require(duplicateMenu is null || !duplicateMenu.IsVisible,
                    "Nine Lives shows its deprecated footer Casino/back navigation control.");
                TextBlock winText = Find<TextBlock>(window).FirstOrDefault(text => text.Name == "WinText")
                    ?? throw new InvalidOperationException("Nine Lives last-win display is missing.");
                Require(winText.Text == "—", "Nine Lives must show an em dash rather than NO/0 for an empty last win.");

                Button settings = Find<Button>(window).FirstOrDefault(button => button.Name == "SettingsButton")
                    ?? throw new InvalidOperationException("Nine Lives settings button is missing.");
                settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(60);
                window.UpdateLayout();
                Border settingsFlyout = Find<Border>(window).FirstOrDefault(border => border.Name == "SettingsFlyout")
                    ?? throw new InvalidOperationException("Nine Lives custom settings flyout is missing.");
                Require(settingsFlyout.Visibility == Visibility.Visible,
                    "Nine Lives settings did not open the custom in-app flyout.");
                Require(settings.ContextMenu is null,
                    "Nine Lives settings regressed to a native Windows ContextMenu.");
                SaveScreenshot(window, Path.Combine(output, "nine-lives-settings.png"));
                settings.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Button paytable = Find<Button>(window).FirstOrDefault(button => button.Name == "InfoButton")
                    ?? throw new InvalidOperationException("Nine Lives paytable button is missing.");
                paytable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(60);
                window.UpdateLayout();
                Border paytableFlyout = Find<Border>(window).FirstOrDefault(border => border.Name == "PaytableFlyout")
                    ?? throw new InvalidOperationException("Nine Lives custom paytable flyout is missing.");
                Require(paytableFlyout.Visibility == Visibility.Visible,
                    "Nine Lives paytable did not open inside the game surface.");
                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
                    "Nine Lives paytable opened native Windows chrome.");
                SaveScreenshot(window, Path.Combine(output, "nine-lives-paytable.png"));
                Button closePaytable = Find<Button>(paytableFlyout).FirstOrDefault(button => Equals(button.Content, "BACK TO GAME"))
                    ?? throw new InvalidOperationException("Nine Lives paytable has no visible return control.");
                Require(closePaytable.ActualWidth >= 180,
                    "Nine Lives paytable return button is too narrow and can clip its label.");
                closePaytable.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Require(!string.IsNullOrWhiteSpace(back.Content?.ToString()),
                    "Nine Lives Casino/back button is an invisible hotspot without a label.");

                back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(150);
                window.UpdateLayout();

                Require(!Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Nine Lives remained hosted after using the Casino/back button.");
                Require(Find<Button>(window).Any(button => button.Name == "Mode_nine_lives" && button.IsVisible),
                    "Casino lobby was not restored after leaving Nine Lives.");

                await SudokuSmoke.Run(window, output);
                await KniffelSmoke.Run(window, output, profile);
                await KniffelCasinoSmoke.Run(window, output, profile);
                await KniffelFeedbackSmoke.Run(window, output);
                await EmpireStrategySmoke.Run(window, output);

                File.WriteAllText(Path.Combine(output, "result.txt"),
                    "PASS: Nine Lives opened with the restored concept-art symbols, single Casino navigation, clean empty-win state, themed in-app flyouts and non-clipping paytable return control; Chill Lounge Sudoku smoke also passed.");
            }
            catch (Exception ex)
            {
                exit = 1;
                File.WriteAllText(Path.Combine(output, "failure.txt"), ex.ToString());
            }
            finally
            {
                window.Close();
                CasinoAudio.Shutdown();
                app.Shutdown(exit);
            }
        };

        window.Show();
        app.Run();
        return exit;
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
        if (root is T match)
        {
            yield return match;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(VisualTreeHelper.GetChild(root, i)))
            {
                yield return child;
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
