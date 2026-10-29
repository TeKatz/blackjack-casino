using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>Resolution-independent 2D furnishings. No guest simulation or 3D scene.</summary>
internal sealed class EmpireFloorArtwork : FrameworkElement
{
    internal static readonly Rect[] RoomBounds =
    [
        new(20, 85, 300, 165), new(340, 85, 300, 165), new(660, 85, 320, 165),
        new(20, 270, 460, 175), new(500, 270, 480, 175),
    ];
    internal EmpireState? State { get; set; }
    internal string SelectedRoom { get; set; } = "";
    private double phase;
    internal double Phase { get => phase; set { phase = value; InvalidateVisual(); } }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.PushTransform(new ScaleTransform(ActualWidth / 1000, ActualHeight / 470));
        dc.DrawRoundedRectangle(Ink("#0D1B1E"), new Pen(Ink("#53604C"), 2), new Rect(1, 1, 998, 468), 16, 16);
        for (int x = 20; x < 1000; x += 30)
            dc.DrawLine(new Pen(Ink("#14292A"), 1), new Point(x, 65), new Point(x, 450));
        Text(dc, "M I D N I G H T   E S T A T E", 30, 22, 23, "#E4C48C");
        Text(dc, State is null ? "YOUR NEXT CHAPTER" : $"{State.Levels.Values.Count(level => level > 0)} / 5 ROOMS OPEN", 738, 28, 14, "#91AC9E");
        dc.DrawLine(new Pen(Ink("#59604A"), 1), new Point(30, 63), new Point(970, 63));
        for (int i = 0; i < EmpireCatalog.Departments.Count; i++)
        {
            EmpireDepartment room = EmpireCatalog.Departments[i];
            int level = State is null ? 10 : State.Levels.GetValueOrDefault(room.Id);
            Rect bounds = RoomBounds[i];
            bool selected = SelectedRoom == room.Id;
            dc.DrawRoundedRectangle(Ink(level > 0 ? "#1A302F" : "#111F23"),
                new Pen(Ink(selected ? "#F0D49B" : level > 0 ? "#576452" : "#344443"), selected ? 3 : 1.5), bounds, 10, 10);
            dc.PushTransform(new TranslateTransform(bounds.X, bounds.Y));
            Text(dc, room.Name.ToUpperInvariant(), 15, 13, 17, level > 0 ? room.Accent : "#71827D");
            Text(dc, level > 0 ? "LV " + level : "LOCKED", bounds.Width - 75, 16, 12, "#A5B9AB");
            dc.DrawLine(new Pen(Ink("#3B4E45"), 1), new Point(14, 40), new Point(bounds.Width - 14, 40));
            if (level == 0)
            {
                Text(dc, "ROOM AVAILABLE", 20, 65, 16, "#7B9189");
                Text(dc, "Open for " + EmpireView.Number(room.BaseCost) + " credits", 20, 93, 14, "#A2B7A9");
                dc.DrawRoundedRectangle(null, new Pen(Ink("#3B4E45"), 1), new Rect(bounds.Width - 66, 69, 34, 36), 5, 5);
                dc.DrawEllipse(null, new Pen(Ink("#7B9189"), 2), new Point(bounds.Width - 49, 71), 9, 12);
            }
            else
            {
                // Each ten levels adds richer upholstery and a second line of gold trim.
                string trim = level >= 10 ? "#E0C58A" : "#8A825F";
                if (level >= 10)
                    dc.DrawRoundedRectangle(null, new Pen(Ink("#766B49"), 1), new Rect(6, 6, bounds.Width - 12, bounds.Height - 12), 8, 8);
                switch (room.Id)
                {
                    case "slots": DrawSlots(dc, level, trim); break;
                    case "tables": DrawCards(dc, level, trim); break;
                    case "bar": DrawBar(dc, level, trim); break;
                    case "roulette": DrawRoulette(dc, level, trim); break;
                    case "vip": DrawLounge(dc, level, trim); break;
                }
                Text(dc, "+" + EmpireView.Number(EmpireRules.Production(room, level)) + " cr / sec", 15, bounds.Height - 27, 13, "#A7CFB7");
                // A restrained pulse marks production; no simulation influences earnings.
                double light = .55 + .35 * Math.Sin(phase + i);
                dc.PushOpacity(light);
                dc.DrawEllipse(Ink(room.Accent), null, new Point(bounds.Width - 22, bounds.Height - 22), 4, 4);
                dc.Pop();
            }
            dc.Pop();
        }
        dc.Pop();
    }

    private void DrawSlots(DrawingContext dc, int level, string trim)
    {
        int count = Math.Min(5, 3 + level / 10);
        for (int i = 0; i < count; i++)
        {
            double x = 20 + i * 51;
            dc.DrawRoundedRectangle(Ink("#0B181C"), new Pen(Ink(trim), 2), new Rect(x, 54, 40, 67), 5, 5);
            dc.DrawRectangle(Ink("#274C48"), null, new Rect(x + 5, 61, 30, 12));
            for (int reel = 0; reel < 3; reel++)
            {
                dc.DrawRectangle(Ink("#E0D2A6"), null, new Rect(x + 5 + reel * 10, 78, 8, 20));
                Text(dc, ((i + reel + (int)(phase * 2)) % 3 + 7).ToString(CultureInfo.InvariantCulture), x + 5 + reel * 10, 80, 10, "#6A3C44");
            }
            dc.DrawLine(new Pen(Ink(trim), 2), new Point(x + 6, 109), new Point(x + 33, 109));
        }
    }

    private void DrawCards(DrawingContext dc, int level, string trim)
    {
        dc.DrawRoundedRectangle(Ink(level >= 10 ? "#315A47" : "#25483D"), new Pen(Ink(trim), 3), new Rect(38, 59, 223, 62), 28, 28);
        for (int i = 0; i < 3; i++)
        {
            double x = 78 + i * 52;
            dc.DrawRoundedRectangle(Ink("#E9DFC4"), null, new Rect(x, 71, 18, 25), 2, 2);
            Text(dc, i == 1 ? "A" : "K", x + 3, 73, 12, i == 1 ? "#9C4E56" : "#18332B");
            dc.DrawEllipse(Ink("#B39158"), new Pen(Ink("#F3D8A3"), 1), new Point(x + 9, 108), 5, 3);
        }
    }

    private void DrawBar(DrawingContext dc, int level, string trim)
    {
        dc.DrawRoundedRectangle(Ink(level >= 10 ? "#58445E" : "#3C3447"), new Pen(Ink(trim), 2), new Rect(22, 82, 270, 32), 8, 8);
        for (int i = 0; i < 7; i++)
        {
            double x = 40 + i * 36;
            dc.DrawRectangle(Ink(i % 2 == 0 ? "#77B3A0" : "#B99BD8"), null, new Rect(x, 58, 9, 24));
            dc.DrawRectangle(Ink(trim), null, new Rect(x + 2, 53, 5, 7));
        }
        for (int i = 0; i < 4; i++) dc.DrawEllipse(Ink("#75596F"), new Pen(Ink(trim), 1), new Point(57 + i * 63, 124), 12, 5);
    }

    private void DrawRoulette(DrawingContext dc, int level, string trim)
    {
        var wheel = new Point(95, 94);
        dc.DrawEllipse(Ink("#6B4740"), new Pen(Ink(trim), 3), wheel, 40, 40);
        for (int i = 0; i < 16; i++)
        {
            double angle = i * Math.PI / 8;
            dc.DrawLine(new Pen(Ink(i % 2 == 0 ? "#BF7771" : "#142421"), 4),
                new Point(wheel.X + Math.Cos(angle) * 19, wheel.Y + Math.Sin(angle) * 19),
                new Point(wheel.X + Math.Cos(angle) * 36, wheel.Y + Math.Sin(angle) * 36));
        }
        dc.DrawEllipse(Ink(trim), null, wheel, 9, 9);
        dc.DrawEllipse(Ink("#F9E7B6"), null, new Point(wheel.X + Math.Cos(phase) * 29, wheel.Y + Math.Sin(phase) * 29), 3, 3);
        dc.DrawRoundedRectangle(Ink(level >= 10 ? "#3B5C47" : "#2B463C"), new Pen(Ink(trim), 2), new Rect(165, 59, 255, 69), 8, 8);
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 9; column++)
                dc.DrawRectangle(Ink((row + column) % 2 == 0 ? "#925758" : "#152A29"), new Pen(Ink("#8B926D"), .7), new Rect(177 + column * 25, 66 + row * 18, 23, 16));
    }

    private void DrawLounge(DrawingContext dc, int level, string trim)
    {
        string upholstery = level >= 10 ? "#775A7E" : "#4E415A";
        dc.DrawRoundedRectangle(Ink(upholstery), new Pen(Ink(trim), 2), new Rect(26, 58, 105, 65), 12, 12);
        dc.DrawRoundedRectangle(Ink(upholstery), new Pen(Ink(trim), 2), new Rect(343, 58, 105, 65), 12, 12);
        dc.DrawEllipse(Ink("#343E35"), new Pen(Ink(trim), 2), new Point(237, 96), 62, 31);
        for (int i = 0; i < 3; i++)
            dc.DrawEllipse(Ink(i == 1 ? "#B893C8" : "#C8AA69"), new Pen(Ink("#F0D6A7"), 1), new Point(218 + i * 20, 96), 7, 4);
        dc.DrawLine(new Pen(Ink(trim), 2), new Point(237, 50), new Point(237, 62));
        dc.DrawEllipse(Ink("#EBD094"), null, new Point(237, 58), 13, 5);
    }

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private void Text(DrawingContext dc, string text, double x, double y, double size, string color)
        => dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, Ink(color), VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
}
