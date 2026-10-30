using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private bool IsOwned(CosmeticDefinition cosmetic) => controller.Profile.OwnedCosmeticIds.Contains(cosmetic.Id, StringComparer.Ordinal);

    private bool IsEquipped(CosmeticDefinition cosmetic) => cosmetic.Id == (cosmetic.Kind switch
    {
        CosmeticKind.Title => controller.Profile.EquippedTitleId,
        CosmeticKind.Frame => controller.Profile.EquippedFrameId,
        CosmeticKind.Portrait => controller.Profile.EquippedPortraitId,
        CosmeticKind.CardSet => controller.Profile.EquippedCardSetId,
        _ => string.Empty,
    });

    private void RefreshShop()
    {
        if (!collectionsInitialized) return;
        PlayerProfileState profile = controller.Profile;
        ShopCoinsText.Text = $"Balance: ◆ {profile.Coins:N0}";
        ShopCoinsText.Foreground = CoinBrush;
        ShopSummaryText.Text = $"{CosmeticCatalog.All.Count(IsOwned):N0} / {CosmeticCatalog.All.Count:N0} owned · card sets, portraits, animated frames & titles";
        CosmeticKind? kind = (ShopKindFilter.SelectedItem as FilterChoice)?.Value is CosmeticKind selected ? selected : null;
        IEnumerable<CosmeticDefinition> filtered = CosmeticCatalog.All.Where(cosmetic => (!kind.HasValue || cosmetic.Kind == kind)
            && MatchesSearch(ShopSearchBox.Text, cosmetic.Name, cosmetic.Description, cosmetic.Kind.ToString())
            && (ShopOwnershipFilter.SelectedIndex switch
            {
                1 => cosmetic.ShopAvailable && !IsOwned(cosmetic),
                2 => cosmetic.ShopAvailable && !IsOwned(cosmetic) && profile.Prestige >= cosmetic.RequiredPrestige && profile.Coins >= cosmetic.Price,
                3 => IsOwned(cosmetic),
                4 => IsEquipped(cosmetic),
                5 => !IsOwned(cosmetic),
                6 => (!cosmetic.ShopAvailable || profile.Prestige < cosmetic.RequiredPrestige) && !IsOwned(cosmetic),
                _ => true,
            }));
        filtered = ShopSortFilter.SelectedIndex switch
        {
            1 => filtered.OrderBy(cosmetic => (!cosmetic.ShopAvailable || profile.Prestige < cosmetic.RequiredPrestige) && !IsOwned(cosmetic)).ThenBy(cosmetic => cosmetic.Price).ThenBy(cosmetic => cosmetic.Name),
            2 => filtered.OrderByDescending(cosmetic => cosmetic.Price).ThenBy(cosmetic => cosmetic.Name),
            3 => filtered.OrderBy(cosmetic => cosmetic.Name),
            _ => filtered.OrderBy(cosmetic => IsEquipped(cosmetic) ? 0 : IsOwned(cosmetic) ? 1 : cosmetic.ShopAvailable && profile.Prestige >= cosmetic.RequiredPrestige ? 2 : 3).ThenBy(cosmetic => cosmetic.Kind).ThenBy(cosmetic => cosmetic.Price),
        };
        CosmeticDefinition[] items = filtered.ToArray();
        shopPage = Math.Clamp(shopPage, 0, LastPage(items.Length, ShopPageSize));
        ShopPanel.Children.Clear();
        foreach (CosmeticDefinition cosmetic in items.Skip(shopPage * ShopPageSize).Take(ShopPageSize))
            ShopPanel.Children.Add(BuildShopRow(cosmetic));
        if (items.Length == 0)
            ShopPanel.Children.Add(MutedText("No cosmetics match these filters. Try another type, status or search, or choose Reset."));
        ShopPageText.Text = PageDescription(items.Length, shopPage, ShopPageSize, "cosmetics");
        ShopPreviousButton.IsEnabled = shopPage > 0;
        ShopNextButton.IsEnabled = shopPage < LastPage(items.Length, ShopPageSize);
    }

    private FrameworkElement BuildShopRow(CosmeticDefinition cosmetic)
    {
        bool owned = IsOwned(cosmetic);
        bool equipped = IsEquipped(cosmetic);
        bool prestigeLocked = !owned && controller.Profile.Prestige < cosmetic.RequiredPrestige;
        bool rewardLocked = !owned && !cosmetic.ShopAvailable;
        bool locked = prestigeLocked || rewardLocked;
        bool canBuy = cosmetic.ShopAvailable && !prestigeLocked && controller.Profile.Coins >= cosmetic.Price;
        var border = PanelBorder();
        border.Margin = new Thickness(0, 0, 4, 6);
        border.Padding = new Thickness(12, 8, 12, 8);
        if (equipped) border.BorderBrush = CoinMutedBrush;
        if (cosmetic.RequiredPrestige > 0 && !owned) border.BorderBrush = new SolidColorBrush(Color.FromRgb(168, 129, 55));
        var grid = new Grid { MinHeight = 62 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.Children.Add(new Viewbox { Width = 72, Height = 58, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, Child = CreateCosmeticPreview(cosmetic), ToolTip = cosmetic.Name });
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        text.Children.Add(new TextBlock { Text = cosmetic.Name, FontWeight = FontWeights.SemiBold, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = cosmetic.Name });
        text.Children.Add(new TextBlock { Text = cosmetic.Description, FontSize = 11, Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        var state = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        string availability = equipped ? "EQUIPPED" : owned ? "OWNED" : prestigeLocked ? $"PRESTIGE {cosmetic.RequiredPrestige} REQUIRED" : rewardLocked ? "LOCKED" : "FOR SALE";
        state.Children.Add(new TextBlock { Text = $"{CosmeticKindName(cosmetic.Kind).ToUpperInvariant()} · {availability}", Foreground = owned ? CoinBrush : prestigeLocked ? new SolidColorBrush(Color.FromRgb(226, 190, 111)) : MutedBrush, FontWeight = FontWeights.SemiBold, FontSize = 10 });
        state.Children.Add(new TextBlock
        {
            Text = owned ? "In your collection" : prestigeLocked ? $"Reach Player Prestige {cosmetic.RequiredPrestige} to buy · ◆ {cosmetic.Price:N0}" : rewardLocked ? "Season / prestige reward" : $"◆ {cosmetic.Price:N0} coins",
            Foreground = owned || (!locked && cosmetic.ShopAvailable) ? CoinBrush : MutedBrush, FontSize = 11, Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap,
        });
        if (!owned && !locked && !canBuy)
            state.Children.Add(new TextBlock { Text = $"Need ◆ {cosmetic.Price - controller.Profile.Coins:N0} more", Foreground = MutedBrush, FontSize = 10, Margin = new Thickness(0, 3, 0, 0) });
        Grid.SetColumn(state, 2);
        grid.Children.Add(state);
        var button = new Button
        {
            Tag = cosmetic.Id,
            Content = equipped ? "Equipped" : owned ? "Equip" : prestigeLocked ? $"Prestige {cosmetic.RequiredPrestige} required" : rewardLocked ? "Reward locked" : "Buy & equip",
            IsEnabled = !equipped && (owned || canBuy), VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(8, 7, 8, 7),
            ToolTip = prestigeLocked ? $"Reach Player Prestige {cosmetic.RequiredPrestige} to unlock this premium card set." : rewardLocked ? cosmetic.Description : !owned && !canBuy ? $"You need ◆ {cosmetic.Price - controller.Profile.Coins:N0} more coins." : cosmetic.Name,
        };
        System.Windows.Automation.AutomationProperties.SetName(button, $"{button.Content}: {cosmetic.Name}");
        button.Click += ShopItemButton_Click;
        Grid.SetColumn(button, 3);
        grid.Children.Add(button);
        border.Child = grid;
        return border;
    }

    private static string CosmeticKindName(CosmeticKind kind) => kind == CosmeticKind.CardSet ? "Card Set" : kind.ToString();

    private void ShopItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id }) return;
        CosmeticDefinition cosmetic = CosmeticCatalog.Get(id);
        if (!IsOwned(cosmetic))
        {
            PurchaseResult result = controller.PurchaseCosmetic(id);
            ShopStatusText.Text = result.Message;
            if (!result.Success)
            {
                RefreshShop();
                return;
            }
        }
        bool equipped = controller.EquipCosmetic(id);
        ShopStatusText.Text = equipped ? $"Equipped {cosmetic.Name}." : $"{cosmetic.Name} is owned; equipping was unsuccessful.";
        RefreshAll();
    }
}
