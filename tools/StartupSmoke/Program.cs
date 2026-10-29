using System.IO;
using System.Windows;
using System.Windows.Controls;
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
                await Task.Delay(200);
                window.UpdateLayout();

                Require(Application.Current.Windows.Cast<Window>().Count(w => w.IsVisible) == 1,
                    "Nine Lives opened another visible native window.");
                Require(Find<Image>(window).Any(image => image.IsVisible && image.Source is not null),
                    "Nine Lives did not render any artwork after opening.");
                Require(Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Nine Lives Spin button is missing from the hosted surface.");

                Button? back = Find<Button>(window).FirstOrDefault(button => button.Name == "CasinoButton");
                Require(back is not null && back.IsVisible,
                    "Nine Lives does not expose a visible Casino/back button.");
                Require(!string.IsNullOrWhiteSpace(back.Content?.ToString()),
                    "Nine Lives Casino/back button is an invisible hotspot without a label.");

                back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(150);
                window.UpdateLayout();

                Require(!Find<Button>(window).Any(button => button.Name == "SpinButton" && button.IsVisible),
                    "Nine Lives remained hosted after using the Casino/back button.");
                Require(Find<Button>(window).Any(button => button.Name == "Mode_nine_lives" && button.IsVisible),
                    "Casino lobby was not restored after leaving Nine Lives.");

                File.WriteAllText(Path.Combine(output, "result.txt"),
                    "PASS: MainWindow rendered the casino lobby, opened Nine Lives in the single-window host, exposed visible controls, and returned to the lobby through the Casino button.");
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

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }

        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            foreach (T child in Find<T>(System.Windows.Media.VisualTreeHelper.GetChild(root, i)))
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
