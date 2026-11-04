using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private bool cardSetStyleBusy;
    private bool cardSetStylingInitialized;
    private string? lastAppliedCardSetId;
    internal int CardSetFullScanCount { get; private set; }

    private void InitializeCardSetStyling()
    {
        if (cardSetStylingInitialized) return;
        cardSetStylingInitialized = true;
        ApplyEquippedCardSetToRenderedCards();
    }

    internal static void RegisterCardStyling(Border card) => card.Loaded += PlayingCardLoaded;

    private static void PlayingCardLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is not Border card || !IsPlayingCardBorder(card)) return;
        MainWindow? owner = Window.GetWindow(card) as MainWindow;
        for (DependencyObject? parent = card; owner is null && parent is not null; parent = VisualTreeHelper.GetParent(parent))
            owner = parent as MainWindow;
        owner?.ApplyEquippedCardSet(card);
    }

    private void ApplyEquippedCardSetToRenderedCards()
    {
        if (cardSetStyleBusy || progressionController is null)
        {
            return;
        }

        string cardSetId = progressionController.Profile.EquippedCardSetId;
        if (lastAppliedCardSetId == cardSetId) return;
        lastAppliedCardSetId = cardSetId;
        CardSetFullScanCount++;
        CosmeticDefinition cosmetic = CosmeticCatalog.TryGet(cardSetId) is { Kind: CosmeticKind.CardSet } valid
            ? valid
            : CosmeticCatalog.Get(CosmeticCatalog.DefaultCardSetId);

        cardSetStyleBusy = true;
        try
        {
            foreach (Border card in EnumerateVisuals<Border>(this).Where(IsPlayingCardBorder))
            {
                if (card.Tag is string applied && string.Equals(applied, cardSetId, StringComparison.Ordinal))
                {
                    continue;
                }

                RestyleCard(card, cosmetic);
                card.Tag = cardSetId;
            }
        }
        finally
        {
            cardSetStyleBusy = false;
        }
    }

    private void ApplyEquippedCardSet(Border card)
    {
        if (progressionController is null || !cardSetStylingInitialized) return;
        string id = progressionController.Profile.EquippedCardSetId;
        if (card.Tag is string applied && applied == id) return;
        CosmeticDefinition cosmetic = CosmeticCatalog.TryGet(id) is { Kind: CosmeticKind.CardSet } valid
            ? valid : CosmeticCatalog.Get(CosmeticCatalog.DefaultCardSetId);
        RestyleCard(card, cosmetic);
        card.Tag = id;
    }

    private static bool IsPlayingCardBorder(Border border)
    {
        bool normal = Math.Abs(border.Width - 76) < .1 && Math.Abs(border.Height - 108) < .1;
        bool compact = Math.Abs(border.Width - 48) < .1 && Math.Abs(border.Height - 68) < .1;
        if (!normal && !compact) return false;
        return border.Child is Grid || border.Child is TextBlock;
    }

    private static void RestyleCard(Border card, CosmeticDefinition cosmetic) =>
        PlayingCardPresentation.ApplyTheme(card, cosmetic);

    private static IEnumerable<T> EnumerateVisuals<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (T nested in EnumerateVisuals<T>(child)) yield return nested;
        }
    }

}
