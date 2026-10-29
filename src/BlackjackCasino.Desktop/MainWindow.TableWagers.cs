using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly TableWagerHistory tableWagerHistory = new();
    private readonly object tableWagerPreview = new();
    private readonly List<TableWagerView> tableWagerViews = [];

    private void InitializeTableWagerPresentation()
    {
        AnimationsCheckBox.Checked += TableWagerAnimationsChanged;
        AnimationsCheckBox.Unchecked += TableWagerAnimationsChanged;
    }

    private void PresentTableWagers()
    {
        foreach (TableWagerView old in tableWagerViews) old.StopAnimations();
        tableWagerViews.Clear();
        for (int i = 0; i < PlayerHandsPanel.Children.Count; i++)
        {
            if (PlayerHandsPanel.Children[i] is FrameworkElement hand) PresentTableWager(hand, i);
        }
    }

    private void TableWagerAnimationsChanged(object sender, RoutedEventArgs e)
    {
        if (!AnimationsEnabled)
        {
            foreach (TableWagerView view in tableWagerViews) view.StopAnimations();
        }
        UpdateSplitOpportunityCue();
    }

    private void PresentTableWager(FrameworkElement element, int index)
    {
        if (element is not Border seat) return;
        BlackjackRound? round = session.CurrentRound;
        bool preview = round is null;
        if (!preview && index >= round!.PlayerHands.Count) return;
        BlackjackHand? hand = preview ? null : round!.PlayerHands[index];
        decimal amount = hand?.Bet ?? selectedBet;
        bool settled = round?.Phase == RoundPhase.Settled;
        HandResult? result = settled ? round!.Result?.Hands[index] : null;

        var transition = tableWagerHistory.Observe((object?)round ?? session,
            (object?)hand ?? tableWagerPreview, amount, settled, AnimationsEnabled);
        var wager = new TableWagerView(amount, settled, result?.Net ?? 0m, preview);
        tableWagerViews.Add(wager);

        if (preview)
        {
            // Preserve the existing empty-seat message in a compact, scalable seat.
            UIElement message = seat.Child;
            if (message is TextBlock text)
            {
                text.Text = "YOUR SEAT · PLACE A BET AND DEAL";
                text.FontSize = 10;
            }
            var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            seat.Height = double.NaN;
            seat.MinHeight = 150;
            seat.Padding = new Thickness(8);
            seat.Child = null;
            content.Children.Add(message);
            content.Children.Add(wager);
            seat.Child = content;
        }
        else if (seat.Child is StackPanel content)
        {
            if (content.Children[0] is TextBlock heading)
            {
                heading.Text = round!.PlayerHands.Count == 1 ? "YOUR HAND" : $"HAND {index + 1}";
            }
            // The wager sits on the betting arc above the cards. Reserving its
            // space here also lowers YOUR HAND instead of pushing its heading up
            // when the centered seat grows. The existing Viewbox scales both.
            wager.Margin = new Thickness(0, 0, 0, 10);
            content.Children.Insert(0, wager);
        }

        wager.Animate(transition, index);
    }
}
