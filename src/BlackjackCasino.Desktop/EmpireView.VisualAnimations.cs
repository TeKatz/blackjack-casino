using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal static class EmpireViewVisualAnimations
{
    /// <summary>Immediately settles Empire artwork when the global animation preference is disabled.</summary>
    internal static void StopVisualAnimations(this EmpireView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        foreach (EmpireRoomCard card in Descendants<EmpireRoomCard>(view)) card.StopAnimation();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (T nested in Descendants<T>(child)) yield return nested;
        }
    }
}
