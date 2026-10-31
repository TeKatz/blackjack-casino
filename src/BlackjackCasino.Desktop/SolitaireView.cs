using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Desktop;

internal sealed class SolitaireView : UserControl, IDisposable
{
    private readonly SolitaireGameState state;
    private readonly Action<SolitaireGameState> persist;
    private readonly Action back;
    private readonly Func<SolitaireCompletionRecord, string> complete;
    private readonly Func<string?> cardSetId;
    private readonly Canvas board = new() { Width = 1120, Height = 640, Background = Brushes.Transparent };
    private readonly TextBlock info = new() { Foreground = LoungeUi.Ink("#8DA9A0"), FontSize = 11 };
    private readonly TextBlock notice = new() { Foreground = LoungeUi.Ink("#E7CCA0"), FontSize = 12, TextWrapping = TextWrapping.Wrap };
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? selectedPile;
    private int selectedIndex = -1;
    private bool completionHandled;
    private bool disposed;

    internal SolitaireView(
        SolitaireGameState game,
        Action<SolitaireGameState> persist,
        Action back,
        Func<SolitaireCompletionRecord, string> complete,
        Func<string?>? cardSetId = null)
    {
        state = game?.Normalize() ?? throw new ArgumentNullException(nameof(game));
        this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        this.back = back ?? throw new ArgumentNullException(nameof(back));
        this.complete = complete ?? throw new ArgumentNullException(nameof(complete));
        this.cardSetId = cardSetId ?? (() => null);
        Content = BuildShell();
        timer.Tick += Timer_Tick;
        if (!state.Won) timer.Start();
        Render();
        if (state.Won) HandleCompletion();
    }

    internal SolitaireGameState Snapshot => state;

    private UIElement BuildShell()
    {
        var root = new Grid { Background = LoungeUi.Ink("#091513") };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Margin = new Thickness(24, 15, 24, 10) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Button backButton = LoungeUi.Button("← SOLITAIRE");
        backButton.Click += (_, _) => { SaveNow(); back(); };
        header.Children.Add(backButton);

        var title = new StackPanel { Margin = new Thickness(18, 0, 18, 0), VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock
        {
            Text = VariantTitle(state), FontFamily = new FontFamily("Georgia"), FontSize = 24, Foreground = LoungeUi.Ink("#F1E6D5")
        });
        title.Children.Add(info);
        title.Children.Add(notice);
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var hints = new TextBlock
        {
            Text = HintText(state.Variant), Foreground = LoungeUi.Ink("#6F8981"), FontSize = 10,
            TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 300, TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(hints, 2);
        header.Children.Add(hints);
        root.Children.Add(header);

        var scroll = new ScrollViewer
        {
            Content = board,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(14, 0, 14, 14),
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        return root;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (state.Won || disposed) return;
        state.ElapsedSeconds++;
        UpdateInfo();
        if (state.ElapsedSeconds % 10 == 0) persist(state);
    }

    private void Render()
    {
        board.Children.Clear();
        UpdateInfo();
        switch (state.Variant)
        {
            case SolitaireVariant.Klondike: RenderKlondike(); break;
            case SolitaireVariant.FreeCell: RenderFreeCell(); break;
            case SolitaireVariant.Spider: RenderSpider(); break;
            case SolitaireVariant.Pyramid: RenderPyramid(); break;
            case SolitaireVariant.Golf: RenderGolf(); break;
            case SolitaireVariant.TriPeaks: RenderTriPeaks(); break;
        }
        if (state.Won) RenderWinBanner();
    }

    private void RenderKlondike()
    {
        RenderStock(35, 20);
        RenderPile("Waste", 135, 20, 0, false);
        string[] foundations = ["F-Clubs", "F-Diamonds", "F-Hearts", "F-Spades"];
        for (int i = 0; i < 4; i++) RenderPile(foundations[i], 600 + i * 105, 20, 0, false, foundations[i][2..]);
        for (int i = 0; i < 7; i++) RenderPile($"T{i}", 45 + i * 145, 170, 31, true);
    }

    private void RenderFreeCell()
    {
        for (int i = 0; i < 4; i++) RenderPile($"Cell{i}", 35 + i * 105, 20, 0, false, $"CELL {i + 1}");
        string[] foundations = ["F-Clubs", "F-Diamonds", "F-Hearts", "F-Spades"];
        for (int i = 0; i < 4; i++) RenderPile(foundations[i], 650 + i * 105, 20, 0, false, foundations[i][2..]);
        for (int i = 0; i < 8; i++) RenderPile($"C{i}", 25 + i * 135, 170, 31, true);
    }

    private void RenderSpider()
    {
        RenderStock(30, 20);
        AddText($"COMPLETED RUNS  {state.CompletedRuns}/8", 160, 50, 12, "#8EC9B0");
        AddText($"{state.SpiderSuits}-SUIT", 380, 50, 11, "#6F8981");
        for (int i = 0; i < 10; i++) RenderPile($"T{i}", 18 + i * 108, 145, 27, true);
    }

    private void RenderPyramid()
    {
        for (int row = 0; row < 7; row++)
        {
            int start = row * (row + 1) / 2;
            for (int col = 0; col <= row; col++)
            {
                double x = 540 + (col - row / 2d) * 92;
                double y = 25 + row * 67;
                RenderPile($"P{start + col}", x, y, 0, false);
            }
        }
        RenderStock(70, 505);
        RenderPile("Waste", 175, 505, 0, false);
        AddText("KINGS CLEAR ALONE · ALL OTHER PAIRS TOTAL 13", 320, 535, 11, "#718E86");
    }

    private void RenderGolf()
    {
        for (int i = 0; i < 7; i++) RenderPile($"G{i}", 55 + i * 145, 80, 43, true);
        RenderStock(380, 445);
        RenderPile("Waste", 500, 445, 0, false);
        AddText("CLEAR THE COLUMNS WITH RANKS ONE HIGHER OR LOWER", 620, 480, 11, "#718E86");
    }

    private void RenderTriPeaks()
    {
        Point[] p =
        [
            new(165,20), new(495,20), new(825,20),
            new(110,85), new(220,85), new(440,85), new(550,85), new(770,85), new(880,85),
            new(55,150), new(165,150), new(275,150), new(385,150), new(495,150), new(605,150), new(715,150), new(825,150), new(935,150),
            new(0,215), new(110,215), new(220,215), new(330,215), new(440,215), new(550,215), new(660,215), new(770,215), new(880,215), new(990,215),
        ];
        for (int i = 0; i < 28; i++) RenderPile($"P{i}", p[i].X, p[i].Y, 0, false);
        RenderStock(390, 440);
        RenderPile("Waste", 510, 440, 0, false);
        AddText("ONE HIGHER OR LOWER · CLEAR ALL THREE PEAKS", 625, 475, 11, "#718E86");
    }

    private void RenderStock(double x, double y)
    {
        IReadOnlyList<SolitaireCardState> stock = SolitaireGame.Pile(state, "Stock");
        if (stock.Count == 0)
        {
            AddPlaceholder("Stock", x, y, "STOCK\nEMPTY", () => { if (SolitaireGame.TryDraw(state)) Changed(); });
            return;
        }
        Border card = PlayingCardPresentation.Create(stock[^1].Card, faceDown: true, compact: false, cardSetId());
        card.Cursor = Cursors.Hand;
        card.ToolTip = state.Variant == SolitaireVariant.Spider ? $"Deal row · {stock.Count} cards remain" : $"Draw · {stock.Count} cards remain";
        card.MouseLeftButtonUp += (_, _) => { if (SolitaireGame.TryDraw(state)) Changed(); };
        Place(card, x, y);
    }

    private void RenderPile(string id, double x, double y, double step, bool stack, string? emptyLabel = null)
    {
        IReadOnlyList<SolitaireCardState> pile = SolitaireGame.Pile(state, id);
        if (pile.Count == 0)
        {
            AddPlaceholder(id, x, y, emptyLabel ?? "EMPTY", () => MoveSelected(id));
            return;
        }

        for (int i = 0; i < pile.Count; i++)
        {
            SolitaireCardState item = pile[i];
            Border card = PlayingCardPresentation.Create(item.Card, !item.FaceUp, compact: false, cardSetId());
            if (selectedPile == id && selectedIndex == i)
            {
                card.BorderBrush = LoungeUi.Ink("#E7CCA0");
                card.BorderThickness = new Thickness(3);
            }
            int index = i;
            card.Cursor = Cursors.Hand;
            card.MouseLeftButtonDown += (_, e) => CardClicked(id, index, e.ClickCount);
            double vertical = stack ? StackOffset(pile, i, step) : 0;
            Place(card, x, y + vertical);
        }
    }

    private static double StackOffset(IReadOnlyList<SolitaireCardState> pile, int index, double faceUpStep)
    {
        double y = 0;
        for (int i = 0; i < index; i++) y += pile[i].FaceUp ? faceUpStep : 15;
        return y;
    }

    private void CardClicked(string pileId, int index, int clickCount)
    {
        if (state.Won) return;

        if (state.Variant is SolitaireVariant.Golf or SolitaireVariant.TriPeaks)
        {
            if (SolitaireGame.TryActivateCard(state, pileId, index)) Changed();
            return;
        }

        if (state.Variant == SolitaireVariant.Pyramid)
        {
            if (SolitaireGame.TryActivateCard(state, pileId, index)) { ClearSelection(); Changed(); return; }
            if (!SolitaireGame.IsSelectable(state, pileId, index)) return;
            if (selectedPile is null) { Select(pileId, index); return; }
            if (SolitaireGame.TryMove(state, selectedPile, selectedIndex, pileId)) { ClearSelection(); Changed(); }
            else Select(pileId, index);
            return;
        }

        IReadOnlyList<SolitaireCardState> pile = SolitaireGame.Pile(state, pileId);
        if (index < pile.Count && !pile[index].FaceUp)
        {
            if (SolitaireGame.TryActivateCard(state, pileId, index)) Changed();
            return;
        }
        if (clickCount >= 2 && SolitaireGame.TryAutoFoundation(state, pileId, index))
        {
            ClearSelection(); Changed(); return;
        }
        if (selectedPile is not null && !string.Equals(selectedPile, pileId, StringComparison.Ordinal))
        {
            if (SolitaireGame.TryMove(state, selectedPile, selectedIndex, pileId)) { ClearSelection(); Changed(); return; }
        }
        if (SolitaireGame.IsSelectable(state, pileId, index)) Select(pileId, index);
    }

    private void MoveSelected(string targetId)
    {
        if (selectedPile is null) return;
        if (SolitaireGame.TryMove(state, selectedPile, selectedIndex, targetId)) { ClearSelection(); Changed(); }
    }

    private void Select(string pileId, int index)
    {
        selectedPile = pileId;
        selectedIndex = index;
        Render();
    }

    private void ClearSelection()
    {
        selectedPile = null;
        selectedIndex = -1;
    }

    private void Changed()
    {
        persist(state);
        if (state.Won)
        {
            timer.Stop();
            HandleCompletion();
        }
        Render();
    }

    private void HandleCompletion()
    {
        if (completionHandled) return;
        completionHandled = true;
        notice.Text = complete(new SolitaireCompletionRecord(
            state.GameId, state.Variant, state.SpiderSuits, state.DealNumber, state.ElapsedSeconds, state.Moves));
    }

    private void SaveNow() => persist(state);

    private void UpdateInfo()
    {
        string time = TimeSpan.FromSeconds(state.ElapsedSeconds).ToString(state.ElapsedSeconds >= 3600 ? @"hh\:mm\:ss" : @"mm\:ss");
        info.Text = $"DEAL #{state.DealNumber:N0}   ·   {state.Moves:N0} moves   ·   {time} elapsed";
    }

    private void RenderWinBanner()
    {
        var banner = new Border
        {
            Width = 460, Height = 100, Background = LoungeUi.Ink("#122B25"), BorderBrush = LoungeUi.Ink("#E7CCA0"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(20)
        };
        banner.Child = new TextBlock
        {
            Text = "SOLVED\nA quiet victory. Start another deal whenever you like.", TextAlignment = TextAlignment.Center,
            FontFamily = new FontFamily("Georgia"), FontSize = 20, Foreground = LoungeUi.Ink("#F1E6D5"), TextWrapping = TextWrapping.Wrap
        };
        Place(banner, 330, 515);
    }

    private void AddPlaceholder(string id, double x, double y, string label, Action action)
    {
        var placeholder = new Border
        {
            Width = 76, Height = 108, CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(1),
            BorderBrush = LoungeUi.Ink("#35524B"), Background = LoungeUi.Ink("#0D201C"), Cursor = Cursors.Hand,
            Child = new TextBlock { Text = label.ToUpperInvariant(), Foreground = LoungeUi.Ink("#58736B"), FontSize = 9, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap }
        };
        placeholder.MouseLeftButtonUp += (_, _) => action();
        Place(placeholder, x, y);
    }

    private void AddText(string text, double x, double y, double size, string color)
    {
        var block = new TextBlock { Text = text, FontSize = size, Foreground = LoungeUi.Ink(color) };
        Place(block, x, y);
    }

    private void Place(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        board.Children.Add(element);
    }

    private static string VariantTitle(SolitaireGameState state) => state.Variant switch
    {
        SolitaireVariant.Spider => $"Spider · {state.SpiderSuits} Suit",
        SolitaireVariant.TriPeaks => "TriPeaks",
        _ => state.Variant.ToString(),
    };

    private static string HintText(SolitaireVariant variant) => variant switch
    {
        SolitaireVariant.Klondike => "Click a card, then its destination. Double-click a top card for foundation. Stock draws one.",
        SolitaireVariant.FreeCell => "Build down in alternating colors. Empty cells hold one card. Double-click sends a card home.",
        SolitaireVariant.Spider => "Build down freely; only same-suit runs move together. Complete King-to-Ace runs. Stock deals ten.",
        SolitaireVariant.Pyramid => "Select two exposed cards totaling 13. Kings clear alone. Draw when no pair is available.",
        SolitaireVariant.Golf => "Click a top card one rank above or below the waste. Aces and Kings do not wrap.",
        SolitaireVariant.TriPeaks => "Clear exposed cards one rank above or below the waste. Aces and Kings do not wrap.",
        _ => string.Empty,
    };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        SaveNow();
    }
}
