using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Dedicated collector surface hosted by the casino meta-page stack. Regular themes use a
/// fixed 7 + 3 card layout at the 1920x1080 desktop baseline; pack staging, reveals and card
/// inspection stay inside this surface so the collection never needs a separate native window.
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
    private readonly TextBlock duplicatePoints = new();
    private readonly ProgressBar duplicateProgress = new() { Minimum = 0, Maximum = CollectionCatalog.DuplicatePackCost };
    private readonly TextBlock themeTitle = new();
    private readonly TextBlock themeProgress = new();

    private readonly Border openingOverlay = new();
    private readonly Border openingStage = new();
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
    private readonly Border artCardStage = new();
    private readonly Border lorePanel = new();
    private readonly TextBlock loreTheme = new();
    private readonly TextBlock loreCard = new();
    private readonly TextBlock loreMeta = new();
    private readonly TextBlock loreBody = new() { Name = "AlbumLoreBody" };
    private readonly Button artClose = new() { Name = "AlbumArtClose", Content = "Back to album" };

    private string activeThemeId = CollectionCatalog.Themes[0].Id;
    private Guid openingId;
    private readonly HashSet<int> revealed = [];
    private readonly List<ContentControl> revealSlots = [];
    private CancellationTokenSource? presentation;
    private readonly object audioOwner = new();
    private IInputElement? previousFocus;

    private CollectionRarity? stagedPackRarity;
    private bool stagingPack;
    private int queuedPacksRemaining;
    private int queuedPackTotal;
    private int queuedPackIndex;

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
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var header = new Grid { Margin = new Thickness(22, 9, 22, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var heading = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        heading.Children.Add(new TextBlock
        {
            Text = "THE COLLECTOR'S ALBUM",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = GoldBrush,
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Every table leaves a story behind.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 22,
            Foreground = Brush("#F5E8D2"),
            Margin = new Thickness(0, 2, 0, 0),
        });
        header.Children.Add(heading);

        var back = AlbumButton("AlbumBackButton", "Back", new Thickness(20, 8, 20, 8));
        back.MinHeight = 38;
        back.VerticalAlignment = VerticalAlignment.Center;
        back.Click += (_, _) => close();
        Grid.SetColumn(back, 1);
        header.Children.Add(back);
        root.Children.Add(header);

        var overview = new Border
        {
            Margin = new Thickness(22, 0, 22, 10),
            Padding = new Thickness(14, 9, 14, 9),
            Background = Brush("#0A2118"),
            BorderBrush = PanelBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 10, ShadowDepth = 2, Opacity = .18 },
        };
        Grid.SetRow(overview, 1);

        var overviewGrid = new Grid();
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition());
        overviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });

        summary.Name = "AlbumSummary";
        summary.FontSize = 12;
        summary.FontWeight = FontWeights.SemiBold;
        summary.VerticalAlignment = VerticalAlignment.Center;
        overviewGrid.Children.Add(summary);

        var duplicate = new StackPanel { Margin = new Thickness(18, 0, 0, 0) };
        Grid.SetColumn(duplicate, 1);
        duplicate.Children.Add(new TextBlock
        {
            Text = "DUPLICATE RECYCLING",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#C494FF"),
        });
        duplicateProgress.Height = 6;
        duplicateProgress.Margin = new Thickness(0, 4, 0, 3);
        duplicateProgress.Foreground = Brush("#C494FF");
        duplicateProgress.Background = Brush("#17251F");
        duplicate.Children.Add(duplicateProgress);

        duplicatePoints.Name = "AlbumDuplicatePoints";
        duplicatePoints.FontSize = 10;
        duplicatePoints.FontWeight = FontWeights.SemiBold;
        duplicatePoints.Foreground = Brush("#D7C4F5");
        duplicate.Children.Add(duplicatePoints);

        status.Name = "AlbumCollectionStatus";
        status.Foreground = MutedBrush;
        status.FontSize = 9.5;
        status.TextWrapping = TextWrapping.Wrap;
        status.Margin = new Thickness(0, 2, 0, 0);
        status.Text = "Duplicates automatically recycle into Common packs.";
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

        var themesPanel = new Border
        {
            Background = PanelBrush,
            BorderBrush = PanelBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8),
        };
        var themesGrid = new Grid();
        themesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        themesGrid.RowDefinitions.Add(new RowDefinition());
        themesGrid.Children.Add(new TextBlock
        {
            Text = "COLLECTIONS",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = GoldBrush,
            Margin = new Thickness(8, 4, 8, 8),
        });
        themeList.Name = "AlbumThemes";
        var themeScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = themeList,
        };
        Grid.SetRow(themeScroll, 1);
        themesGrid.Children.Add(themeScroll);
        themesPanel.Child = themesGrid;
        browseSurface.Children.Add(themesPanel);

        var content = new Grid();
        Grid.SetColumn(content, 2);
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition());
        browseSurface.Children.Add(content);

        var packs = new Border
        {
            Background = PanelBrush,
            BorderBrush = PanelBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 8),
        };
        var packGrid = new Grid();
        packGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        packGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        packGrid.Children.Add(new TextBlock
        {
            Text = "YOUR PACKS",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = GoldBrush,
            Margin = new Thickness(2, 0, 0, 5),
        });
        packStrip.Name = "AlbumPacks";
        Grid.SetRow(packStrip, 1);
        packGrid.Children.Add(packStrip);
        packs.Child = packGrid;
        content.Children.Add(packs);

        var themeHeader = new Grid { Margin = new Thickness(4, 2, 4, 5) };
        Grid.SetRow(themeHeader, 1);
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
        Grid.SetRow(cardArea, 2);
        cardArea.VerticalAlignment = VerticalAlignment.Top;
        content.Children.Add(cardArea);

        BuildOpeningOverlay();
        BuildArtOverlay();
        return root;
    }

    private void BuildOpeningOverlay()
    {
        openingOverlay.Name = "AlbumOpeningOverlay";
        openingOverlay.Background = new LinearGradientBrush(Color.FromRgb(5, 12, 20), Color.FromRgb(7, 20, 21), 75);
        openingOverlay.BorderBrush = Brush("#5C5272");
        openingOverlay.BorderThickness = new Thickness(1);
        openingOverlay.CornerRadius = new CornerRadius(12);
        openingOverlay.Padding = new Thickness(26, 20, 26, 18);
        openingOverlay.Visibility = Visibility.Collapsed;
        Panel.SetZIndex(openingOverlay, 20);
        Grid.SetRowSpan(openingOverlay, 3);

        var rootGrid = new Grid();
        var ambient = new Grid { IsHitTestVisible = false };
        ambient.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 760,
            Height = 420,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Fill = new SolidColorBrush(Color.FromArgb(24, 126, 221, 184)),
            Effect = new BlurEffect { Radius = 85 },
        });
        ambient.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 430,
            Height = 430,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Fill = new SolidColorBrush(Color.FromArgb(18, 196, 148, 255)),
            Effect = new BlurEffect { Radius = 90 },
        });
        rootGrid.Children.Add(ambient);

        var shell = new Border
        {
            MaxWidth = 1320,
            Margin = new Thickness(24, 12, 24, 8),
            Padding = new Thickness(34, 24, 34, 22),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new LinearGradientBrush(Color.FromRgb(8, 19, 27), Color.FromRgb(9, 28, 24), 90),
            BorderBrush = Brush("#415E59"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 28, ShadowDepth = 8, Opacity = .42 },
        };
        rootGrid.Children.Add(shell);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.Child = grid;

        var head = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        head.Children.Add(new TextBlock
        {
            Text = "MIDNIGHT ARCHIVE · SEALED EDITIONS",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#8EA9A1"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
        });
        openingTitle.FontFamily = new FontFamily("Georgia");
        openingTitle.FontSize = 29;
        openingTitle.FontWeight = FontWeights.Bold;
        openingTitle.HorizontalAlignment = HorizontalAlignment.Center;
        head.Children.Add(openingTitle);

        var packPedestal = new Border
        {
            Margin = new Thickness(0, 8, 0, 5),
            Padding = new Thickness(10, 4, 10, 4),
            Background = Brush("#B40B171B"),
            BorderBrush = Brush("#314C48"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            HorizontalAlignment = HorizontalAlignment.Center,
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 14, ShadowDepth = 4, Opacity = .35 },
            Child = openingPack,
        };
        head.Children.Add(packPedestal);

        openingSummary.Foreground = Brush("#C5D2CF");
        openingSummary.TextAlignment = TextAlignment.Center;
        openingSummary.TextWrapping = TextWrapping.Wrap;
        openingSummary.MaxWidth = 780;
        openingSummary.FontSize = 11.5;
        openingSummary.Margin = new Thickness(8, 4, 8, 10);
        head.Children.Add(openingSummary);
        grid.Children.Add(head);

        openingStage.Name = "AlbumOpeningStage";
        openingStage.Margin = new Thickness(8, 8, 8, 14);
        openingStage.Padding = new Thickness(22, 24, 22, 24);
        openingStage.Background = new LinearGradientBrush(Color.FromRgb(10, 25, 30), Color.FromRgb(7, 16, 22), 90);
        openingStage.BorderBrush = Brush("#35504F");
        openingStage.BorderThickness = new Thickness(1);
        openingStage.CornerRadius = new CornerRadius(16);
        openingStage.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 18, ShadowDepth = 5, Opacity = .28 };
        openingCards.VerticalAlignment = VerticalAlignment.Center;
        openingCards.HorizontalAlignment = HorizontalAlignment.Center;
        openingStage.Child = openingCards;
        Grid.SetRow(openingStage, 1);
        grid.Children.Add(openingStage);

        var actionFrame = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(10, 2, 10, 2),
            Background = Brush("#9A0A1A18"),
            BorderBrush = Brush("#2F4D45"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
        };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        ConfigureActionButton(revealAll);
        ConfigureActionButton(collect);
        revealAll.Click += OpeningPrimary_Click;
        collect.Click += OpeningSecondary_Click;
        actions.Children.Add(revealAll);
        actions.Children.Add(collect);
        actionFrame.Child = actions;
        Grid.SetRow(actionFrame, 2);
        grid.Children.Add(actionFrame);

        openingOverlay.Child = rootGrid;
        root.Children.Add(openingOverlay);
    }

    private void BuildArtOverlay()
    {
        artOverlay.Name = "AlbumArtOverlay";
        artOverlay.Background = new LinearGradientBrush(Color.FromRgb(5, 11, 18), Color.FromRgb(7, 18, 22), 78);
        artOverlay.BorderBrush = Brush("#796744");
        artOverlay.BorderThickness = new Thickness(1);
        artOverlay.CornerRadius = new CornerRadius(12);
        artOverlay.Padding = new Thickness(28, 18, 28, 18);
        artOverlay.Visibility = Visibility.Collapsed;
        Panel.SetZIndex(artOverlay, 30);
        Grid.SetRowSpan(artOverlay, 3);

        var rootGrid = new Grid();
        var ambient = new Grid { IsHitTestVisible = false };
        ambient.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 620,
            Height = 620,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(110, 0, 0, 0),
            Fill = new SolidColorBrush(Color.FromArgb(18, 226, 196, 123)),
            Effect = new BlurEffect { Radius = 95 },
        });
        ambient.Children.Add(new System.Windows.Shapes.Ellipse
        {
            Width = 720,
            Height = 460,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 80, 0),
            Fill = new SolidColorBrush(Color.FromArgb(14, 85, 174, 174)),
            Effect = new BlurEffect { Radius = 100 },
        });
        rootGrid.Children.Add(ambient);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.Children.Add(grid);

        var head = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 4) };
        head.Children.Add(new TextBlock
        {
            Text = "COLLECTOR'S ARCHIVE",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#A99872"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        artTitle.FontFamily = new FontFamily("Georgia");
        artTitle.FontSize = 25;
        artTitle.Foreground = Brush("#FFE5B2");
        artTitle.TextAlignment = TextAlignment.Center;
        head.Children.Add(artTitle);
        artEdition.FontSize = 10.5;
        artEdition.Foreground = Brush("#A9BBC2");
        artEdition.TextAlignment = TextAlignment.Center;
        artEdition.Margin = new Thickness(0, 3, 0, 0);
        head.Children.Add(artEdition);
        grid.Children.Add(head);

        var body = new Grid
        {
            Margin = new Thickness(28, 10, 28, 10),
            MaxWidth = 1440,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56, GridUnitType.Star) });

        artCardStage.Name = "AlbumArtCardStage";
        artCardStage.Margin = new Thickness(18, 16, 34, 16);
        artCardStage.Padding = new Thickness(34, 28, 34, 24);
        artCardStage.HorizontalAlignment = HorizontalAlignment.Center;
        artCardStage.VerticalAlignment = VerticalAlignment.Center;
        artCardStage.Background = new LinearGradientBrush(Color.FromRgb(11, 20, 27), Color.FromRgb(6, 13, 19), 90);
        artCardStage.BorderBrush = Brush("#5D5848");
        artCardStage.BorderThickness = new Thickness(1);
        artCardStage.CornerRadius = new CornerRadius(18);
        artCardStage.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 25, ShadowDepth = 8, Opacity = .45 };

        var cardStageStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        cardStageStack.Children.Add(new TextBlock
        {
            Text = "DISCOVERED EDITION",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#988B70"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        });
        artHost.HorizontalAlignment = HorizontalAlignment.Center;
        artHost.VerticalAlignment = VerticalAlignment.Center;
        cardStageStack.Children.Add(artHost);
        cardStageStack.Children.Add(new TextBlock
        {
            Text = "MIDNIGHT CLUB COLLECTION",
            FontSize = 8.5,
            Foreground = Brush("#71858A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
        });
        artCardStage.Child = cardStageStack;
        body.Children.Add(artCardStage);

        lorePanel.Name = "AlbumLorePanel";
        lorePanel.Margin = new Thickness(20, 28, 20, 28);
        lorePanel.Padding = new Thickness(32, 28, 32, 30);
        lorePanel.Background = new LinearGradientBrush(Color.FromRgb(11, 28, 34), Color.FromRgb(7, 19, 25), 90);
        lorePanel.BorderBrush = Brush("#47636B");
        lorePanel.BorderThickness = new Thickness(1);
        lorePanel.CornerRadius = new CornerRadius(16);
        lorePanel.VerticalAlignment = VerticalAlignment.Center;
        lorePanel.MaxWidth = 720;
        lorePanel.MinWidth = 500;
        lorePanel.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 22, ShadowDepth = 7, Opacity = .4 };
        Grid.SetColumn(lorePanel, 1);

        var loreStack = new StackPanel();
        var loreHead = new Grid();
        loreHead.ColumnDefinitions.Add(new ColumnDefinition());
        loreHead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        loreHead.Children.Add(new TextBlock
        {
            Text = "ARCHIVE ENTRY",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = GoldBrush,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var seal = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            BorderBrush = Brush("#6E806F"),
            BorderThickness = new Thickness(1),
            Background = Brush("#13272A"),
            Child = new TextBlock
            {
                Text = "✦",
                FontSize = 12,
                Foreground = GoldBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        Grid.SetColumn(seal, 1);
        loreHead.Children.Add(seal);
        loreStack.Children.Add(loreHead);

        loreStack.Children.Add(new Border
        {
            Height = 1,
            Margin = new Thickness(0, 12, 0, 14),
            Background = new LinearGradientBrush(BrushColor("#7E6A42"), Colors.Transparent, 0),
            Opacity = .8,
        });

        loreTheme.FontSize = 11.5;
        loreTheme.FontWeight = FontWeights.SemiBold;
        loreTheme.Foreground = Brush("#91A9B7");
        loreStack.Children.Add(loreTheme);

        loreCard.FontFamily = new FontFamily("Georgia");
        loreCard.FontSize = 30;
        loreCard.Foreground = Brush("#F4E6CF");
        loreCard.Margin = new Thickness(0, 2, 0, 4);
        loreStack.Children.Add(loreCard);

        loreMeta.FontSize = 9.5;
        loreMeta.Foreground = Brush("#82959D");
        loreMeta.Margin = new Thickness(0, 0, 0, 16);
        loreStack.Children.Add(loreMeta);

        loreBody.FontFamily = new FontFamily("Georgia");
        loreBody.FontSize = 15.5;
        loreBody.LineHeight = 25;
        loreBody.Foreground = Brush("#D0D9DA");
        loreBody.TextWrapping = TextWrapping.Wrap;
        loreStack.Children.Add(loreBody);

        lorePanel.Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = loreStack,
        };
        body.Children.Add(lorePanel);
        Grid.SetRow(body, 1);
        grid.Children.Add(body);

        ConfigureActionButton(artClose);
        artClose.HorizontalAlignment = HorizontalAlignment.Center;
        artClose.Click += (_, _) => CloseArt();
        Grid.SetRow(artClose, 2);
        grid.Children.Add(artClose);

        artOverlay.Child = rootGrid;
        root.Children.Add(artOverlay);
    }

    private void Refresh()
    {
        controller.AccruePlaytime();
        CollectionState state = controller.Profile.Collection;
        int complete = CollectionCatalog.Themes.Count(t => t.Cards.All(c => state.OwnedCardIds.Contains(c.Id)));

        summary.Text = $"{state.OwnedCardIds.Count} / {CollectionCatalog.Cards.Count} cards   ·   {complete} / {CollectionCatalog.Themes.Count} themes complete   ·   {state.PacksOpened:N0} packs opened";
        duplicateProgress.Value = state.DuplicatePoints;
        duplicatePoints.Text = $"{state.DuplicatePoints} / {CollectionCatalog.DuplicatePackCost} duplicate points";

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
            stagingPack = false;
            if (opening.Id != openingId) ShowOpening(opening);
            openingOverlay.Visibility = Visibility.Visible;
            browseSurface.IsEnabled = false;
        }
        else if (stagingPack)
        {
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
            content.Children.Add(new TextBlock
            {
                Text = theme.Symbol,
                FontSize = 15,
                Foreground = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var name = new TextBlock
            {
                Text = theme.Name,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(name, 1);
            content.Children.Add(name);

            var progress = new TextBlock
            {
                Text = $"{owned}/{theme.Cards.Count}",
                FontSize = 10,
                Foreground = owned == theme.Cards.Count ? new SolidColorBrush(accent) : MutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };
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
            button.Click += (_, _) =>
            {
                activeThemeId = theme.Id;
                Refresh();
            };
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
            content.Children.Add(new TextBlock
            {
                Text = "✦",
                FontSize = 16,
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            labels.Children.Add(new TextBlock
            {
                Text = pack.Rarity.ToString().ToUpperInvariant(),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
            });
            labels.Children.Add(new TextBlock { Text = $"{count} available", FontSize = 10, Foreground = TextBrush });
            Grid.SetColumn(labels, 1);
            content.Children.Add(labels);

            var action = new TextBlock
            {
                Text = count > 0 && state.PendingOpening is null ? "VIEW →" : "—",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = count > 0 ? GoldBrush : MutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 2, 0),
            };
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
            button.Click += (_, _) => StagePack(pack.Rarity);
            packStrip.Children.Add(button);
        }
    }

    private void StagePack(CollectionRarity rarity)
    {
        CollectionState state = controller.Profile.Collection;
        int available = state.Packs.GetValueOrDefault(rarity);
        if (state.PendingOpening is not null || available <= 0) return;

        StopPresentation();
        stagedPackRarity = rarity;
        stagingPack = true;
        queuedPacksRemaining = 0;
        queuedPackTotal = 0;
        queuedPackIndex = 0;
        openingId = Guid.Empty;
        revealed.Clear();
        revealSlots.Clear();
        openingCards.Children.Clear();

        Color color = CollectionArtwork.RarityColor(rarity);
        openingOverlay.BorderBrush = new SolidColorBrush(Color.FromArgb(150, color.R, color.G, color.B));
        openingStage.BorderBrush = new SolidColorBrush(Color.FromArgb(135, color.R, color.G, color.B));
        openingTitle.Text = rarity.ToString().ToUpperInvariant() + " PACK";
        openingTitle.Foreground = new SolidColorBrush(color);
        openingPack.Content = CollectionArtwork.Pack(rarity);
        openingSummary.Text = available == 1
            ? "One sealed pack is ready. Choose how you want to open it."
            : $"{available} sealed packs are ready. Open one now or queue all {available} packs.";

        for (int i = 0; i < CollectionCatalog.CardsPerPack; i++)
        {
            openingCards.Children.Add(new ContentControl
            {
                Content = CreateConcealedCard(rarity, interactive: false),
                Width = 176,
                Height = 260,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            });
        }

        revealAll.Content = "Open 1 Pack";
        revealAll.IsEnabled = true;
        collect.Content = available == 1 ? "Open All Packs" : $"Open All Packs ({available})";
        collect.IsEnabled = true;
        openingOverlay.Visibility = Visibility.Visible;
        browseSurface.IsEnabled = false;
        revealAll.Focus();
    }

    private void OpeningPrimary_Click(object sender, RoutedEventArgs e)
    {
        if (stagingPack)
        {
            StartPackSequence(openAll: false);
            return;
        }
        RevealAll();
    }

    private void OpeningSecondary_Click(object sender, RoutedEventArgs e)
    {
        if (stagingPack)
        {
            StartPackSequence(openAll: true);
            return;
        }
        CompleteCurrentOpening();
    }

    private void StartPackSequence(bool openAll)
    {
        if (stagedPackRarity is not CollectionRarity rarity) return;
        int available = controller.Profile.Collection.Packs.GetValueOrDefault(rarity);
        if (available <= 0)
        {
            stagingPack = false;
            status.Text = "That pack is no longer available.";
            Refresh();
            return;
        }

        queuedPackTotal = openAll ? available : 1;
        queuedPacksRemaining = queuedPackTotal;
        queuedPackIndex = 0;
        stagingPack = false;
        OpenNextQueuedPack();
    }

    private void OpenNextQueuedPack()
    {
        if (stagedPackRarity is not CollectionRarity rarity || queuedPacksRemaining <= 0)
        {
            ResetPackSequence();
            Refresh();
            return;
        }

        CollectionActionResult result = controller.OpenCollectionPack(rarity);
        status.Text = result.Message;
        if (!result.Success || result.Opening is null)
        {
            ResetPackSequence();
            Refresh();
            return;
        }

        queuedPacksRemaining--;
        queuedPackIndex++;
        ShowOpening(result.Opening);
        openingOverlay.Visibility = Visibility.Visible;
        browseSurface.IsEnabled = false;
        PlayCue(rarity, true);
    }

    private void ShowOpening(CollectionOpening opening)
    {
        StopPresentation();
        stagingPack = false;
        stagedPackRarity ??= opening.PackRarity;
        openingId = opening.Id;
        revealed.Clear();
        revealSlots.Clear();
        openingCards.Children.Clear();

        Color color = CollectionArtwork.RarityColor(opening.PackRarity);
        openingOverlay.BorderBrush = new SolidColorBrush(Color.FromArgb(150, color.R, color.G, color.B));
        openingStage.BorderBrush = new SolidColorBrush(Color.FromArgb(135, color.R, color.G, color.B));
        openingPack.Content = CollectionArtwork.Pack(opening.PackRarity);
        openingTitle.Text = opening.PackRarity.ToString().ToUpperInvariant() + " PACK";
        openingTitle.Foreground = new SolidColorBrush(color);
        openingSummary.Text = queuedPackTotal > 1
            ? $"Pack {queuedPackIndex} of {queuedPackTotal} is safely saved. Reveal each card or reveal all."
            : "Five cards are safely saved. Reveal each card or reveal all.";

        revealAll.Content = "Reveal all";
        revealAll.IsEnabled = true;
        collect.Content = queuedPacksRemaining > 0 ? "Next pack" : "Continue collecting";
        collect.IsEnabled = false;

        for (int i = 0; i < opening.Cards.Count; i++)
        {
            int index = i;
            Border concealed = CreateConcealedCard(opening.PackRarity, interactive: true);
            var button = new Button
            {
                Content = concealed,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                ToolTip = $"Reveal card {i + 1}",
                Cursor = CasinoCursors.Hand,
                OverridesDefaultStyle = true,
                Template = BareButtonTemplate(),
            };
            System.Windows.Automation.AutomationProperties.SetName(button, $"Reveal collection card {i + 1}");
            button.Click += (_, _) => RevealCard(index, animationsEnabled);

            var slot = new ContentControl
            {
                Content = button,
                Width = 176,
                Height = 260,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            revealSlots.Add(slot);
            openingCards.Children.Add(slot);
        }
    }

    private static Border CreateConcealedCard(CollectionRarity rarity, bool interactive)
    {
        Color color = CollectionArtwork.RarityColor(rarity);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.Children.Add(new TextBlock
        {
            Text = new string('✦', 1 + (int)rarity),
            FontSize = 9.5,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 13, 0, 0),
        });

        var sigil = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        sigil.Children.Add(new TextBlock
        {
            Text = "♠",
            FontSize = 50,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        sigil.Children.Add(new TextBlock
        {
            Text = "SEALED",
            FontSize = 8,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromArgb(190, color.R, color.G, color.B)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
        });
        Grid.SetRow(sigil, 1);
        grid.Children.Add(sigil);

        var sealLine = new Border
        {
            Height = 1,
            Margin = new Thickness(20, 0, 20, 16),
            Background = new SolidColorBrush(Color.FromArgb(100, color.R, color.G, color.B)),
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        Grid.SetRow(sealLine, 2);
        grid.Children.Add(sealLine);

        var card = new Border
        {
            Width = 148,
            Height = 222,
            Margin = new Thickness(10),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1.2 + (int)rarity * .25),
            BorderBrush = new SolidColorBrush(Color.FromArgb(210, color.R, color.G, color.B)),
            Background = new LinearGradientBrush(Color.FromRgb(17, 31, 43),
                Color.FromRgb((byte)Math.Max(9, color.R / 5), (byte)Math.Max(12, color.G / 5), (byte)Math.Max(15, color.B / 5)), 45),
            Child = grid,
            Effect = new DropShadowEffect
            {
                Color = color,
                BlurRadius = rarity >= CollectionRarity.Rare ? 14 : 9,
                ShadowDepth = 0,
                Opacity = interactive ? .25 : .16,
            },
            RenderTransformOrigin = new Point(.5, .5),
        };

        if (interactive)
        {
            var scale = new ScaleTransform(1, 1);
            card.RenderTransform = scale;
            card.MouseEnter += (_, _) =>
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 1.025, TimeSpan.FromMilliseconds(120)));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 1.025, TimeSpan.FromMilliseconds(120)));
            };
            card.MouseLeave += (_, _) =>
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scale.ScaleX, 1, TimeSpan.FromMilliseconds(120)));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scale.ScaleY, 1, TimeSpan.FromMilliseconds(120)));
            };
        }

        return card;
    }

    private static ControlTemplate BareButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        template.VisualTree = presenter;
        return template;
    }

    private void RevealCard(int index, bool animate, bool sound = true)
    {
        CollectionOpening? opening = controller.Profile.Collection.PendingOpening;
        if (opening is null || opening.Id != openingId || !revealed.Add(index)) return;

        CollectionDraw draw = opening.Cards[index];
        CollectionCard card = CollectionCatalog.Find(draw.CardId)!;
        bool ultra = CollectionCatalog.Theme(card).UltraRare;
        Border front = CollectionArtwork.Card(
            card,
            true,
            draw.IsNew ? "NEW TO YOUR ALBUM" : $"DUPLICATE · +{draw.DuplicatePoints} PTS",
            animationsEnabled,
            scale: .90);
        revealSlots[index].Content = front;

        if (sound) PlayCue(card.Rarity, false);
        if (animate)
        {
            var scale = new ScaleTransform(.05, .95);
            front.RenderTransformOrigin = new Point(.5, .5);
            front.RenderTransform = scale;
            TimeSpan duration = TimeSpan.FromMilliseconds(250 + (int)card.Rarity * 65);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.05, 1, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.95, 1, duration));

            if (card.Rarity >= CollectionRarity.Rare && front.Effect is DropShadowEffect glow)
            {
                glow.BeginAnimation(
                    DropShadowEffect.BlurRadiusProperty,
                    new DoubleAnimation(ultra ? 58 : 20 + (int)card.Rarity * 5, glow.BlurRadius, TimeSpan.FromMilliseconds(850)));
                AddFoilSweep(front, card.Rarity, ultra);
            }
        }

        if (revealed.Count == CollectionCatalog.CardsPerPack)
        {
            collect.IsEnabled = true;
            revealAll.IsEnabled = false;
            openingSummary.Text = queuedPackTotal > 1 && queuedPacksRemaining > 0
                ? $"{opening.Cards.Count(c => c.IsNew)} new cards · +{opening.PointsEarned} duplicate points · {opening.PacksEarned} bonus packs. Pack {queuedPackIndex} of {queuedPackTotal} is saved — continue to the next pack."
                : $"{opening.Cards.Count(c => c.IsNew)} new cards · +{opening.PointsEarned} duplicate points · {opening.PacksEarned} bonus packs. Album progress is saved.";
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

    private void CompleteCurrentOpening()
    {
        CollectionActionResult result = controller.CompleteCollectionOpening(openingId);
        status.Text = result.Message;
        if (!result.Success) return;

        StopPresentation();
        openingId = Guid.Empty;
        if (queuedPacksRemaining > 0)
        {
            OpenNextQueuedPack();
            return;
        }

        ResetPackSequence();
        Refresh();
    }

    private void ResetPackSequence()
    {
        stagingPack = false;
        stagedPackRarity = null;
        queuedPacksRemaining = 0;
        queuedPackTotal = 0;
        queuedPackIndex = 0;
        revealAll.Content = "Reveal all";
        collect.Content = "Continue collecting";
    }

    private void CancelPackStaging()
    {
        if (!stagingPack || controller.Profile.Collection.PendingOpening is not null) return;
        stagingPack = false;
        stagedPackRarity = null;
        openingOverlay.Visibility = Visibility.Collapsed;
        browseSurface.IsEnabled = true;
        openingCards.Children.Clear();
        Refresh();
    }

    private void BuildCards(CollectionTheme theme, CollectionState state)
    {
        cardArea.Children.Clear();
        cardArea.RowDefinitions.Clear();
        cardArea.ColumnDefinitions.Clear();

        if (theme.UltraRare)
        {
            cardArea.RowDefinitions.Add(new RowDefinition());
            FrameworkElement group = BuildCardGroup("ULTRA RARE · LEGENDARY EDITIONS", theme.Cards, state, CollectionRarity.Legendary, 4);
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

        FrameworkElement common = BuildCardGroup(
            "COMMON",
            theme.Cards.Where(c => c.Rarity == CollectionRarity.Common).ToArray(),
            state,
            CollectionRarity.Common,
            4);
        upper.Children.Add(common);

        FrameworkElement uncommon = BuildCardGroup(
            "UNCOMMON",
            theme.Cards.Where(c => c.Rarity == CollectionRarity.Uncommon).ToArray(),
            state,
            CollectionRarity.Uncommon,
            3);
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
            FrameworkElement group = BuildCardGroup(
                rarity.ToString().ToUpperInvariant(),
                theme.Cards.Where(c => c.Rarity == rarity).ToArray(),
                state,
                rarity,
                1);
            Grid.SetColumn(group, i * 2);
            lower.Children.Add(group);
        }

        Grid.SetRow(lower, 2);
        cardArea.Children.Add(lower);
    }

    private FrameworkElement BuildCardGroup(
        string label,
        IReadOnlyList<CollectionCard> cards,
        CollectionState state,
        CollectionRarity rarity,
        int columns)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(CollectionArtwork.RarityColor(rarity)),
            Margin = new Thickness(8, 0, 8, 2),
            TextAlignment = TextAlignment.Center,
        });

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

    private void OpenArt(CollectionCard card)
    {
        if (!controller.Profile.Collection.OwnedCardIds.Contains(card.Id)) return;

        previousFocus = Keyboard.FocusedElement;
        CollectionTheme theme = CollectionCatalog.Theme(card);
        Color rarityColor = CollectionArtwork.RarityColor(card.Rarity);

        artTitle.Text = card.Name;
        artEdition.Text = $"{theme.Name} · {card.Rarity}" + (theme.UltraRare ? " · ULTRA RARE" : string.Empty);
        artHost.Content = CollectionArtwork.Card(card, true, animationsEnabled: animationsEnabled, scale: InspectorCardScale);

        artCardStage.BorderBrush = new SolidColorBrush(Color.FromArgb(155, rarityColor.R, rarityColor.G, rarityColor.B));
        artCardStage.Effect = new DropShadowEffect
        {
            Color = rarityColor,
            BlurRadius = card.Rarity >= CollectionRarity.Rare ? 22 : 15,
            ShadowDepth = 0,
            Opacity = card.Rarity >= CollectionRarity.Rare ? .28 : .16,
        };
        lorePanel.BorderBrush = new SolidColorBrush(Color.FromArgb(115, rarityColor.R, rarityColor.G, rarityColor.B));

        loreTheme.Text = theme.Name.ToUpperInvariant();
        loreCard.Text = card.Name;
        loreMeta.Text = $"CARD {card.Number:00} / {theme.Cards.Count:00}  ·  {card.Rarity.ToString().ToUpperInvariant()} EDITION";
        loreBody.Text = CollectionLore.BodyFor(card);
        System.Windows.Automation.AutomationProperties.SetName(loreBody, card.Name + " archive lore");

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
        browseSurface.IsEnabled = controller.Profile.Collection.PendingOpening is null && !stagingPack;
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

        if (stagingPack && controller.Profile.Collection.PendingOpening is null)
        {
            CancelPackStaging();
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
                new(Colors.Transparent, 0),
                new(Colors.Transparent, .38),
                new(Color.FromArgb(ultra ? (byte)170 : (byte)100, 255, 240, 205), .5),
                new(Colors.Transparent, .62),
                new(Colors.Transparent, 1),
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
        string odds = string.Join(" · ", Enum.GetValues<CollectionRarity>()
            .Select(r => $"{r} {pack.Weights[(int)r] / 10d:0.#}%"));
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
        button.MinWidth = 138;
        button.Padding = new Thickness(20, 10, 20, 10);
        button.Margin = new Thickness(6);
        button.Background = Brush("#123322");
        button.Foreground = TextBrush;
        button.BorderBrush = Brush("#567261");
        button.BorderThickness = new Thickness(1);
        button.FontWeight = FontWeights.SemiBold;
        button.Cursor = CasinoCursors.Hand;
        button.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 8, ShadowDepth = 2, Opacity = .22 };
    }

    private static Color BrushColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static SolidColorBrush Brush(string hex) => new(BrushColor(hex));
}
