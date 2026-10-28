using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ComboBox? tablePresetComboBox;
    private TextBlock? tablePresetDescription;
    private Button? advancedTableSetupButton;
    private GameRules currentTableRules = TablePresets.Get(TablePresetId.Casual).Rules with { };
    private GameRules customTableRules = TablePresets.Get(TablePresetId.Casual).Rules with { };
    private TablePresetId currentTablePreset = TablePresetId.Casual;
    private bool tableRulesInitialized;

    private void InitializeTableRulesFeature()
    {
        if (tableRulesInitialized)
        {
            return;
        }

        tableRulesInitialized = true;
        currentTablePreset = preferences.TablePreset;
        customTableRules = preferences.CustomRules with { };
        currentTableRules = currentTablePreset == TablePresetId.Custom
            ? customTableRules with { }
            : TablePresets.Get(currentTablePreset).Rules with { };

        BuildTableRulesUi();
        RecreateSessionForConfiguredRules(announce: false);

        // The original handlers recreate a default session when mode/seats change.
        // This second handler restores the chosen rule set immediately afterwards.
        GameModeComboBox.SelectionChanged += RestoreConfiguredRulesAfterTableModeChange;
        AiSeatCountComboBox.SelectionChanged += RestoreConfiguredRulesAfterTableModeChange;

        if (ResetBankrollButton.Parent is StackPanel tableSection)
        {
            tableSection.AddHandler(Button.ClickEvent, new RoutedEventHandler(TableSectionButtonClicked));
        }

        if (DealButton.Parent is StackPanel bettingSection)
        {
            bettingSection.AddHandler(Button.ClickEvent, new RoutedEventHandler(TableSectionButtonClicked));
        }

        UpdateTableRulePresentation();
    }

    private void BuildTableRulesUi()
    {
        if (ResetBankrollButton.Parent is not StackPanel tableSection)
        {
            return;
        }

        var label = new TextBlock
        {
            Text = "Table preset",
            Margin = new Thickness(0, 12, 0, 6),
            Foreground = UiBrush("MutedTextBrush"),
        };

        tablePresetComboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        foreach (TablePreset preset in TablePresets.All)
        {
            tablePresetComboBox.Items.Add(new ComboBoxItem
            {
                Content = preset.Name,
                Tag = preset.Id,
            });
        }

        tablePresetComboBox.Items.Add(new ComboBoxItem
        {
            Content = "Custom",
            Tag = TablePresetId.Custom,
        });
        tablePresetComboBox.SelectedIndex = PresetIndex(currentTablePreset);
        tablePresetComboBox.SelectionChanged += TablePresetComboBox_SelectionChanged;

        tablePresetDescription = new TextBlock
        {
            Margin = new Thickness(1, 7, 1, 0),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = UiBrush("SoftTextBrush"),
        };

        advancedTableSetupButton = new Button
        {
            Content = "Advanced table setup…",
            Margin = new Thickness(4, 10, 4, 0),
            Style = (Style)FindResource("MutedButton"),
        };
        advancedTableSetupButton.Click += AdvancedTableSetupButton_Click;

        var casualNote = new TextBlock
        {
            Text = "Casual Solo stays the default: no setup is required to just sit down and play.",
            Margin = new Thickness(1, 8, 1, 0),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 10,
            Foreground = UiBrush("SoftTextBrush"),
        };

        int insertIndex = Math.Max(1, tableSection.Children.IndexOf(ResetBankrollButton));
        tableSection.Children.Insert(insertIndex++, label);
        tableSection.Children.Insert(insertIndex++, tablePresetComboBox);
        tableSection.Children.Insert(insertIndex++, tablePresetDescription);
        tableSection.Children.Insert(insertIndex++, advancedTableSetupButton);
        tableSection.Children.Insert(insertIndex, casualNote);
    }

    private void TablePresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!tableRulesInitialized || suppressPreferenceEvents || tablePresetComboBox?.SelectedItem is not ComboBoxItem item || item.Tag is not TablePresetId preset)
        {
            return;
        }

        if (session.RoundInProgress)
        {
            tablePresetComboBox.SelectedIndex = PresetIndex(currentTablePreset);
            StatusText.Text = "Table rules can only be changed between hands.";
            return;
        }

        currentTablePreset = preset;
        currentTableRules = preset == TablePresetId.Custom
            ? customTableRules with { }
            : TablePresets.Get(preset).Rules with { };

        RecreateSessionForConfiguredRules(announce: true);
    }

    private void AdvancedTableSetupButton_Click(object sender, RoutedEventArgs e)
    {
        if (session.RoundInProgress)
        {
            StatusText.Text = "Finish the current hand before changing table rules.";
            return;
        }

        var dialog = new TableRulesDialog(currentTableRules)
        {
            Owner = this,
        };

        if (dialog.ShowDialog() != true || dialog.ResultRules is null)
        {
            return;
        }

        customTableRules = dialog.ResultRules;
        currentTableRules = customTableRules with { };
        currentTablePreset = TablePresetId.Custom;

        suppressPreferenceEvents = true;
        if (tablePresetComboBox is not null)
        {
            tablePresetComboBox.SelectedIndex = PresetIndex(TablePresetId.Custom);
        }
        suppressPreferenceEvents = false;

        RecreateSessionForConfiguredRules(announce: true);
    }

    private void RestoreConfiguredRulesAfterTableModeChange(object? sender, SelectionChangedEventArgs e)
    {
        if (!tableRulesInitialized || suppressPreferenceEvents || session.RoundInProgress)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (!session.RoundInProgress && !RulesEquivalent(session.Rules, currentTableRules))
            {
                RecreateSessionForConfiguredRules(announce: false);
            }
        }, DispatcherPriority.ContextIdle);
    }

    private void RecreateSessionForConfiguredRules(bool announce)
    {
        decimal bankroll = session.Bankroll;
        IReadOnlyList<AiPlayerProfile> aiPlayers = playMode == PlayModePreference.FullTable
            ? BuildAiProfiles(aiSeatCount)
            : [];

        session = new CasinoSession(currentTableRules with { }, bankroll, aiPlayers: aiPlayers);
        NormalizeBetForTable();

        preferences = preferences with
        {
            TablePreset = currentTablePreset,
            CustomRules = customTableRules with { },
        };

        if (announce)
        {
            string name = currentTablePreset == TablePresetId.Custom
                ? "Custom"
                : TablePresets.Get(currentTablePreset).Name;
            StatusText.Text = $"{name} table ready. Place a bet to begin.";
        }

        SavePreferences();
        RefreshGameUi();
        UpdateTableRulePresentation();
        ApplyBetLimitState();
    }

    private void NormalizeBetForTable()
    {
        decimal ceiling = Math.Min(session.Bankroll, currentTableRules.MaximumBet);
        selectedBet = Math.Min(selectedBet, ceiling);

        if (selectedBet > 0m && selectedBet < currentTableRules.MinimumBet && session.Bankroll >= currentTableRules.MinimumBet)
        {
            selectedBet = currentTableRules.MinimumBet;
        }

        if (selectedBet == 0m && session.Bankroll >= currentTableRules.MinimumBet)
        {
            selectedBet = Math.Min(25m, ceiling);
            if (selectedBet < currentTableRules.MinimumBet)
            {
                selectedBet = currentTableRules.MinimumBet;
            }
        }
    }

    private void TableSectionButtonClicked(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(ApplyBetLimitState, DispatcherPriority.ContextIdle);
    }

    private void ApplyBetLimitState()
    {
        if (session.RoundInProgress)
        {
            return;
        }

        if (selectedBet > currentTableRules.MaximumBet)
        {
            selectedBet = Math.Min(currentTableRules.MaximumBet, session.Bankroll);
            SavePreferences();
            RefreshGameUi();
        }

        bool validBet = selectedBet >= currentTableRules.MinimumBet
            && selectedBet <= currentTableRules.MaximumBet
            && selectedBet <= session.Bankroll;
        DealButton.IsEnabled = validBet;
        if (tablePresetComboBox is not null)
        {
            tablePresetComboBox.IsEnabled = true;
        }
        if (advancedTableSetupButton is not null)
        {
            advancedTableSetupButton.IsEnabled = true;
        }
    }

    private void UpdateTableRulePresentation()
    {
        if (tablePresetDescription is not null)
        {
            tablePresetDescription.Text = currentTablePreset == TablePresetId.Custom
                ? "Your saved custom rules. Advanced setup changes only this profile."
                : TablePresets.Get(currentTablePreset).Description;
        }

        if (ResetBankrollButton.Parent is StackPanel tableSection
            && tableSection.Children.OfType<Grid>().FirstOrDefault() is Grid summary)
        {
            SetSummaryValue(summary, 1, RuleHeadline());
            SetSummaryValue(summary, 2, BlackjackPayoutText());
            SetSummaryValue(summary, 3, currentTableRules.AllowLateSurrender ? "Late" : "Off");
        }

        TextBlock[] tablePrint = FindVisualChildren<TextBlock>(TableSurface).ToArray();
        TextBlock? payout = tablePrint.FirstOrDefault(x => x.Text.StartsWith("BLACKJACK PAYS", StringComparison.Ordinal));
        if (payout is not null)
        {
            payout.Text = currentTableRules.BlackjackPayout == 1.2m ? "BLACKJACK PAYS 6 TO 5" : "BLACKJACK PAYS 3 TO 2";
        }

        TextBlock? dealerRule = tablePrint.FirstOrDefault(x => x.Text.StartsWith("DEALER ", StringComparison.Ordinal) && x.Text.Contains("17", StringComparison.Ordinal));
        if (dealerRule is not null)
        {
            dealerRule.Text = currentTableRules.DealerHitsSoft17 ? "DEALER HITS SOFT 17" : "DEALER STANDS ON ALL 17";
        }
    }

    private string RuleHeadline()
    {
        string hole = currentTableRules.DealerTakesHoleCard ? string.Empty : " · ENHC";
        return $"{currentTableRules.DeckCount}D · {(currentTableRules.DealerHitsSoft17 ? "H17" : "S17")}{hole}";
    }

    private string BlackjackPayoutText() => currentTableRules.BlackjackPayout switch
    {
        1.2m => "6:5",
        1.5m => "3:2",
        _ => $"{currentTableRules.BlackjackPayout:0.##}:1",
    };

    private static void SetSummaryValue(Grid summary, int row, string value)
    {
        TextBlock? target = summary.Children.OfType<TextBlock>()
            .FirstOrDefault(x => Grid.GetRow(x) == row && Grid.GetColumn(x) == 1);
        if (target is not null)
        {
            target.Text = value;
        }
    }

    private static int PresetIndex(TablePresetId preset) => preset switch
    {
        TablePresetId.Casual => 0,
        TablePresetId.LasVegasStrip => 1,
        TablePresetId.DowntownVegas => 2,
        TablePresetId.AtlanticCity => 3,
        TablePresetId.European => 4,
        TablePresetId.SingleDeck => 5,
        TablePresetId.Custom => 6,
        _ => 0,
    };

    private static bool RulesEquivalent(GameRules left, GameRules right) => left == right;

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (T nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }
}
