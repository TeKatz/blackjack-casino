using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Keeps live WPF text sharp inside fixed-design Viewboxes. Inverse layout scaling
/// cancels the ancestor visual transform for glyph rendering while proportional
/// font sizing preserves the original measure and wrap points.
/// </summary>
public static class CrispViewboxText
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(CrispViewboxText),
        new PropertyMetadata(false, OnEnabledChanged));

    private static readonly DependencyProperty OriginalFontSizeProperty = DependencyProperty.RegisterAttached(
        "OriginalFontSize", typeof(double), typeof(CrispViewboxText),
        new PropertyMetadata(double.NaN));
    private static readonly DependencyProperty AppliedFontSizeProperty = DependencyProperty.RegisterAttached(
        "AppliedFontSize", typeof(double), typeof(CrispViewboxText),
        new PropertyMetadata(double.NaN));
    private static readonly DependencyProperty OwnedTransformProperty = DependencyProperty.RegisterAttached(
        "OwnedTransform", typeof(ScaleTransform), typeof(CrispViewboxText));

    static CrispViewboxText()
    {
        // Dynamic game labels may join the visual tree after the initial layout.
        EventManager.RegisterClassHandler(typeof(TextBlock), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is TextBlock label && FindEnabledViewbox(label) is { } box)
                    Adjust(label, GetScale(box));
            }));
    }

    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject target, bool enabled) =>
        target.SetValue(EnabledProperty, enabled);

    private static void OnEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not Viewbox box) return;
        if ((bool)args.NewValue)
        {
            box.UseLayoutRounding = true;
            box.SnapsToDevicePixels = true;
            TextOptions.SetTextRenderingMode(box, TextRenderingMode.ClearType);
            TextOptions.SetTextHintingMode(box, TextHintingMode.Fixed);
            TextOptions.SetTextFormattingMode(box, TextFormattingMode.Display);
            box.Loaded += OnLoaded;
            box.SizeChanged += OnSizeChanged;
            Refresh(box);
        }
        else
        {
            box.Loaded -= OnLoaded;
            box.SizeChanged -= OnSizeChanged;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Viewbox box) Refresh(box);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Viewbox box) Refresh(box);
    }

    private static double GetScale(Viewbox box)
    {
        if (box.Child is not FrameworkElement surface ||
            surface.Width <= 0 || surface.Height <= 0 ||
            box.ActualWidth <= 0 || box.ActualHeight <= 0)
            return double.NaN;
        double s = Math.Min(box.ActualWidth / surface.Width, box.ActualHeight / surface.Height);
        if (box.StretchDirection == StretchDirection.DownOnly) s = Math.Min(s, 1);
        if (box.StretchDirection == StretchDirection.UpOnly) s = Math.Max(s, 1);
        return double.IsFinite(s) && s > 0 ? s : double.NaN;
    }

    private static void Refresh(Viewbox box)
    {
        double scale = GetScale(box);
        if (double.IsFinite(scale) && box.Child is not null) Walk(box.Child, scale);
    }

    private static void Walk(DependencyObject node, double scale)
    {
        if (node is TextBlock text) Adjust(text, scale);
        if (node is not Visual and not Visual3D) return;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            Walk(VisualTreeHelper.GetChild(node, i), scale);
    }

    private static Viewbox? FindEnabledViewbox(DependencyObject node)
    {
        DependencyObject? current = node;
        while (current is not null)
        {
            if (current is Viewbox box && GetEnabled(box)) return box;
            current = current is Visual or Visual3D
                ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }

    private static void Adjust(TextBlock text, double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0) return;
        var owned = text.GetValue(OwnedTransformProperty) as ScaleTransform;
        if (owned is null && text.LayoutTransform is not null &&
            !ReferenceEquals(text.LayoutTransform, Transform.Identity)) return;
        double baseSize = (double)text.GetValue(OriginalFontSizeProperty);
        double appliedSize = (double)text.GetValue(AppliedFontSizeProperty);
        if (double.IsNaN(baseSize) || (!double.IsNaN(appliedSize) && Math.Abs(text.FontSize - appliedSize) > 0.01))
        {
            baseSize = text.FontSize;
            text.SetValue(OriginalFontSizeProperty, baseSize);
        }
        double effective = baseSize * scale;
        if (!double.IsFinite(effective) || effective <= 0) return;
        if (Math.Abs(text.FontSize - effective) > 0.01) text.FontSize = effective;
        text.SetValue(AppliedFontSizeProperty, effective);
        if (owned is null)
        {
            owned = new ScaleTransform(1, 1);
            text.SetValue(OwnedTransformProperty, owned);
            text.LayoutTransform = owned;
        }
        double inverse = 1 / scale;
        if (Math.Abs(owned.ScaleX - inverse) > 0.0001) owned.ScaleX = inverse;
        if (Math.Abs(owned.ScaleY - inverse) > 0.0001) owned.ScaleY = inverse;
    }
}
