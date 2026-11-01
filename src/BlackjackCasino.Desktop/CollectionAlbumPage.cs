using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Dedicated collector surface hosted by the casino meta-page stack. The regular 10-card
/// themes are deliberately laid out as 4 Common + 3 Uncommon above Rare/Epic/Legendary,
/// so a 1920x1080 host never needs to scroll the collection itself.
/// </summary>
internal sealed class CollectionAlbumPage : Window
{
    private const double AlbumCardScale = .92;
    private const double InspectorCardScale = 2.0;

    private readonly PlayerProgressionController controller;
    private readonly bool animationsEnabled;
    private readonly bool soundsEnabled;
    private readonly Action close;
    private readonly Grid root = new();
    private readonly Grid browseSurface = new();
    private readonly StackPanel themeList = new();
    private readonly UniformGrid packStrip = new() { Columns = 5 };
    private readonly Grid cardArea = new();
    private readonly TextBlock summary = new();
    private readonly TextBlock status = new();
    private readonly ProgressBar duplicateProgress = new() { Minimum = 0, Maximum = CollectionCatalog.DuplicatePackCost };
    private readonly TextBlock themeTitle = new();
    private readonly TextBlock themeProgress = new();

    private readonly Border openingOverlay = new();
    private readonly TextBlock openingTitle = new();
    private readonly ContentControl openingPack = new();
    private readonly TextBlock openingSummary = new();
    private readonly WrapPanel openingCards = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Button revealAll = new() { Name = "AlbumRevealAll", Content = "Reveal all" };
    private readonly Button collect = new() { Name = "AlbumCollect", Content = "Continue collecting" };

    private readonly Border artOverlay = new();
    private readonly TextBlock artTitle = new();
    private readonly TextBlock artEdition = new();
    private readonly ContentControl artHost = new();
    private readonly TextBlock loreTheme = new();
    private readonly TextBlock loreCard = new();
    private readonly TextBlock loreBody = new();
    private readonly Button artClose = new() { Name = "AlbumArtClose", Content = "Back to album" };

    private string activeThemeId = CollectionCatalog.Themes[0].Id;
    private Guid openingId;
    private readonly HashSet<int> revealed = [];
    private readonly List<ContentControl> revealSlots = [];
    private CancellationTokenSource? presentation;
    private readonly object audioOwner = new();
    private IInputElement? previousFocus;
    private static readonly Dictionary<(CollectionRarity Rarity, bool Opening), AudioClip> Cues = [];

    private static readonly Brush TextBrush = Brush("#F2EEE4");
    private static readonly Brush MutedBrush = Brush("#9DB1A6");
    private static readonly Brush GoldBrush = Brush("#E2C47B");
    private static readonly Brush PanelBrush = Brush("#0B1D18");
    private static readonly Brush PanelBorderBrush = Brush("#315A43");

    internal CollectionAlbumPage(PlayerProgressionController controller, bool animationsEnabled, bool soundsEnabled, Action close)
    {
        this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        this.animationsEnabled = animationsEnabled;
        this.soundsEnabled = soundsEnabled;
        this.close = close ?? throw new ArgumentNullException(nameof(close));

        Title = "Blackjack Casino — Collector's Album";
        Width = 1600;
        Height = 900;
        MinWidth = 1100;
        MinHeight = 720;
        Background = Brush("#07120D");
        Foreground = TextBrush;
        FontFamily = new FontFamily("Segoe UI");
        Content = BuildSurface();

        root.PreviewKeyDown += Root_PreviewKeyDown;
        root.Unloaded += (_, _) => StopPresentation();
        Refresh();
    }

    private FrameworkElement BuildSurface()
    {
        root.Name = "CollectionAlbumRoot";
        root.Background = new LinearGradientBrush(Color.FromRgb(6, 18, 13), Color.FromRgb(9, 20, 21), 80);
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var header = new Grid { Margin = new Thickness(22, 8, 18, 6) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var heading = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        heading.Children.Add(new TextBlock { Text = "THE COLLECTOR'S ALBUM", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = GoldBrush });
        heading.Children.Add(new TextBlock { Text = "Every table leaves a story behind.", FontFamily = new FontFamily("Georgia"), FontSize = 22, Foreground = Brush("#F5E8D2"), Margin = new Thickness(0, 2, 0, 0) });
        header.Children.Add(heading);
        var back = AlbumButton("AlbumBackButton", "Back", new Thickness(18, 7, 18, 7));
        back.Click += (_, _) => close();
        Grid.SetColumn(back, 1);
        header.Children.Add(back);
        root.Children.Add(header);

        var overview = new Border
        {
            GridRow = 1,
            Margin = new Thickness(22, 0, 22, 10),
            Padding = new Thickness(14, 9, 14, 9),
            Background = Brush("#0A2118"),
            BorderBrush = PanelBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
        };
        var overviewGrid = new Grid();
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition());
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        summary.Name = "AlbumSummary";
        summary.FontSize = 12;
        summary.FontWeight = FontWeights.SemiBold;
        summary.VerticalAlignment = VerticalAlignment.Center;
        overviewGrid.Children.Add(summary);
        var duplicate = new StackPanel { GridColumn = 1, Margin = new Thickness(18, 0, 0, 0) };
        duplicate.Children.Add(new TextBlock { Text = "DUPLICATE RECYCLING", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brush("#C494FF") });
        duplicateProgress.Height = 6;
        duplicateProgress.Margin = new Thickness(0, 4, 0, 3);
        duplicateProgress.Foreground = Brush("#C494FF");
        duplicateProgress.Background = Brush("#17251F");
        duplicate.Children.Add(duplicateProgress);
        status.Foreground = MutedBrush;
        status.FontSize = 10;
        status.TextWrapping = TextWrapping.Wrap;
        duplicate.Children.Add(status);
        overviewGrid.Children.Add(duplicate);
        overview.Child = overviewGrid;
        root.Children.Add(overview);

        browseSurface.Name = "AlbumBrowseSurface";
        browseSurface.Margin = new Thickness(22, 0, 22, 20);
        browseSurface.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(226) });
        browseSurface.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        browseSurface.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(browseSurface, 2);
        root.Children.Add(browseSurface);

        var themesPanel = new Border { Background = PanelBrush, BorderBrush = PanelBorderBrush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(8) };
        var themesGrid = new Grid();
        themesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        themesGrid.RowDefinitions.Add(new RowDefinition());
        themesGrid.Children.Add(new TextBlock { Text = "COLLECTIONS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = GoldBrush, Margin = new Thickness(8, 4, 8, 8) });
        themeList.Name = "AlbumThemes";
        var themeScroll = new ScrollViewer { GridRow = 1, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = themeList };
        themesGrid.Children.Add(themeScroll);
        themesPanel.Child = themesGrid;
        browseSurface.Children.Add(themesPanel);

        var content = new Grid { GridColumn = 2 };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition());
        browseSurface.Children.Add(content);

        var packs = new Border { Background = PanelBrush, BorderBrush = PanelBorderBrush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 8) };
        var packGrid = new Grid();
        packGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        packGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        packGrid.Children.Add(new TextBlock { Text = "YOUR PACKS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = GoldBrush, Margin = new Thickness(2, 0, 0, 5) });
        packStrip.Name = "AlbumPacks";
        Grid.SetRow(packStrip, 1);
        packGrid.Children.Add(packStrip);
        packs.Child = packGrid;
        content.Children.Add(packs);

        var themeHeader = new Grid { GridRow = 1, Margin = new Thickness(4, 2, 4, 5) };
        themeHeader.ColumnDefinitions.Add(new ColumnDefinition());
        themeHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        themeTitle.Name = "AlbumThemeTitle";
        themeTitle.FontFamily = new FontFamily("Georgia");
        themeTitle.FontSize = 21;
        themeTitle.Foreground = Brush("#F4E6CF");
        themeHeader.Children.Add(themeTitle);
        themeProgress.FontSize = 11;
        themeProgress.Foreground = MutedBrush;
        themeProgress.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(themeProgress, 1);
        themeHeader.Children.Add(themeProgress);
        content.Children.Add(themeHeader);

        cardArea.Name = "AlbumCards";
        cardArea.GridRow = 2;
        cardArea.VerticalAlignment = VerticalAlignment.Top;
        content.Children.Add(cardArea);

        BuildOpeningOverlay();
        BuildArtOverlay();
        return root;
    }

    private void BuildOpeningOverlay()
    {
        openingOverlay.Name = "AlbumOpeningOverlay";
        openingOverlay.Background = Brush("#FD071018");
        openingOverlay.BorderBrush = Brush("#C494FF");
        openingOverlay.BorderThickness = new Thickness(1);
        openingOverlay.CornerRadius = new CornerRadius(12);
        openingOverlay.Padding = new Thickness(18);
        openingOverlay.Visibility = Visibility.Collapsed;
        Panel.SetZIndex(openingOverlay, 20);
        Grid.SetRowSpan(openingOverlay, 3);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var head = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        openingTitle.FontFamily = new FontFamily("Georgia");
        openingTitle.FontSize = 25;
        openingTitle.FontWeight = FontWeights.Bold;
        openingTitle.HorizontalAlignment = HorizontalAlignment.Center;
        head.Children.Add(openingTitle);
        openingPack.HorizontalAlignment = HorizontalAlignment.Center;
        openingPack.Margin = new Thickness(0, 6, 0, 2);
        head.Children.Add(openingPack);
        openingSummary.Foreground = Brush("#C5CEDB");
        openingSummary.TextAlignment = TextAlignment.Center;
        openingSummary.TextWrapping = TextWrapping.Wrap;
        openingSummary.Margin = new Thickness(8, 3, 8, 8);
        head.Children.Add(openingSummary);
        grid.Children.Add(head);
        openingCards.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(openingCards, 1);
        grid.Children.Add(openingCards);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        ConfigureActionButton(revealAll);
        ConfigureActionButton(collect);
        revealAll.Click += (_, _) => RevealAll();
        collect.Click += Collect_Click;
        actions.Children.Add(revealAll);
        actions.Children.Add(collect);
        Grid.SetRow(actions, 2);
        grid.Children.Add(actions);
        openingOverlay.Child = grid;
        root.Children.Add(openingOverlay);
    }

    private void BuildArtOverlay()
    {
        artOverlay.Name = "AlbumArtOverlay";
        artOverlay.Background = Brush("#FE080E17");
        artOverlay.BorderBrush = Brush("#AD9161");
        artOverlay.BorderThickness = new Thickness(1);
        artOverlay.CornerRadius = new CornerRadius(12);
        artOverlay.Padding = new Thickness(28, 20, 28, 18);
        artOverlay.Visibility = Visibility.Collapsed;
        Panel.SetZIndex(artOverlay, 30);
        Grid.SetRowSpan(artOverlay, 3);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var head = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        artTitle.FontFamily = new FontFamily("Georgia");
        artTitle.FontSize = 24;
        artTitle.Foreground = Brush("#FFE5B2");
        artTitle.TextAlignment = TextAlignment.Center;
        head.Children.Add(artTitle);
        artEdition.FontSize = 11;
        artEdition.Foreground = Brush("#B3C2CF");
        artEdition.TextAlignment = TextAlignment.Center;
        artEdition.Margin = new Thickness(0, 3, 0, 0);
        head.Children.Add(artEdition);
        grid.Children.Add(head);

        var body = new Grid { Margin = new Thickness(22, 12, 22, 10) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54, GridUnitType.Star) });
        artHost.HorizontalAlignment = HorizontalAlignment.Center;
        artHost.VerticalAlignment = VerticalAlignment.Center;
        body.Children.Add(artHost);

        var lore = new Border { GridColumn = 1, Margin = new Thickness(28, 20, 20, 20), Padding = new Thickness(26), Background = Brush("#0B1820"), BorderBrush = Brush("#3E5360"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), VerticalAlignment = VerticalAlignment.Center, MaxWidth = 720 };
        var loreStack = new StackPanel();
        loreStack.Children.Add(new TextBlock { Text = "ARCHIVE ENTRY", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = GoldBrush });
        loreTheme.FontSize = 12;
        loreTheme.Foreground = Brush("#91A9B7");
        loreTheme.Margin = new Thickness(0, 12, 0, 2);
        loreStack.Children.Add(loreTheme);
        loreCard.FontFamily = new FontFamily("Georgia");
        loreCard.FontSize = 28;
        loreCard.Foreground = Brush("#F4E6CF");
        loreCard.Margin = new Thickness(0, 0, 0, 16);
        loreStack.Children.Add(loreCard);
        loreBody.FontFamily = new FontFamily("Georgia");
        loreBody.FontSize = 15;
        loreBody.LineHeight = 24;
        loreBody.Foreground = Brush("#C5D0D5");
        loreBody.TextWrapping = TextWrapping.Wrap;
        loreStack.Children.Add(loreBody);
        lore.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = loreStack };
        body.Children.Add(lore);
        Grid.SetRow(body, 1);
        grid.Children.Add(body);

        ConfigureActionButton(artClose);
        artClose.HorizontalAlignment = HorizontalAlignment.Center;
        artClose.Click += (_, _) => CloseArt();
        Grid.SetRow(artClose, 2);
        grid.Children.Add(artClose);
        artOverlay.Child = grid;
        root.Children.Add(artOverlay);
    }

    private void Refresh()
    {
        controller.AccruePlaytime();
        CollectionState state = controller.Profile.Collection;
        int complete = CollectionCatalog.Themes.Count(t => t.Cards.All(c => state.OwnedCardIds.Contains(c.Id)));
        summary.Text = $"{state.OwnedCardIds.Count} / {CollectionCatalog.Cards.Count} cards   ·   {complete} / {CollectionCatalog.Themes.Count} themes complete   ·   {state.PacksOpened:N0} packs opened";
        duplicateProgress.Value = state.DuplicatePoints;
        if (string.IsNullOrWhiteSpace(status.Text)) status.Text = $"{state.DuplicatePoints} / {CollectionCatalog.DuplicatePackCost} duplicate points";

        RefreshThemes(state);
        RefreshPacks(state);
        CollectionTheme active = CollectionCatalog.Themes.FirstOrDefault(t => t.Id == activeThemeId) ?? CollectionCatalog.Themes[0];
        activeThemeId = active.Id;
        int owned = active.Cards.Count(c => state.OwnedCardIds.Contains(c.Id));
        themeTitle.Text = $"{active.Symbol}  {active.Name}" + (active.UltraRare ? "   ·   ULTRA RARE" : string.Empty);
        themeProgress.Text = $"{owned} / {active.Cards.Count} collected";
        BuildCards(active, state);

        if (state.PendingOpening is { } opening)
        {
            if (opening.Id != openingId) ShowOpening(opening);
            openingOverlay.Visibility = Visibility.Visible;
            browseSurface.IsEnabled = false;
        }
        else
        {
            openingOverlay.Visibility = Visibility.Collapsed;
            browseSurface.IsEnabled = true;
        }
    }

    private void RefreshThemes(CollectionState state)
    {
        themeList.Children.Clear();
        foreach (CollectionTheme theme in CollectionCatalog.Themes)
        {
            int owned = theme.Cards.Count(c => state.OwnedCardIds.Contains(c.Id));
            Color accent = (Color)ColorConverter.ConvertFromString(theme.Accent);
            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(new TextBlock { Text = theme.Symbol, FontSize = 15, Foreground = new SolidColorBrush(accent), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            var name = new TextBlock { Text = theme.Name, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = TextBrush, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(name, 1);
            content.Children.Add(name);
            var progress = new TextBlock { Text = $"{owned}/{theme.Cards.Count}", FontSize = 10, Foreground = owned == theme.Cards.Count ? new SolidColorBrush(accent) : MutedBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(progress, 2);
            content.Children.Add(progress);
            var button = new Button
            {
                Name = "AlbumTheme_" + theme.Id.Replace('-', '_'),
                Tag = theme.Id,
                Content = content,
                Height = 42,
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(6, 4, 8, 4),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = theme.Id == activeThemeId ? Brush("#17382A") : Brush("#10271E"),
                BorderBrush = new SolidColorBrush(theme.Id == activeThemeId ? accent : Color.FromRgb(42, 76, 59)),
                BorderThickness = new Thickness(theme.Id == activeThemeId ? 1.5 : 1),
                Foreground = TextBrush,
                Cursor = CasinoCursors.Hand,
            };
            System.Windows.Automation.AutomationProperties.SetName(button, $"{theme.Name}, {owned} of {theme.Cards.Count} cards");
            button.Click += (_, _) => { activeThemeId = theme.Id; Refresh(); };
            themeList.Children.Add(button);
        }
    }

    private void RefreshPacks(CollectionState state)
    {
        packStrip.Children.Clear();
        foreach (CollectionPack pack in CollectionCatalog.Packs)
        {
            int count = state.Packs.GetValueOrDefault(pack.Rarity);
            Color color = CollectionArtwork.RarityColor(pack.Rarity);
            var content = new Grid { Margin = new Thickness(2) };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            content.ColumnDefinitions.Add(new ColumnDefinition());
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(new TextBlock { Text = "✦", FontSize = 16, Foreground = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            labels.Children.Add(new TextBlock { Text = pack.Rarity.ToString().ToUpperInvariant(), FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color) });
            labels.Children.Add(new TextBlock { Text = $"{count} available", FontSize = 10, Foreground = TextBrush });
            Grid.SetColumn(labels, 1);
            content.Children.Add(labels);
            var action = new TextBlock { Text = count > 0 && state.PendingOpening is null ? "OPEN →" : "—", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = count > 0 ? GoldBrush : MutedBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 2, 0) };
            Grid.SetColumn(action, 2);
            content.Children.Add(action);
            var button = new Button
            {
                Tag = pack.Rarity,
                Content = content,
                Height = 52,
                Margin = new Thickness(3, 0, 3, 0),
                Padding = new Thickness(7, 4, 7, 4),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Brush("#10231F"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B)),
                BorderThickness = new Thickness(1),
                IsEnabled = count > 0 && state.PendingOpening is null,
                ToolTip = PackOdds(pack),
                Cursor = CasinoCursors.Hand,
            };
            button.Click += OpenPack_Click;
            packStrip.Children.Add(button);
        }
    }

    private void BuildCards(CollectionTheme theme, CollectionState state)
    {
        cardArea.Children.Clear();
        cardArea.RowDefinitions.Clear();
        cardArea.ColumnDefinitions.Clear();
        if (theme.UltraRare)
        {
            cardArea.RowDefinitions.Add(new RowDefinition());
            var group = BuildCardGroup("ULTRA RARE · LEGENDARY EDITIONS", theme.Cards, state, CollectionRarity.Legendary, 4);
            group.HorizontalAlignment = HorizontalAlignment.Center;
            cardArea.Children.Add(group);
            return;
        }

        cardArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        cardArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
        cardArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var upper = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        upper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
        upper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        upper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        FrameworkElement common = BuildCardGroup("COMMON", theme.Cards.Where(c => c.Rarity == CollectionRarity.Common).ToArray(), state, CollectionRarity.Common, 4);
        upper.Children.Add(common);
        FrameworkElement uncommon = BuildCardGroup("UNCOMMON", theme.Cards.Where(c => c.Rarity == CollectionRarity.Uncommon).ToArray(), state, CollectionRarity.Uncommon, 3);
        Grid.SetColumn(uncommon, 2);
        upper.Children.Add(uncommon);
        cardArea.Children.Add(upper);

        var lower = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        CollectionRarity[] lowerRarities = [CollectionRarity.Rare, CollectionRarity.Epic, CollectionRarity.Legendary];
        for (int i = 0; i < lowerRarities.Length; i++)
        {
            CollectionRarity rarity = lowerRarities[i];
            FrameworkElement group = BuildCardGroup(rarity.ToString().ToUpperInvariant(), theme.Cards.Where(c => c.Rarity == rarity).ToArray(), state, rarity, 1);
            Grid.SetColumn(group, i * 2);
            lower.Children.Add(group);
        }
        Grid.SetRow(lower, 2);
        cardArea.Children.Add(lower);
    }

    private FrameworkElement BuildCardGroup(string label, IReadOnlyList<CollectionCard> cards, CollectionState state, CollectionRarity rarity, int columns)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = label, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(CollectionArtwork.RarityColor(rarity)), Margin = new Thickness(8, 0, 8, 2), TextAlignment = TextAlignment.Center });
        var grid = new UniformGrid { Columns = Math.Max(1, columns), HorizontalAlignment = HorizontalAlignment.Center };
        foreach (CollectionCard card in cards)
        {
            bool owned = state.OwnedCardIds.Contains(card.Id);
            Border preview = CollectionArtwork.Card(card, owned, animationsEnabled: animationsEnabled, scale: AlbumCardScale);
            var button = new Button
            {
                Tag = "inspect:" + card.Id,
                Content = preview,
                IsEnabled = owned,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ToolTip = owned ? "Inspect card" : card.Name + " · undiscovered",
                Cursor = owned ? CasinoCursors.Hand : Cursors.Arrow,
            };
            System.Windows.Automation.AutomationProperties.SetName(button, card.Name + (owned ? " · inspect card" : " · undiscovered"));
            if (owned) button.Click += (_, _) => OpenArt(card);
            grid.Children.Add(button);
        }
        stack.Children.Add(grid);
        return stack;
    }

    private async void OpenPack_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CollectionRarity rarity }) return;
        CollectionActionResult result = controller.OpenCollectionPack(rarity);
        status.Text = result.Message;
        if (!result.Success) return;
        Refresh();
        PlayCue(rarity, true);
        if (!animationsEnabled) { RevealAll(); return; }
        presentation = new CancellationTokenSource();
        CancellationToken token = presentation.Token;
        FrameworkElement pack = (FrameworkElement)openingPack.Content;
        try
        {
            var transform = new RotateTransform();
            pack.RenderTransformOrigin = new Point(.5, .5);
            pack.RenderTransform = transform;
            transform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-3 - (int)rarity, 3 + (int)rarity, TimeSpan.FromMilliseconds(80)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(3 + (int)rarity) });
            await Task.Delay(650 + (int)rarity * 150, token);
            transform.BeginAnimation(RotateTransform.AngleProperty, null);
            transform.Angle = 0;
            for (int i = 0; i < CollectionCatalog.CardsPerPack; i++)
            {
                token.ThrowIfCancellationRequested();
                RevealCard(i, true);
                CollectionRarity cardRarity = CollectionCatalog.Find(result.Opening!.Cards[i].CardId)!.Rarity;
                await Task.Delay(230 + (int)cardRarity * 100, token);
            }
        }
        catch (OperationCanceledException) { }
        finally { pack.RenderTransform = Transform.Identity; }
    }

    private void ShowOpening(CollectionOpening opening)
    {
        StopPresentation();
        openingId = opening.Id;
        revealed.Clear();
        revealSlots.Clear();
        openingCards.Children.Clear();
        openingPack.Content = CollectionArtwork.Pack(opening.PackRarity);
        openingTitle.Text = opening.PackRarity.ToString().ToUpperInvariant() + " PACK";
        openingTitle.Foreground = new SolidColorBrush(CollectionArtwork.RarityColor(opening.PackRarity));
        openingSummary.Text = "Five cards are safely saved. Reveal each card or reveal all.";
        revealAll.IsEnabled = true;
        collect.IsEnabled = false;
        for (int i = 0; i < opening.Cards.Count; i++)
        {
            int index = i;
            var back = new Button { Content = CollectionArtwork.Pack(opening.PackRarity), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(7), Margin = new Thickness(0), ToolTip = $"Reveal card {i + 1}" };
            back.Click += (_, _) => RevealCard(index, animationsEnabled);
            var slot = new ContentControl { Content = back, Width = 176, Height = 260, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            revealSlots.Add(slot);
            openingCards.Children.Add(slot);
        }
    }

    private void RevealCard(int index, bool animate, bool sound = true)
    {
        CollectionOpening? opening = controller.Profile.Collection.PendingOpening;
        if (opening is null || opening.Id != openingId || !revealed.Add(index)) return;
        CollectionDraw draw = opening.Cards[index];
        CollectionCard card = CollectionCatalog.Find(draw.CardId)!;
        bool ultra = CollectionCatalog.Theme(card).UltraRare;
        Border front = CollectionArtwork.Card(card, true, draw.IsNew ? "NEW TO YOUR ALBUM" : $"DUPLICATE · +{draw.DuplicatePoints} PTS", animationsEnabled, scale: .90);
        revealSlots[index].Content = front;
        if (sound) PlayCue(card.Rarity, false);
        if (animate)
        {
            var scale = new ScaleTransform(.05, .95);
            front.RenderTransformOrigin = new Point(.5, .5);
            front.RenderTransform = scale;
            TimeSpan duration = TimeSpan.FromMilliseconds(250 + (int)card.Rarity * 65);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.05, 1, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.95, 1, duration));
            if (card.Rarity >= CollectionRarity.Rare && front.Effect is DropShadowEffect glow)
            {
                glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(ultra ? 58 : 20 + (int)card.Rarity * 5, glow.BlurRadius, TimeSpan.FromMilliseconds(850)));
                AddFoilSweep(front, card.Rarity, ultra);
            }
        }
        if (revealed.Count == CollectionCatalog.CardsPerPack)
        {
            collect.IsEnabled = true;
            revealAll.IsEnabled = false;
            openingSummary.Text = $"{opening.Cards.Count(c => c.IsNew)} new cards · +{opening.PointsEarned} duplicate points · {opening.PacksEarned} bonus packs. Album progress is saved.";
        }
    }

    private void RevealAll()
    {
        StopPresentation();
        CollectionOpening? opening = controller.Profile.Collection.PendingOpening;
        if (opening is null) return;
        if (revealed.Count < opening.Cards.Count)
            PlayCue(opening.Cards.Select(c => CollectionCatalog.Find(c.CardId)!.Rarity).Max(), false);
        for (int i = 0; i < opening.Cards.Count; i++) RevealCard(i, false, false);
    }

    private void Collect_Click(object sender, RoutedEventArgs e)
    {
        CollectionActionResult result = controller.CompleteCollectionOpening(openingId);
        status.Text = result.Message;
        if (!result.Success) return;
        StopPresentation();
        openingId = Guid.Empty;
        Refresh();
    }

    private void OpenArt(CollectionCard card)
    {
        if (!controller.Profile.Collection.OwnedCardIds.Contains(card.Id)) return;
        previousFocus = Keyboard.FocusedElement;
        CollectionTheme theme = CollectionCatalog.Theme(card);
        artTitle.Text = card.Name;
        artEdition.Text = $"{theme.Name} · {card.Rarity}" + (theme.UltraRare ? " · ULTRA RARE" : string.Empty);
        artHost.Content = CollectionArtwork.Card(card, true, animationsEnabled: animationsEnabled, scale: InspectorCardScale);
        loreTheme.Text = theme.Name.ToUpperInvariant();
        loreCard.Text = card.Name;
        loreBody.Text = "This card's story has not been catalogued yet.\n\nThe archive panel is ready for the dedicated theme and card lore pass.";
        browseSurface.IsEnabled = false;
        openingOverlay.IsEnabled = false;
        artOverlay.Visibility = Visibility.Visible;
        artClose.Focus();
    }

    private void CloseArt()
    {
        artOverlay.Visibility = Visibility.Collapsed;
        artHost.Content = null;
        openingOverlay.IsEnabled = true;
        browseSurface.IsEnabled = controller.Profile.Collection.PendingOpening is null;
        if (previousFocus is UIElement { IsVisible: true, IsEnabled: true } element) element.Focus();
        previousFocus = null;
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (artOverlay.Visibility == Visibility.Visible)
        {
            CloseArt();
            e.Handled = true;
            return;
        }
        if (openingOverlay.Visibility != Visibility.Visible)
        {
            close();
            e.Handled = true;
        }
    }

    private void StopPresentation()
    {
        presentation?.Cancel();
        presentation?.Dispose();
        presentation = null;
        CasinoAudio.Mixer.Stop(audioOwner);
    }

    private void PlayCue(CollectionRarity rarity, bool opening)
    {
        if (!soundsEnabled || !MenuAudio.Enabled) return;
        if (!Cues.TryGetValue((rarity, opening), out AudioClip? clip))
            Cues[(rarity, opening)] = clip = CollectionSoundSynthesis.Build(rarity, opening);
        CasinoAudio.Play(clip, audioOwner, .8);
    }

    private static void AddFoilSweep(Border card, CollectionRarity rarity, bool ultra)
    {
        var content = (Grid)card.Child;
        var sweep = new Border
        {
            IsHitTestVisible = false,
            Opacity = .7,
            Background = new LinearGradientBrush(new GradientStopCollection
            {
                new(Colors.Transparent, 0), new(Colors.Transparent, .38),
                new(Color.FromArgb(ultra ? (byte)170 : (byte)100, 255, 240, 205), .5),
                new(Colors.Transparent, .62), new(Colors.Transparent, 1),
            }, 25),
        };
        Grid.SetRowSpan(sweep, 3);
        content.Children.Add(sweep);
        var shift = new TranslateTransform();
        sweep.RenderTransform = shift;
        var animation = new DoubleAnimation(-210, 210, TimeSpan.FromMilliseconds(550 + (int)rarity * 100));
        animation.Completed += (_, _) => content.Children.Remove(sweep);
        shift.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private static string PackOdds(CollectionPack pack)
    {
        string odds = string.Join(" · ", Enum.GetValues<CollectionRarity>().Select(r => $"{r} {pack.Weights[(int)r] / 10d:0.#}%"));
        return $"Five cards. One guaranteed {pack.GuaranteedRarity} or better. Other regular slots: {odds}. Each slot first has a {pack.UltraRareChance:P2} chance for an ultra rare theme.";
    }

    private static Button AlbumButton(string name, string content, Thickness padding) => new()
    {
        Name = name,
        Content = content,
        Padding = padding,
        Margin = new Thickness(4),
        Background = Brush("#123322"),
        Foreground = TextBrush,
        BorderBrush = Brush("#466552"),
        BorderThickness = new Thickness(1),
        FontWeight = FontWeights.SemiBold,
        Cursor = CasinoCursors.Hand,
    };

    private static void ConfigureActionButton(Button button)
    {
        button.Padding = new Thickness(20, 9, 20, 9);
        button.Margin = new Thickness(6);
        button.Background = Brush("#123322");
        button.Foreground = TextBrush;
        button.BorderBrush = Brush("#466552");
        button.BorderThickness = new Thickness(1);
        button.FontWeight = FontWeights.SemiBold;
        button.Cursor = CasinoCursors.Hand;
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}

internal static class CollectionAlbumGridExtensions
{
    internal static int GridRow
    {
        set { }
    }
    internal static int GridColumn
    {
        set { }
    }
}
