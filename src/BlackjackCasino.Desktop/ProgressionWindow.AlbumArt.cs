using System.Windows;
using System.Windows.Input;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private IInputElement? albumArtPreviousFocus;

    private void OpenAlbumArt(CollectionCard card)
    {
        if (!controller.Profile.Collection.OwnedCardIds.Contains(card.Id)) return;
        albumArtPreviousFocus = Keyboard.FocusedElement;
        AlbumArtTitle.Text = card.Name;
        AlbumArtClose.Content = Localization.T("Album.BackToAlbum");
        AlbumArtEdition.Text = $"{CollectionCatalog.Theme(card).Name} · {card.Rarity}";
        AlbumArtHost.Content = CollectionArtwork.Card(card, true, animationsEnabled: albumAnimations);
        AlbumBrowseSurface.IsEnabled = false;
        AlbumOpeningOverlay.IsEnabled = false;
        AlbumArtOverlay.Visibility = Visibility.Visible;
        AlbumArtClose.Focus();
    }

    private void AlbumArtClose_Click(object sender, RoutedEventArgs e) => CloseAlbumArt();

    internal bool TryCloseAlbumArt()
    {
        if (AlbumArtOverlay.Visibility != Visibility.Visible) return false;
        CloseAlbumArt();
        return true;
    }

    private void CloseAlbumArt()
    {
        AlbumArtOverlay.Visibility = Visibility.Collapsed;
        AlbumArtHost.Content = null;
        AlbumBrowseSurface.IsEnabled = AlbumOpeningOverlay.IsEnabled = true;
        if (albumArtPreviousFocus is UIElement { IsVisible: true, IsEnabled: true } previous) previous.Focus();
        albumArtPreviousFocus = null;
    }

    private void AlbumArt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && TryCloseAlbumArt()) e.Handled = true;
    }
}
