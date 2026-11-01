using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Keeps Sudoku's game-list navigation in the persistent Chill Lounge header. Sudoku is the one
/// Lounge game whose hub is owned directly by LoungeWindow rather than the hosted-game registry,
/// so its local back button appears after the shared menu has already loaded.
/// </summary>
internal static class SudokuHeaderNavigationUpgrade
{
    private static readonly HashSet<Window> WatchedWindows = [];
    private static bool installed;

    internal static void EnsureInstalled()
    {
        if (installed) return;
        installed = true;

        EventManager.RegisterClassHandler(
            typeof(LoungeGameMenu),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoungeMenuLoaded));
    }

    private static void OnLoungeMenuLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not LoungeGameMenu menu) return;
        menu.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => AttachWatcher(menu)));
    }

    private static void AttachWatcher(DependencyObject origin)
    {
        Window? window = Window.GetWindow(origin);
        if (window is null || !WatchedWindows.Add(window)) return;

        window.LayoutUpdated += (_, _) => SyncSudokuNavigation(window);
        SyncSudokuNavigation(window);
    }

    private static void SyncSudokuNavigation(Window window)
    {
        Button? localBack = FindDescendant<Button>(window, button => button.Name == "SudokuBackToLoungeGames");
        if (localBack is null) return;

        Button? headerBack = FindDescendant<Button>(window, button => button.Name == "LoungeGamesHeaderBack")
                             ?? CreateHeaderBack(window);
        if (headerBack is null) return;

        localBack.Visibility = Visibility.Collapsed;
        headerBack.Content = LoungeGameMenu.GamesBackLabel();
        headerBack.Tag = (Action)(() => localBack.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
        headerBack.Visibility = Visibility.Visible;
    }

    private static Button? CreateHeaderBack(Window window)
    {
        Button? close = FindDescendant<Button>(window, button =>
            button.Name != "LoungeGamesHeaderBack" &&
            button.Content is string text &&
            text.Contains("CLUB", StringComparison.OrdinalIgnoreCase) &&
            (text.Contains("RETURN", StringComparison.OrdinalIgnoreCase) ||
             text.Contains("ZURÜCK", StringComparison.OrdinalIgnoreCase)));
        if (close is null) return null;

        var back = new Button
        {
            Name = "LoungeGamesHeaderBack",
            Content = LoungeGameMenu.GamesBackLabel(),
            Foreground = LoungeUi.Ink("#EEE4D4"),
            Background = LoungeUi.Ink("#17282A"),
            BorderBrush = LoungeUi.Ink("#4A6661"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(15, 8, 15, 8),
            FontWeight = FontWeights.SemiBold,
            Cursor = CasinoCursors.Hand,
            Margin = new Thickness(0, 0, 10, 0),
            Visibility = Visibility.Collapsed,
        };
        back.Click += (_, _) =>
        {
            if (back.Tag is Action action) action();
        };

        if (close.Parent is Grid grid)
        {
            int column = Grid.GetColumn(close);
            grid.Children.Remove(close);
            var actions = new StackPanel { Orientation = Orientation.Horizontal };
            actions.Children.Add(back);
            actions.Children.Add(close);
            Grid.SetColumn(actions, column);
            grid.Children.Add(actions);
            return back;
        }

        if (close.Parent is StackPanel stack)
        {
            int index = stack.Children.IndexOf(close);
            stack.Children.Insert(Math.Max(0, index), back);
            return back;
        }

        return null;
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
