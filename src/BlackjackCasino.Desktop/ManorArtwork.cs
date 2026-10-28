using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

/// <summary>Project-owned resolution-independent drawings; no external artwork or icon fonts.</summary>
internal static class ManorArtwork
{
    private static readonly Dictionary<ManorSymbol,DrawingImage> symbols=[];
    internal static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    internal static Pen Edge(string hex,double width=1) => new(Ink(hex),width);
    internal static Brush Gradient(string a,string b,bool horizontal=false) => new LinearGradientBrush(
        (Color)ColorConverter.ConvertFromString(a),(Color)ColorConverter.ConvertFromString(b),horizontal ? 0 : 90);
    internal static void Shape(DrawingContext d,string path,string fill,string? stroke=null,double width=1)
        => d.DrawGeometry(Ink(fill),stroke is null ? null : Edge(stroke,width),Geometry.Parse(path));
    internal static void Label(DrawingContext d,string text,double size,Point at,string color="#E4C98D")
    {
        var letters=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Georgia"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal),size,Ink(color),1);
        d.DrawGeometry(Ink(color),null,letters.BuildGeometry(at));
    }
    public static DrawingImage Symbol(ManorSymbol symbol)
    {
        if(symbols.TryGetValue(symbol,out DrawingImage? image)) return image;
        var drawing=new DrawingGroup();
        using(DrawingContext d=drawing.Open())
        {
            d.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,100,100));
            if(symbol<=ManorSymbol.Ace)
            {
                string letter=new[]{"J","Q","K","A"}[(int)symbol];
                Shape(d,"M50,4 L85,20 L91,50 L85,80 L50,96 L15,80 L9,50 L15,20 Z","#281926","#967C50",1.6);
                Shape(d,"M50,10 L79,25 L84,50 L79,75 L50,89 L21,75 L16,50 L21,25 Z","#3F2536","#58432F");
                Label(d,letter,57,new Point(letter=="J"?29:22,12),"#E5C593");
                Shape(d,"M17,37 Q4,20 24,18 M83,37 Q96,20 76,18 M17,63 Q4,80 24,82 M83,63 Q96,80 76,82","Transparent","#B1915A",2);
                Shape(d,"M41,10 L50,3 L59,10 L50,15 Z M41,89 L50,96 L59,89 L50,84 Z","#BA9563");
            }
            else switch(symbol)
            {
                case ManorSymbol.Candelabrum:
                    Shape(d,"M46,40 H54 V80 H69 V86 H31 V80 H46 Z M22,41 H29 V57 Q46,64 48,54 L50,64 Q21,74 22,57 Z M71,41 H78 V57 Q79,74 50,64 L52,54 Q54,64 71,57 Z","#AC8550","#E8CD8D",1.2);
                    foreach(int x in new[]{25,50,75})
                    {
                        d.DrawRectangle(Gradient("#F5DFB1","#9E8B65"),null,new Rect(x-3,x==50?20:30,6,x==50?27:17));
                        Shape(d,$"M{x},9 Q{x-10},22 {x},27 Q{x+10},22 {x},9 Z","#F9D69A");
                    }
                    break;
                case ManorSymbol.Watch:
                    d.DrawEllipse(Ink("#84643F"),Edge("#D5B876",2),new Point(50,17),9,9);
                    d.DrawEllipse(Gradient("#D1B177","#65513A"),Edge("#E5CE9C",2),new Point(50,57),33,34);
                    d.DrawEllipse(Ink("#D5CEB3"),Edge("#494239",2),new Point(50,57),27,28);
                    for(int i=0;i<12;i++) { double a=i*Math.PI/6; d.DrawLine(Edge("#51483C",1.5),new(50+21*Math.Sin(a),57-21*Math.Cos(a)),new(50+24*Math.Sin(a),57-24*Math.Cos(a))); }
                    d.DrawLine(Edge("#302933",2.5),new(50,57),new(50,36)); d.DrawLine(Edge("#302933",2),new(50,57),new(65,65));
                    break;
                case ManorSymbol.Raven:
                    Shape(d,"M12,83 Q47,76 82,82 M43,78 L47,66 M54,78 L56,64","Transparent","#9E855A",3);
                    Shape(d,"M24,67 Q20,35 49,37 Q55,18 73,27 L87,32 L73,37 Q70,48 63,52 Q69,70 54,75 L39,69 L21,81 L29,67 Z","#20232D","#8C8B9A",1.3);
                    Shape(d,"M33,47 Q47,39 58,55 L51,67 L34,64 Z","#353744","#615F75");
                    d.DrawEllipse(Ink("#C5AD89"),null,new Point(68,31),2,2);
                    break;
                case ManorSymbol.Key:
                    d.DrawEllipse(Gradient("#D8C8A2","#75614C"),Edge("#D2B77C",2),new Point(37,30),19,19);
                    d.DrawEllipse(Ink("#332C35"),Edge("#9D855F",2),new Point(37,30),10,10);
                    Shape(d,"M47,42 L57,37 L84,77 L77,83 L68,71 L62,76 L57,69 L64,64 Z","#B29E77","#E5D4AA",1.2);
                    break;
                case ManorSymbol.Portrait:
                    Shape(d,"M15,11 H85 V90 H15 Z","#6D5239","#D6BA82",3);
                    Shape(d,"M22,18 H78 V83 H22 Z","#23252F","#A58D69",2);
                    DrawLady(d,0.78,11,13);
                    foreach(int x in new[]{15,85}) foreach(int y in new[]{11,90}) d.DrawEllipse(Ink("#A58D69"),null,new Point(x,y),6,5);
                    break;
                case ManorSymbol.Rose:
                    Shape(d,"M51,52 Q39,73 52,93 M47,77 Q17,79 23,64 Q38,61 47,77 M48,84 Q73,88 79,73 Q63,68 48,84","#3E4A42","#71806A",1.4);
                    for(int i=0;i<7;i++)
                    {
                        double a=i*Math.PI*2/7;
                        d.DrawEllipse(Gradient(i%2==0?"#50404F":"#352B3B","#171820"),Edge("#827082",1),new Point(50+13*Math.Sin(a),36+13*Math.Cos(a)),17,13);
                    }
                    d.DrawEllipse(Ink("#5C3D51"),Edge("#AD8294"),new Point(50,36),12,10);
                    break;
                case ManorSymbol.Lady: DrawLady(d,1,0,0); break;
                case ManorSymbol.Wild:
                    d.DrawEllipse(Gradient("#AAA1A9","#61505E"),Edge("#D2C8B8",2),new Point(50,49),33,42);
                    d.DrawEllipse(Gradient("#71899B","#242538"),Edge("#B5ACAA",2),new Point(50,49),26,35);
                    DrawLady(d,0.60,20,18);
                    Shape(d,"M30,38 L65,21 M36,58 L72,29","Transparent","#6ABCD2D8",2);
                    Shape(d,"M32,8 L50,1 L68,8 M32,90 L50,98 L68,90","Transparent","#B5ACAA",3);
                    break;
                case ManorSymbol.Scatter:
                    d.DrawEllipse(Gradient("#F0ECD9","#8D9EB4"),Edge("#D2DBDF"),new Point(51,44),35,35);
                    Shape(d,"M16,85 V54 L33,36 L49,54 V48 L66,28 L85,49 V85 Z","#252633","#777381",1.2);
                    Shape(d,"M22,58 H28 V66 H22 Z M57,53 H63 V63 H57 Z M70,53 H76 V63 H70 Z","#D8B978");
                    break;
                case ManorSymbol.Bonus:
                    Shape(d,"M12,95 V31 L50,3 L88,31 V95 Z","#5E4D3B","#C7AF7D",2);
                    Shape(d,"M23,91 V36 Q50,8 77,36 V91 Z","#241B25","#B19A6B",2);
                    Shape(d,"M29,88 V39 Q50,18 71,39 V88 Z","#37222B","#654B3E",2);
                    Shape(d,"M50,29 V88 M34,47 H44 V72 H34 Z M56,47 H66 V72 H56 Z","Transparent","#8F7151",2);
                    d.DrawEllipse(Ink("#D8BB7D"),Edge("#8C704D"),new Point(57,69),3.5,4);
                    break;
            }
        }
        image=new DrawingImage(drawing); image.Freeze(); symbols[symbol]=image; return image;
    }
    private static void DrawLady(DrawingContext d,double scale,double x,double y)
    {
        d.PushTransform(new TranslateTransform(x,y)); d.PushTransform(new ScaleTransform(scale,scale));
        Shape(d,"M22,92 Q29,63 37,51 Q24,25 43,12 Q67,2 74,31 L68,52 Q79,70 83,92 Z","#B1BEC4","#E6E1CE",1.5);
        Shape(d,"M32,40 Q26,14 47,11 Q71,4 75,33 L64,26 L62,53 L57,38 L42,31 L39,57 Z","#6C7485");
        d.DrawEllipse(Ink("#DFDDD0"),null,new Point(51,36),10,15);
        Shape(d,"M43,39 L48,38 M55,38 L60,39 M50,47 L55,47 M37,61 Q50,72 67,60 L75,88 H28 Z","Transparent","#6F7B8D",1.2);
        d.Pop(); d.Pop();
    }
}
