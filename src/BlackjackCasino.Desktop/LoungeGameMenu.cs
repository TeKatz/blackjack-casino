using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

/// <summary>Each game registers one tile and its route; the menu does not know its rules or save format.</summary>
internal sealed class LoungeGameMenu : UserControl
{
    private readonly IReadOnlyList<LoungeGame> baseGames;
    private readonly Action<string?, Action?>? setShellNavigation;
    private readonly Action? returnToModernHub;
    private Button? injectedShellBack;
    private Action? injectedShellBackAction;

    internal LoungeGameMenu(IEnumerable<LoungeGame> games, Action<string?, Action?>? setShellNavigation = null,
        Action? returnToModernHub = null)
    {
        baseGames = games?.ToArray() ?? throw new ArgumentNullException(nameof(games));
        this.setShellNavigation = setShellNavigation;
        this.returnToModernHub = returnToModernHub;
        Loaded += (_, _) => ApplyInjectedShellNavigation();
        ShowMenu();
    }

    internal bool OpenGame(string id)
    {
        if (string.Equals(id, "sudoku", StringComparison.Ordinal))
        {
            LoungeGame? sudoku = baseGames.FirstOrDefault(game => game.Id == id);
            if (sudoku is null) return false;
            sudoku.Open();
            return true;
        }
        LoungeHostedGame? module = LoungeHostedGameRegistry.Snapshot.FirstOrDefault(game => game.Definition.Id == id);
        if (module is null) return false;
        OpenHosted(module);
        return true;
    }

    private void ShowMenu()
    {
        ConfigureShellNavigation(null, null);

        IReadOnlyList<LoungeHostedGame> hosted = LoungeHostedGameRegistry.Snapshot;
        IEnumerable<LoungeGame> combined = baseGames.Concat(hosted.Select(module => module.Definition with
        {
            Open = () => OpenHosted(module),
        }));
        IReadOnlyList<LoungeGame> catalog = LoungeGameCatalog.Normalize(combined);

        var root = new StackPanel { Margin = new Thickness(42, 30, 42, 30) };
        root.Children.Add(new TextBlock { Text = "A QUIETER CORNER", FontSize = 11, Foreground = LoungeUi.Ink("#8EC9B0") });
        root.Children.Add(new TextBlock { Text = "Find your own pace.", FontSize = 34, FontFamily = new FontFamily("Georgia"), Margin = new Thickness(0, 8, 0, 10) });
        root.Children.Add(new TextBlock { Text = "Choose a game. No entry fees, no rush — just a little time for yourself.", FontSize = 14, Foreground = LoungeUi.Ink("#AFC4BC") });

        var tiles = new WrapPanel { Margin = new Thickness(0, 24, 0, 0) };
        foreach (LoungeGame game in catalog)
        {
            var body = new StackPanel();
            if (game.Artwork is not null)
            {
                FrameworkElement art = game.Artwork();
                art.Height = 130;
                body.Children.Add(art);
            }

            body.Children.Add(new TextBlock
            {
                Text = game.Title,
                FontFamily = new FontFamily("Georgia"),
                FontSize = game.Title.Length > 20 ? 28 : 30,
                LineHeight = 33,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.None,
                Margin = new Thickness(0, 12, 0, 5),
            });
            body.Children.Add(new TextBlock
            {
                Text = game.Subtitle,
                FontSize = 14,
                Foreground = LoungeUi.Ink("#A4D3BA"),
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 34,
            });
            body.Children.Add(new TextBlock
            {
                Text = game.Description,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = LoungeUi.Ink("#B0C1B8"),
                Margin = new Thickness(0, 10, 0, 12),
            });

            string? status = game.Status?.Invoke();
            if (!string.IsNullOrWhiteSpace(status))
            {
                body.Children.Add(new TextBlock
                {
                    Text = status.ToUpperInvariant(),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = LoungeUi.Ink("#718E86"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 18),
                });
            }
            else
            {
                body.Children.Add(new Border { Height = 8 });
            }

            body.Children.Add(new TextBlock { Text = "PLAY   →", FontWeight = FontWeights.Bold, Foreground = LoungeUi.Ink("#E7CCA0") });
            var tile = new Button
            {
                Name = ControlName(game.Id),
                Content = body,
                Width = 340,
                Padding = new Thickness(22),
                Margin = new Thickness(0, 0, 18, 18),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = LoungeUi.Ink("#142725"),
                BorderBrush = LoungeUi.Ink("#628B78"),
            };
            tile.Click += (_, _) => game.Open();
            if (game.Id == "sudoku") LoungeVisualUpgrade.PrepareSudokuTile(tile);
            tiles.Children.Add(tile);
        }

        root.Children.Add(tiles);
        Content = new ScrollViewer
        {
            Content = root,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
    }

    internal static string ControlName(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        char[] safe = gameId.Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
        return "LoungeGame_" + new string(safe);
    }

    internal static string GamesBackLabel() =>
        Localization.Language == UiLanguagePreference.German ? "← SPIELE" : "← GAMES";

    private void OpenHosted(LoungeHostedGame module)
    {
        string label = GamesBackLabel();
        Action action = returnToModernHub ?? ShowMenu;
        ConfigureShellNavigation(label, action);
        Content = module.CreateContent(ShowMenu);

        Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(() => ConfigureShellNavigation(label, action)));
    }

    private void ConfigureShellNavigation(string? label, Action? action)
    {
        if (setShellNavigation is not null)
        {
            setShellNavigation(label, action);
            return;
        }

        injectedShellBackAction = action;
        if (injectedShellBack is not null)
        {
            UpdateShellBack(injectedShellBack);
        }
        ApplyInjectedShellNavigation();
    }

    private void ApplyInjectedShellNavigation()
    {
        if (!IsLoaded) return;
        Window? window = Window.GetWindow(this);
        if (window is null) return;

        injectedShellBack ??= FindDescendant<Button>(window, button =>
            string.Equals(button.Name, "LoungeGamesHeaderBack", StringComparison.Ordinal));
        if (injectedShellBack is not null)
        {
            UpdateShellBack(injectedShellBack);
            return;
        }

        Button? close = FindDescendant<Button>(window, IsReturnToClubButton);
        if (close is null) return;

        injectedShellBack = new Button
        {
            Name = "LoungeGamesHeaderBack",
            Content = GamesBackLabel(),
            Foreground = LoungeUi.Ink("#EEE4D4"),
            Background = LoungeUi.Ink("#17282A"),
            BorderBrush = LoungeUi.Ink("#4A6661"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(15, 8, 15, 8),
            FontWeight = FontWeights.SemiBold,
            Cursor = CasinoCursors.Hand,
            Margin = new Thickness(0, 0, 10, 0),
        };
        injectedShellBack.Click += ShellBackClicked;
        UpdateShellBack(injectedShellBack);

        if (close.Parent is Grid grid)
        {
            int column = Grid.GetColumn(close);
            grid.Children.Remove(close);
            var actions = new StackPanel { Orientation = Orientation.Horizontal };
            actions.Children.Add(injectedShellBack);
            actions.Children.Add(close);
            Grid.SetColumn(actions, column);
            grid.Children.Add(actions);
        }
        else if (close.Parent is StackPanel stack)
        {
            int index = stack.Children.IndexOf(close);
            stack.Children.Insert(Math.Max(0, index), injectedShellBack);
        }
    }

    private void UpdateShellBack(Button button)
    {
        button.Content = GamesBackLabel();
        button.Tag = injectedShellBackAction;
        button.Visibility = injectedShellBackAction is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private static bool IsReturnToClubButton(Button button)
    {
        if (button.Name == "LoungeGamesHeaderBack" || button.Content is not string text) return false;
        return text.Contains("CLUB", StringComparison.OrdinalIgnoreCase) &&
               (text.Contains("RETURN", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ZURÜCK", StringComparison.OrdinalIgnoreCase));
    }

    private static void ShellBackClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Action action }) action();
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T candidate && predicate(candidate)) return candidate;
            T? nested = FindDescendant(child, predicate);
            if (nested is not null) return nested;
        }
        return null;
    }
}
