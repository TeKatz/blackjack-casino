using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

internal sealed class KniffelSetupView : UserControl
{
    private readonly TextBlock notice = Text("", 12, "#E3BD88");
    internal KniffelSetupView(decimal bankroll, KniffelState casual, KniffelTableState? table,
        Action<bool> playCasual, Action<decimal> startCasino, Action resumeCasino, Action forfeit, Action back)
    {
        var body = new StackPanel();
        var heading = new DockPanel();
        var balance = Text("BANKROLL\n" + Money(bankroll), 16, "#E7CD97");
        balance.TextAlignment = TextAlignment.Right;
        DockPanel.SetDock(balance, Dock.Right);
        heading.Children.Add(balance);
        heading.Children.Add(Text("Choose your dice night", 28, "#F3E6CF"));
        body.Children.Add(heading);
        body.Children.Add(Text("Two ways to play. Your casual sheet and paid table save separately.", 13));
        var choices = new Grid { Margin = new Thickness(0, 18, 0, 10) };
        choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.85, GridUnitType.Star) });
        choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
        body.Children.Add(choices);
        var casualBody = new StackPanel();
        casualBody.Children.Add(Text("CASUAL · SOLO", 12, "#9FD1B6"));
        casualBody.Children.Add(Text("A game at your pace", 22, "#F3E6CF"));
        casualBody.Children.Add(Text("Free entry · completion XP · no opponents", 13));
        casualBody.Children.Add(Text("Roll, hold and fill your sheet whenever you feel like it.", 13));
        bool hasSaved = casual.Entries.Length > 0 || casual.RollsUsed > 0;
        var resume = Button("Continue saved game", "ResumeCasualButton", () => playCasual(false));
        resume.IsEnabled = hasSaved;
        casualBody.Children.Add(resume);
        casualBody.Children.Add(Button("New casual game", "StartCasualButton", () => playCasual(true)));
        casualBody.Children.Add(Text(hasSaved ? "Starting a new game replaces this casual sheet." : "No casual game in progress yet.", 11));
        choices.Children.Add(Choice(casualBody, new Thickness(0, 0, 10, 0)));
        var casino = new StackPanel();
        casino.Children.Add(Text("CASINO · YOU + 3 AI", 12, "#9FD1B6"));
        casino.Children.Add(Text("Take a seat at the table", 22, "#F3E6CF"));
        casino.Children.Add(Text("Mara · balanced   /   Felix · collector   /   June · adventurous", 11));
        casino.Children.Add(Text("13 turns each · same rules · independent dice", 13));
        var stakes = new ComboBox { Name = "DiceStakeCombo", Margin = new Thickness(0, 12, 0, 8), MinWidth = 170, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (decimal stake in KniffelTable.Stakes) stakes.Items.Add(new ComboBoxItem { Content = Money(stake), Tag = stake });
        stakes.SelectedIndex = 0;
        casino.Children.Add(Text("ENTRY FEE", 10, "#9FD1B6"));
        casino.Children.Add(stakes);
        var prizeGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2, Margin = new Thickness(0, 3, 0, 8) };
        var prizeLabels = new TextBlock[4];
        for (int i = 0; i < 4; i++) { prizeLabels[i] = Text("", 13, "#E7CD97"); prizeGrid.Children.Add(prizeLabels[i]); }
        casino.Children.Add(prizeGrid);
        casino.Children.Add(Text("Prizes include your stake. Ties share the occupied places' prizes, rounded down to cents.", 11));
        Button start = Button("Pay entry & start table", "StartCasinoButton", () => startCasino((decimal)((ComboBoxItem)stakes.SelectedItem).Tag));
        casino.Children.Add(start);
        void RefreshStake()
        {
            decimal stake = (decimal)((ComboBoxItem)stakes.SelectedItem).Tag;
            for (int i = 0; i < 4; i++) prizeLabels[i].Text = $"#{i + 1}   {Money(KniffelTable.PrizeForPlace(stake, i + 1))}";
            start.IsEnabled = bankroll >= stake && !KniffelTable.IsActive(table);
            start.Content = KniffelTable.IsActive(table) ? "A paid table is already in progress"
                : bankroll < stake ? "Not enough casino cash" : "Pay " + Money(stake) + " & start table";
        }
        stakes.SelectionChanged += (_, _) => RefreshStake();
        RefreshStake();
        if (table is not null)
        {
            casino.Children.Add(Button(table.Settled ? "View last casino result" : "Resume paid table · no new fee", "ResumeCasinoButton", resumeCasino));
            if (!table.Settled)
            {
                Button abandon = Button("Forfeit table…", "ForfeitCasinoButton", () => { });
                bool confirming = false;
                abandon.Click += (_, _) =>
                {
                    if (!confirming)
                    {
                        confirming = true;
                        abandon.Content = "Confirm forfeit";
                        notice.Text = "Forfeit loses your entry with no prize or XP. Back to game keeps your table.";
                    }
                    else forfeit();
                };
                casino.Children.Add(abandon);
            }
        }
        var casinoCard = Choice(casino, new Thickness(0));
        Grid.SetColumn(casinoCard, 1);
        choices.Children.Add(casinoCard);
        body.Children.Add(Text("Paid once. Leaving the table pauses your game.", 11));
        body.Children.Add(notice);
        body.Children.Add(Button("Back to game", "CloseDiceSetupButton", back));
        Content = Panel(body, 960);
    }
    private static Border Choice(UIElement content, Thickness margin) => new()
    {
        Background = Ink("#102A24"), BorderBrush = Ink("#426959"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10), Padding = new Thickness(18), Margin = margin, Child = content,
    };
    internal void SetNotice(string message) => notice.Text = message;
    internal static string Money(decimal value) => value.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    private static TextBlock Text(string value, double size = 13, string color = "#B7CABB", double top = 5)
        => new() { Text = value, FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 3) };
    private static Button Button(string label, string name, Action action)
    {
        var button = new Button { Content = label, Name = name, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 7, 8, 0), HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => action();
        return button;
    }
    private static Border Panel(UIElement child, double width) => new()
    {
        Width = width, MaxHeight = 710, Background = Ink("#19352B"), BorderBrush = Ink("#BAA777"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(14), Padding = new Thickness(24), HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Child = new ScrollViewer { Content = child, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
    };
    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    internal static UIElement OpponentSheets(KniffelTableState table, Action back)
    {
        var body = new StackPanel();
        body.Children.Add(Text("Casino table · opponent score sheets", 26, "#F3E6CF"));
        var grid = new Grid();
        for (int i = 0; i < 3; i++)
        {
            int seat = i;
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            KniffelGame game = KniffelGame.Restore(table.Opponents[i]);
            var column = new StackPanel { Margin = new Thickness(8) };
            column.Children.Add(Text(KniffelTable.Names[i] + " · " + ((KniffelAiStyle)i).ToString(), 16, "#F0D395"));
            foreach (KniffelCategory category in KniffelScoring.Categories)
                column.Children.Add(Text(KniffelView.CategoryName(category) + ": " + (game.ScoreFor(category)?.ToString(CultureInfo.InvariantCulture) ?? "—"), 13));
            column.Children.Add(Text($"Upper bonus: {game.UpperBonus} · Extra: {game.AdditionalKniffelBonus}", 12));
            column.Children.Add(Text($"Total: {game.Total} · {game.FilledCount}/13 filled", 16, "#F0D395"));
            Grid.SetColumn(column, seat);
            grid.Children.Add(column);
        }
        body.Children.Add(grid);
        body.Children.Add(Button("Back to table", "CloseOpponentSheetsButton", back));
        return Panel(body, 1000);
    }
}
