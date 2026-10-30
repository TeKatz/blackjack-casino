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
        body.Children.Add(Text("Choose your dice night", 28, "#F3E6CF"));
        body.Children.Add(Text("Your casino bankroll: " + Money(bankroll), 15, "#E7CD97"));
        body.Children.Add(Text("CASUAL · SOLO", 12, "#9FD1B6", 18));
        body.Children.Add(Text("No entry fee. Play at your own pace and earn completion XP.", 13));
        var casualButtons = new WrapPanel();
        casualButtons.Children.Add(Button("Continue saved casual game", "ResumeCasualButton", () => playCasual(false)));
        casualButtons.Children.Add(Button("New casual game", "StartCasualButton", () => playCasual(true)));
        body.Children.Add(casualButtons);
        if (casual.Entries.Length > 0 || casual.RollsUsed > 0)
            body.Children.Add(Text("New casual game replaces your saved casual sheet. Casino tables are saved separately.", 11));
        body.Children.Add(Text("CASINO TABLE · YOU + 3 AI OPPONENTS", 12, "#9FD1B6", 17));
        body.Children.Add(Text("Mara · balanced     Felix · upper-section collector     June · adventurous", 12));
        body.Children.Add(Text("Same rules, independent dice and 13 turns each. Entry is charged once. Leaving the table pauses it.", 13));
        var stakes = new ComboBox { Name = "DiceStakeCombo", Margin = new Thickness(0, 10, 0, 7), MinWidth = 180, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (decimal stake in KniffelTable.Stakes) stakes.Items.Add(new ComboBoxItem { Content = Money(stake), Tag = stake });
        stakes.SelectedIndex = 0;
        body.Children.Add(stakes);
        var prizes = Text("", 13, "#E7CD97");
        body.Children.Add(prizes);
        body.Children.Add(Text("Prize money includes your stake return. Ties share the prizes for their occupied places, rounded down to cents. Fixed place bonuses make a full game worthwhile at small tables.", 11));
        Button start = Button("Pay entry & start table", "StartCasinoButton", () => startCasino((decimal)((ComboBoxItem)stakes.SelectedItem).Tag));
        body.Children.Add(start);
        void RefreshStake()
        {
            decimal stake = (decimal)((ComboBoxItem)stakes.SelectedItem).Tag;
            prizes.Text = string.Join("   ·   ", Enumerable.Range(1, 4).Select(place => $"#{place}: {Money(KniffelTable.PrizeForPlace(stake, place))}"));
            start.IsEnabled = bankroll >= stake && !KniffelTable.IsActive(table);
            start.Content = KniffelTable.IsActive(table) ? "Finish or forfeit your current casino table first"
                : bankroll < stake ? "Not enough casino cash" : "Pay " + Money(stake) + " & start table";
        }
        stakes.SelectionChanged += (_, _) => RefreshStake();
        RefreshStake();
        if (table is not null)
        {
            body.Children.Add(Button(table.Settled ? "View last casino result" : "Resume paid casino table · no new entry fee", "ResumeCasinoButton", resumeCasino));
            if (!table.Settled)
            {
                Button abandon = Button("Forfeit table…", "ForfeitCasinoButton", () => { });
                bool confirming = false;
                abandon.Click += (_, _) =>
                {
                    if (!confirming)
                    {
                        confirming = true;
                        abandon.Content = "Confirm forfeit · lose entry, no prize or XP";
                        notice.Text = "Forfeiting permanently ends this table. Use Back to game to keep playing instead.";
                    }
                    else forfeit();
                };
                body.Children.Add(abandon);
            }
        }
        body.Children.Add(notice);
        body.Children.Add(Button("Back to game", "CloseDiceSetupButton", back));
        Content = Panel(body, 650);
    }
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
