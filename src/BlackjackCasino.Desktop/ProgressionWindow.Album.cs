using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private string albumThemeId = CollectionCatalog.Themes[0].Id;
    private Guid albumOpeningId;
    private readonly HashSet<int> albumRevealed = [];
    private CancellationTokenSource? albumPresentation;
    private readonly object albumAudioOwner = new();
    private readonly List<ContentControl> albumSlots = [];
    private static readonly Dictionary<(CollectionRarity, bool), AudioClip> AlbumCues = [];

    private void RefreshAlbum()
    {
        CollectionState state = controller.Profile.Collection;
        int complete = CollectionCatalog.Themes.Count(t => t.Cards.All(c => state.OwnedCardIds.Contains(c.Id)));
        AlbumSummary.Text = Localization.F("Album.Summary", state.OwnedCardIds.Count, complete, state.PacksOpened, state.DuplicatePoints);
        AlbumDuplicateProgress.Value = state.DuplicatePoints;
        if (string.IsNullOrEmpty(AlbumStatus.Text))
            AlbumStatus.Text = Localization.T("Album.Help");
        AlbumThemes.Children.Clear();
        foreach (CollectionTheme theme in CollectionCatalog.Themes)
        {
            int owned = theme.Cards.Count(c => state.OwnedCardIds.Contains(c.Id));
            var label = new StackPanel();
            label.Children.Add(new TextBlock { Text = $"{theme.Symbol}  {theme.Name}", FontSize = 12, TextWrapping = TextWrapping.Wrap });
            label.Children.Add(new TextBlock { Text = $"{owned} / {theme.Cards.Count}" + (theme.UltraRare ? " · " + Localization.T("Album.UltraRare") : owned == theme.Cards.Count ? " · " + Localization.T("Album.Complete") : ""), FontSize = 9, Foreground = MutedBrush, Margin = new Thickness(0, 4, 0, 0) });
            var button = new Button { Content = label, Tag = theme.Id, Margin = new Thickness(2, 3, 4, 3), Padding = new Thickness(9), HorizontalContentAlignment = HorizontalAlignment.Left,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Accent)), BorderThickness = new Thickness(theme.Id == albumThemeId ? 2 : .5) };
            System.Windows.Automation.AutomationProperties.SetName(button, $"{theme.Name}, {owned} of {theme.Cards.Count} cards");
            button.Click += (_, _) => { albumThemeId = theme.Id; RefreshAlbum(); };
            AlbumThemes.Children.Add(button);
        }
        AlbumPacks.Children.Clear();
        foreach (CollectionPack pack in CollectionCatalog.Packs)
        {
            int count = state.Packs.GetValueOrDefault(pack.Rarity);
            var stack = new StackPanel { Width = 118, Margin = new Thickness(3) };
            stack.Children.Add(CollectionArtwork.Pack(pack.Rarity));
            stack.Children.Add(new TextBlock { Text = $"{pack.Rarity} × {count}", TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(CollectionArtwork.RarityColor(pack.Rarity)), FontSize = 12 });
            var open = new Button { Content = Localization.T("Album.Open"), Tag = pack.Rarity, IsEnabled = count > 0 && state.PendingOpening is null, Margin = new Thickness(3, 7, 3, 3), Padding = new Thickness(5, 7, 5, 7), ToolTip = PackOdds(pack) };
            System.Windows.Automation.AutomationProperties.SetName(open, $"Open {pack.Rarity} pack, {count} available");
            open.Click += AlbumOpen_Click;
            stack.Children.Add(open); AlbumPacks.Children.Add(stack);
        }
        CollectionTheme active = CollectionCatalog.Themes.First(t => t.Id == albumThemeId);
        AlbumThemeTitle.Text = $"{active.Symbol}  {active.Name}" + (active.UltraRare ? " · " + Localization.T("Album.UltraRare") : "");
        AlbumCards.Children.Clear();
        foreach (CollectionCard card in active.Cards)
        {
            bool owned = state.OwnedCardIds.Contains(card.Id);
            Border preview = CollectionArtwork.Card(card, owned, animationsEnabled: albumAnimations);
            var inspect = new Button { Content = preview, Tag = "inspect:" + card.Id, IsEnabled = owned, Padding = new Thickness(0), Margin = new Thickness(0),
                Template = (ControlTemplate)ProgressionRoot.FindResource("CollectionCardButtonTemplate"), ToolTip = owned ? Localization.T("Album.ArtHelp") : card.Name };
            System.Windows.Automation.AutomationProperties.SetName(inspect, card.Name + (owned ? " · " + Localization.T("Album.ArtHelp") : ""));
            inspect.Click += (_, _) => OpenAlbumArt(card);
            AlbumCards.Children.Add(inspect);
        }
        if (state.PendingOpening is { } opening)
        {
            if (opening.Id != albumOpeningId) ShowAlbumOpening(opening);
            AlbumOpeningOverlay.Visibility = Visibility.Visible;
        }
        else AlbumOpeningOverlay.Visibility = Visibility.Collapsed;
    }

    private static string PackOdds(CollectionPack pack)
    {
        string odds = string.Join(" · ", Enum.GetValues<CollectionRarity>().Select(r => $"{r} {pack.Weights[(int)r] / 10d:0.#}%"));
        return Localization.F("Album.Odds", pack.GuaranteedRarity, odds, pack.UltraRareChance);
    }

    private async void AlbumOpen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CollectionRarity rarity }) return;
        CollectionActionResult result = controller.OpenCollectionPack(rarity);
        AlbumStatus.Text = result.Message;
        if (!result.Success) return;
        RefreshAll();
        PlayAlbumCue(rarity, true);
        if (!albumAnimations) { RevealAlbumAll(); return; }
        albumPresentation = new CancellationTokenSource();
        CancellationToken token = albumPresentation.Token;
        AlbumRevealAll.IsEnabled = true;
        FrameworkElement pack = (FrameworkElement)AlbumOpeningPack.Content;
        try
        {
            var transform = new RotateTransform(); pack.RenderTransformOrigin = new Point(.5, .5); pack.RenderTransform = transform;
            transform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-3 - (int)rarity, 3 + (int)rarity, TimeSpan.FromMilliseconds(80))
            { AutoReverse = true, RepeatBehavior = new RepeatBehavior(3 + (int)rarity) });
            await Task.Delay(650 + (int)rarity * 150, token);
            transform.BeginAnimation(RotateTransform.AngleProperty, null);
            transform.Angle = 0;
            pack.BeginAnimation(OpacityProperty, new DoubleAnimation(1, .25, TimeSpan.FromMilliseconds(200)));
            for (int i = 0; i < CollectionCatalog.CardsPerPack; i++)
            {
                token.ThrowIfCancellationRequested();
                RevealAlbumCard(i, true);
                await Task.Delay(230 + (int)CollectionCatalog.Find(result.Opening!.Cards[i].CardId)!.Rarity * 100, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            pack.RenderTransform = Transform.Identity;
            pack.BeginAnimation(OpacityProperty, null);
        }
    }

    private void ShowAlbumOpening(CollectionOpening opening)
    {
        StopAlbumPresentation();
        albumOpeningId = opening.Id;
        albumRevealed.Clear(); albumSlots.Clear();
        AlbumOpeningCards.Children.Clear();
        AlbumOpeningPack.Content = CollectionArtwork.Pack(opening.PackRarity);
        AlbumOpeningTitle.Text = $"{opening.PackRarity.ToString().ToUpperInvariant()} PACK";
        AlbumOpeningTitle.Foreground = new SolidColorBrush(CollectionArtwork.RarityColor(opening.PackRarity));
        AlbumOpeningOverlay.BorderBrush = AlbumOpeningTitle.Foreground;
        AlbumOpeningSummary.Text = Localization.T("Album.SafeReveal");
        AlbumCollect.IsEnabled = false; AlbumRevealAll.IsEnabled = true;
        for (int i = 0; i < opening.Cards.Count; i++)
        {
            int index = i;
            var back = new Button { Content = CollectionArtwork.Pack(opening.PackRarity), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(8), Margin = new Thickness(0), ToolTip = Localization.F("Album.RevealCard", i + 1) };
            System.Windows.Automation.AutomationProperties.SetName(back, Localization.F("Album.RevealCard", i + 1));
            back.Click += (_, _) => RevealAlbumCard(index, albumAnimations);
            var slot = new ContentControl { Content = back, Width = 180, Height = 268, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
            albumSlots.Add(slot); AlbumOpeningCards.Children.Add(slot);
        }
    }

    private void RevealAlbumCard(int index, bool animate, bool sound = true)
    {
        CollectionOpening? opening = controller.Profile.Collection.PendingOpening;
        if (opening is null || opening.Id != albumOpeningId || !albumRevealed.Add(index)) return;
        CollectionDraw draw = opening.Cards[index];
        CollectionCard card = CollectionCatalog.Find(draw.CardId)!;
        bool ultra = CollectionCatalog.Theme(card).UltraRare;
        Border front = CollectionArtwork.Card(card, true, draw.IsNew ? Localization.T("Album.New") : Localization.F("Album.Duplicate", draw.DuplicatePoints), albumAnimations);
        albumSlots[index].Content = front;
        if (sound) PlayAlbumCue(card.Rarity, false);
        if (animate)
        {
            var scale = new ScaleTransform(.05, .95); front.RenderTransformOrigin = new Point(.5, .5); front.RenderTransform = scale;
            var duration = TimeSpan.FromMilliseconds(250 + (int)card.Rarity * 65);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.05, 1, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.95, 1, duration));
            if (card.Rarity >= CollectionRarity.Rare)
            {
                var glow = (DropShadowEffect)front.Effect;
                glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(ultra ? 58 : 20 + (int)card.Rarity * 5, glow.BlurRadius, TimeSpan.FromMilliseconds(850)));
                // Foil sweep travels across the card as an ephemeral overlay.
                AddFoilSweep(front, card.Rarity, ultra);
            }
        }
        if (albumRevealed.Count == CollectionCatalog.CardsPerPack)
        {
            AlbumCollect.IsEnabled = true; AlbumRevealAll.IsEnabled = false;
            AlbumOpeningSummary.Text = Localization.F("Album.Result", opening.Cards.Count(c => c.IsNew), opening.PointsEarned, opening.PacksEarned);
        }
    }

    private static void AddFoilSweep(Border card, CollectionRarity rarity, bool ultra)
    {
        var content = (Grid)card.Child;
        var sweep = new Border { IsHitTestVisible = false, Opacity = .7, Background = new LinearGradientBrush(new GradientStopCollection
        {
            new(Colors.Transparent, 0), new(Colors.Transparent, .38), new(Color.FromArgb(ultra ? (byte)170 : (byte)100, 255, 240, 205), .5), new(Colors.Transparent, .62), new(Colors.Transparent, 1),
        }, 25) };
        Grid.SetRowSpan(sweep, 3); content.Children.Add(sweep);
        var shift = new TranslateTransform(); sweep.RenderTransform = shift;
        var animation = new DoubleAnimation(-170, 170, TimeSpan.FromMilliseconds(550 + (int)rarity * 100));
        animation.Completed += (_, _) => content.Children.Remove(sweep);
        shift.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void AlbumRevealAll_Click(object sender, RoutedEventArgs e) => RevealAlbumAll();
    private void RevealAlbumAll()
    {
        StopAlbumPresentation();
        // A single highest-rarity cue avoids five overlapping sounds on skip.
        CollectionOpening? opening = controller.Profile.Collection.PendingOpening;
        if (opening is null) return;
        CollectionRarity highest = opening.Cards.Select(c => CollectionCatalog.Find(c.CardId)!.Rarity).Max();
        if (albumRevealed.Count < opening.Cards.Count) PlayAlbumCue(highest, false);
        for (int i = 0; i < opening.Cards.Count; i++) RevealAlbumCard(i, false, false);
    }

    private void AlbumCollect_Click(object sender, RoutedEventArgs e)
    {
        CollectionActionResult result = controller.CompleteCollectionOpening(albumOpeningId);
        AlbumOpeningSummary.Text = AlbumStatus.Text = result.Message;
        if (!result.Success) return;
        StopAlbumPresentation(); albumOpeningId = Guid.Empty;
        RefreshAll();
    }

    private void StopAlbumPresentation()
    {
        albumPresentation?.Cancel(); albumPresentation?.Dispose(); albumPresentation = null;
        CasinoAudio.Mixer.Stop(albumAudioOwner);
    }

    private void PlayAlbumCue(CollectionRarity rarity, bool opening)
    {
        if (!albumSounds) return;
        if (!AlbumCues.TryGetValue((rarity, opening), out AudioClip? clip))
            AlbumCues[(rarity, opening)] = clip = CollectionSoundSynthesis.Build(rarity, opening);
        CasinoAudio.Play(clip, albumAudioOwner, .8);
    }

    private void RefreshPackShop()
    {
        PackShopPanel.Children.Clear();
        foreach (CollectionPack pack in CollectionCatalog.Packs)
        {
            var row = new StackPanel { Width = 132, Margin = new Thickness(3, 4, 3, 8) };
            row.Children.Add(new TextBlock { Text = $"✦ {pack.Rarity}", Foreground = new SolidColorBrush(CollectionArtwork.RarityColor(pack.Rarity)), FontWeight = FontWeights.SemiBold, FontSize = 12, TextAlignment = TextAlignment.Center });
            var buy = new Button { Content = Localization.F("Album.Buy", pack.Price), Tag = pack.Rarity, Padding = new Thickness(5, 8, 5, 8), Margin = new Thickness(2, 7, 2, 2), IsEnabled = controller.Profile.Coins >= pack.Price, ToolTip = PackOdds(pack) };
            System.Windows.Automation.AutomationProperties.SetName(buy, $"Buy {pack.Rarity} card pack for {pack.Price} coins");
            buy.Click += (_, _) =>
            {
                CollectionActionResult result = controller.PurchaseCollectionPack(pack.Rarity);
                ShopStatusText.Text = result.Message;
                RefreshAll();
            };
            row.Children.Add(buy); PackShopPanel.Children.Add(row);
        }
    }
}
