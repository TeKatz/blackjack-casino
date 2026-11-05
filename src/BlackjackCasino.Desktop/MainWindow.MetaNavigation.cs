using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private sealed record MetaPage(Window Shell, FrameworkElement Content);

    private readonly Stack<MetaPage> metaPages = new();
    private Grid? metaOverlay;
    private ContentControl? metaHost;
    private IInputElement? metaPreviousFocus;
    private readonly List<(UIElement Element, bool Enabled)> metaCoveredElements = [];
    private bool MetaPageOpen => metaPages.Count > 0;

    private void OpenPlayerProfile()
    {
        if (progressionController is null || MetaPageOpen) return;
        progressionController.AccruePlaytime();
        ProcessLatestCompletedRound();
        var profile = new PlayerProfileWindow(progressionController,
            () => OpenProgressionPage("Season Pass"), CloseMetaPage, OpenProfileStatistics);
        ShowMetaPage(profile);
    }

    private void OpenProgressionPage(string tab)
    {
        if (progressionController is null) return;
        if (modernLobby is { Ready: true })
        {
            if (tab == "Cosmetic Shop")
            {
                OpenModernShopFromProgression();
                return;
            }
            if (tab is "Challenges" or "Statistics" or "Overview")
            {
                OpenModernProgression(tab == "Challenges" ? "challenges" : "statistics");
                return;
            }
        }
        progressionController.AccruePlaytime();
        ProcessLatestCompletedRound();
        var page = new ProgressionWindow(progressionController, preferences.Presentation("progression").AnimationsEnabled, preferences.Presentation("progression").SoundEnabled);
        page.RemoveAlbumTab();
        page.RemoveProfileTabs();
        page.ConfigureEmbeddedPresentation(CloseMetaPage);
        if (modernLobby is { Ready: true })
        {
            page.ConfigureModernShop(OpenModernShopFromProgression);
            page.ConfigureModernRecords(OpenModernProgression);
        }
        page.SelectTab(tab);
        ShowMetaPage(page);
    }

    private void OpenProfileStatistics(string section)
    {
        if (progressionController is null) return;
        if (section != "Hand History" && modernLobby is { Ready: true })
        {
            OpenModernProgression("statistics");
            return;
        }
        var page = new ProgressionWindow(progressionController, preferences.Presentation("progression").AnimationsEnabled, preferences.Presentation("progression").SoundEnabled);
        page.ConfigureProfileSection(section);
        page.ConfigureEmbeddedPresentation(CloseMetaPage);
        ShowMetaPage(page);
    }

    private void OpenCollectionAlbumPage()
    {
        if (progressionController is null || MetaPageOpen) return;
        progressionController.AccruePlaytime();
        ProcessLatestCompletedRound();
        bool animations = preferences.Presentation("album").AnimationsEnabled;
        var page = new CollectionAlbumPage(progressionController, animations, preferences.Presentation("album").SoundEnabled, CloseMetaPage);
        CollectionThemeRewardPresenter.Attach(page, progressionController, animations);
        ShowMetaPage(page);
    }

    private void ShowMetaPage(Window shell)
    {
        if (shell.Content is not FrameworkElement content)
            throw new InvalidOperationException("Casino pages must expose hostable content.");

        if (metaOverlay is null)
        {
            metaHost = new ContentControl
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            metaOverlay = new Grid
            {
                Name = "CasinoMetaOverlay",
                Background = new SolidColorBrush(Color.FromRgb(7, 18, 13)),
                Focusable = true,
            };
            KeyboardNavigation.SetTabNavigation(metaOverlay, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetControlTabNavigation(metaOverlay, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetDirectionalNavigation(metaOverlay, KeyboardNavigationMode.Contained);
            metaOverlay.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(MetaOverlay_PreviewKeyDown), true);
            metaOverlay.Children.Add(metaHost);
            Grid.SetRowSpan(metaOverlay, Math.Max(1, CasinoRoot.RowDefinitions.Count));
            Grid.SetColumnSpan(metaOverlay, Math.Max(1, CasinoRoot.ColumnDefinitions.Count));
            Panel.SetZIndex(metaOverlay, 2000);
            CasinoRoot.Children.Add(metaOverlay);
            Closed += (_, _) => CloseAllMetaPages();
        }

        if (!MetaPageOpen)
        {
            metaPreviousFocus = Keyboard.FocusedElement;
            foreach (UIElement covered in CasinoRoot.Children.OfType<UIElement>().Where(child => child != metaOverlay))
            {
                metaCoveredElements.Add((covered, covered.IsEnabled));
                covered.SetCurrentValue(IsEnabledProperty, false);
            }
        }
        var resources = new ResourceDictionary();
        CopyResources(shell.Resources, resources);
        content.Resources.MergedDictionaries.Insert(0, resources);
        content.SetValue(TextElement.ForegroundProperty, shell.Foreground);
        content.SetValue(TextElement.FontFamilyProperty, shell.FontFamily);
        content.UseLayoutRounding = true;
        content.SnapsToDevicePixels = true;
        content.Focusable = true;
        shell.Content = null;
        metaPages.Push(new MetaPage(shell, content));
        metaHost!.Content = content;
        metaOverlay.Visibility = Visibility.Visible;
        // WebView2 is an HWND/composition surface: keep it out of the native page's
        // airspace while an embedded WPF profile, shop or album is foregrounded.
        modernLobby?.SetLobbyVisible(false);
        UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
        Dispatcher.BeginInvoke(() => content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
    }

    private void MetaOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || metaHost?.Content is not FrameworkElement content ||
            !metaPages.TryPeek(out MetaPage? page) || page.Shell is not CollectionAlbumPage)
            return;

        Border? inspector = VisualDescendants<Border>(content)
            .FirstOrDefault(border => border.Name == "AlbumArtOverlay" && border.IsVisible);
        if (inspector is null) return;

        string? cardId = VisualDescendants<Image>(inspector)
            .FirstOrDefault(image => image.Name == "CollectionCardArt")?.Tag as string;
        if (string.IsNullOrEmpty(cardId)) return;

        Button? cardButton = VisualDescendants<Button>(content)
            .FirstOrDefault(button => Equals(button.Tag, "inspect:" + cardId));
        if (cardButton is null) return;

        // The album closes the inspector later in this routed key event. Restore focus only after
        // layout/enabled-state propagation is complete; otherwise WPF can leave focus on the now
        // hidden close button even though the card itself is available again.
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (!cardButton.IsVisible || !cardButton.IsEnabled) return;
            cardButton.BringIntoView();
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(cardButton), cardButton);
            cardButton.Focus();
            Keyboard.Focus(cardButton);
        });
    }

    private static IEnumerable<T> VisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (T nested in VisualDescendants<T>(child)) yield return nested;
        }
    }

    private void CloseMetaPage()
    {
        if (!metaPages.TryPop(out MetaPage? page)) return;
        metaHost!.Content = null;
        page.Shell.Close();
        RefreshProfileButton();
        RefreshCollectionAlbumHeaderButton();
        if (metaPages.TryPeek(out MetaPage? previous))
        {
            if (previous.Shell is PlayerProfileWindow profile) profile.RefreshPlayer();
            metaHost.Content = previous.Content;
            previous.Content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
        else
        {
            metaOverlay!.Visibility = Visibility.Collapsed;
            foreach (var (element, enabled) in metaCoveredElements) element.SetCurrentValue(IsEnabledProperty, enabled);
            metaCoveredElements.Clear();
            if (metaPreviousFocus is not null) Keyboard.Focus(metaPreviousFocus);
            metaPreviousFocus = null;
            // The lobby is visible underneath the native meta page, so its own
            // IsVisibleChanged handler never fires. Restore React explicitly.
            modernLobby?.SetLobbyVisible(lobby?.Visibility == Visibility.Visible);
            UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
        }
    }

    private void CloseAllMetaPages()
    {
        if (metaHost is not null) metaHost.Content = null;
        while (metaPages.TryPop(out MetaPage? page)) page.Shell.Close();
    }
}
