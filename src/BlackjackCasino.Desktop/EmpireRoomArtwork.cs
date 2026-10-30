using System.Windows.Automation;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal static class EmpireRoomTextures
{
    private static readonly BitmapSource[] images = Load();
    internal static BitmapSource Get(int index) => images[Math.Abs(index) % images.Length];
    private static BitmapSource[] Load()
    {
        var atlas = new BitmapImage();
        atlas.BeginInit();
        atlas.UriSource = new Uri("pack://application:,,,/BlackjackCasino.Desktop;component/Assets/Empire/rooms-atlas.png", UriKind.Absolute);
        atlas.CacheOption = BitmapCacheOption.OnLoad;
        atlas.EndInit();
        atlas.Freeze();
        return Enumerable.Range(0, 6).Select(i =>
        {
            var crop = new CroppedBitmap(atlas, new Int32Rect(i % 3 * atlas.PixelWidth / 3, i / 3 * atlas.PixelHeight / 2, atlas.PixelWidth / 3, atlas.PixelHeight / 2));
            crop.Freeze();
            return (BitmapSource)crop;
        }).ToArray();
    }
}

/// <summary>Cached room textures with independent light, coin and purchase effects.</summary>
internal sealed class EmpireRoomArtwork(int textureIndex) : FrameworkElement
{
    private double phase;
    internal double Phase { get => phase; set { phase = value; InvalidateVisual(); } }
    internal bool Open { get; set; }
    internal int Level { get; set; }
    internal bool Selected { get; set; }
    internal int TextureIndex => textureIndex;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        Rect area = new(0, 0, ActualWidth, ActualHeight);
        dc.PushClip(new RectangleGeometry(area, 7, 7));
        var photo = new ImageBrush(EmpireRoomTextures.Get(textureIndex)) { Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Center };
        dc.DrawRectangle(photo, null, area);
        dc.DrawRectangle(new LinearGradientBrush(Color.FromArgb(0, 4, 13, 13), Color.FromArgb(210, 4, 13, 13), 90), null, area);
        if (!Open)
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(145, 5, 14, 17)), null, area);
            Text(dc, "ROOM AVAILABLE", 12, ActualHeight - 29, 10, "#CAB992");
        }
        else
        {
            // Ambient motion belongs to the renderer only; it cannot alter income or clocks.
            for (int i = 0; i < 3; i++)
            {
                double t = (phase * .15 + i / 3d) % 1;
                double x = ActualWidth * (.25 + i * .23) + Math.Sin(phase + i) * 5;
                double y = ActualHeight * (.88 - t * .6);
                dc.PushOpacity(Math.Sin(t * Math.PI) * .65);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 200, 115)), null, new Point(x, y), 2, 3);
                dc.Pop();
            }
            var glow = new RadialGradientBrush(Color.FromArgb((byte)(23 + 14 * Math.Sin(phase + textureIndex)), 255, 199, 78), Colors.Transparent);
            dc.DrawEllipse(glow, null, new Point(ActualWidth * .65, ActualHeight * .33), ActualWidth * .6, ActualHeight * .7);
            if (Level >= 10)
            {
                double x = ((phase * .09) % 1) * ActualWidth;
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(95, 238, 207, 139)), 1), new Point(x, 2), new Point(Math.Min(ActualWidth, x + 25), 2));
            }
            Text(dc, "●  OPERATING", 12, ActualHeight - 29, 10, "#BEE3C4");
        }
        if (Selected) dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(130, 241, 211, 149)), 2), new Rect(1, 1, ActualWidth - 2, ActualHeight - 2), 7, 7);
        dc.Pop();
    }

    private void Text(DrawingContext dc, string text, double x, double y, double size, string color)
        => dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, (Brush)new BrushConverter().ConvertFromString(color)!, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
}

internal sealed class EmpireRoomCard : Button
{
    internal EmpireDepartment Department { get; }
    internal EmpireRoomArtwork Artwork { get; }
    private readonly TextBlock level = Text("", 11, "#BFAE8A");
    private readonly TextBlock income = Text("", 13, "#B3D4BE");
    private readonly TextBlock next = Text("", 10, "#94AAA0");
    private readonly Border milestoneFill = new() { Background = Ink("#B59A62"), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(2) };
    private readonly Border frame;
    private readonly Grid milestoneTrack;
    private readonly Border flash = new() { Background = Ink("#FFE0A0"), Opacity = 0, IsHitTestVisible = false, CornerRadius = new CornerRadius(9) };
    private int previousLevel = -1;
    private double progress;

    internal EmpireRoomCard(EmpireDepartment room, int index, Action<string> select)
    {
        Department = room;
        Name = "EmpireRoom_" + room.Id.Replace('-', '_');
        AutomationProperties.SetName(this, "Select " + room.Name);
        Margin = new Thickness(0, 0, 12, 12);
        Height = 222;
        Style = MakeStyle();
        Artwork = new EmpireRoomArtwork(index) { Height = 126 };
        var content = new Grid();
        var stack = new StackPanel();
        stack.Children.Add(Artwork);
        var title = new DockPanel { Margin = new Thickness(12, 8, 12, 2) };
        DockPanel.SetDock(level, Dock.Right);
        title.Children.Add(level);
        var name = Text(room.Name, 18, "#F2E4C8");
        name.FontFamily = new FontFamily("Georgia");
        title.Children.Add(name);
        stack.Children.Add(title);
        income.Margin = new Thickness(12, 0, 12, 5);
        stack.Children.Add(income);
        milestoneTrack = new Grid { Height = 3, Margin = new Thickness(12, 0, 12, 4), Background = Ink("#263C34") };
        milestoneTrack.Children.Add(milestoneFill);
        milestoneTrack.SizeChanged += (_, _) => milestoneFill.Width = milestoneTrack.ActualWidth * progress;
        stack.Children.Add(milestoneTrack);
        next.Margin = new Thickness(12, 0, 12, 7);
        stack.Children.Add(next);
        content.Children.Add(stack);
        content.Children.Add(flash);
        frame = new Border { Background = Ink("#142722"), BorderBrush = Ink("#3F5143"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Child = content, ClipToBounds = true };
        Content = frame;
        Click += (_, _) => select(room.Id);
    }

    internal void Refresh(EmpireService service, bool selected, bool animate)
    {
        int value = service.Level(Department.Id);
        level.Text = value == 0 ? "LOCKED" : "LV " + value;
        income.Text = value == 0
            ? "Open for " + EmpireView.Number(Department.BaseCost) + " cr"
            : "+" + EmpireView.Number(service.RoomCreditsPerSecond(Department.Id)) + " cr / sec";
        next.Text = value == 0 ? "Select to open this room" : value == EmpireRules.MaximumLevel ? "All milestones complete" : "Next ×2 at level " + ((value / 10 + 1) * 10);
        if (service.Synergies.Any(synergy => synergy.Tier == 3 && (synergy.Definition.Beneficiaries & Department.Tags) != 0))
            next.Text = "SYNERGY III ✦ · " + next.Text;
        progress = value == EmpireRules.MaximumLevel ? 1 : value % 10 / 10d;
        milestoneFill.Width = milestoneTrack.ActualWidth * progress;
        frame.BorderBrush = Ink(selected ? "#E6C68A" : "#3F5143");
        Artwork.Open = value > 0;
        Artwork.Level = value;
        Artwork.Selected = selected;
        Artwork.InvalidateVisual();
        if (previousLevel >= 0 && value > previousLevel && animate)
            flash.BeginAnimation(OpacityProperty, new DoubleAnimation(.35, 0, TimeSpan.FromMilliseconds(650)) { FillBehavior = FillBehavior.Stop });
        if (!animate) flash.BeginAnimation(OpacityProperty, null);
        previousLevel = value;
        ToolTip = Department.Description;
    }

    internal void StopAnimation()
    {
        flash.BeginAnimation(OpacityProperty, null);
        Artwork.Phase = 0;
    }

    private static Style MakeStyle()
    {
        var chrome = new FrameworkElementFactory(typeof(Border)) { Name = "Chrome" };
        chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        chrome.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        chrome.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        chrome.AppendChild(presenter);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = chrome };
        foreach (DependencyProperty property in new[] { IsMouseOverProperty, IsKeyboardFocusedProperty })
        {
            var trigger = new Trigger { Property = property, Value = true };
            trigger.Setters.Add(new Setter(Border.BorderBrushProperty, Ink("#E6C68A"), "Chrome"));
            template.Triggers.Add(trigger);
        }
        var pressed = new Trigger { Property = IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(OpacityProperty, .88, "Chrome"));
        template.Triggers.Add(pressed);
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(TemplateProperty, template));
        style.Setters.Add(new Setter(CursorProperty, System.Windows.Input.Cursors.Hand));
        return style;
    }
    private static TextBlock Text(string value, double size, string color) => new() { Text = value, FontSize = size, Foreground = Ink(color), TextTrimming = TextTrimming.CharacterEllipsis };
    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
