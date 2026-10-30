using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoLobbyView : UserControl
{
    private readonly CasinoModeRegistry modes;
    private readonly WrapPanel cards = new() {HorizontalAlignment=HorizontalAlignment.Center};
    internal CasinoLobbyView(CasinoModeRegistry modes,Action<string> progression)
    {
        this.modes=modes;
        Background=new LinearGradientBrush(Color.FromRgb(10,17,22),Color.FromRgb(23,32,28),80);
        var root=new DockPanel {Margin=new Thickness(40,24,40,18)};
        var footer=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,20,0,0)};
        foreach(string section in new[]{"Season Pass","Cosmetic Shop","Challenges","Overview"})
        {
            string selected=section;
            var button=new Button {Content=section,Margin=new Thickness(6),Padding=new Thickness(22,10,22,10)};
            button.Click+=(_,_)=>progression(selected); footer.Children.Add(button);
        }
        DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var heading=new StackPanel {Margin=new Thickness(12,10,12,28)};
        heading.Children.Add(new TextBlock {Text="THE MIDNIGHT CLUB",Foreground=Ink("#D6B478"),FontSize=12,FontWeight=FontWeights.SemiBold});
        heading.Children.Add(new TextBlock {Text="A different kind of night.",FontFamily=new FontFamily("Georgia"),FontSize=42,Foreground=Ink("#F7EBD5"),Margin=new Thickness(0,10,0,9)});
        heading.Children.Add(new TextBlock {Text="Choose your table. Find your rhythm. Make yourself at home.",Foreground=Ink("#B8C7BE"),FontSize=15});
        heading.Children.Add(new TextBlock {Text="ONE PROFILE  /  SHARED PLAY-MONEY BANKROLL  /  OFFLINE CASINO",Foreground=Ink("#718F83"),FontSize=10,Margin=new Thickness(0,18,0,0)});
        DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        root.Children.Add(new ScrollViewer {Content=cards,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        Content=root; modes.Changed+=Refresh; SizeChanged+=(_,_)=>ResizeCards();
        Unloaded+=(_,_)=>modes.Changed-=Refresh;
        Loaded+=(_,_)=> {modes.Changed-=Refresh;modes.Changed+=Refresh;Refresh();};
        Refresh();
    }
    private void Refresh()
    {
        cards.Children.Clear();
        foreach(CasinoMode mode in modes.Modes)
        {
            Brush accent=Ink(mode.Accent);
            var panel=new StackPanel();
            var art=new Grid {Height=185,ClipToBounds=true,Background=new LinearGradientBrush(((SolidColorBrush)accent).Color,Color.FromRgb(9,17,22),65)};
            if(mode.Artwork is not null) art.Children.Add(mode.Artwork());
            art.Children.Add(new Ellipse {Width=210,Height=210,Stroke=Ink("#45EDD5A4"),StrokeThickness=1,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,-65,-45,0)});
            art.Children.Add(new Ellipse {Width=175,Height=175,Stroke=Ink("#30EDD5A4"),StrokeThickness=1,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,-65,-45,0)});
            if(mode.Artwork is null) art.Children.Add(new TextBlock {Text=mode.Emblem,FontSize=75,FontFamily=new FontFamily("Georgia"),Foreground=Ink("#F4DFC0"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});
            art.Children.Add(new TextBlock {Text=mode.Category.ToUpperInvariant(),FontSize=10,Foreground=Ink("#F4DFC0"),Margin=new Thickness(18),VerticalAlignment=VerticalAlignment.Bottom});
            panel.Children.Add(art);
            var text=new StackPanel {Margin=new Thickness(22,20,22,20)};
            text.Children.Add(new TextBlock {Text=mode.Subtitle.ToUpperInvariant(),FontSize=10,Foreground=accent});
            text.Children.Add(new TextBlock {Text=mode.Title,FontFamily=new FontFamily("Georgia"),FontSize=27,Foreground=Ink("#F3E6D1"),Margin=new Thickness(0,8,0,12)});
            text.Children.Add(new TextBlock {Text=mode.Description,FontSize=14,Foreground=Ink("#ADBCB6"),TextWrapping=TextWrapping.Wrap,MinHeight=57});
            text.Children.Add(new TextBlock {Text=modes.CanOpen(mode.Id)?"TAKE A SEAT   →":"OPEN IN SEPARATE WINDOW",FontSize=12,FontWeight=FontWeights.Bold,Foreground=Ink("#E7CCA0"),Margin=new Thickness(0,20,0,0)});
            panel.Children.Add(text);

            var primary=new Button
            {
                Name="Mode_"+mode.Id.Replace('-','_'),Tag=mode.Id,Padding=new Thickness(0),Content=panel,
                Background=Ink("#111F20"),BorderBrush=accent,HorizontalContentAlignment=HorizontalAlignment.Stretch,
                IsEnabled=modes.CanOpen(mode.Id),HorizontalAlignment=HorizontalAlignment.Stretch,
            };
            primary.Click+=(_,_)=>modes.Open(mode.Id);

            var wrapper=new StackPanel {Margin=new Thickness(10)};
            wrapper.Children.Add(primary);
            if(modes.HasSecondary(mode.Id))
            {
                var secondary=new Button
                {
                    Name="ModeSecondary_"+mode.Id.Replace('-','_'),Tag=mode.Id+"-secondary",
                    Content=modes.SecondaryLabel(mode.Id),Margin=new Thickness(0,7,0,0),Padding=new Thickness(12,8,12,8),
                    IsEnabled=modes.CanOpenSecondary(mode.Id),HorizontalAlignment=HorizontalAlignment.Stretch,
                };
                secondary.Click+=(_,_)=>modes.OpenSecondary(mode.Id);
                wrapper.Children.Add(secondary);
            }
            cards.Children.Add(wrapper);
        }
        ResizeCards();
    }
    private void ResizeCards()
    {
        double available=Math.Max(300,ActualWidth-110);
        int columns=Math.Clamp((int)(available/300),1,Math.Max(1,cards.Children.Count));
        double width=Math.Clamp(available/columns-24,280,430);
        foreach(FrameworkElement card in cards.Children) card.Width=width;
    }
    private static SolidColorBrush Ink(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
}
