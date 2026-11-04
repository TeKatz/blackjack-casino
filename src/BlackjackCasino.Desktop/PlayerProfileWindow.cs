using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;
using Microsoft.Win32;

namespace BlackjackCasino.Desktop;

internal sealed class PlayerProfileWindow : Window
{
    private readonly PlayerProgressionController controller;
    private readonly Action openProgression;
    private readonly Action closePage;
    private readonly Action<string> openStatistics;
    private readonly WrapPanel playerTitlesPanel = new();
    private readonly Grid profileRoot;
    private readonly WrapPanel playerPortraitPanel = new();
    private readonly ContentControl equippedPortraitHost = new();
    private readonly TextBlock playerStatusText = new();
    private readonly TextBlock nameLabel = new();
    private readonly TextBlock levelLabel = new();
    private readonly TextBlock titleLabel = new();
    private readonly TextBlock collectionLabel = new();
    private readonly TextBox nameBox = new();
    private readonly TextBox portraitSearch = new();
    private static readonly Brush MutedBrush = Brush("#A6BBAE");
    private static readonly Brush PositiveBrush = Brush("#8EC781");
    private static readonly Brush CoinBrush = Brush("#54E1B7");

    internal PlayerProfileWindow(PlayerProgressionController controller,
        Action? openProgression = null, Action? closePage = null, Action<string>? openStatistics = null)
    {
        this.controller = controller;
        this.openProgression = openProgression ?? (() => { });
        this.closePage = closePage ?? Close;
        this.openStatistics = openStatistics ?? (_ => { });
        Title = T("WindowTitle");
        Width = 1200; Height = 800; MinWidth = 940; MinHeight = 650;
        WindowStyle = WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new LinearGradientBrush(Color.FromRgb(7, 18, 20), Color.FromRgb(13, 30, 27), 55); Foreground = Brush("#F2EEE4");
        FontFamily = new FontFamily("Segoe UI");
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/BlackjackCasino.Desktop;component/Presentation/CasinoControls.xaml", UriKind.Relative),
        });
        profileRoot = BuildPlayerContent();
        Content = profileRoot;
        MenuAudio.Attach(this);
        RefreshPlayer();
    }

    private static string T(string key) => Localization.T("Shell.Profile." + key);
    private static string F(string key, params object?[] args) => Localization.F("Shell.Profile." + key, args);

    private static Brush Brush(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    private static TextBlock Caption(string text) => new()
    {
        Text = text, Foreground = Brush("#FFD77C"), FontSize = 11, FontWeight = FontWeights.Bold,
    };
    private static Border PanelBorder() => new()
    {
        Background = Brush("#0B2117"), BorderBrush = Brush("#315A43"),
        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(18),
    };
    private Button ActionButton(string text, Action action)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 5, 0, 0) };
        button.Click += (_, _) => action();
        return button;
    }

    private Grid BuildPlayerContent()
    {
        var root = new Grid { Background = Background, Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 18) };
        var back = ActionButton(T("Back"), closePage);
        back.Name = "ProfileBackButton";
        DockPanel.SetDock(back, Dock.Right); heading.Children.Add(back);
        var title = new StackPanel();
        title.Children.Add(Caption(T("Header")));
        title.Children.Add(new TextBlock
        {
            Text = T("Identity"), FontSize = 27, FontWeight = FontWeights.Bold,
            Foreground = Brush("#FFE09B"), Margin = new Thickness(0, 5, 0, 0),
        });
        heading.Children.Add(title); root.Children.Add(heading);

        var body = new Grid(); Grid.SetRow(body, 1); root.Children.Add(body);
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        var summary = PanelBorder(); summary.Margin = new Thickness(0, 0, 16, 0);
        var identity = new StackPanel();
        equippedPortraitHost.Width = 202; equippedPortraitHost.Height = 202;
        equippedPortraitHost.HorizontalAlignment = HorizontalAlignment.Center;
        identity.Children.Add(equippedPortraitHost);
        nameLabel.FontSize = 25; nameLabel.FontWeight = FontWeights.Bold;
        nameLabel.TextWrapping = TextWrapping.Wrap; nameLabel.Margin = new Thickness(0, 14, 0, 0);
        identity.Children.Add(nameLabel);
        titleLabel.FontSize = 15; titleLabel.Margin = new Thickness(0, 5, 0, 0);
        identity.Children.Add(titleLabel);
        levelLabel.FontSize = 20; levelLabel.FontWeight = FontWeights.Bold; levelLabel.LineHeight = 30;
        levelLabel.Foreground = CoinBrush; levelLabel.Margin = new Thickness(0, 5, 0, 18);
        levelLabel.TextWrapping = TextWrapping.Wrap; identity.Children.Add(levelLabel);
        identity.Children.Add(Caption(T("DisplayName")));
        nameBox.Name = "ProfileNameBox"; nameBox.Text = controller.Profile.DisplayName;
        nameBox.MaxLength = 256; nameBox.ToolTip = T("NameHelp");
        nameBox.Margin = new Thickness(0, 8, 0, 0); identity.Children.Add(nameBox);
        var saveName = ActionButton(T("SaveName"), () =>
        {
            bool saved = controller.RenameProfile(nameBox.Text, out string message);
            playerStatusText.Text = saved ? T("NameSaved") : message switch
            {
                "Enter a profile name." => T("NameRequired"),
                "Use at most 24 characters." => T("NameTooLong"),
                _ => Localization.TranslateSource(message),
            };
            nameBox.Text = controller.Profile.DisplayName;
            RefreshPlayer();
        });
        saveName.Name = "RenameProfileButton"; identity.Children.Add(saveName);
        var progression = ActionButton(T("Progression"), openProgression);
        progression.Name = "ProfileProgressionButton";
        var stats = ActionButton(T("Statistics"), () => openStatistics("Overview"));
        stats.Name = "ProfileStatisticsButton";
        stats.Margin = new Thickness(0, 20, 0, 0); identity.Children.Add(stats);
        var history = ActionButton(T("HandHistory"), () => openStatistics("Hand History"));
        history.Name = "ProfileHandHistoryButton"; identity.Children.Add(history);
        progression.Margin = new Thickness(0, 12, 0, 0); identity.Children.Add(progression);
        var export = new Button { Content = T("Export"), Margin = new Thickness(0, 8, 0, 0) };
        export.Click += SaveProgress_Click; identity.Children.Add(export);
        playerStatusText.Foreground = PositiveBrush; playerStatusText.TextWrapping = TextWrapping.Wrap;
        playerStatusText.Margin = new Thickness(0, 14, 0, 0); identity.Children.Add(playerStatusText);
        summary.Child = new ScrollViewer { Content = identity, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        body.Children.Add(summary);

        var collection = PanelBorder(); Grid.SetColumn(collection, 1); body.Children.Add(collection);
        var gallery = new Grid();
        gallery.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        gallery.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        gallery.RowDefinitions.Add(new RowDefinition());
        var galleryHeader = new StackPanel();
        galleryHeader.Children.Add(Caption(T("CollectionHeader")));
        collectionLabel.Foreground = MutedBrush; collectionLabel.Margin = new Thickness(0, 5, 0, 12);
        galleryHeader.Children.Add(collectionLabel); gallery.Children.Add(galleryHeader);
        portraitSearch.Name = "PortraitSearchBox";
        portraitSearch.ToolTip = T("SearchHelp");
        portraitSearch.TextChanged += (_, _) => RefreshPlayer();
        portraitSearch.Margin = new Thickness(0, 0, 0, 12);
        Grid.SetRow(portraitSearch, 1); gallery.Children.Add(portraitSearch);
        var scroll = new ScrollViewer
        {
            Content = BuildIdentityCollections(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Grid.SetRow(scroll, 2); gallery.Children.Add(scroll); collection.Child = gallery;
        return root;
    }

    private UIElement BuildIdentityCollections()
    {
        var stack = new StackPanel();
        stack.Children.Add(Caption(T("Portraits")));
        playerPortraitPanel.Margin = new Thickness(0, 12, 0, 12);
        stack.Children.Add(playerPortraitPanel);
        stack.Children.Add(new Border { Height = 1, Background = Brush("#34534D"), Margin = new Thickness(0, 6, 0, 20) });
        stack.Children.Add(Caption(T("Titles")));
        playerTitlesPanel.Margin = new Thickness(0, 12, 0, 0);
        stack.Children.Add(playerTitlesPanel);
        return stack;
    }

    internal void RefreshPlayer()
    {
        PlayerProfileState profile = controller.Profile;
        nameLabel.Text = profile.DisplayName;
        CosmeticDefinition title = CosmeticCatalog.Get(profile.EquippedTitleId);
        titleLabel.Text = title.Name;
        titleLabel.Foreground = CosmeticAppearance.TitleBrush(title);
        levelLabel.Text = F("LevelSummary", profile.Level, profile.Prestige, profile.Coins);
        equippedPortraitHost.Content = ProgressionWindow.CreatePortraitVisual(CosmeticCatalog.Get(profile.EquippedPortraitId), 198);
        CosmeticDefinition[] owned = CosmeticCatalog.All.Where(x => x.Kind == CosmeticKind.Portrait
            && profile.OwnedCosmeticIds.Contains(x.Id, StringComparer.Ordinal)).ToArray();
        CosmeticDefinition[] titles = CosmeticCatalog.All.Where(x => x.Kind == CosmeticKind.Title
            && profile.OwnedCosmeticIds.Contains(x.Id, StringComparer.Ordinal)).ToArray();
        collectionLabel.Text = F("CollectionSummary", owned.Length, titles.Length);
        playerTitlesPanel.Children.Clear();
        foreach (CosmeticDefinition item in titles
            .Where(x => x.Name.Contains(portraitSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Id == profile.EquippedTitleId).ThenBy(x => x.Name))
        {
            bool equipped = item.Id == profile.EquippedTitleId;
            var card = PanelBorder(); card.Width = 240; card.Margin = new Thickness(0, 0, 12, 12);
            card.BorderBrush = equipped ? CoinBrush : Brush("#315A43");
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "✦  " + item.Name, Foreground = CosmeticAppearance.TitleBrush(item), FontSize = 18, FontFamily = new FontFamily("Georgia"), TextWrapping = TextWrapping.Wrap, MinHeight = 52 });
            var equip = ActionButton(T(equipped ? "Equipped" : "UseTitle"), () =>
            {
                if (controller.EquipCosmetic(item.Id)) { playerStatusText.Text = F("Using", item.Name); RefreshPlayer(); }
            });
            equip.IsEnabled = !equipped;
            stack.Children.Add(equip); card.Child = stack; playerTitlesPanel.Children.Add(card);
        }
        if (playerTitlesPanel.Children.Count == 0)
            playerTitlesPanel.Children.Add(new TextBlock { Text = T("NoTitles"), Foreground = MutedBrush });
        playerPortraitPanel.Children.Clear();
        foreach (CosmeticDefinition portrait in owned
            .Where(x => x.Name.Contains(portraitSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Id == profile.EquippedPortraitId).ThenBy(x => x.Name))
        {
            bool selected = portrait.Id == profile.EquippedPortraitId;
            var card = PanelBorder(); card.Width = 148; card.Padding = new Thickness(10);
            card.Margin = new Thickness(0, 0, 10, 10);
            card.BorderBrush = selected ? CoinBrush : Brush("#315A43");
            var stack = new StackPanel();
            var visual = ProgressionWindow.CreatePortraitVisual(portrait, 92);
            visual.HorizontalAlignment = HorizontalAlignment.Center; stack.Children.Add(visual);
            stack.Children.Add(new TextBlock
            {
                Text = portrait.Name, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center, MinHeight = 34, Margin = new Thickness(0, 8, 0, 4),
            });
            var equip = ActionButton(T(selected ? "Equipped" : "UsePortrait"), () =>
            {
                if (controller.EquipCosmetic(portrait.Id))
                {
                    playerStatusText.Text = F("Using", portrait.Name); RefreshPlayer();
                }
            });
            equip.IsEnabled = !selected; equip.FontSize = 12; equip.Padding = new Thickness(6);
            stack.Children.Add(equip); card.Child = stack; playerPortraitPanel.Children.Add(card);
        }
        if (playerPortraitPanel.Children.Count == 0)
            playerPortraitPanel.Children.Add(new TextBlock { Text = T("NoPortraits"), Foreground = MutedBrush });
    }

    private void SaveProgress_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = T("ExportTitle"),
            FileName = $"blackjack-casino-progress-{DateTime.Now:yyyy-MM-dd}.json",
            DefaultExt = ".json",
            Filter = T("ExportFilter"),
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(Window.GetWindow(profileRoot)) != true)
        {
            return;
        }

        try
        {
            PlayerProfileState profile = controller.Profile;
            var export = new
            {
                ExportVersion = 2,
                GeneratedAt = DateTimeOffset.Now,
                Player = new
                {
                    profile.DisplayName,
                    profile.Level,
                    profile.Prestige,
                    CosmeticCoins = profile.Coins,
                    profile.LifetimeExperience,
                    PlaytimeSeconds = (long)controller.TotalPlaytime.TotalSeconds,
                    Title = CosmeticCatalog.Get(profile.EquippedTitleId).Name,
                    Frame = CosmeticCatalog.Get(profile.EquippedFrameId).Name,
                    Portrait = CosmeticCatalog.Get(profile.EquippedPortraitId).Name,
                },
                Blackjack = profile.LifetimeStatistics,
                MidnightReels = profile.Slots,
                MidnightManor = profile.Manor,
                NeonVault = profile.NeonVault,
                LegacyPooledSlots = profile.LegacyPooledSlots,
                Seasons = profile.Seasons.Values.OrderBy(x => x.SeasonId).ToArray(),
                Challenges = ChallengeCatalog.All.Select(challenge => new
                {
                    challenge.Id,
                    challenge.Name,
                    Category = challenge.Category.ToString(),
                    Progress = ChallengeCatalog.Progress(profile, challenge),
                    challenge.Goal,
                    Complete = ChallengeCatalog.Complete(profile, challenge),
                    Claimed = profile.ClaimedChallengeIds.Contains(challenge.Id, StringComparer.Ordinal),
                    challenge.CoinReward,
                }).ToArray(),
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(export, options));
            if (playerStatusText is not null)
            {
                playerStatusText.Text = F("Exported", dialog.FileName);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            if (playerStatusText is not null)
            {
                playerStatusText.Text = F("ExportFailed", ex.Message);
            }
        }
    }

}
