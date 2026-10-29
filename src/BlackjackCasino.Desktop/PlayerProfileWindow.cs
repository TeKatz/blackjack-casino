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
    private WrapPanel? playerPortraitPanel;
    private ContentControl? equippedPortraitHost;
    private TextBlock? playerStatusText;
    private TextBox? nameBox;
    private TextBlock? nameLabel;
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(166,187,174));
    private static readonly Brush PositiveBrush = new SolidColorBrush(Color.FromRgb(142,199,129));
    private static readonly Brush CoinBrush = new SolidColorBrush(Color.FromRgb(84,225,183));
    internal PlayerProfileWindow(PlayerProgressionController controller)
    {
        this.controller=controller;
        Title="Player profile · Blackjack Casino"; Width=1040; Height=780; MinWidth=940; MinHeight=650;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Background=new SolidColorBrush(Color.FromRgb(7,18,13)); Foreground=Brushes.Ivory;
        var root=new DockPanel {Margin=new Thickness(18)};
        var heading=new DockPanel {Margin=new Thickness(6,0,6,12)};
        var back=new Button {Content="Close profile",HorizontalAlignment=HorizontalAlignment.Right}; back.Click+=(_,_)=>Close(); DockPanel.SetDock(back,Dock.Right); heading.Children.Add(back);
        heading.Children.Add(new TextBlock {Text="YOUR CASINO IDENTITY",FontSize=24,FontWeight=FontWeights.Bold,VerticalAlignment=VerticalAlignment.Center});
        DockPanel.SetDock(heading,Dock.Top); root.Children.Add(heading);
        root.Children.Add(BuildPlayerContent()); Content=root;
        MenuAudio.Attach(this); RefreshPlayer();
    }
    private static Border PanelBorder() => new() {Background=new SolidColorBrush(Color.FromRgb(11,33,23)),BorderBrush=new SolidColorBrush(Color.FromRgb(49,90,67)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Margin=new Thickness(5),Padding=new Thickness(12)};
    private static FrameworkElement CreatePortraitVisual(CosmeticDefinition portrait,double size) => ProgressionWindow.CreatePortraitVisual(portrait,size);
    private FrameworkElement BuildPlayerContent()
    {
        var root = new Grid { Margin = new Thickness(10) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var summary = PanelBorder();
        summary.Padding = new Thickness(18);
        var summaryGrid = new Grid();
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        equippedPortraitHost = new ContentControl { Width = 118, Height = 118, HorizontalAlignment = HorizontalAlignment.Left };
        summaryGrid.Children.Add(equippedPortraitHost);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock { Text = "PLAYER", Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 124)), FontSize = 11, FontWeight = FontWeights.Bold });
        nameLabel = new TextBlock { Text=controller.Profile.DisplayName,FontSize=22,FontWeight=FontWeights.Bold,Margin=new Thickness(0,4,0,0) }; info.Children.Add(nameLabel);
        info.Children.Add(new TextBlock {Text=$"Level {controller.Profile.Level} · Prestige {controller.Profile.Prestige} · ◆ {controller.Profile.Coins:N0}",Foreground=CoinBrush,Margin=new Thickness(0,5,0,0)});
        info.Children.Add(new TextBlock { Text = "Choose an owned profile picture. Portraits can be unlocked in the Shop and Season Pass.", Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 10, 0) });
        playerStatusText = new TextBlock { Foreground = PositiveBrush, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
        info.Children.Add(playerStatusText);
        var rename = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,12,0,0) };
        nameBox = new TextBox { Name="ProfileNameBox", Text=controller.Profile.DisplayName, Width=200, MaxLength=256, ToolTip="Profile name, up to 24 characters" };
        var saveName = new Button { Name="RenameProfileButton", Content="Save name", Margin=new Thickness(8,0,0,0) };
        saveName.Click += (_,_) => { controller.RenameProfile(nameBox.Text,out string message); playerStatusText.Text=message; nameBox.Text=controller.Profile.DisplayName; RefreshPlayer(); };
        rename.Children.Add(nameBox); rename.Children.Add(saveName); info.Children.Add(rename);

        Grid.SetColumn(info, 1);
        summaryGrid.Children.Add(info);

        var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var export = new Button { Content = "Save Progress…", ToolTip = "Export your current player statistics and progression as a JSON file." };
        export.Click += SaveProgress_Click;
        actions.Children.Add(export);
        var progression = new Button {Content="Progression & shop",Margin=new Thickness(0,8,0,0)};
        progression.Click+=(_,_)=> { var window=new ProgressionWindow(controller) {Owner=this}; window.ShowDialog(); RefreshPlayer(); };
        actions.Children.Add(progression);
        actions.Children.Add(new TextBlock { Text = "Exports stats, progression, seasons and challenge progress. Your live save is not moved or modified.", Foreground = MutedBrush, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5, 6, 5, 0) });
        Grid.SetColumn(actions, 2);
        summaryGrid.Children.Add(actions);
        summary.Child = summaryGrid;
        root.Children.Add(summary);

        var portraitPanel = new StackPanel { Margin = new Thickness(5, 8, 5, 5) };
        portraitPanel.Children.Add(new TextBlock { Text = "OWNED PROFILE PICTURES", Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 124)), FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        playerPortraitPanel = new WrapPanel();
        portraitPanel.Children.Add(playerPortraitPanel);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = portraitPanel };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        return root;
    }

    private void RefreshPlayer()
    {
        if (playerPortraitPanel is null || equippedPortraitHost is null)
        {
            return;
        }

        PlayerProfileState profile = controller.Profile;
        if(nameLabel is not null) nameLabel.Text=profile.DisplayName;
        CosmeticDefinition equipped = CosmeticCatalog.Get(profile.EquippedPortraitId);
        equippedPortraitHost.Content = CreatePortraitVisual(equipped, 112);
        playerPortraitPanel.Children.Clear();

        foreach (CosmeticDefinition portrait in CosmeticCatalog.All
                     .Where(x => x.Kind == CosmeticKind.Portrait && profile.OwnedCosmeticIds.Contains(x.Id, StringComparer.Ordinal))
                     .OrderBy(x => x.Name))
        {
            bool selected = portrait.Id == profile.EquippedPortraitId;
            var card = new Border
            {
                Width = 174,
                MinHeight = 190,
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(selected ? 2 : 1),
                BorderBrush = selected ? CoinBrush : new SolidColorBrush(Color.FromRgb(49, 90, 67)),
                Background = new SolidColorBrush(Color.FromArgb(180, 11, 33, 23)),
            };
            var stack = new StackPanel();
            FrameworkElement visual = CreatePortraitVisual(portrait, 104);
            visual.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Add(visual);
            stack.Children.Add(new TextBlock { Text = portrait.Name, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 7, 0, 3) });
            var equip = new Button { Tag = portrait.Id, Content = selected ? "Equipped" : "Use portrait", IsEnabled = !selected };
            equip.Click += PortraitEquip_Click;
            stack.Children.Add(equip);
            card.Child = stack;
            playerPortraitPanel.Children.Add(card);
        }
    }

    private void PortraitEquip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id })
        {
            return;
        }

        if (controller.EquipCosmetic(id))
        {
            if (playerStatusText is not null)
            {
                playerStatusText.Text = $"Using {CosmeticCatalog.Get(id).Name}.";
            }
            RefreshPlayer();
        }
    }

    private void SaveProgress_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Player Progress",
            FileName = $"blackjack-casino-progress-{DateTime.Now:yyyy-MM-dd}.json",
            DefaultExt = ".json",
            Filter = "JSON progress export (*.json)|*.json",
            AddExtension = true,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            PlayerProfileState profile = controller.Profile;
            var export = new
            {
                ExportVersion = 1,
                GeneratedAt = DateTimeOffset.Now,
                Player = new
                {
                    profile.DisplayName,
                    profile.Level,
                    profile.Prestige,
                    CosmeticCoins = profile.Coins,
                    profile.LifetimeExperience,
                    PlaytimeSeconds = profile.TotalPlaytimeSeconds + (long)controller.SessionElapsed.TotalSeconds,
                    Title = CosmeticCatalog.Get(profile.EquippedTitleId).Name,
                    Frame = CosmeticCatalog.Get(profile.EquippedFrameId).Name,
                    Portrait = CosmeticCatalog.Get(profile.EquippedPortraitId).Name,
                },
                Blackjack = profile.LifetimeStatistics,
                MidnightReels = profile.Slots,
                MidnightManor = profile.Manor,
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
                playerStatusText.Text = $"Progress exported to {dialog.FileName}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            if (playerStatusText is not null)
            {
                playerStatusText.Text = "Export failed: " + ex.Message;
            }
        }
    }

}
