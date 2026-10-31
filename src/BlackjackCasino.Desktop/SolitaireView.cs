using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
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
    private readonly TextBlock selectionInfo = new()
    {
        Foreground = LoungeUi.Ink("#E7CCA0"),
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 2, 0, 0),
    };
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
        title.Children.Add(selectionInfo);
        title.Children.Add(notice);
        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        var hints = new TextBlock
        {
            Text = HintText(state.Variant), Foreground = LoungeUi.Ink("#6F8981"), FontSize = 10,
            TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 320, TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(hints, 2);
        header.Children.Add(hints);
        root.Children.Add(header);

        var boardFrame = new Border
        {
            Margin = new Thickness(14, 0, 14, 14),
            Background = LoungeUi.Ink("#071713"),
            BorderBrush = LoungeUi.Ink("#294940"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(5),
        };
        var scroll = new ScrollViewer
        {
            Content = board,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
        };
        boardFrame.Child = scroll;
        Grid.SetRow(boardFrame, 1);
        root.Children.Add(boardFrame);
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
        RenderBoardSurface();
        UpdateInfo();
        UpdateSelectionInfo();
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

    private void RenderBoardSurface()
    {
        var surface = new Border
        {
            Width = 1100,
            Height = 620,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            BorderBrush = LoungeUi.Ink("#1E3D35"),
            Background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#0A211B"),
                (Color)ColorConverter.ConvertFromString("#071914"),
                new Point(0, 0),
                new Point(1, 1)),
            IsHitTestVisible = false,
        };
        Place(surface, 10, 8);

        // Quiet corner accents give the table a finished surface without competing with the cards.
        AddGuideLine(28, 24, 120, 24, "#527267", 0.45);
        AddGuideLine(1000, 24, 1092, 24, "#527267", 0.45);
        AddGuideLine(28, 610, 120, 610, "#527267", 0.28);
        AddGuideLine(1000, 610, 1092, 610, "#527267", 0.28);
    }

    private void RenderKlondike()
    {
        AddZoneCaption("DRAW", 35, 134);
        AddZoneCaption("WASTE", 135, 134);
        AddZoneCaption("FOUNDATIONS", 600, 134);
        AddSectionRule(154, "TABLEAU");

        RenderStock(35, 20);
        RenderPile("Waste", 135, 20, 0, false);
        string[] foundations = ["F-Clubs", "F-Diamonds", "F-Hearts", "F-Spades"];
        for (int i = 0; i < 4; i++) RenderPile(foundations[i], 600 + i * 105, 20, 0, false, FoundationLabel(foundations[i]));
        for (int i = 0; i < 7; i++) RenderPile($"T{i}", 45 + i * 145, 178, 31, true);
    }

    private void RenderFreeCell()
    {
        AddZoneCaption("FREE CELLS", 35, 134);
        AddZoneCaption("FOUNDATIONS", 650, 134);
        AddSectionRule(154, "TABLEAU");

        for (int i = 0; i < 4; i++) RenderPile($"Cell{i}", 35 + i * 105, 20, 0, false, $"CELL\n{i + 1}");
        string[] foundations = ["F-Clubs", "F-Diamonds", "F-Hearts", "F-Spades"];
        for (int i = 0; i < 4; i++) RenderPile(foundations[i], 650 + i * 105, 20, 0, false, FoundationLabel(foundations[i]));
        for (int i = 0; i < 8; i++) RenderPile($"C{i}", 25 + i * 135, 178, 31, true);
    }

    private void RenderSpider()
    {
        AddZoneCaption("DEAL", 30, 123);
        AddSectionRule(132, "TABLEAU");
        RenderStock(30, 20);
        AddText($"COMPLETED RUNS  {state.CompletedRuns}/8", 160, 49, 12, "#8EC9B0");
        AddText($"{state.SpiderSuits}-SUIT", 380, 49, 11, "#6F8981");
        for (int i = 0; i < 10; i++) RenderPile($"T{i}", 18 + i * 108, 153, 27, true);
    }

    private void RenderPyramid()
    {
        AddZoneCaption("PYRAMID", 36, 26);
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
        AddSectionRule(487, "DRAW & WASTE");
        RenderStock(70, 505);
        RenderPile("Waste", 175, 505, 0, false);
        AddText("KINGS CLEAR ALONE · ALL OTHER PAIRS TOTAL 13", 320, 539, 11, "#718E86");
    }

    private void RenderGolf()
    {
        AddZoneCaption("SEVEN COLUMNS", 55, 56);
        for (int i = 0; i < 7; i++) RenderPile($"G{i}", 55 + i * 145, 80, 43, true);
        AddSectionRule(423, "DRAW & WASTE");
        RenderStock(380, 445);
        RenderPile("Waste", 500, 445, 0, false);
        AddText("CLEAR THE COLUMNS WITH RANKS ONE HIGHER OR LOWER", 620, 480, 11, "#718E86");
    }

    private void RenderTriPeaks()
    {
        AddZoneCaption("THREE PEAKS", 36, 26);
        Point[] p =
        [
            new(165,20), new(495,20), new(825,20),
            new(110,85), new(220,85), new(440,85), new(550,85), new(770,85), new(880,85),
            new(55,150), new(165,150), new(275,150), new(385,150), new(495,150), new(605,150), new(715,150), new(825,150), new(935,150),
            new(0,215), new(110,215), new(220,215), new(330,215), new(440,215), new(550,215), new(660,215), new(770,215), new(880,215), new(990,215),
        ];
        for (int i = 0; i < 28; i++) RenderPile($"P{i}", p[i].X, p[i].Y, 0, false);
        AddSectionRule(416, "DRAW & WASTE");
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

        if (selectedPile == id && selectedIndex >= 0 && selectedIndex < pile.Count)
        {
            double firstOffset = stack ? StackOffset(pile, selectedIndex, step) : 0;
            double lastOffset = stack ? StackOffset(pile, pile.Count - 1, step) : 0;
            double selectedHeight = 116 + Math.Max(0, lastOffset - firstOffset);
            AddSelectionHalo(x, y + firstOffset, selectedHeight);
        }

        for (int i = 0; i < pile.Count; i++)
        {
            SolitaireCardState item = pile[i];
            Border card = PlayingCardPresentation.Create(item.Card, !item.FaceUp, compact: false, cardSetId());
            int index = i;
            card.Cursor = Cursors.Hand;
            card.MouseLeftButtonDown += (_, e) => CardClicked(id, index, e.ClickCount);
            double vertical = stack ? StackOffset(pile, i, step) : 0;
            Place(card, x, y + vertical);
        }
    }

    private void AddSelectionHalo(double x, double y, double height)
    {
        var halo = new Border
        {
            Width = 84,
            Height = height,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(3),
            BorderBrush = LoungeUi.Ink("#F2C96D"),
            Background = new SolidColorBrush(Color.FromArgb(22, 242, 201, 109)),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#F2C96D"),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.72,
            },
        };
        Place(halo, x, y);
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

        // Clicking the exact same selection again is an explicit cancel. This also gives the
        // player a predictable way out of a selection without having to find an invalid target.
        if (selectedPile == pileId && selectedIndex == index)
        {
            ClearSelection();
            Render();
            return;
        }

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
        UpdateSelectionInfo();
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

    private void UpdateSelectionInfo()
    {
        if (selectedPile is null || selectedIndex < 0 ||
            !state.Piles.TryGetValue(selectedPile, out List<SolitaireCardState>? pile) || selectedIndex >= pile.Count)
        {
            selectionInfo.Text = "";
            return;
        }

        SolitaireCardState selected = pile[selectedIndex];
        int count = pile.Count - selectedIndex;
        string run = count > 1 ? $" · {count} CARD RUN" : "";
        selectionInfo.Text = $"SELECTED  ·  {CardLabel(selected.Card)}{run}  ·  click a destination or click the selection again to cancel";
    }

    private void RenderWinBanner()
    {
        var banner = new Border
        {
            Width = 460, Height = 100, Background = LoungeUi.Ink("#122B25"), BorderBrush = LoungeUi.Ink("#E7CCA0"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(20),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 20, ShadowDepth = 6, Opacity = .35 }
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
        bool destination = selectedPile is not null && IsPotentialDestination(id);
        var placeholder = new Border
        {
            Width = 76, Height = 108, CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(destination ? 1.6 : 1),
            BorderBrush = LoungeUi.Ink(destination ? "#668F82" : "#35524B"),
            Background = destination ? new SolidColorBrush(Color.FromArgb(26, 102, 143, 130)) : LoungeUi.Ink("#0D201C"),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = label.ToUpperInvariant(),
                Foreground = LoungeUi.Ink(destination ? "#85A99E" : "#58736B"),
                FontSize = label.Length <= 2 ? 24 : 9,
                FontFamily = label.Length <= 2 ? new FontFamily("Georgia") : SystemFonts.MessageFontFamily,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            }
        };
        placeholder.MouseLeftButtonUp += (_, _) => action();
        Place(placeholder, x, y);
    }

    private bool IsPotentialDestination(string id) => state.Variant switch
    {
        SolitaireVariant.Klondike => id.StartsWith("T", StringComparison.Ordinal) || id.StartsWith("F-", StringComparison.Ordinal),
        SolitaireVariant.FreeCell => id.StartsWith("C", StringComparison.Ordinal) || id.StartsWith("Cell", StringComparison.Ordinal) || id.StartsWith("F-", StringComparison.Ordinal),
        SolitaireVariant.Spider => id.StartsWith("T", StringComparison.Ordinal),
        SolitaireVariant.Pyramid => id.StartsWith("P", StringComparison.Ordinal) || id == "Waste",
        _ => false,
    };

    private void AddSectionRule(double y, string label)
    {
        AddGuideLine(30, y, 1090, y, "#4C7166", 0.42);
        var badge = new Border
        {
            Background = LoungeUi.Ink("#0A211B"),
            Padding = new Thickness(8, 1, 8, 1),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = label,
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = LoungeUi.Ink("#6F9187"),
            }
        };
        Place(badge, 35, y - 8);
    }

    private void AddZoneCaption(string text, double x, double y)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = 8.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = LoungeUi.Ink("#597B71"),
            IsHitTestVisible = false,
        };
        Place(block, x, y);
    }

    private void AddGuideLine(double x1, double y1, double x2, double y2, string color, double opacity)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = LoungeUi.Ink(color),
            StrokeThickness = 1,
            Opacity = opacity,
            IsHitTestVisible = false,
        };
        board.Children.Add(line);
    }

    private void AddText(string text, double x, double y, double size, string color)
    {
        var block = new TextBlock { Text = text, FontSize = size, Foreground = LoungeUi.Ink(color), IsHitTestVisible = false };
        Place(block, x, y);
    }

    private void Place(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        board.Children.Add(element);
    }

    private static string FoundationLabel(string id) => id switch
    {
        "F-Clubs" => "♣",
        "F-Diamonds" => "♦",
        "F-Hearts" => "♥",
        "F-Spades" => "♠",
        _ => "FOUNDATION",
    };

    private static string CardLabel(Card card) => $"{RankLabel(card.Rank)}{SuitLabel(card.Suit)}";

    private static string RankLabel(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    private static string SuitLabel(Suit suit) => suit switch
    {
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        _ => "?",
    };

    private static string VariantTitle(SolitaireGameState state) => state.Variant switch
    {
        SolitaireVariant.Spider => $"Spider · {state.SpiderSuits} Suit",
        SolitaireVariant.TriPeaks => "TriPeaks",
        _ => state.Variant.ToString(),
    };

    private static string HintText(SolitaireVariant variant) => variant switch
    {
        SolitaireVariant.Klondike => "Click a card, then its destination. Click the selected card again to cancel. Double-click a top card for foundation. Stock draws one.",
        SolitaireVariant.FreeCell => "Build down in alternating colors. Empty cells hold one card. Click the selected card again to cancel. Double-click sends a card home.",
        SolitaireVariant.Spider => "Build down freely; only same-suit runs move together. The selected run gets a gold outline. Complete King-to-Ace runs. Stock deals ten.",
        SolitaireVariant.Pyramid => "Select two exposed cards totaling 13. The selected card gets a gold outline. Kings clear alone. Draw when no pair is available.",
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
