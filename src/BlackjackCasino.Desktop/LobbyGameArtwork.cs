using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class LobbyGameArtwork(bool reels) : FrameworkElement
{
    protected override void OnRender(DrawingContext d)
    {
        if(ActualWidth<=0 || ActualHeight<=0)return;
        double scale=Math.Min(ActualWidth/360,ActualHeight/185);
        d.PushTransform(new TranslateTransform((ActualWidth-360*scale)/2,(ActualHeight-185*scale)/2));
        d.PushTransform(new ScaleTransform(scale,scale));
        if(reels)
        {
            d.DrawRoundedRectangle(Ink("#2B1F31"),new Pen(Ink("#E8C37D"),2),new Rect(63,22,234,145),18,18);
            d.DrawRoundedRectangle(Ink("#11101C"),new Pen(Ink("#9F7B48"),1),new Rect(77,42,206,105),8,8);
            for(int i=0;i<3;i++)
            {
                d.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(232,215,177),Color.FromRgb(151,140,122),90),null,new Rect(85+i*65,48,59,92),5,5);
                Label(d,"7",48,new Point(114+i*65,65),"#B53746");
            }
            d.DrawLine(new Pen(Ink("#ECCD82"),1),new Point(78,93),new Point(280,93));
            d.DrawEllipse(Ink("#C8A45F"),null,new Point(309,64),8,8);d.DrawLine(new Pen(Ink("#B9B0A2"),4),new Point(309,71),new Point(301,123));
        }
        else
        {
            string[] ranks={"A","K","Q"};string[] suits={"♠","♥","♣"};
            for(int i=0;i<3;i++)
            {
                d.PushTransform(new RotateTransform((i-1)*14,180,150));
                d.DrawRoundedRectangle(Ink("#F1E9D6"),new Pen(Ink("#BEB299"),1),new Rect(105+i*24,23,90,123),7,7);
                Label(d,ranks[i],23,new Point(120+i*24,30),i==1?"#973B44":"#25352D");
                Label(d,suits[i],36,new Point(151+i*24,65),i==1?"#973B44":"#25352D");d.Pop();
            }
            for(int i=0;i<4;i++)d.DrawEllipse(Ink(i%2==0?"#BB8D54":"#233F37"),new Pen(Ink("#E4CE9A"),1),new Point(71,145-i*5),23,8);
            for(int i=0;i<6;i++)d.DrawEllipse(Ink("#6D4053"),new Pen(Ink("#D7B886"),1),new Point(287,151-i*5),23,8);
        }
        d.Pop();d.Pop();
    }
    private static Brush Ink(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    private static void Label(DrawingContext d,string text,double size,Point centre,string color)
    {var label=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Georgia"),size,Ink(color),1);d.DrawText(label,new Point(centre.X-label.Width/2,centre.Y));}
}
