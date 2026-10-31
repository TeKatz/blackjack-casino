using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Each game registers one tile and its route; the menu does not know its rules or save format.</summary>
internal sealed class LoungeGameMenu : UserControl
{
    private readonly IReadOnlyList<LoungeGame> baseGames;

    internal LoungeGameMenu(IEnumerable<LoungeGame> games)
    {
        baseGames = games?.ToArray() ?? throw new ArgumentNullException(nameof(games));
        ShowMenu();
    }

    private void ShowMenu()
    {
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
                FontSize = 30,
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
                Name = "LoungeGame_" + game.Id,
                Content = body,
                Width = 340,
                Padding = new Thickness(22),
                Margin = new Thickness(0, 0, 18, 18),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = LoungeUi.Ink("#142725"),
                BorderBrush = LoungeUi.Ink("#628B78"),
            };
            tile.Click += (_, _) => game.Open();
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

    private void OpenHosted(LoungeHostedGame module)
    {
        Content = module.CreateContent(ShowMenu);
    }
}
