using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Desktop;

internal sealed class MemoryCardArtwork : FrameworkElement
{
    private const double CardWidth = 84;
    private const double CardHeight = 102;
    private readonly string deckId;
    private readonly MemorySymbol? symbol;
    private readonly bool matched;

    internal MemoryCardArtwork(string deckId, MemorySymbol? symbol = null, bool matched = false)
    {
        this.deckId = deckId;
        this.symbol = symbol;
        this.matched = matched;
        Width = CardWidth;
        Height = CardHeight;
        SnapsToDevicePixels = true;
        ClipToBounds = true;
        IsHitTestVisible = false;
    }

    internal bool IsFaceUp => symbol is not null;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (symbol is null) DrawBack(dc);
        else
        {
            switch (deckId)
            {
                case "card-suits": DrawPlayingCard(dc, symbol); break;
                case "animals": DrawMedallionCard(dc, symbol, fruit: false); break;
                case "fruits": DrawMedallionCard(dc, symbol, fruit: true); break;
                default: DrawCasinoCard(dc, symbol); break;
            }
            if (matched) DrawMatchedSeal(dc);
        }
    }

    private void DrawBack(DrawingContext dc)
    {
        Rect card = new(1, 1, CardWidth - 2, CardHeight - 2);
        var bg = Gradient("#102A24", "#173D32", 90);
        dc.DrawRoundedRectangle(bg, new Pen(Brush("#57735E"), 1.4), card, 9, 9);
        Rect inner = new(6, 6, CardWidth - 12, CardHeight - 12);
        dc.DrawRoundedRectangle(null, new Pen(Brush("#C7A85F"), 1), inner, 6, 6);
        Rect inner2 = new(9, 9, CardWidth - 18, CardHeight - 18);
        dc.DrawRoundedRectangle(null, new Pen(Brush("#35584B"), .8), inner2, 5, 5);

        Pen lattice = new(Brush("#315349"), .65);
        for (double x = -45; x < 110; x += 16)
        {
            dc.DrawLine(lattice, new Point(x, 12), new Point(x + 74, 90));
            dc.DrawLine(lattice, new Point(x + 74, 12), new Point(x, 90));
        }

        dc.DrawEllipse(Brush("#122E27"), new Pen(Brush("#C7A85F"), 1.1), new Point(42, 51), 19, 19);
        dc.DrawEllipse(null, new Pen(Brush("#4A6B5A"), .9), new Point(42, 51), 15, 15);
        DrawText(dc, "M", 20, "Georgia", Brush("#E3C779"), new Point(42, 48), FontWeights.Bold);
        DrawText(dc, "◆", 8, "Georgia", Brush("#7E9D88"), new Point(42, 66));

        DrawCornerDiamond(dc, 12, 15);
        DrawCornerDiamond(dc, 72, 87);
    }

    private void DrawPlayingCard(DrawingContext dc, MemorySymbol s)
    {
        Rect card = new(1, 1, CardWidth - 2, CardHeight - 2);
        dc.DrawRoundedRectangle(Gradient("#F3EAD7", "#E7D9BF", 90), new Pen(Brush(matched ? "#77A989" : "#B69B65"), matched ? 2 : 1.2), card, 9, 9);
        dc.DrawRoundedRectangle(null, new Pen(Brush("#D0B77D"), .8), new Rect(5, 5, 74, 92), 6, 6);

        string suit = s.Glyph[^1].ToString();
        string rank = s.Glyph[..^1];
        Brush ink = suit is "♥" or "♦" ? Brush("#B6484E") : Brush("#25312F");

        DrawText(dc, rank, rank.Length > 1 ? 10 : 12, "Georgia", ink, new Point(13, 14), FontWeights.Bold);
        DrawText(dc, suit, 10, "Segoe UI Symbol", ink, new Point(13, 27), FontWeights.SemiBold);

        DrawText(dc, suit, 38, "Segoe UI Symbol", ink, new Point(42, 49), FontWeights.SemiBold);
        dc.DrawEllipse(null, new Pen(Brush("#C6AA70"), .8), new Point(42, 49), 24, 24);
        dc.DrawEllipse(null, new Pen(Brush("#D9C69A"), .7), new Point(42, 49), 20, 20);

        dc.PushTransform(new RotateTransform(180, 71, 88));
        DrawText(dc, rank, rank.Length > 1 ? 10 : 12, "Georgia", ink, new Point(71, 80), FontWeights.Bold);
        DrawText(dc, suit, 10, "Segoe UI Symbol", ink, new Point(71, 93), FontWeights.SemiBold);
        dc.Pop();
    }

    private void DrawCasinoCard(DrawingContext dc, MemorySymbol s)
    {
        Rect card = new(1, 1, CardWidth - 2, CardHeight - 2);
        dc.DrawRoundedRectangle(Gradient("#10251F", "#17352E", 90), new Pen(Brush(matched ? "#7AB08D" : "#846E42"), matched ? 2 : 1.25), card, 9, 9);
        dc.DrawRoundedRectangle(null, new Pen(Brush("#D3B568"), .9), new Rect(5, 5, 74, 92), 6, 6);
        dc.DrawLine(new Pen(Brush("#3A574B"), .7), new Point(10, 78), new Point(74, 78));

        Brush accent = Brush(s.Accent);
        dc.DrawEllipse(Gradient("#17362E", "#0E211D", 45), new Pen(accent, 1.25), new Point(42, 43), 27, 27);
        dc.DrawEllipse(null, new Pen(Brush("#4C6456"), .7), new Point(42, 43), 23, 23);
        DrawCasinoMotif(dc, s.Glyph, accent);

        string label = s.Name.ToUpperInvariant();
        if (label.Length > 16) label = label[..16];
        DrawText(dc, label, label.Length > 12 ? 6.6 : 7.4, "Segoe UI Semibold", Brush("#E6D9BC"), new Point(42, 86), FontWeights.SemiBold);
        DrawText(dc, "MIDNIGHT CLUB", 5.7, "Segoe UI", Brush("#759184"), new Point(42, 94));
    }

    private void DrawCasinoMotif(DrawingContext dc, string key, Brush accent)
    {
        Brush cream = Brush("#F0E5CD");
        Pen pen = new(accent, 2.1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        switch (key)
        {
            case "chip":
                dc.DrawEllipse(Brush("#263E36"), pen, new Point(42, 43), 16, 16);
                dc.DrawEllipse(null, new Pen(cream, 1), new Point(42, 43), 10, 10);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    dc.DrawLine(new Pen(cream, 2), new Point(42 + Math.Cos(a) * 12, 43 + Math.Sin(a) * 12), new Point(42 + Math.Cos(a) * 16, 43 + Math.Sin(a) * 16));
                }
                DrawText(dc, "25", 9, "Georgia", cream, new Point(42, 43), FontWeights.Bold);
                break;
            case "dice":
                dc.DrawRoundedRectangle(cream, pen, new Rect(27, 28, 30, 30), 5, 5);
                foreach (Point p in new[] { new Point(34,35), new Point(50,35), new Point(42,43), new Point(34,51), new Point(50,51) }) dc.DrawEllipse(accent, null, p, 2.3, 2.3);
                break;
            case "roulette": case "wheel": case "spin":
                dc.DrawEllipse(null, pen, new Point(42, 43), 17, 17);
                dc.DrawEllipse(accent, null, new Point(42, 43), 4, 4);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    dc.DrawLine(new Pen(accent, 1), new Point(42 + Math.Cos(a) * 5, 43 + Math.Sin(a) * 5), new Point(42 + Math.Cos(a) * 16, 43 + Math.Sin(a) * 16));
                }
                break;
            case "vault":
                dc.DrawRoundedRectangle(null, pen, new Rect(25, 25, 34, 36), 4, 4);
                dc.DrawEllipse(null, new Pen(cream, 1.6), new Point(42, 43), 9, 9);
                for (int i = 0; i < 4; i++)
                {
                    double a = i * Math.PI / 2;
                    dc.DrawLine(new Pen(cream, 1.4), new Point(42 + Math.Cos(a) * 3,43 + Math.Sin(a) * 3), new Point(42 + Math.Cos(a) * 8,43 + Math.Sin(a) * 8));
                }
                break;
            case "cocktail":
                var glass = new StreamGeometry();
                using (StreamGeometryContext g = glass.Open()) { g.BeginFigure(new Point(28,29), false, false); g.LineTo(new Point(56,29), true, false); g.LineTo(new Point(42,46), true, false); g.LineTo(new Point(28,29), true, false); }
                dc.DrawGeometry(null, pen, glass);
                dc.DrawLine(pen, new Point(42,46), new Point(42,57));
                dc.DrawLine(pen, new Point(34,57), new Point(50,57));
                dc.DrawEllipse(Brush("#C86A75"), null, new Point(51,27), 3.5, 3.5);
                break;
            case "cards":
                dc.PushTransform(new RotateTransform(-12, 38, 43));
                dc.DrawRoundedRectangle(cream, pen, new Rect(25,28,23,31), 3, 3);
                dc.Pop();
                dc.PushTransform(new RotateTransform(12, 46, 43));
                dc.DrawRoundedRectangle(cream, pen, new Rect(38,28,23,31), 3, 3);
                dc.Pop();
                DrawText(dc, "A", 10, "Georgia", Brush("#B6484E"), new Point(45, 41), FontWeights.Bold);
                break;
            case "key":
                dc.DrawEllipse(null, pen, new Point(34,37), 7, 7);
                dc.DrawLine(pen, new Point(39,42), new Point(54,56));
                dc.DrawLine(pen, new Point(49,51), new Point(53,47));
                dc.DrawLine(pen, new Point(53,55), new Point(57,51));
                break;
            case "crown":
                var crown = new StreamGeometry();
                using (StreamGeometryContext g = crown.Open()) { g.BeginFigure(new Point(26,52), true, true); g.LineTo(new Point(29,32), true, false); g.LineTo(new Point(38,42), true, false); g.LineTo(new Point(42,27), true, false); g.LineTo(new Point(47,42), true, false); g.LineTo(new Point(56,32), true, false); g.LineTo(new Point(59,52), true, false); }
                dc.DrawGeometry(accent, new Pen(cream, 1), crown);
                break;
            case "diamond":
                var diamond = new StreamGeometry();
                using (StreamGeometryContext g = diamond.Open()) { g.BeginFigure(new Point(42,25), true, true); g.LineTo(new Point(58,40), true, false); g.LineTo(new Point(42,60), true, false); g.LineTo(new Point(26,40), true, false); }
                dc.DrawGeometry(accent, new Pen(cream, 1), diamond);
                break;
            case "star": DrawText(dc, "★", 32, "Segoe UI Symbol", accent, new Point(42,42)); break;
            case "clover": DrawText(dc, "♣", 34, "Segoe UI Symbol", accent, new Point(42,42)); break;
            case "moon": DrawText(dc, "☾", 34, "Segoe UI Symbol", accent, new Point(42,42)); break;
            case "horseshoe": DrawText(dc, "U", 31, "Georgia", accent, new Point(42,40), FontWeights.Bold); break;
            case "bell": DrawText(dc, "♢", 30, "Georgia", accent, new Point(42,40), FontWeights.Bold); DrawText(dc, "•", 16, "Segoe UI", cream, new Point(42,54)); break;
            case "slot": DrawText(dc, "7 7 7", 12, "Georgia", accent, new Point(42,43), FontWeights.Bold); break;
            case "seven": DrawText(dc, "7", 35, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            case "ace": DrawText(dc, "A♠", 27, "Georgia", cream, new Point(42,42), FontWeights.Bold); break;
            case "twentyone": DrawText(dc, "21", 27, "Georgia", cream, new Point(42,42), FontWeights.Bold); break;
            case "king": DrawText(dc, "K", 31, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            case "queen": DrawText(dc, "Q", 31, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            case "jack": DrawText(dc, "J", 31, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            case "bar": DrawText(dc, "BAR", 17, "Segoe UI Black", accent, new Point(42,43), FontWeights.Bold); break;
            case "vip": DrawText(dc, "VIP", 17, "Segoe UI Black", accent, new Point(42,43), FontWeights.Bold); break;
            case "gold": DrawText(dc, "GOLD", 12, "Segoe UI Black", accent, new Point(42,43), FontWeights.Bold); break;
            case "cash": DrawText(dc, "$", 31, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            case "dealer": DrawText(dc, "D", 29, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            case "table": DrawText(dc, "♠ ♥", 14, "Segoe UI Symbol", accent, new Point(42,42), FontWeights.Bold); break;
            case "club": DrawText(dc, "♣", 34, "Segoe UI Symbol", accent, new Point(42,42)); break;
            case "token": DrawText(dc, "M", 27, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
            default: DrawText(dc, "M", 28, "Georgia", accent, new Point(42,42), FontWeights.Bold); break;
        }
    }

    private void DrawMedallionCard(DrawingContext dc, MemorySymbol s, bool fruit)
    {
        Rect card = new(1, 1, CardWidth - 2, CardHeight - 2);
        string top = fruit ? "#432A26" : "#173029";
        string bottom = fruit ? "#2E1F1C" : "#10241E";
        string frame = fruit ? "#A77858" : "#647B5F";
        string plate = fruit ? "#F0DCC0" : "#E6DDC8";
        dc.DrawRoundedRectangle(Gradient(top, bottom, 90), new Pen(Brush(matched ? "#78AD88" : frame), matched ? 2 : 1.2), card, 9, 9);
        dc.DrawRoundedRectangle(null, new Pen(Brush(fruit ? "#D2A96E" : "#AF9D6A"), .8), new Rect(5, 5, 74, 92), 6, 6);

        dc.DrawEllipse(Brush(plate), new Pen(Brush(s.Accent), 1.3), new Point(42, 42), 27, 27);
        dc.DrawEllipse(null, new Pen(Brush("#C8B58F"), .8), new Point(42, 42), 23, 23);
        DrawText(dc, s.Glyph, fruit ? 29 : 30, "Segoe UI Emoji", Brush("#3A3029"), new Point(42, 41));

        dc.DrawRoundedRectangle(Brush(fruit ? "#56362E" : "#203B31"), new Pen(Brush(s.Accent), .8), new Rect(10, 76, 64, 17), 7, 7);
        string label = s.Name.ToUpperInvariant();
        DrawText(dc, label, label.Length > 12 ? 6.2 : 7.2, "Segoe UI Semibold", Brush("#F0E4CC"), new Point(42, 84.5), FontWeights.SemiBold);

        if (fruit)
        {
            DrawText(dc, "✦", 8, "Segoe UI Symbol", Brush("#D7B56F"), new Point(14, 17));
            DrawText(dc, "✦", 6, "Segoe UI Symbol", Brush("#A77D59"), new Point(69, 67));
        }
        else
        {
            dc.DrawLine(new Pen(Brush("#718B71"), 1), new Point(14,18), new Point(22,13));
            dc.DrawLine(new Pen(Brush("#718B71"), 1), new Point(14,18), new Point(21,22));
            dc.DrawLine(new Pen(Brush("#718B71"), 1), new Point(70,66), new Point(62,71));
            dc.DrawLine(new Pen(Brush("#718B71"), 1), new Point(70,66), new Point(63,62));
        }
    }

    private void DrawMatchedSeal(DrawingContext dc)
    {
        dc.DrawEllipse(Brush("#173D31"), new Pen(Brush("#8FC69D"), 1.2), new Point(70,14), 9, 9);
        DrawText(dc, "✓", 11, "Segoe UI Semibold", Brush("#CFE6D4"), new Point(70,13.5), FontWeights.Bold);
    }

    private void DrawCornerDiamond(DrawingContext dc, double x, double y)
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext g = geometry.Open())
        {
            g.BeginFigure(new Point(x, y - 4), true, true);
            g.LineTo(new Point(x + 4, y), true, false);
            g.LineTo(new Point(x, y + 4), true, false);
            g.LineTo(new Point(x - 4, y), true, false);
        }
        dc.DrawGeometry(Brush("#C7A85F"), null, geometry);
    }

    private void DrawText(DrawingContext dc, string text, double size, string font, Brush brush, Point center, FontWeight? weight = null)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(font), FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }

    private static LinearGradientBrush Gradient(string start, string end, double angle)
    {
        var brush = new LinearGradientBrush(Brush(start).Color, Brush(end).Color, angle);
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
