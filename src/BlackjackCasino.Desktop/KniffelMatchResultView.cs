using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

/// <summary>A read-only receipt: opening this screen never changes cash, XP or statistics.</summary>
internal sealed class KniffelMatchResultView : UserControl
{
    internal KniffelMatchResultView(KniffelState player, KniffelTableState? table, decimal bankroll, string reward,
        Action close, Action setup, Action casino)
    {
        Name = "DiceMatchResult";
        KniffelGame game = KniffelGame.Restore(player);
        bool forfeited = table?.Forfeited == true;
        var body = new StackPanel();
        body.Children.Add(Text("THE MIDNIGHT CLUB  /  MATCH RECEIPT", 11, "#9FD1B6"));
        body.Children.Add(Text(forfeited ? "Table forfeited" : table is null ? "Your sheet is complete" : table.Placement == 1 ? "Top of the table" : "One last look at the table", 30, "#F5E8CB"));
        body.Children.Add(Text(table is null ? $"Casual · {game.Total} points · 13 turns"
            : forfeited ? $"Casino · {game.FilledCount}/13 turns played · Entry fee lost"
            : $"Casino · Place #{table.Placement}{(table.Opponents.Any(s => KniffelGame.Restore(s).Total == game.Total) ? " (tied)" : "")} of 4 · 13 turns", 16, "#E8CB8F"));

        if (table is not null)
        {
            decimal net = table.Payout - table.Stake;
            var cash = new UniformGrid { Columns = 3, Margin = new Thickness(0, 10, 0, 6) };
            cash.Children.Add(Metric("ENTRY PAID", "−" + KniffelSetupView.Money(table.Stake), "Already charged when you joined", "MatchEntryText"));
            cash.Children.Add(Metric("PRIZE RETURNED", "+" + KniffelSetupView.Money(table.Payout), forfeited ? "No prize for a forfeited table" : "Includes the stake return", "MatchPayoutText"));
            cash.Children.Add(Metric(net > 0 ? "NET WIN" : net < 0 ? "NET LOSS" : "BREAK EVEN", Signed(net), "Prize returned minus entry paid", "MatchNetText", net < 0 ? "#E9AD99" : "#A5E6C4"));
            body.Children.Add(cash);
            body.Children.Add(Text("Current casino bankroll: " + KniffelSetupView.Money(bankroll) + " · Cash already saved", 13, "#C9DACD", "MatchBankrollText"));
            if (!forfeited && table.Opponents.Any(s => KniffelGame.Restore(s).Total == game.Total))
                body.Children.Add(Text("Tied scores share the prizes for their occupied places, rounded down to cents.", 12));
        }
        else body.Children.Add(Text("Casual game · No entry fee or casino cash payout", 13, "#C9DACD"));

        body.Children.Add(Text(forfeited ? "SCORES WHEN YOU LEFT" : "FINAL STANDINGS & SCORE BREAKDOWN", 12, "#9FD1B6", top: 12));
        var standings = new Grid { Margin = new Thickness(0, 8, 0, 5) };
        double[] widths = [44, 135, 76, 80, 80, 96, 110, 85];
        foreach (double width in widths) standings.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) });
        string[] headers = ["Place", "Player", "Total", "Upper", "Lower", "Upper bonus", "Extra bonus", "5 of a kind"];
        standings.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int i = 0; i < headers.Length; i++) AddCell(standings, headers[i], 0, i, false, true);
        var seats = new List<(string Name, KniffelGame Game, bool Player)> { ("You", game, true) };
        if (table is not null) seats.AddRange(table.Opponents.Select((state, i) => (KniffelTable.Names[i], KniffelGame.Restore(state), false)));
        var ordered = seats.OrderByDescending(s => s.Game.Total).ToArray();
        for (int i = 0; i < ordered.Length; i++)
        {
            var seat = ordered[i];
            standings.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            string place = forfeited ? "—" : "#" + (1 + seats.Count(s => s.Game.Total > seat.Game.Total));
            string[] values = [place, seat.Name, seat.Game.Total.ToString(CultureInfo.InvariantCulture), seat.Game.UpperSubtotal.ToString(CultureInfo.InvariantCulture),
                seat.Game.LowerSubtotal.ToString(CultureInfo.InvariantCulture), "+" + seat.Game.UpperBonus, "+" + seat.Game.AdditionalKniffelBonus, seat.Game.KniffelsScored.ToString(CultureInfo.InvariantCulture)];
            for (int c = 0; c < values.Length; c++) AddCell(standings, values[c], i + 1, c, seat.Player, false);
        }
        body.Children.Add(standings);
        int scratches = player.Entries.Count(e => e.Points == 0);
        string[] made = player.Entries.Where(e => e.Points > 0 && e.Category is KniffelCategory.FullHouse or KniffelCategory.SmallStraight or KniffelCategory.LargeStraight or KniffelCategory.Kniffel)
            .Select(e => KniffelView.CategoryName(e.Category)).ToArray();
        body.Children.Add(Text($"Your sheet: {game.FilledCount}/13 boxes · {scratches} scratched · {game.KniffelsScored} five-of-a-kind rolls", 13, "#C9DACD"));
        if (made.Length > 0) body.Children.Add(Text("Scored combinations: " + string.Join(" · ", made), 12));
        body.Children.Add(Text(forfeited ? "No completion XP · Forfeited tables do not earn completion rewards."
            : $"Completion reward: {KniffelProgression.PlayerExperience(game.Total)} XP · {KniffelProgression.SeasonExperience(game.Total)} Season XP", 15, "#E8CB8F", "MatchXpText", 10));
        if (!string.IsNullOrWhiteSpace(reward)) body.Children.Add(Text(reward, 12, "#B7CABB", "MatchRewardStatusText"));
        var buttons = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        buttons.Children.Add(Button("Back to score sheet", "CloseMatchResultButton", close));
        buttons.Children.Add(Button("Game setup", "ResultGameSetupButton", setup));
        buttons.Children.Add(Button("Return to casino", "ResultCasinoButton", casino));
        var layout = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        layout.Children.Add(buttons);
        layout.Children.Add(new ScrollViewer { Name = "MatchDetailsScroll", Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Content = new Border
        {
            Width = 960, MaxHeight = 710, Padding = new Thickness(24), CornerRadius = new CornerRadius(18),
            Background = Ink("#19352B"), BorderBrush = Ink("#D7BC7B"), BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Child = layout,
        };
    }
    private static Border Metric(string title, string value, string detail, string name, string color = "#F0D395")
    {
        var content = new StackPanel();
        content.Children.Add(Text(title, 11, "#B4CDBD"));
        content.Children.Add(Text(value, 26, color, name));
        content.Children.Add(Text(detail, 11));
        return new Border { Background = Ink("#10291F"), CornerRadius = new CornerRadius(9), Padding = new Thickness(12), Margin = new Thickness(0, 0, 10, 0), Child = content };
    }
    private static void AddCell(Grid grid, string value, int row, int column, bool player, bool header)
    {
        TextBlock label = Text(value, header ? 11 : 14, player ? "#F4D797" : header ? "#A9C2AF" : "#D7E1D5");
        label.FontWeight = player ? FontWeights.Bold : FontWeights.Normal;
        label.Margin = new Thickness(0);
        var cell = new Border { Padding = new Thickness(8, 4, 4, 4), Background = player ? Ink("#294B38") : Brushes.Transparent, Child = label };
        Grid.SetRow(cell, row); Grid.SetColumn(cell, column); grid.Children.Add(cell);
    }
    internal static string Signed(decimal value) => (value < 0 ? "−" : value > 0 ? "+" : "") + KniffelSetupView.Money(Math.Abs(value));
    private static TextBlock Text(string text, double size, string color = "#B7CABB", string? name = null, double top = 3)
        => new() { Text = text, Name = name ?? "", FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 1) };
    private static Button Button(string text, string name, Action action)
    {
        var button = new Button { Name = name, Content = text, Padding = new Thickness(16, 10, 16, 10), Margin = new Thickness(0, 0, 10, 0) };
        button.Click += (_, _) => action(); return button;
    }
    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
