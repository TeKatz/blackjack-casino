using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

internal sealed class AdventureFiveOfAKindEncounterAdapter : IAdventureEncounterAdapter
{
    public AdventureEncounterType Type => AdventureEncounterType.FiveOfAKind;

    public FrameworkElement Create(
        AdventureEncounterDefinition definition,
        bool animationsEnabled,
        Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed) =>
        new AdventureFiveOfAKindEncounterView(definition, animationsEnabled, completed);
}

internal sealed class AdventureFiveOfAKindEncounterView : Grid
{
    private readonly AdventureEncounterDefinition definition;
    private readonly AdventureFiveOfAKindEncounterRules rules;
    private readonly bool animationsEnabled;
    private readonly Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed;
    private readonly KniffelGame player = new();
    private readonly KniffelGame opponent = new();
    private readonly KniffelDie[] playerDice = new KniffelDie[5];
    private readonly KniffelDie[] opponentDice = new KniffelDie[5];
    private readonly Button[] playerDiceButtons = new Button[5];
    private readonly RotateTransform[] playerRotations = new RotateTransform[5];
    private readonly RotateTransform[] opponentRotations = new RotateTransform[5];
    private readonly TranslateTransform[] playerTranslations = new TranslateTransform[5];
    private readonly TranslateTransform[] opponentTranslations = new TranslateTransform[5];
    private readonly TextBlock playerSummary = SummaryText();
    private readonly TextBlock opponentSummary = SummaryText();
    private readonly TextBlock statusText = new()
    {
        FontFamily = new FontFamily("Georgia"),
        FontSize = 14.5,
        Foreground = AdventureUi.Ink("#DCCDAF"),
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 760,
    };
    private readonly StackPanel actionRow = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Border panel;
    private AdventureEncounterOutcome? outcome;
    private KniffelCategory? playerCategory;
    private KniffelCategory? opponentCategory;
    private int? playerScore;
    private int? opponentScore;
    private bool busy;
    private bool returning;

    internal AdventureFiveOfAKindEncounterView(
        AdventureEncounterDefinition definition,
        bool animationsEnabled,
        Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        rules = definition.FiveOfAKind
            ?? throw new ArgumentException("Five of a Kind encounter is missing dice rules.", nameof(definition));
        this.animationsEnabled = animationsEnabled;
        this.completed = completed ?? throw new ArgumentNullException(nameof(completed));

        Background = new SolidColorBrush(Color.FromArgb(190, 3, 8, 7));
        panel = AdventureUi.Panel("#15231C", "#080E0C", "#8B7445", new Thickness(30, 24, 30, 27), radius: 19);
        panel.Width = 980;
        panel.MaxHeight = 780;
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.Effect = new DropShadowEffect
        {
            BlurRadius = 35,
            ShadowDepth = 8,
            Opacity = .55,
            Color = Colors.Black,
        };
        Children.Add(panel);

        BuildLayout();
        Refresh();

        if (animationsEnabled)
        {
            Opacity = 0;
            panel.RenderTransformOrigin = new Point(.5, .5);
            panel.RenderTransform = new ScaleTransform(.97, .97);
            Loaded += (_, _) => AnimateIn();
        }
    }

    private void BuildLayout()
    {
        var root = new StackPanel();
        root.Children.Add(AdventureUi.Eyebrow("ADVENTURE ENCOUNTER · FIVE OF A KIND", 10.5));
        root.Children.Add(new TextBlock
        {
            Text = definition.Title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 34,
            Foreground = AdventureUi.Ink("#F3DFC0"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 3),
        });
        root.Children.Add(new TextBlock
        {
            Text = definition.OpponentName,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13,
            Foreground = AdventureUi.Ink("#90C6AE"),
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        root.Children.Add(new TextBlock
        {
            Text = definition.Intro,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 13.5,
            LineHeight = 20,
            Foreground = AdventureUi.Ink("#B9C7BF"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 760,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 10),
        });

        Border stakes = AdventureUi.Panel("#1A271F", "#101813", "#594B31", new Thickness(13, 8, 13, 8), radius: 10, shadow: false);
        stakes.HorizontalAlignment = HorizontalAlignment.Center;
        stakes.Child = new TextBlock
        {
            Text = definition.Stakes,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11,
            Foreground = AdventureUi.Ink("#D2B978"),
            TextAlignment = TextAlignment.Center,
        };
        root.Children.Add(stakes);
        root.Children.Add(AdventureUi.OrnamentRule("◆"));

        root.Children.Add(HandLabel(definition.OpponentName.ToUpperInvariant()));
        root.Children.Add(BuildDiceRow(opponentDice, opponentRotations, opponentTranslations, null, "dice-obsidian"));
        root.Children.Add(opponentSummary);

        root.Children.Add(new Border
        {
            Height = 1,
            Margin = new Thickness(70, 10, 70, 10),
            Background = AdventureUi.Ink("#354A42"),
        });

        root.Children.Add(HandLabel("YOUR DICE"));
        root.Children.Add(BuildDiceRow(playerDice, playerRotations, playerTranslations, playerDiceButtons, "dice-ivory"));
        root.Children.Add(playerSummary);

        statusText.Margin = new Thickness(0, 12, 0, 9);
        root.Children.Add(statusText);
        root.Children.Add(actionRow);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root,
        };
        panel.Child = scroll;
    }

    private StackPanel BuildDiceRow(
        KniffelDie[] dice,
        RotateTransform[] rotations,
        TranslateTransform[] translations,
        Button[]? buttons,
        string variant)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2),
        };

        for (int i = 0; i < dice.Length; i++)
        {
            rotations[i] = new RotateTransform();
            translations[i] = new TranslateTransform();
            var transform = new TransformGroup();
            transform.Children.Add(rotations[i]);
            transform.Children.Add(translations[i]);
            dice[i] = new KniffelDie
            {
                Width = 82,
                Height = 86,
                Value = 0,
                VisualVariant = variant,
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = transform,
            };

            if (buttons is null)
            {
                var holder = new Border { Margin = new Thickness(4, 0, 4, 0), Child = dice[i] };
                row.Children.Add(holder);
                continue;
            }

            int index = i;
            var button = new Button
            {
                Content = dice[i],
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = CasinoCursors.Hand,
                ToolTip = $"Hold or release die {i + 1}",
            };
            button.Click += (_, _) => ToggleHold(index);
            buttons[i] = button;
            row.Children.Add(button);
        }

        return row;
    }

    private void Refresh()
    {
        if (playerScore is null)
        {
            for (int i = 0; i < 5; i++)
            {
                playerDice[i].Value = player.Dice[i];
                playerDice[i].Held = player.Held[i];
                if (!busy) playerTranslations[i].Y = player.Held[i] ? -6 : 0;
                playerDiceButtons[i].IsEnabled = !busy && player.CanScore && player.RollsUsed < 3;
            }

            if (player.CanScore)
            {
                (KniffelCategory category, int score) = BestPreview(player);
                playerSummary.Text = $"Roll {player.RollsUsed}/3 · Best now: {CategoryName(category)} · {score} points";
            }
            else
            {
                playerSummary.Text = "Roll 0/3 · click ROLL to begin";
            }
        }
        else
        {
            playerSummary.Text = $"BANKED · {CategoryName(playerCategory!.Value)} · {playerScore.Value} points";
        }

        if (opponentScore is null)
        {
            opponentSummary.Text = opponent.RollsUsed == 0
                ? "Waiting"
                : $"Roll {opponent.RollsUsed}/3";
        }
        else
        {
            opponentSummary.Text = $"BANKED · {CategoryName(opponentCategory!.Value)} · {opponentScore.Value} points";
        }

        if (outcome is not null)
        {
            ShowResultActions();
            return;
        }

        actionRow.Children.Clear();
        actionRow.IsEnabled = !busy;
        if (playerScore is null)
        {
            Button roll = ActionButton(player.RollsUsed == 0 ? "ROLL DICE" : "ROLL AGAIN", RollPlayerAsync, accent: player.RollsUsed == 0);
            roll.IsEnabled = !busy && player.CanRoll;
            actionRow.Children.Add(roll);

            if (player.CanScore && (rules.AllowEarlyBank || player.RollsUsed == 3))
            {
                Button bank = ActionButton("BANK BEST HAND", BankPlayerAsync, accent: player.RollsUsed == 3);
                bank.IsEnabled = !busy;
                actionRow.Children.Add(bank);
            }
        }

        if (busy) return;
        statusText.Text = player.RollsUsed switch
        {
            0 => "Your turn. Roll all five dice, then keep the ones you trust.",
            1 => player.Dice.Distinct().Count() == 1
                ? "Five of a kind on the first throw. You can bank it now."
                : "Click dice to hold them. Roll the rest again or bank the hand.",
            2 => player.Dice.Distinct().Count() == 1
                ? "Five of a kind. There is nothing stronger to chase."
                : "One throw remains. Held dice stay on the table.",
            _ => "Third throw. The dice are final; bank the strongest score.",
        };
    }

    private async Task RollPlayerAsync()
    {
        if (busy || playerScore is not null || !player.CanRoll) return;
        busy = true;
        SetPlayerInteraction(false);
        bool[] rolling = player.Held.Select(held => !held).ToArray();
        player.Roll();
        statusText.Text = "The bone dice tumble across the table…";
        await AnimateRollAsync(playerDice, playerRotations, playerTranslations, player.Dice, player.Held, rolling);
        busy = false;
        Refresh();
        if (player.Dice.Distinct().Count() == 1) await CelebrateFiveOfAKindAsync(playerDice);
    }

    private void ToggleHold(int index)
    {
        if (busy || playerScore is not null || !player.CanScore || player.RollsUsed == 3) return;
        player.ToggleHold(index);
        playerDice[index].Held = player.Held[index];
        double target = player.Held[index] ? -6 : 0;
        if (animationsEnabled)
        {
            playerTranslations[index].BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation(playerTranslations[index].Y, target, TimeSpan.FromMilliseconds(150))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });
        }
        else playerTranslations[index].Y = target;
        Refresh();
    }

    private async Task BankPlayerAsync()
    {
        if (busy || playerScore is not null || !player.CanScore) return;
        busy = true;
        SetPlayerInteraction(false);

        (KniffelCategory category, int score) = BestPreview(player);
        int[] finalDice = player.Dice.ToArray();
        bool five = finalDice.Distinct().Count() == 1;
        playerCategory = category;
        playerScore = player.Commit(category);
        for (int i = 0; i < 5; i++)
        {
            playerDice[i].Value = finalDice[i];
            playerDice[i].Held = true;
            playerTranslations[i].Y = -4;
        }
        playerSummary.Text = $"BANKED · {CategoryName(category)} · {score} points";
        statusText.Text = five
            ? $"Five of a kind · {score} points. The stranger reaches for his dice."
            : $"{CategoryName(category)} · {score} points banked. The stranger reaches for his dice.";
        if (five) await CelebrateFiveOfAKindAsync(playerDice);
        await Task.Delay(animationsEnabled ? 350 : 1);
        await PlayOpponentAsync();
    }

    private async Task PlayOpponentAsync()
    {
        KniffelAiStyle style = rules.OpponentStyle switch
        {
            AdventureFiveOfAKindOpponentStyle.Collector => KniffelAiStyle.Collector,
            AdventureFiveOfAKindOpponentStyle.Balanced => KniffelAiStyle.Balanced,
            _ => KniffelAiStyle.Adventurous,
        };

        while (true)
        {
            bool[] rolling = opponent.Held.Select(held => !held).ToArray();
            opponent.Roll();
            statusText.Text = $"{definition.OpponentName} rolls…";
            await AnimateRollAsync(opponentDice, opponentRotations, opponentTranslations, opponent.Dice, opponent.Held, rolling);
            RefreshOpponentDice();

            KniffelAiChoice choice = KniffelAi.Choose(opponent, style);
            if (choice.ScoreNow || opponent.RollsUsed == 3)
            {
                int[] finalDice = opponent.Dice.ToArray();
                opponentCategory = choice.Category;
                opponentScore = opponent.Commit(choice.Category);
                for (int i = 0; i < 5; i++)
                {
                    opponentDice[i].Value = finalDice[i];
                    opponentDice[i].Held = true;
                    opponentTranslations[i].Y = -4;
                }
                opponentSummary.Text = $"BANKED · {CategoryName(choice.Category)} · {opponentScore.Value} points";
                if (finalDice.Distinct().Count() == 1) await CelebrateFiveOfAKindAsync(opponentDice);
                break;
            }

            ApplyOpponentHoldMask(choice.HoldMask);
            statusText.Text = $"{definition.OpponentName} keeps {CountBits(choice.HoldMask)} dice and reaches for the rest.";
            await Task.Delay(animationsEnabled ? 330 : 1);
        }

        ResolveOutcome();
        busy = false;
        ShowResultActions();
    }

    private void ApplyOpponentHoldMask(int mask)
    {
        for (int i = 0; i < 5; i++)
        {
            bool shouldHold = (mask & (1 << i)) != 0;
            if (opponent.Held[i] != shouldHold) opponent.ToggleHold(i);
            opponentDice[i].Held = opponent.Held[i];
            opponentTranslations[i].Y = opponent.Held[i] ? -6 : 0;
        }
        RefreshOpponentDice();
    }

    private void RefreshOpponentDice()
    {
        for (int i = 0; i < 5; i++)
        {
            opponentDice[i].Value = opponent.Dice[i];
            opponentDice[i].Held = opponent.Held[i];
            if (!busy) opponentTranslations[i].Y = opponent.Held[i] ? -6 : 0;
        }
        opponentSummary.Text = $"Roll {opponent.RollsUsed}/3";
    }

    private void ResolveOutcome()
    {
        int playerPoints = playerScore ?? 0;
        int opponentPoints = opponentScore ?? 0;
        outcome = playerPoints > opponentPoints
            ? AdventureEncounterOutcome.Win
            : playerPoints < opponentPoints
                ? AdventureEncounterOutcome.Loss
                : AdventureEncounterOutcome.Draw;

        statusText.Text = outcome switch
        {
            AdventureEncounterOutcome.Win => $"{playerPoints} to {opponentPoints}. Your hand holds. The stranger leaves the raven token where you can reach it.",
            AdventureEncounterOutcome.Loss => $"{opponentPoints} to {playerPoints}. The stranger's dice take the table and his hand closes over the wager.",
            _ => $"{playerPoints} to {opponentPoints}. Equal marks. The stranger looks almost amused.",
        };
        FadeIn(statusText);
    }

    private void ShowResultActions()
    {
        actionRow.Children.Clear();
        actionRow.IsEnabled = true;
        if (outcome is null) return;
        Button returnButton = ActionButton("RETURN TO STORY", ReturnToStoryAsync, accent: true);
        returnButton.MinWidth = 210;
        actionRow.Children.Add(returnButton);
    }

    private async Task AnimateRollAsync(
        KniffelDie[] dice,
        RotateTransform[] rotations,
        TranslateTransform[] translations,
        IReadOnlyList<int> finalValues,
        IReadOnlyList<bool> held,
        IReadOnlyList<bool> rolling)
    {
        if (!animationsEnabled)
        {
            for (int i = 0; i < dice.Length; i++)
            {
                dice[i].Value = finalValues[i];
                translations[i].X = 0;
                translations[i].Y = held[i] ? -6 : 0;
                rotations[i].Angle = 0;
            }
            return;
        }

        const int frameMs = 30;
        const int totalMs = 820;
        for (int elapsed = 0; elapsed <= totalMs; elapsed += frameMs)
        {
            for (int i = 0; i < dice.Length; i++)
            {
                if (!rolling[i]) continue;
                double duration = 570 + i * 55;
                if (elapsed < duration)
                {
                    double strength = 1 - elapsed / duration;
                    dice[i].Value = Random.Shared.Next(1, 7);
                    rotations[i].Angle = Math.Sin(elapsed / 40d + i) * 24 * strength;
                    translations[i].Y = -Math.Abs(Math.Sin(elapsed / 63d + i)) * 34 * strength;
                    translations[i].X = Math.Sin(elapsed / 55d + i) * 10 * strength;
                }
                else
                {
                    dice[i].Value = finalValues[i];
                    rotations[i].Angle = 0;
                    translations[i].X = 0;
                    translations[i].Y = held[i] ? -6 : 0;
                }
            }
            await Task.Delay(frameMs);
        }

        for (int i = 0; i < dice.Length; i++)
        {
            dice[i].Value = finalValues[i];
            rotations[i].Angle = 0;
            translations[i].X = 0;
            translations[i].Y = held[i] ? -6 : 0;
        }
    }

    private async Task CelebrateFiveOfAKindAsync(IEnumerable<KniffelDie> dice)
    {
        if (!animationsEnabled) return;
        foreach (KniffelDie die in dice)
        {
            die.Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = .75,
                Color = Color.FromRgb(222, 184, 92),
            };
            die.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(.72, 1, TimeSpan.FromMilliseconds(260))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2),
                });
        }
        await Task.Delay(720);
        foreach (KniffelDie die in dice) die.Effect = null;
    }

    private void SetPlayerInteraction(bool enabled)
    {
        actionRow.IsEnabled = enabled;
        foreach (Button button in playerDiceButtons) button.IsEnabled = enabled && player.CanScore && player.RollsUsed < 3;
    }

    private async Task ReturnToStoryAsync()
    {
        if (returning || outcome is null) return;
        returning = true;
        try
        {
            if (animationsEnabled)
            {
                BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
                    });
                await Task.Delay(235);
            }
            await completed(definition, outcome.Value);
        }
        finally
        {
            returning = false;
        }
    }

    private Button ActionButton(string text, Func<Task> action, bool accent = false)
    {
        Button button = AdventureUi.Button(text, accent: accent, fontSize: 13, scaleOnHover: false);
        button.MinWidth = 132;
        button.Margin = new Thickness(6, 0, 6, 0);
        button.Click += async (_, _) => await action();
        return button;
    }

    private void AnimateIn()
    {
        BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
        if (panel.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(.97, 1, TimeSpan.FromMilliseconds(290))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(.97, 1, TimeSpan.FromMilliseconds(290))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });
        }
    }

    private static (KniffelCategory Category, int Score) BestPreview(KniffelGame game) =>
        KniffelScoring.Categories
            .Select(category => (Category: category, Score: game.Preview(category)))
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Category == KniffelCategory.Kniffel)
            .First();

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }
        return count;
    }

    private static string CategoryName(KniffelCategory category) => category switch
    {
        KniffelCategory.ThreeOfAKind => "Three of a Kind",
        KniffelCategory.FourOfAKind => "Four of a Kind",
        KniffelCategory.FullHouse => "Full House",
        KniffelCategory.SmallStraight => "Small Straight",
        KniffelCategory.LargeStraight => "Large Straight",
        KniffelCategory.Kniffel => "Five of a Kind",
        _ => category.ToString(),
    };

    private static void FadeIn(UIElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            });
    }

    private static TextBlock HandLabel(string text) => new()
    {
        Text = text,
        FontFamily = new FontFamily("Georgia"),
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Foreground = AdventureUi.Ink("#78968B"),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 2, 0, 4),
    };

    private static TextBlock SummaryText() => new()
    {
        FontFamily = new FontFamily("Georgia"),
        FontSize = 11.5,
        FontWeight = FontWeights.SemiBold,
        Foreground = AdventureUi.Ink("#C7B98E"),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 2, 0, 0),
    };
}
