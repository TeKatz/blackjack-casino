using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using BlackjackCasino.Desktop;

internal static partial class Program
{
    private static async Task CheckDistinctFrames(MainWindow window)
    {
        var root = (Grid)window.Content;
        var overlay = new Grid { Background = new SolidColorBrush(Color.FromRgb(7, 15, 22)) };
        Grid.SetRowSpan(overlay, 2); Grid.SetColumnSpan(overlay, 2); Panel.SetZIndex(overlay, int.MaxValue);
        root.Children.Add(overlay);
        try
        {
            var spread = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            overlay.Children.Add(spread);
            spread.Children.Add(new TextBlock { Text = "THE MIDNIGHT CLUB / SIGNATURE FRAMES", Foreground = Brushes.Bisque, FontSize = 22, Margin = new Thickness(12, 0, 0, 30) });
            var frames = new StackPanel { Orientation = Orientation.Horizontal }; spread.Children.Add(frames);
            var signatures = new HashSet<string>();
            foreach ((string variant, string name) in new[] { ("neon-noir", "Neon Noir"), ("eclipse-thread", "Eclipse Thread"), ("eclipse-halo", "Eclipse Halo"), ("aurora-drift", "Aurora Glass") })
            {
                var stack = new StackPanel { Margin = new Thickness(14) };
                var border = new Border { Width = 170, Height = 104, Padding = new Thickness(12), Background = new SolidColorBrush(Color.FromRgb(13, 29, 31)), Child = new TextBlock { Text = "✦  MIDNIGHT", FontSize = 16, Foreground = Brushes.WhiteSmoke, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                Require(DistinctCosmeticFrames.Apply(border, variant, false), "Distinct frame renderer rejected " + variant);
                Require(signatures.Add(border.BorderBrush.GetType().Name + border.BorderThickness + border.CornerRadius), "Cosmetic frame geometry/material duplicates another edition.");
                Require(!((DropShadowEffect)border.Effect).HasAnimatedProperties && !border.BorderBrush.HasAnimatedProperties, "Reduced-motion frame still animates.");
                stack.Children.Add(border); stack.Children.Add(new TextBlock { Text = name, Foreground = Brushes.Bisque, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 18, 0, 0) });
                frames.Children.Add(stack);
            }
            window.UpdateLayout(); await Task.Delay(35); Capture(window, "signature-frame-variants");
        }
        finally { root.Children.Remove(overlay); }
    }
}
