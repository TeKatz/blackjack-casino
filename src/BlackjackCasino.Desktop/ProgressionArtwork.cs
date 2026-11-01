using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlackjackCasino.Desktop;

/// <summary>Generated, bundled navigation art; one frozen decode per emblem for lobby and header.</summary>
internal static class ProgressionArtwork
{
    private static readonly Dictionary<string, BitmapImage> Images = new(StringComparer.Ordinal);

    internal static FrameworkElement Create(string section, double size)
    {
        string key = section switch
        {
            "Season Pass" => "season", "Cosmetic Shop" => "shop", "Challenges" => "challenges",
            "Overview" or "Statistics" => "statistics", _ => "album",
        };
        if (!Images.TryGetValue(key, out BitmapImage? source))
        {
            source = new BitmapImage(); source.BeginInit();
            source.UriSource = new Uri("pack://application:,,,/BlackjackCasino.Desktop;component/Assets/Navigation/" + key + ".png");
            source.CacheOption = BitmapCacheOption.OnLoad; source.DecodePixelWidth = 256;
            source.EndInit(); source.Freeze(); Images.Add(key, source);
        }
        var image = new Image { Name = "NavigationEmblem", Tag = key, Width = size, Height = size, Source = source, Stretch = Stretch.Uniform, IsHitTestVisible = false };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        System.Windows.Automation.AutomationProperties.SetName(image, section + " emblem");
        return image;
    }
}
