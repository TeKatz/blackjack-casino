using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Small vector compositions that remain readable in the compact lobby tiles.</summary>
internal sealed class LoungeGameArtwork(bool dice) : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double scale = Math.Min(ActualWidth / 340, ActualHeight / 110);
        dc.PushTransform(new TranslateTransform((ActualWidth - 340 * scale) / 2, (ActualHeight - 110 * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));
        if (dice)
        {
            Die(105, 18, -12, 5); Die(172, 26, 13, 6);
        }
        else
        {
            dc.DrawRoundedRectangle(Brush("#ECE3CC"), new Pen(Brush("#C9B88C"), 2), new Rect(117, 8, 106, 94), 6, 6);
            for (int i = 1; i < 9; i++)
            {
                var pen = new Pen(Brush(i % 3 == 0 ? "#566E63" : "#B4B9A3"), i % 3 == 0 ? 1.6 : .5);
                dc.DrawLine(pen, new Point(125+i*10,15), new Point(125+i*10,95));
                dc.DrawLine(pen, new Point(125,15+i*10), new Point(215,15+i*10));
            }
            var glyph = new FormattedText("7  2\n  5\n9  1", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Georgia"), 17, Brush("#294A40"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(glyph, new Point(135,19));
        }
        dc.Pop(); dc.Pop();
        void Die(double x, double y, double angle, int value)
        {
            dc.PushTransform(new RotateTransform(angle, x+28, y+28));
            dc.DrawRoundedRectangle(Brush("#EBE2C9"), new Pen(Brush("#D1AC67"),2), new Rect(x,y,56,56),10,10);
            if (value % 2 == 1) Pip(.5,.5);
            Pip(.25,.25); Pip(.75,.75); Pip(.25,.75); Pip(.75,.25);
            if(value==6) {Pip(.25,.5);Pip(.75,.5);}
            dc.Pop();
            void Pip(double a,double b) => dc.DrawEllipse(Brush("#264B40"),null,new Point(x+56*a,y+56*b),4,4);
        }
    }
    private static Brush Brush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
