using System.Windows.Controls;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    internal void RemoveAlbumTab()
    {
        TabItem? album = ProgressionTabs.Items.OfType<TabItem>()
            .FirstOrDefault(tab => string.Equals(tab.Header?.ToString(), "Album", StringComparison.OrdinalIgnoreCase));
        if (album is not null) ProgressionTabs.Items.Remove(album);
    }
}
