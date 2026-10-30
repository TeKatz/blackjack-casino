using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed record LoungeGame(string Id, string Title, string Subtitle, string Description, Action Open, Func<FrameworkElement>? Artwork = null);

/// <summary>Each game registers one tile and its route; the menu does not know its rules or save format.</summary>
internal sealed class LoungeGameMenu : UserControl
{
    internal LoungeGameMenu(IEnumerable<LoungeGame> games)
    {
        var root = new StackPanel { Margin = new Thickness(42, 30, 42, 30) };
        root.Children.Add(new TextBlock { Text = "A QUIETER CORNER", FontSize = 11, Foreground = Ink("#8EC9B0") });
        root.Children.Add(new TextBlock { Text = "Find your own pace.", FontSize = 34, FontFamily = new FontFamily("Georgia"), Margin = new Thickness(0, 8, 0, 10) });
        root.Children.Add(new TextBlock { Text = "Choose a game. No entry fees, no rush — just a little time for yourself.", FontSize = 14, Foreground = Ink("#AFC4BC") });
        var tiles = new WrapPanel { Margin = new Thickness(0, 24, 0, 0) };
        foreach (LoungeGame game in games)
        {
            var body = new StackPanel();
            if (game.Artwork is not null)
            {
                FrameworkElement art = game.Artwork(); art.Height = 130; body.Children.Add(art);
            }
            body.Children.Add(new TextBlock { Text = game.Title, FontFamily = new FontFamily("Georgia"), FontSize = 30, Margin = new Thickness(0, 12, 0, 5) });
            body.Children.Add(new TextBlock { Text = game.Subtitle, FontSize = 14, Foreground = Ink("#A4D3BA") });
            body.Children.Add(new TextBlock { Text = game.Description, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Ink("#B0C1B8"), Margin = new Thickness(0, 10, 0, 20) });
            body.Children.Add(new TextBlock { Text = "PLAY   →", FontWeight = FontWeights.Bold, Foreground = Ink("#E7CCA0") });
            var tile = new Button { Name = "LoungeGame_" + game.Id, Content = body, Width = 340, Padding = new Thickness(22),
                Margin = new Thickness(0, 0, 18, 18), HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Ink("#142725"), BorderBrush = Ink("#628B78") };
            tile.Click += (_, _) => game.Open();
            tiles.Children.Add(tile);
        }
        root.Children.Add(tiles);
        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
    private static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
