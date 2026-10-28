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

        foreach (Button button in FindPresentationChildren<Button>(this).ToArray())
        {
            if (button.Content is not string label)
            {
                continue;
            }

            if (label == "MIDNIGHT REELS · Slots")
            {
                button.Click -= Slots_Click;
                button.Click += EmbeddedSlots_Click;
            }
            else if (label == "MIDNIGHT MANOR · Video Slot")
            {
                button.Click -= Manor_Click;
                button.Click += EmbeddedManor_Click;
            }
        }
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
        };

        embeddedGameOverlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(16, 13, 24)),
            Visibility = Visibility.Collapsed,
            Focusable = true,
            Child = null,
        };
        embeddedGameOverlay.Children.Add(embeddedGameHost);
        Grid.SetRowSpan(embeddedGameOverlay, Math.Max(1, root.RowDefinitions.Count));
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
            embeddedGameOverlay.Focus();
            if (content is FrameworkElement element)
            {
                element.Focus();
            }
        });
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
        if (embeddedPreviousTitle is not null)
        {
            Title = embeddedPreviousTitle;
        }
        embeddedPreviousTitle = null;

        closed();

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

    private void RecoverSlotPresentationDefaultsIfNeeded()
    {
        // New profiles already default all three presentation systems to on. Recover the
        // accidental legacy all-off state once it is encountered without overriding a
        // deliberate choice to disable only one or two systems.
        if (preferences.SoundEnabled || preferences.AnimationsEnabled || preferences.MusicEnabled)
        {
            return;
        }

        preferences = preferences with
        {
            SoundEnabled = true,
            AnimationsEnabled = true,
            MusicEnabled = true,
            MusicVolume = preferences.MusicVolume > 0d ? preferences.MusicVolume : 0.10d,
        };
        suppressPreferenceEvents = true;
        SoundCheckBox.IsChecked = true;
        AnimationsCheckBox.IsChecked = true;
        MusicCheckBox.IsChecked = true;
        MusicVolumeSlider.Value = preferences.MusicVolume;
        suppressPreferenceEvents = false;
        PlayerStateStore.Save(preferences);
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

        RecoverSlotPresentationDefaultsIfNeeded();
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
            });

        ShowEmbeddedGame(window, "Midnight Reels · Blackjack Casino", muteHostMusic: false, closed: () =>
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
                Dispatcher.BeginInvoke(AnimateLevelUpCue);
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

        RecoverSlotPresentationDefaultsIfNeeded();
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
