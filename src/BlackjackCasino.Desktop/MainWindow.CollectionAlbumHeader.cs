using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private Button? collectionAlbumHeaderButton;
    private TextBlock? collectionAlbumHeaderStatus;
    private Border? collectionAlbumHeaderBadge;
    private TextBlock? collectionAlbumHeaderBadgeText;
    private DropShadowEffect? collectionAlbumHeaderBadgeGlow;

    private void BuildCollectionAlbumHeaderButton(Panel headerActions)
    {
        if (collectionAlbumHeaderButton is not null) return;

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(ProgressionArtwork.Create("Collector's Album", 34));

        var labels = new StackPanel { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock
        {
            Text = "ALBUM",
            FontSize = 8.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(199, 177, 124)),
        });
        collectionAlbumHeaderStatus = new TextBlock
        {
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(225, 232, 224)),
        };
        labels.Children.Add(collectionAlbumHeaderStatus);
        Grid.SetColumn(labels, 1);
        content.Children.Add(labels);

        collectionAlbumHeaderBadgeText = new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(19, 35, 30)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        collectionAlbumHeaderBadgeGlow = new DropShadowEffect
        {
            Color = Color.FromRgb(103, 230, 184),
            BlurRadius = 10,
            ShadowDepth = 0,
            Opacity = .35,
        };
        collectionAlbumHeaderBadge = new Border
        {
            MinWidth = 38,
            Height = 30,
            Margin = new Thickness(9, 0, 0, 0),
            Padding = new Thickness(6, 0, 6, 0),
            CornerRadius = new CornerRadius(15),
            Background = new SolidColorBrush(Color.FromRgb(112, 220, 172)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(177, 250, 215)),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = collectionAlbumHeaderBadgeText,
            Effect = collectionAlbumHeaderBadgeGlow,
            Visibility = Visibility.Collapsed,
        };
        collectionAlbumHeaderBadge.Name = "HeaderAlbumPackBadge";
        PackBadgeMotion.Attach(collectionAlbumHeaderBadge, () => CurrentPresentation.AnimationsEnabled);
        Grid.SetColumn(collectionAlbumHeaderBadge, 2);
        content.Children.Add(collectionAlbumHeaderBadge);

        collectionAlbumHeaderButton = new Button
        {
            Name = "HeaderCollectionAlbumButton",
            Height = 52,
            MinWidth = 118,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(8, 5, 8, 5),
            Style = (Style)FindResource("MutedButton"),
            Content = content,
            VerticalAlignment = VerticalAlignment.Center,
        };
        collectionAlbumHeaderButton.Click += (_, _) =>
        {
            if (progressionController is null || MetaPageOpen) return;
            OpenCollectionAlbumPage();
        };
        headerActions.Children.Add(collectionAlbumHeaderButton);
        RefreshCollectionAlbumHeaderButton();
    }

    private void RefreshCollectionAlbumHeaderButton()
    {
        if (collectionAlbumHeaderButton is null || collectionAlbumHeaderStatus is null || progressionController is null) return;

        CollectionState state = progressionController.Profile.Collection;
        int owned = state.OwnedCardIds.Count;
        int unopened = state.Packs.Values.Sum();
        collectionAlbumHeaderStatus.Text = $"{owned} / {CollectionCatalog.Cards.Count}";
        collectionAlbumHeaderButton.ToolTip = unopened > 0
            ? $"Collector's Album · {owned} of {CollectionCatalog.Cards.Count} cards collected · {unopened} unopened pack{(unopened == 1 ? string.Empty : "s")}" 
            : $"Collector's Album · {owned} of {CollectionCatalog.Cards.Count} cards collected";

        if (collectionAlbumHeaderBadge is not null && collectionAlbumHeaderBadgeText is not null)
        {
            collectionAlbumHeaderBadge.Visibility = unopened > 0 ? Visibility.Visible : Visibility.Collapsed;
            collectionAlbumHeaderBadgeText.Text = unopened > 99 ? "+99" : $"+{unopened}";
        }

        lobby?.SetAlbumPackCount(unopened);

        PackBadgeMotion.Sync(collectionAlbumHeaderBadge);
    }
}
