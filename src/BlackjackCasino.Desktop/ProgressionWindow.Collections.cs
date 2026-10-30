using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private const int ChallengePageSize = 4;
    private const int ShopPageSize = 4;
    private int challengePage;
    private int shopPage;
    private bool collectionsInitialized;

    private sealed record FilterChoice(string Label, object? Value = null)
    {
        public override string ToString() => Label;
    }

    private void InitializeCollections()
    {
        if (collectionsInitialized) return;
        ChallengeCategoryFilter.Items.Add(new FilterChoice("All games"));
        foreach (ChallengeCategory category in Enum.GetValues<ChallengeCategory>())
            ChallengeCategoryFilter.Items.Add(new FilterChoice(CategoryName(category), category));
        ChallengeStatusFilter.ItemsSource = new[] { "All statuses", "Ready to claim", "In progress", "Claimed", "Unclaimed" };
        ShopKindFilter.Items.Add(new FilterChoice("All types"));
        foreach ((CosmeticKind kind, string label) in new[]
        {
            (CosmeticKind.CardSet, "Card Sets"),
            (CosmeticKind.Frame, "Frames"),
            (CosmeticKind.Portrait, "Portraits"),
            (CosmeticKind.Title, "Titles"),
        })
            ShopKindFilter.Items.Add(new FilterChoice(label, kind));
        ShopOwnershipFilter.ItemsSource = new[] { "All items", "For sale", "Affordable", "Owned", "Equipped", "Unowned", "Locked rewards" };
        ShopSortFilter.ItemsSource = new[] { "Recommended", "Cost: low to high", "Cost: high to low", "Name" };
        ChallengeCategoryFilter.SelectedIndex = ChallengeStatusFilter.SelectedIndex = 0;
        ShopKindFilter.SelectedIndex = ShopOwnershipFilter.SelectedIndex = ShopSortFilter.SelectedIndex = 0;
        collectionsInitialized = true;
    }

    private static string CategoryName(ChallengeCategory category) => category switch
    {
        ChallengeCategory.Blackjack => "Blackjack",
        ChallengeCategory.MidnightReels => "Midnight Reels",
        ChallengeCategory.MidnightManor => "Midnight Manor",
        ChallengeCategory.NeonVault => "Neon Vault",
        ChallengeCategory.NineLives => "Nine Lives",
        _ => category.ToString(),
    };

    private static bool MatchesSearch(string query, params string[] fields) =>
        string.IsNullOrWhiteSpace(query) || fields.Any(field => field.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));

    private static int LastPage(int count, int pageSize) => Math.Max(0, (count - 1) / pageSize);

    private static string PageDescription(int count, int page, int pageSize, string noun) => count == 0
        ? $"0 {noun} · no matches"
        : $"{page * pageSize + 1}–{Math.Min(count, (page + 1) * pageSize)} of {count} {noun} · Page {page + 1} / {LastPage(count, pageSize) + 1}";

    private void ChallengeFilters_Changed(object sender, SelectionChangedEventArgs e) => ResetChallengePage();
    private void ChallengeSearch_Changed(object sender, TextChangedEventArgs e) => ResetChallengePage();
    private void ResetChallengePage()
    {
        if (!collectionsInitialized) return;
        challengePage = 0;
        RefreshChallenges();
        ChallengesScroll.ScrollToTop();
    }

    private void ResetChallengeFilters_Click(object sender, RoutedEventArgs e)
    {
        collectionsInitialized = false;
        ChallengeCategoryFilter.SelectedIndex = ChallengeStatusFilter.SelectedIndex = 0;
        ChallengeSearchBox.Clear();
        collectionsInitialized = true;
        ResetChallengePage();
    }

    private void ChallengePrevious_Click(object sender, RoutedEventArgs e)
    {
        challengePage = Math.Max(0, challengePage - 1);
        RefreshChallenges();
        ChallengesScroll.ScrollToTop();
    }

    private void ChallengeNext_Click(object sender, RoutedEventArgs e)
    {
        challengePage++;
        RefreshChallenges();
        ChallengesScroll.ScrollToTop();
    }

    private void ShopFilters_Changed(object sender, SelectionChangedEventArgs e) => ResetShopPage();
    private void ShopSearch_Changed(object sender, TextChangedEventArgs e) => ResetShopPage();
    private void ResetShopPage()
    {
        if (!collectionsInitialized) return;
        shopPage = 0;
        RefreshShop();
        ShopScroll.ScrollToTop();
    }

    private void ResetShopFilters_Click(object sender, RoutedEventArgs e)
    {
        collectionsInitialized = false;
        ShopKindFilter.SelectedIndex = ShopOwnershipFilter.SelectedIndex = ShopSortFilter.SelectedIndex = 0;
        ShopSearchBox.Clear();
        collectionsInitialized = true;
        ResetShopPage();
    }

    private void ShopPrevious_Click(object sender, RoutedEventArgs e)
    {
        shopPage = Math.Max(0, shopPage - 1);
        RefreshShop();
        ShopScroll.ScrollToTop();
    }

    private void ShopNext_Click(object sender, RoutedEventArgs e)
    {
        shopPage++;
        RefreshShop();
        ShopScroll.ScrollToTop();
    }
}
