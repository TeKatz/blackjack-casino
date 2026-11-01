using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private Button? collectionAlbumHeaderButton;
    private TextBlock? collectionAlbumHeaderStatus;

    private void BuildCollectionAlbumHeaderButton(Panel headerActions)
    {
        if (collectionAlbumHeaderButton is not null) return;

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.Children.Add(new TextBlock
        {
            Text = "▣",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromRgb(218, 187, 112)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
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

        collectionAlbumHeaderButton = new Button
        {
            Name = "HeaderCollectionAlbumButton",
            Height = 52,
            MinWidth = 104,
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
        int owned = progressionController.Profile.Collection.OwnedCardIds.Count;
        collectionAlbumHeaderStatus.Text = $"{owned} / {CollectionCatalog.Cards.Count}";
        collectionAlbumHeaderButton.ToolTip = $"Collector's Album · {owned} of {CollectionCatalog.Cards.Count} cards collected";
    }
}
