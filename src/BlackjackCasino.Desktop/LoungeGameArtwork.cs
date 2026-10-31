using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Compact vector artwork shared by the Lounge menu and the main casino lobby.</summary>
internal sealed class LoungeGameArtwork(bool dice) : FrameworkElement
{
    private const double DesignWidth = 340d;
    private const double DesignHeight = 130d;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        double scale = Math.Min(ActualWidth / DesignWidth, ActualHeight / DesignHeight);
        dc.PushTransform(new TranslateTransform(
            (ActualWidth - DesignWidth * scale) / 2,
            (ActualHeight - DesignHeight * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));

        if (dice) DrawDice(dc);
        else DrawSudoku(dc);

        dc.Pop();
        dc.Pop();
    }

    private void DrawSudoku(DrawingContext dc)
    {
        // A small desk vignette instead of the old bare grid: warm paper, a proper 9×9
        // board, a pencil and a soft cup silhouette. It remains vector-only and scales cleanly.
        dc.DrawEllipse(Ink("#0A1917", 0.55), null, new Point(170, 119), 128, 10);

        dc.PushTransform(new RotateTransform(-4, 171, 62));
        dc.DrawRoundedRectangle(Ink("#CFC2A3", 0.20), null, new Rect(98, 12, 150, 108), 8, 8);
        dc.Pop();

        dc.DrawRoundedRectangle(Ink("#F0E6CC"), new Pen(Ink("#CDBB8C"), 1.6), new Rect(103, 7, 142, 112), 8, 8);
        dc.DrawRoundedRectangle(Ink("#E8DFC5"), null, new Rect(109, 13, 130, 100), 4, 4);

        const double boardX = 121;
        const double boardY = 17;
        const double cell = 10.2;
        const double boardSize = cell * 9;
        dc.DrawRectangle(Ink("#F5EDD8"), new Pen(Ink("#49675E"), 1.5), new Rect(boardX, boardY, boardSize, boardSize));

        // A few softly tinted cells make the artwork feel like an actual in-progress puzzle.
        foreach ((int row, int col) in new[] { (0, 1), (1, 6), (3, 3), (4, 4), (5, 5), (7, 2), (8, 7) })
        {
            dc.DrawRectangle(Ink("#C9DCCB", 0.52), null,
                new Rect(boardX + col * cell + 0.6, boardY + row * cell + 0.6, cell - 1.2, cell - 1.2));
        }

        for (int i = 1; i < 9; i++)
        {
            bool major = i % 3 == 0;
            var pen = new Pen(Ink(major ? "#49675E" : "#AAB6A8"), major ? 1.45 : 0.55);
            double offset = i * cell;
            dc.DrawLine(pen, new Point(boardX + offset, boardY), new Point(boardX + offset, boardY + boardSize));
            dc.DrawLine(pen, new Point(boardX, boardY + offset), new Point(boardX + boardSize, boardY + offset));
        }

        (int row, int col, string value)[] givens =
        [
            (0, 0, "7"), (0, 4, "2"), (1, 2, "4"), (1, 7, "8"),
            (2, 1, "6"), (2, 5, "3"), (3, 0, "9"), (3, 6, "5"),
            (4, 2, "5"), (4, 4, "8"), (4, 8, "1"), (5, 3, "1"),
            (5, 7, "4"), (6, 1, "2"), (6, 5, "9"), (7, 3, "7"),
            (7, 8, "6"), (8, 4, "5"), (8, 6, "3"),
        ];
        foreach ((int row, int col, string value) in givens)
        {
            DrawLabel(dc, value, 8.6,
                new Point(boardX + col * cell + cell / 2, boardY + row * cell + 0.45),
                "#294A40", FontWeights.SemiBold);
        }

        // Pencil.
        dc.PushTransform(new RotateTransform(-18, 263, 71));
        dc.DrawRoundedRectangle(Ink("#D5A95F"), new Pen(Ink("#A77F42"), 0.8), new Rect(258, 30, 9, 73), 3, 3);
        dc.DrawRectangle(Ink("#5E7B70"), null, new Rect(258, 30, 9, 8));
        var tip = new StreamGeometry();
        using (StreamGeometryContext g = tip.Open())
        {
            g.BeginFigure(new Point(258, 103), true, true);
            g.LineTo(new Point(267, 103), true, false);
            g.LineTo(new Point(262.5, 114), true, false);
        }
        dc.DrawGeometry(Ink("#DCC9A0"), new Pen(Ink("#9B8661"), 0.7), tip);
        dc.DrawEllipse(Ink("#37423D"), null, new Point(262.5, 112), 1.6, 1.6);
        dc.Pop();

        // A small cup grounds the composition without turning the preview into a busy illustration.
        dc.DrawRoundedRectangle(Ink("#203B35"), new Pen(Ink("#6F9487"), 1), new Rect(67, 77, 30, 26), 5, 5);
        dc.DrawEllipse(null, new Pen(Ink("#6F9487"), 1.4), new Point(99, 89), 7, 8);
        dc.DrawEllipse(Ink("#C9B078", 0.48), null, new Point(82, 80), 11, 2.4);
        dc.DrawLine(new Pen(Ink("#79958B", 0.45), 1), new Point(78, 70), new Point(75, 62));
        dc.DrawLine(new Pen(Ink("#79958B", 0.38), 1), new Point(86, 70), new Point(89, 61));
    }

    private void DrawDice(DrawingContext dc)
    {
        Die(dc, 105, 31, -12, 5);
        Die(dc, 172, 38, 13, 6);
    }

    private void Die(DrawingContext dc, double x, double y, double angle, int value)
    {
        dc.PushTransform(new RotateTransform(angle, x + 28, y + 28));
        dc.DrawRoundedRectangle(Ink("#EBE2C9"), new Pen(Ink("#D1AC67"), 2), new Rect(x, y, 56, 56), 10, 10);
        if (value % 2 == 1) Pip(.5, .5);
        Pip(.25, .25); Pip(.75, .75); Pip(.25, .75); Pip(.75, .25);
        if (value == 6) { Pip(.25, .5); Pip(.75, .5); }
        dc.Pop();

        void Pip(double a, double b) => dc.DrawEllipse(Ink("#264B40"), null, new Point(x + 56 * a, y + 56 * b), 4, 4);
    }

    private static void DrawLabel(DrawingContext dc, string text, double size, Point topCentre, string color, FontWeight weight)
    {
        var label = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Georgia"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            Ink(color),
            1d);
        dc.DrawText(label, new Point(topCentre.X - label.Width / 2, topCentre.Y));
    }

    private static SolidColorBrush Ink(string hex, double opacity = 1d)
    {
        Color color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0d, 1d) * 255d), color.R, color.G, color.B));
    }
}
