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
    private static readonly Dictionary<string, string> RoomFiles = new(StringComparer.Ordinal)
    {
        ["high-limit-slots"] = "high-limit-slots", ["poker"] = "poker-room", ["dining"] = "fine-dining",
        ["stage"] = "grand-stage", ["private-salon"] = "private-gaming-salon", ["hotel"] = "luxury-hotel",
        ["spa"] = "spa-wellness", ["sky-bar"] = "sky-bar", ["convention-hall"] = "convention-hall", ["penthouse-club"] = "penthouse-club",
    };
    private static readonly Dictionary<string, BitmapSource> RoomImages = new(StringComparer.Ordinal);
    internal static BitmapSource ForRoom(string roomId, int fallback)
    {
        if (!RoomFiles.TryGetValue(roomId, out string? file)) return Get(fallback);
        if (RoomImages.TryGetValue(roomId, out BitmapSource? cached)) return cached;
        var image = new BitmapImage(); image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/BlackjackCasino.Desktop;component/Assets/Empire/{file}.png", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 768;
        image.EndInit(); image.Freeze(); RoomImages.Add(roomId, image); return image;
    }
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
internal sealed class EmpireRoomArtwork(int textureIndex, string? roomId = null) : FrameworkElement
{
    private ImageBrush? photo;
    private static readonly Brush Shade = Frozen(new LinearGradientBrush(Color.FromArgb(0, 4, 13, 13), Color.FromArgb(210, 4, 13, 13), 90));
    private static readonly Brush ClosedShade = Frozen(new SolidColorBrush(Color.FromArgb(145, 5, 14, 17)));
    private static readonly Brush Coin = Frozen(new SolidColorBrush(Color.FromRgb(235, 200, 115)));
    private static readonly Brush CaptionInk = Frozen(new SolidColorBrush(Color.FromRgb(190, 227, 196)));
    private static readonly Brush LockedInk = Frozen(new SolidColorBrush(Color.FromRgb(202, 185, 146)));
    private readonly RadialGradientBrush glow = new(Colors.Transparent, Colors.Transparent);
    private FormattedText? caption;
    private (string Text, double Dpi) captionKey;
    private double phase;

    private static T Frozen<T>(T value) where T : Freezable { value.Freeze(); return value; }
    private static ImageBrush Photo(BitmapSource image) => Frozen(new ImageBrush(image) { Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Center });
    internal double Phase { get => phase; set { phase = value; InvalidateVisual(); } }
    internal bool Open { get; set; }
    internal bool Available { get; set; } = true;
    internal int Level { get; set; }
    internal bool Selected { get; set; }
    internal int TextureIndex => textureIndex;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        Rect area = new(0, 0, ActualWidth, ActualHeight);
        dc.PushClip(new RectangleGeometry(area, 7, 7));
        photo ??= Photo(roomId is null ? EmpireRoomTextures.Get(textureIndex) : EmpireRoomTextures.ForRoom(roomId, textureIndex));
        dc.DrawRectangle(photo, null, area);
        dc.DrawRectangle(Shade, null, area);
        if (!Open)
        {
            dc.DrawRectangle(ClosedShade, null, area);
            Text(dc, Available ? "ROOM AVAILABLE" : "LOCKED", 12, ActualHeight - 29, 10, "#CAB992");
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
                dc.DrawEllipse(Coin, null, new Point(x, y), 2, 3);
                dc.Pop();
            }
            glow.GradientStops[0].Color = Color.FromArgb((byte)(23 + 14 * Math.Sin(phase + textureIndex)), 255, 199, 78);
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
    {
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (caption is null || captionKey != (text, dpi))
        {
            caption = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), size, Open ? CaptionInk : LockedInk, dpi);
            captionKey = (text, dpi);
        }
        dc.DrawText(caption, new Point(x, y));
    }

}

internal sealed class EmpireRoomCard : Button
{
    internal EmpireDepartment Department { get; }
    internal EmpireRoomArtwork Artwork { get; }
    private readonly TextBlock roomName;
    private readonly TextBlock level = Text("", 11, "#BFAE8A");
    private readonly TextBlock income = Text("", 13, "#B3D4BE");
    private readonly TextBlock next = Text("", 10, "#94AAA0");
    private readonly Border milestoneFill = new() { Background = Ink("#B59A62"), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(2) };
    private readonly Border frame;
    private static readonly System.Windows.Media.Effects.DropShadowEffect selectedGlow = CreateGlow();
    private readonly Grid milestoneTrack;
    private readonly Border flash = new() { Background = Ink("#FFE0A0"), Opacity = 0, IsHitTestVisible = false, CornerRadius = new CornerRadius(9) };
    private int previousLevel = -1;
    private bool previouslySelected;
    private double progress;

    internal EmpireRoomCard(EmpireDepartment room, int index, Action<string> select)
    {
        Department = room;
        Name = "EmpireRoom_" + room.Id.Replace('-', '_');
        AutomationProperties.SetName(this, "Select " + room.Name);
        Margin = new Thickness(0, 0, 9, 10);
        Height = 222;
        Style = MakeStyle();
        Artwork = new EmpireRoomArtwork(index, room.Id) { Height = 126 };
        var content = new Grid();
        var stack = new StackPanel();
        stack.Children.Add(Artwork);
        var title = new DockPanel { Margin = new Thickness(12, 8, 12, 2) };
        DockPanel.SetDock(level, Dock.Right);
        title.Children.Add(level);
        roomName = Text(room.Name, 18, "#F2E4C8");
        roomName.FontFamily = new FontFamily("Georgia");
        title.Children.Add(roomName);
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
        string insignia = room.Tags.HasFlag(EmpireTag.Slots) ? "Slots" : room.Tags.HasFlag(EmpireTag.Tables) ? "Tables"
            : room.Tags.HasFlag(EmpireTag.Hospitality) ? "Hospitality" : room.Tags.HasFlag(EmpireTag.VIP) ? "Prestige" : "Events";
        content.Children.Add(new EmpireIcon(insignia, 18) { HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10) });
        content.Children.Add(flash);
        frame = new Border { Background = new LinearGradientBrush(Color.FromRgb(25, 49, 34), Color.FromRgb(12, 30, 21), 100), BorderBrush = Ink("#3C5643"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Child = content, ClipToBounds = true };
        Content = frame;
        Click += (_, _) => select(room.Id);
    }

    internal void SetSize(double width, double height)
    {
        Width = width; Height = height;
        Artwork.Height = Math.Max(62, height - 96);
        roomName.FontSize = width < 190 ? 14 : 18;
        level.FontSize = width < 190 ? 9 : 11;
        income.FontSize = width < 190 ? 11 : 13;
    }

    internal void Refresh(EmpireService service, bool selected, bool animate)
    {
        int value = service.Level(Department.Id);
        EmpireUpgradeQuote opening = service.QuoteUpgrade(Department.Id, 1);
        level.Text = value == 0 ? (opening.Unlocked ? "NEW" : "LOCKED") : "LV " + value;
        income.Text = value == 0
            ? (opening.Unlocked ? "Open for " + EmpireView.Number(opening.Cost) + " cr" : "Requirements needed")
            : "+" + EmpireView.Number(service.RoomCreditsPerSecond(Department.Id)) + " cr / sec";
        next.Text = value == 0 ? (opening.Unlocked ? "Select to open this room" : Department.UnlockRequirement.Description) : value == EmpireRules.MaximumLevel ? "All milestones complete" : "Next ×2 at level " + ((value / 10 + 1) * 10);
        bool limits = service.Synergies.Any(s => s.NextLevel is int target && value < target && value == s.MinimumLevel
            && Department.Floor <= s.Definition.MaximumFloor && (Department.Tags & s.Definition.Members) != 0);
        if (limits) next.Text = "CIRCUIT LIMIT · " + next.Text;
        else if (service.ManagerAssignments.Values.Contains(Department.Id)) next.Text = "MANAGED · " + next.Text;
        else if (service.AutomationUnlocked && !service.Automation.Enabled) next.Text = "AUTO READY · " + next.Text;
        if (service.Synergies.Any(synergy => synergy.Tier == 3 && (synergy.Definition.Beneficiaries & Department.Tags) != 0))
            next.Text = "SYNERGY III ✦ · " + next.Text;
        progress = value == EmpireRules.MaximumLevel ? 1 : value % 10 / 10d;
        milestoneFill.Width = milestoneTrack.ActualWidth * progress;
        frame.BorderBrush = Ink(selected ? "#E6C68A" : "#3C5643");
        frame.Effect = selected ? selectedGlow : null;
        if (selected && !previouslySelected && animate)
            frame.BeginAnimation(OpacityProperty, new DoubleAnimation(.65, 1, TimeSpan.FromMilliseconds(160)) { FillBehavior = FillBehavior.Stop });
        if (!animate) frame.BeginAnimation(OpacityProperty, null);
        previouslySelected = selected;
        Artwork.Open = value > 0;
        Artwork.Available = opening.Unlocked;
        Artwork.Level = value;
        Artwork.Selected = selected;
        Artwork.InvalidateVisual();
        if (previousLevel >= 0 && value > previousLevel && animate)
            flash.BeginAnimation(OpacityProperty, new DoubleAnimation(.35, 0, TimeSpan.FromMilliseconds(650)) { FillBehavior = FillBehavior.Stop });
        if (!animate) flash.BeginAnimation(OpacityProperty, null);
        previousLevel = value;
        ToolTip = Department.Description + (value == 0 && !opening.Unlocked ? "\n" + Department.UnlockRequirement.Description : "");
    }

    internal void StopAnimation()
    {
        flash.BeginAnimation(OpacityProperty, null);
        frame.BeginAnimation(OpacityProperty, null);
        Artwork.Phase = 0;
    }

    private static System.Windows.Media.Effects.DropShadowEffect CreateGlow()
    {
        var glow = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(212, 179, 113), BlurRadius = 14, ShadowDepth = 0, Opacity = .28 };
        glow.Freeze(); return glow;
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
