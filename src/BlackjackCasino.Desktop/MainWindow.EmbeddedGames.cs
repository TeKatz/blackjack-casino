using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private Grid? embeddedGameOverlay;
    private ContentControl? embeddedGameHost;
    private Window? embeddedGameShell;
    private UIElement? embeddedGameContent;
    private string? embeddedPreviousTitle;
    private bool embeddedGamesInitialized;
    private Viewbox? embeddedScaleViewbox;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += EmbeddedGames_Loaded;
        Closed += (_, _) => CloseEmbeddedGame();
    }

    private void EmbeddedGames_Loaded(object sender, RoutedEventArgs e)
    {
        if (embeddedGamesInitialized)
        {
            return;
        }

        embeddedGamesInitialized = true;
        EnsureEmbeddedGameHost();

    }

    private void EnsureEmbeddedGameHost()
    {
        if (embeddedGameOverlay is not null)
        {
            return;
        }

        if (Content is not Grid root)
        {
            throw new InvalidOperationException("The casino root must be a Grid before embedded games can be hosted.");
        }

        embeddedGameHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };

        embeddedGameOverlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(16, 13, 24)),
            Visibility = Visibility.Collapsed,
            Focusable = true,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
        };
        embeddedGameOverlay.SizeChanged += (_, _) => UpdateEmbeddedGameScale();
        embeddedGameOverlay.Children.Add(embeddedGameHost);
        Grid.SetRow(embeddedGameOverlay, 1);
        Grid.SetRowSpan(embeddedGameOverlay, 1);
        Grid.SetColumnSpan(embeddedGameOverlay, Math.Max(1, root.ColumnDefinitions.Count));
        Panel.SetZIndex(embeddedGameOverlay, 1000);
        root.Children.Add(embeddedGameOverlay);
    }

    private void ShowEmbeddedGame(Window shell, string title, bool muteHostMusic, Action closed)
    {
        EnsureEmbeddedGameHost();
        if (embeddedGameShell is not null || embeddedGameOverlay is null || embeddedGameHost is null)
        {
            return;
        }

        if (shell.Content is not UIElement content)
        {
            shell.Close();
            throw new InvalidOperationException("The casino game does not expose hostable content.");
        }

        // The slot windows own important implicit styles (especially the fully custom Manor
        // ComboBox toggle template). Reparenting their content into MainWindow changes normal
        // WPF resource lookup. Preserve that scope before detaching or Windows theme chrome can
        // leak back in as bright hover/disabled rectangles.
        PreserveEmbeddedPresentation(shell, content);

        embeddedPreviousTitle = Title;
        Title = title;
        embeddedGameShell = shell;
        embeddedGameContent = content;

        // Create an invisible native owner solely so the existing Window lifecycle and
        // Close/Closed cleanup remain reliable. The game UI itself is detached and rendered
        // inside the main casino window, so Windows never shows a second taskbar window.
        shell.Owner = this;
        shell.ShowInTaskbar = false;
        shell.ShowActivated = false;
        _ = new WindowInteropHelper(shell).EnsureHandle();
        shell.Content = null;

        shell.Closed += (_, _) => FinishEmbeddedGame(shell, muteHostMusic, closed);
        embeddedGameHost.Content = content;
        embeddedGameOverlay.Visibility = Visibility.Visible;
        UpdateEmbeddedGameScale();

        // The Blackjack keyboard handler tunnels first. Disable table actions while the
        // overlay is active, then forward unconsumed keys to the original game shell.
        DealButton.IsEnabled = false;
        ClearBetButton.IsEnabled = false;
        SetActionAvailability(new HashSet<PlayerAction>());

        content.PreviewKeyDown += (_, args) => ForwardEmbeddedKey(shell, args);
        if (content is FrameworkElement element)
        {
            element.Focusable = true;
        }

        if (muteHostMusic)
        {
            backgroundMusic.SetEnabled(false);
        }

        // Window.Loaded belongs to the detached shell rather than the rehosted visual tree.
        // Raise it once so Midnight Manor can resume a committed pending feature normally.
        shell.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, shell));

        Dispatcher.BeginInvoke(() =>
        {
            UpdateEmbeddedGameScale();
            embeddedGameOverlay.Focus();
            if (content is FrameworkElement element)
            {
                element.Focus();
            }
        });
    }

    private void PreserveEmbeddedPresentation(Window shell, UIElement content)
    {
        if (content is FrameworkElement root)
        {
            var scopedResources = new ResourceDictionary();
            CopyResources(shell.Resources, scopedResources);
            root.Resources.MergedDictionaries.Insert(0, scopedResources);
            root.UseLayoutRounding = true;
            root.SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(root, TextFormattingMode.Display);
            TextOptions.SetTextHintingMode(root, TextHintingMode.Fixed);
            TextOptions.SetTextRenderingMode(root, TextRenderingMode.ClearType);
        }

        // Fit the authored Viewbox in its real remaining row. A game can have an
        // internal header outside that Viewbox, so overlay-sized overrides clip it.
        embeddedScaleViewbox = FindEmbeddedLogicalChildren<Viewbox>(content).FirstOrDefault();
        if (embeddedScaleViewbox is not null)
        {
            embeddedScaleViewbox.Stretch = Stretch.Uniform;
            embeddedScaleViewbox.StretchDirection = StretchDirection.Both;
            embeddedScaleViewbox.HorizontalAlignment = HorizontalAlignment.Center;
            embeddedScaleViewbox.VerticalAlignment = VerticalAlignment.Center;
            embeddedScaleViewbox.UseLayoutRounding = true;
            embeddedScaleViewbox.SnapsToDevicePixels = true;
        }

        // Resolve the slot-local ComboBox styles while the original Window scope is still
        // authoritative, then pin them explicitly. ItemContainerStyle also covers popup items
        // that are generated only after the user opens the dropdown.
        if (shell.TryFindResource(typeof(ComboBox)) is Style comboStyle)
        {
            Style? itemStyle = shell.TryFindResource(typeof(ComboBoxItem)) as Style;
            foreach (ComboBox combo in FindEmbeddedLogicalChildren<ComboBox>(content))
            {
                combo.Style = comboStyle;
                if (itemStyle is not null)
                {
                    combo.ItemContainerStyle = itemStyle;
                }
            }
        }
    }

    private void UpdateEmbeddedGameScale()
    {
        if (embeddedScaleViewbox is null) return;
        embeddedScaleViewbox.ClearValue(WidthProperty);
        embeddedScaleViewbox.ClearValue(HeightProperty);
        embeddedScaleViewbox.Stretch = Stretch.Uniform;
        embeddedScaleViewbox.StretchDirection = StretchDirection.Both;
    }

    private static IEnumerable<T> FindEmbeddedLogicalChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject dependencyObject)
            {
                continue;
            }

            foreach (T nested in FindEmbeddedLogicalChildren<T>(dependencyObject))
            {
                yield return nested;
            }
        }
    }

    private static void CopyResources(ResourceDictionary source, ResourceDictionary target)
    {
        foreach (DictionaryEntry entry in source)
        {
            target[entry.Key] = entry.Value;
        }

        foreach (ResourceDictionary merged in source.MergedDictionaries)
        {
            var copy = new ResourceDictionary();
            // Copy resolved resources; reloading a relative Source changes its assembly base
            // when the host is a test executable or a different shell.
            CopyResources(merged, copy);
            target.MergedDictionaries.Add(copy);
        }
    }

    private void ForwardEmbeddedKey(Window shell, KeyEventArgs original)
    {
        if (!ReferenceEquals(embeddedGameShell, shell) || original.Handled)
        {
            return;
        }

        PresentationSource? source = PresentationSource.FromVisual(this);
        if (source is null)
        {
            return;
        }

        var forwarded = new KeyEventArgs(Keyboard.PrimaryDevice, source, original.Timestamp, original.Key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent,
            Source = shell,
        };
        shell.RaiseEvent(forwarded);
        if (forwarded.Handled)
        {
            original.Handled = true;
        }
    }

    private void FinishEmbeddedGame(Window shell, bool restoreHostMusic, Action closed)
    {
        if (!ReferenceEquals(embeddedGameShell, shell))
        {
            return;
        }

        if (embeddedGameHost is not null)
        {
            embeddedGameHost.Content = null;
        }
        if (embeddedGameOverlay is not null)
        {
            embeddedGameOverlay.Visibility = Visibility.Collapsed;
        }

        embeddedGameShell = null;
        embeddedGameContent = null;
        embeddedScaleViewbox = null;
        if (embeddedPreviousTitle is not null)
        {
            Title = embeddedPreviousTitle;
        }
        embeddedPreviousTitle = null;

        closed();
        if(lobby is not null) {lobby.Visibility=Visibility.Visible;TableModeText.Text="Casino lobby";}

        if (restoreHostMusic)
        {
            backgroundMusic.SetEnabled(preferences.MusicEnabled);
            backgroundMusic.SetVolume(preferences.MusicVolume);
        }

        NormalizeBetForTable();
        SavePreferences();
        RefreshGameUi();
        ApplyBetLimitState();
        RefreshProfileButton();
    }

    private void CloseEmbeddedGame()
    {
        Window? shell = embeddedGameShell;
        if (shell is null)
        {
            return;
        }

        try
        {
            shell.Close();
        }
        catch (InvalidOperationException)
        {
            // The application is already shutting down; process teardown will own cleanup.
        }
    }

    private void EmbeddedSlots_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current Blackjack hand before visiting the slot machine.";
            return;
        }
        if (progressionController is null)
        {
            return;
        }

        ProcessLatestCompletedRound();
        slotProgression ??= new SlotProgression(progressionController.Service);
        bool levelUp = false;
        var window = new SlotMachineWindow(session.Bankroll, preferences.SlotBet, SoundEnabled, AnimationsEnabled,
            progressionController, spin =>
            {
                session.ResetBankroll(spin.BankrollAfter);
                preferences = preferences with { SlotBet = spin.Bet };
                selectedBet = Math.Min(selectedBet, session.Bankroll);
                SavePreferences();
                ProgressionAward award = slotProgression.Process(spin);
                progressionController.SaveAll();
                levelUp |= award.HasLevelUp;
                return award;
            }, musicEnabled: preferences.MusicEnabled, musicVolume: preferences.MusicVolume);

        ShowEmbeddedGame(window, "Midnight Reels · Blackjack Casino", muteHostMusic: true, closed: () =>
        {
            preferences = preferences with { SlotBet = window.SelectedBet };
            suppressPreferenceEvents = true;
            SoundCheckBox.IsChecked = window.SoundsEnabled;
            AnimationsCheckBox.IsChecked = window.AnimationsEnabled;
            suppressPreferenceEvents = false;
            if (session.CurrentRound is null)
            {
                StatusText.Text = "Welcome back. Place a bet to play Blackjack.";
            }
            if (levelUp)
            {
                Dispatcher.BeginInvoke(new Action(AnimateLevelUpCue));
            }
        });
    }

    private void EmbeddedManor_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current Blackjack hand before entering Midnight Manor.";
            return;
        }
        if (progressionController is null)
        {
            return;
        }

        ProcessLatestCompletedRound();
        var controller = new ManorController(preferences, session.Bankroll, progressionController, committed =>
        {
            session.ResetBankroll(committed.Bankroll);
            preferences = committed;
            selectedBet = Math.Min(selectedBet, session.Bankroll);
        });
        var window = new ManorWindow(controller);

        ShowEmbeddedGame(window, "Midnight Manor · Blackjack Casino", muteHostMusic: true, closed: () =>
        {
            preferences = window.Preferences;
            suppressPreferenceEvents = true;
            SoundCheckBox.IsChecked = preferences.SoundEnabled;
            AnimationsCheckBox.IsChecked = preferences.AnimationsEnabled;
            MusicCheckBox.IsChecked = preferences.MusicEnabled;
            MusicVolumeSlider.Value = preferences.MusicVolume;
            suppressPreferenceEvents = false;
        });
    }
}
