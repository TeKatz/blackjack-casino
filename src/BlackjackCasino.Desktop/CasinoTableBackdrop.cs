using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoTableBackdrop : FrameworkElement
{
    internal static readonly DependencyProperty FeltProperty=DependencyProperty.Register(nameof(Felt),typeof(Brush),typeof(CasinoTableBackdrop),new FrameworkPropertyMetadata(Brushes.DarkGreen,FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Felt {get=>(Brush)GetValue(FeltProperty);set=>SetValue(FeltProperty,value);}
    internal static readonly DependencyProperty PhaseProperty=DependencyProperty.Register(nameof(Phase),typeof(double),typeof(CasinoTableBackdrop),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    public double Phase {get=>(double)GetValue(PhaseProperty);set=>SetValue(PhaseProperty,value);}
    protected override void OnRender(DrawingContext d)
    {
        if(ActualWidth<1 || ActualHeight<1) return;
        double w=ActualWidth,h=ActualHeight;
        d.DrawRectangle(Ink("#080F10"),null,new Rect(0,0,w,h));
        d.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(63,42,32),Color.FromRgb(23,20,20),90),new Pen(Ink("#96724B"),1),new Rect(12,12,w-24,h-24),52,52);
        for(int i=0;i<5;i++) d.DrawRoundedRectangle(null,new Pen(Ink(i%2==0?"#257D5B3C":"#28000000"),1),new Rect(17+i*2,17+i*2,w-34-i*4,h-34-i*4),47-i,47-i);
        d.DrawRoundedRectangle(Ink("#101E1A"),new Pen(Ink("#B19862"),1.4),new Rect(30,30,w-60,h-60),36,36);
        d.DrawRoundedRectangle(Felt,new Pen(Ink("#364E40"),1),new Rect(39,39,w-78,h-78),28,28);
        var glow=new RadialGradientBrush(Color.FromArgb(44,180,211,183),Colors.Transparent) {Center=new Point(.5,.37),GradientOrigin=new Point(.5,.37),RadiusX=.65,RadiusY=.65};
        d.DrawRoundedRectangle(glow,null,new Rect(40,40,w-80,h-80),28,28);
        // Fine felt grain, stitched betting arc and restrained foil inlay.
        d.PushClip(new RectangleGeometry(new Rect(42,42,w-84,h-84),26,26));
        for(int y=44;y<h-40;y+=9) d.DrawLine(new Pen(Ink("#06F4EFDB"),.4),new Point(42,y),new Point(w-42,y));
        var arc=new StreamGeometry();using(var g=arc.Open()) {g.BeginFigure(new Point(w*.14,h*.66),false,false);g.BezierTo(new(w*.32,h*.39),new(w*.68,h*.39),new(w*.86,h*.66),true,false);}
        d.DrawGeometry(null,new Pen(Ink("#45D7BB7C"),1.2) {DashStyle=new DashStyle(new[]{7d,5d},0)},arc);
        foreach(double x in new[]{.23,.5,.77}) d.DrawEllipse(null,new Pen(Ink("#38D9BD85"),1),new Point(w*x,h*.68),39,23);
        Text(d,"THE MIDNIGHT CLUB",13,new Point(w*.5,h-29),"#8CBCAD7E");
        // Dealer tray: independent chips, discard stack and a brass shoe.
        double trayX=65,trayY=66;
        d.DrawRoundedRectangle(Ink("#18231F"),new Pen(Ink("#6E674E"),1),new Rect(trayX-9,trayY-14,151,55),8,8);
        string[] chipColors={"#9B5C57","#D2BD88","#3E7170","#5B567B"};
        for(int col=0;col<4;col++) for(int row=0;row<5;row++)
        {var p=new Point(trayX+col*33,trayY+23-row*5);d.DrawEllipse(Ink(chipColors[col]),new Pen(Ink("#BADED4B4"),1),p,13,5);d.DrawLine(new Pen(Ink("#80342B28"),2),new Point(p.X-5,p.Y),new Point(p.X+5,p.Y));}
        double shoe=w-170;
        for(int i=0;i<4;i++) d.DrawRoundedRectangle(Ink("#D5CDB7"),new Pen(Ink("#786B58"),.7),new Rect(shoe+i*2,68-i*2,46,65),4,4);
        d.DrawRoundedRectangle(Ink("#283D38"),new Pen(Ink("#B39B6C"),1),new Rect(shoe+7,63,40,58),3,3);
        Text(d,"♠",23,new Point(shoe+27,77),"#CFB67D");
        for(int i=0;i<18;i++)
        {
            double x=80+(i*137+Math.Sin(Phase*.35+i)*12)%(Math.Max(1,w-160));
            double y=70+(i*97+Phase*3)%(Math.Max(1,h-140));
            d.DrawEllipse(Ink("#18E8D7AE"),null,new Point(x,y),1,1);
        }
        d.Pop();
    }
    private static Brush Ink(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    private static void Text(DrawingContext d,string text,double size,Point centre,string color)
    {
        var label=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Georgia"),size,Ink(color),1);
        d.DrawText(label,new Point(centre.X-label.Width/2,centre.Y));
    }
}
