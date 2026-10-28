using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

internal sealed class ManorReelView : FrameworkElement
{
    public static readonly DependencyProperty PositionProperty=DependencyProperty.Register(nameof(Position),typeof(double),typeof(ManorReelView),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    public double Position { get=>(double)GetValue(PositionProperty); set=>SetValue(PositionProperty,value); }
    public static readonly DependencyProperty EmphasisProperty=DependencyProperty.Register(nameof(Emphasis),typeof(double),typeof(ManorReelView),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    public double Emphasis { get=>(double)GetValue(EmphasisProperty); set=>SetValue(EmphasisProperty,value); }
    public int Reel { get; init; }
    public HashSet<int> WinningRows { get; }=[];
    public bool DimOthers { get; set; }
    protected override void OnRender(DrawingContext d)
    {
        double cell=ActualHeight/3;
        d.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight),5,5));
        d.DrawRectangle(ManorArtwork.Gradient("#B8B1A6","#D8CFBC"),null,new Rect(0,0,ActualWidth,ActualHeight));
        int first=(int)Math.Floor(Position);
        double fraction=Position-first;
        for(int row=0;row<4;row++)
        {
            double y=(row-fraction)*cell;
            int stop=((first+row)%64+64)%64;
            bool winner=WinningRows.Contains(row);
            d.PushOpacity(DimOthers && !winner ? 0.33 : 1);
            double size=Math.Min(cell-9,ActualWidth-8);
            ManorSymbol symbol=ManorRules.SymbolAt(Reel,stop,0);
            d.DrawImage(ManorArtwork.Symbol(symbol),new Rect((ActualWidth-size)/2,y+(cell-size)/2,size,size));
            if(Emphasis>0 && symbol is ManorSymbol.Wild or ManorSymbol.Scatter or ManorSymbol.Bonus)
            {
                d.PushOpacity(Emphasis*0.6);
                d.DrawRoundedRectangle(ManorArtwork.Ink("#50C7C4E0"),ManorArtwork.Edge("#E8D8AF",2),new Rect(5,y+5,ActualWidth-10,cell-10),9,9);
                if(symbol==ManorSymbol.Wild) d.DrawImage(ManorArtwork.Symbol(ManorSymbol.Lady),new Rect((ActualWidth-size*0.6)/2,y+cell*0.14,size*0.6,size*0.7));
                d.Pop();
            }
            if(winner) d.DrawRoundedRectangle(ManorArtwork.Ink("#22F2D49D"),ManorArtwork.Edge("#E0C394",2),new Rect(3,y+3,ActualWidth-6,cell-6),6,6);
            d.Pop();
        }
        d.DrawRectangle(new LinearGradientBrush(new GradientStopCollection {
            new(Colors.Black,0),new(Color.FromArgb(0,0,0,0),0.2),new(Color.FromArgb(0,0,0,0),0.8),new(Color.FromArgb(110,0,0,0),1)},new Point(0,0),new Point(0,1)),null,new Rect(0,0,ActualWidth,ActualHeight));
        d.Pop();
    }
}

internal sealed class ManorBackdrop : FrameworkElement
{
    public static readonly DependencyProperty PhaseProperty=DependencyProperty.Register(nameof(Phase),typeof(double),typeof(ManorBackdrop),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SceneProperty=DependencyProperty.Register(nameof(Scene),typeof(string),typeof(ManorBackdrop),new FrameworkPropertyMetadata("Exterior",FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ThreatProperty=DependencyProperty.Register(nameof(Threat),typeof(int),typeof(ManorBackdrop),new FrameworkPropertyMetadata(0,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty DoorOpenProperty=DependencyProperty.Register(nameof(DoorOpen),typeof(double),typeof(ManorBackdrop),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    public double Phase { get=>(double)GetValue(PhaseProperty); set=>SetValue(PhaseProperty,value); }
    public string Scene { get=>(string)GetValue(SceneProperty); set=>SetValue(SceneProperty,value); }
    public int Threat { get=>(int)GetValue(ThreatProperty); set=>SetValue(ThreatProperty,value); }
    public double DoorOpen { get=>(double)GetValue(DoorOpenProperty); set=>SetValue(DoorOpenProperty,value); }
    protected override void OnRender(DrawingContext d)
    {
        if(ActualWidth<=0 || ActualHeight<=0) return;
        d.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)));
        d.PushTransform(new ScaleTransform(ActualWidth/1000,ActualHeight/600));
        d.DrawRectangle(ManorArtwork.Gradient("#101421","#28202A"),null,new Rect(0,0,1000,600));
        if(Scene=="Exterior") Exterior(d);
        else if(Scene=="Door") Door(d);
        else Room(d);
        // Slow, low-opacity fog and fixed dust particles; never consume gameplay RNG.
        for(int i=0;i<4;i++)
            d.DrawEllipse(ManorArtwork.Ink("#06304059"),null,new Point((i*300+Phase*9)%1300-150,450+i*28),300,38);
        for(int i=0;i<30;i++)
            d.DrawEllipse(ManorArtwork.Ink("#28E2CFAB"),null,new Point((i*137+Math.Sin(Phase*0.2+i)*9)%1000,(i*71+Phase*1.2)%600),0.8,0.8);
        if(Threat>=6) d.DrawRectangle(ManorArtwork.Ink("#263D0925"),null,new Rect(0,0,1000,600));
        d.Pop(); d.Pop();
    }
    private void Exterior(DrawingContext d)
    {
        d.DrawEllipse(ManorArtwork.Ink("#16D3DDE8"),null,new Point(735,110),100,100);
        d.DrawEllipse(ManorArtwork.Gradient("#DFDCCF","#8996AC"),null,new Point(735,110),60,60);
        ManorArtwork.Shape(d,"M0,390 Q170,310 300,385 Q540,290 700,355 Q850,290 1000,360 V600 H0 Z","#121722");
        ManorArtwork.Shape(d,"M230,490 V230 L310,155 L390,230 V170 L500,75 L610,170 V230 L690,155 L770,230 V490 Z","#24232D","#6C6267",2);
        ManorArtwork.Shape(d,"M210,235 L310,135 L410,235 M370,175 L500,48 L630,175 M590,235 L690,135 L790,235","Transparent","#625966",9);
        for(int col=0;col<7;col++) for(int row=0;row<3;row++)
        {
            double x=276+col*66,y=245+row*73;
            d.DrawRoundedRectangle(ManorArtwork.Gradient(col%3==0?"#C9A06B":"#5B5967","#302B36"),ManorArtwork.Edge("#7C6B5B",3),new Rect(x,y,27,43),13,13);
            d.DrawLine(ManorArtwork.Edge("#3C3439",2),new Point(x+13,y),new Point(x+13,y+43));
        }
        ManorArtwork.Shape(d,"M448,490 V390 Q500,337 552,390 V490 Z","#171420","#A18A60",4);
        ManorArtwork.Shape(d,"M440,491 L340,600 H660 L560,491 Z","#3C3440","#6A5860",1.5);
        for(int i=0;i<6;i++) d.DrawLine(ManorArtwork.Edge("#60515A"),new(440-i*15,500+i*16),new(560+i*15,500+i*16));
        foreach(int x in new[]{75,130,865,930})
        {
            ManorArtwork.Shape(d,$"M{x},600 Q{x+16},300 {x-7},90 M{x+4},300 L{x-65},215 M{x+2},390 L{x+64},280","Transparent","#10121A",14);
        }
    }
    private void Room(DrawingContext d)
    {
        bool crypt=Scene=="Crypt",library=Scene=="Library",ball=Scene=="Ballroom",attic=Scene=="Attic";
        string wall=crypt?"#292B38":ball?"#3E2334":library?"#332A27":"#302331";
        d.DrawRectangle(ManorArtwork.Gradient(wall,"#171522"),null,new Rect(0,0,1000,600));
        ManorArtwork.Shape(d,"M0,0 L280,135 V420 L0,600 Z","#30222A","#7B6451",2);
        ManorArtwork.Shape(d,"M1000,0 L720,135 V420 L1000,600 Z","#241F2B","#7B6451",2);
        ManorArtwork.Shape(d,"M0,600 L280,420 H720 L1000,600 Z",crypt?"#3C3C46":"#49352F","#7B6451",2);
        for(int i=0;i<9;i++) d.DrawLine(ManorArtwork.Edge("#705345",1.2),new Point(280+i*55,420),new Point(-80+i*145,600));
        for(int i=0;i<5;i++) d.DrawLine(ManorArtwork.Edge("#77604E",1),new Point(0,455+i*33),new Point(1000,455+i*33));
        // Recessed arched corridor, moonlit stained glass and carved panelling.
        ManorArtwork.Shape(d,"M401,421 V234 Q500,108 599,234 V421 Z","#10151F","#9F8C70",7);
        ManorArtwork.Shape(d,"M419,420 V243 Q500,140 581,243 V420 Z","#293243","#4D4C61",2);
        d.DrawEllipse(ManorArtwork.Ink("#96CBD0D1"),null,new Point(500,225),29,35);
        d.DrawLine(ManorArtwork.Edge("#4D4B56",4),new(500,190),new(500,260));
        d.DrawLine(ManorArtwork.Edge("#4D4B56",4),new(470,225),new(530,225));
        for(int i=0;i<6;i++)
        {
            double x=25+i*53;
            ManorArtwork.Shape(d,$"M{x},90 V500 M{1000-x},90 V500","Transparent","#806448",3);
        }
        if(library)
        {
            foreach(int x in new[]{65,735})
            {
                d.DrawRectangle(ManorArtwork.Ink("#211B20"),ManorArtwork.Edge("#82623F",6),new Rect(x,137,190,280));
                for(int shelf=0;shelf<4;shelf++) for(int book=0;book<10;book++)
                    d.DrawRectangle(ManorArtwork.Ink(new[]{"#5C3946","#51545A","#584E36"}[(book+shelf)%3]),ManorArtwork.Edge("#897557",0.6),new Rect(x+8+book*17,151+shelf*63,12,44+(book%3)*4));
            }
        }
        if(ball)
        {
            foreach(int x in new[]{45,755})
            {
                ManorArtwork.Shape(d,$"M{x},55 H{x+180} V381 Q{x+100},310 {x+91},113 Q{x+60},309 {x},381 Z","#582B41","#B09366",3);
            }
            d.DrawLine(ManorArtwork.Edge("#B09A6B",3),new(500,0),new(500,70));
            d.DrawEllipse(Brushes.Transparent,ManorArtwork.Edge("#AD9361",5),new(500,87),85,25);
            foreach(int x in new[]{425,460,500,540,575}) Candle(d,x,68);
        }
        if(attic)
        {
            for(int i=0;i<5;i++) ManorArtwork.Shape(d,$"M{i*220},0 L500,118 L{1000-i*220},0","Transparent","#786047",20);
            d.DrawRectangle(ManorArtwork.Ink("#40312D"),ManorArtwork.Edge("#987D56",3),new Rect(80,350,145,95));
            d.DrawRectangle(ManorArtwork.Ink("#362B2B"),ManorArtwork.Edge("#987D56",3),new Rect(780,330,110,110));
        }
        if(crypt)
        {
            foreach(int x in new[]{150,300,700,850})
            {
                d.DrawRectangle(ManorArtwork.Gradient("#66616B","#2C2D3A",true),ManorArtwork.Edge("#777180",2),new Rect(x-20,80,40,370));
                d.DrawRectangle(ManorArtwork.Ink("#5D5965"),null,new Rect(x-30,75,60,14));
            }
            ManorArtwork.Shape(d,"M375,488 L431,444 H565 L626,488 L610,533 H390 Z","#4D4A56","#A49B98",2);
            ManorArtwork.Shape(d,"M484,461 H516 M500,450 V478","Transparent","#A49B98",3);
        }
        if(Scene=="Entrance" || Scene=="Corridor")
        {
            d.DrawImage(ManorArtwork.Symbol(ManorSymbol.Portrait),new Rect(87,145,115,145));
            d.DrawImage(ManorArtwork.Symbol(ManorSymbol.Portrait),new Rect(798,145,115,145));
            ManorArtwork.Shape(d,"M410,600 L458,420 H542 L590,600 Z","#572B3D","#A48A60",2);
        }
        foreach(int x in new[]{305,695}) Candle(d,x,285);
        if(Threat>=4)
        {
            d.PushOpacity((Threat-3)*0.08+(Math.Sin(Phase)*0.015));
            d.DrawImage(ManorArtwork.Symbol(ManorSymbol.Lady),new Rect(455,275,90,145)); d.Pop();
        }
    }
    private void Candle(DrawingContext d,double x,double y)
    {
        d.DrawEllipse(ManorArtwork.Ink("#12F3C18D"),null,new Point(x,y-15),47,58);
        d.DrawRectangle(ManorArtwork.Gradient("#EDCF9E","#88775F"),null,new Rect(x-4,y,8,32));
        double pulse=1+Math.Sin(Phase*2.2+x)*0.08;
        ManorArtwork.Shape(d,FormattableString.Invariant($"M{x},{y-24*pulse} Q{x-12},{y-6} {x},{y} Q{x+12},{y-6} {x},{y-24*pulse} Z"),"#F1D09A");
        d.DrawLine(ManorArtwork.Edge("#AF8D58",4),new Point(x-10,y+34),new Point(x+10,y+34));
    }
    private void Door(DrawingContext d)
    {
        Room(d);
        double open=Math.Clamp(DoorOpen,0,1);
        d.DrawRectangle(ManorArtwork.Ink("#050811"),null,new Rect(220,20,560,580));
        d.PushTransform(new TranslateTransform(-open*270,0));
        d.DrawImage(ManorArtwork.Symbol(ManorSymbol.Bonus),new Rect(225,-25,550,650)); d.Pop();
        d.PushOpacity(open);
        d.DrawImage(ManorArtwork.Symbol(ManorSymbol.Lady),new Rect(450,220,100,180)); d.Pop();
    }
}
