using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;

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
            () => OpenProgressionPage("Overview"), CloseMetaPage);
        ShowMetaPage(profile);
    }

    private void OpenProgressionPage(string tab)
    {
        if (progressionController is null) return;
        progressionController.AccruePlaytime();
        ProcessLatestCompletedRound();
        var page = new ProgressionWindow(progressionController);
        page.ConfigureEmbeddedPresentation(CloseMetaPage);
        if (page.Content is FrameworkElement content)
            content.Loaded += (_, _) => page.SelectTab(tab);
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
        // These pages are visual content only: never Show(), ShowDialog() or create a HWND.
        shell.Content = null;
        metaPages.Push(new MetaPage(shell, content));
        metaHost!.Content = content;
        metaOverlay.Visibility = Visibility.Visible;
        Dispatcher.BeginInvoke(() => content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
    }

    private void CloseMetaPage()
    {
        if (!metaPages.TryPop(out MetaPage? page)) return;
        metaHost!.Content = null;
        page.Shell.Close();
        RefreshProfileButton();
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
        }
    }

    private void CloseAllMetaPages()
    {
        if (metaHost is not null) metaHost.Content = null;
        while (metaPages.TryPop(out MetaPage? page)) page.Shell.Close();
    }
}
