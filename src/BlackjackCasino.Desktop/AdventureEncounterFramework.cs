using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

internal interface IAdventureEncounterAdapter
{
    AdventureEncounterType Type { get; }

    FrameworkElement Create(
        AdventureEncounterDefinition definition,
        bool animationsEnabled,
        Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed);
}

internal static class AdventureEncounterFramework
{
    private static readonly IReadOnlyDictionary<AdventureEncounterType, IAdventureEncounterAdapter> Adapters =
        new Dictionary<AdventureEncounterType, IAdventureEncounterAdapter>
        {
            [AdventureEncounterType.Blackjack] = new AdventureBlackjackEncounterAdapter(),
        };

    internal static bool TryCreate(
        AdventureDefinition adventure,
        AdventureSceneDefinition scene,
        bool animationsEnabled,
        Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed,
        out FrameworkElement? overlay)
    {
        overlay = null;
        if (!AdventureEncounterCatalog.TryGet(adventure.Id, scene.Id, out AdventureEncounterDefinition? definition) || definition is null)
            return false;

        if (!Adapters.TryGetValue(definition.Type, out IAdventureEncounterAdapter? adapter))
            throw new InvalidOperationException($"No adventure encounter adapter is registered for '{definition.Type}'.");

        overlay = adapter.Create(definition, animationsEnabled, completed);
        return true;
    }
}

internal sealed class AdventureBlackjackEncounterAdapter : IAdventureEncounterAdapter
{
    public AdventureEncounterType Type => AdventureEncounterType.Blackjack;

    public FrameworkElement Create(
        AdventureEncounterDefinition definition,
        bool animationsEnabled,
        Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed) =>
        new AdventureBlackjackEncounterView(definition, animationsEnabled, completed);
}

internal sealed class AdventureBlackjackEncounterView : Grid
{
    private readonly AdventureEncounterDefinition definition;
    private readonly bool animationsEnabled;
    private readonly Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed;
    private readonly BlackjackRound round;
    private readonly StackPanel dealerCards = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly StackPanel playerCards = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly TextBlock dealerValue = ValueText();
    private readonly TextBlock playerValue = ValueText();
    private readonly TextBlock statusText = new()
    {
        FontFamily = new FontFamily("Georgia"),
        FontSize = 15,
        Foreground = AdventureUi.Ink("#DCCDAF"),
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 650,
    };
    private readonly StackPanel actionRow = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Border panel;
    private AdventureEncounterOutcome? outcome;
    private bool returning;

    internal AdventureBlackjackEncounterView(
        AdventureEncounterDefinition definition,
        bool animationsEnabled,
        Func<AdventureEncounterDefinition, AdventureEncounterOutcome, Task> completed)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.animationsEnabled = animationsEnabled;
        this.completed = completed ?? throw new ArgumentNullException(nameof(completed));

        AdventureBlackjackEncounterRules encounterRules = definition.Blackjack
            ?? throw new ArgumentException("Blackjack encounter is missing blackjack rules.", nameof(definition));
        GameRules rules = BuildRules(encounterRules);
        round = new BlackjackRound(rules, new Shoe(rules), encounterRules.VirtualBet);

        Background = new SolidColorBrush(Color.FromArgb(188, 3, 8, 7));
        panel = AdventureUi.Panel("#15231C", "#080E0C", "#8B7445", new Thickness(30, 25, 30, 27), radius: 19);
        panel.Width = 940;
        panel.MaxHeight = 720;
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

        BuildLayout(encounterRules);
        round.Start();
        if (round.Phase == RoundPhase.InsuranceDecision)
            round.DeclineInsurance();
        Refresh();

        if (animationsEnabled)
        {
            Opacity = 0;
            panel.RenderTransformOrigin = new Point(.5, .5);
            panel.RenderTransform = new ScaleTransform(.97, .97);
            Loaded += (_, _) => AnimateIn();
        }
    }

    private void BuildLayout(AdventureBlackjackEncounterRules encounterRules)
    {
        var root = new StackPanel();
        root.Children.Add(AdventureUi.Eyebrow("ADVENTURE ENCOUNTER · BLACKJACK", 10.5));
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
            FontSize = 14,
            LineHeight = 21,
            Foreground = AdventureUi.Ink("#B9C7BF"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 730,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 11),
        });

        Border stakes = AdventureUi.Panel("#1A271F", "#101813", "#594B31", new Thickness(13, 9, 13, 9), radius: 10, shadow: false);
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
        root.Children.Add(AdventureUi.OrnamentRule("♠"));

        root.Children.Add(HandLabel(definition.OpponentName.ToUpperInvariant()));
        root.Children.Add(dealerCards);
        root.Children.Add(dealerValue);

        root.Children.Add(new Border
        {
            Height = 1,
            Margin = new Thickness(70, 12, 70, 12),
            Background = AdventureUi.Ink("#354A42"),
        });

        root.Children.Add(HandLabel("YOUR HAND"));
        root.Children.Add(playerCards);
        root.Children.Add(playerValue);
        statusText.Margin = new Thickness(0, 12, 0, 10);
        root.Children.Add(statusText);
        root.Children.Add(actionRow);

        panel.Child = root;
    }

    private void Refresh()
    {
        dealerCards.Children.Clear();
        playerCards.Children.Clear();

        bool settled = round.Phase == RoundPhase.Settled;
        for (int i = 0; i < round.DealerHand.Cards.Count; i++)
        {
            bool faceDown = !settled && i == 1;
            dealerCards.Children.Add(PlayingCardPresentation.Create(round.DealerHand.Cards[i], faceDown, compact: false, cardSetId: null));
        }
        foreach (Card card in round.PlayerHands[0].Cards)
            playerCards.Children.Add(PlayingCardPresentation.Create(card, faceDown: false, compact: false, cardSetId: null));

        dealerValue.Text = settled
            ? $"Total {round.DealerHand.Value.Total}"
            : $"Showing {round.DealerHand.Cards[0].Value}";
        playerValue.Text = $"Total {round.PlayerHands[0].Value.Total}";

        actionRow.Children.Clear();
        if (!settled)
        {
            statusText.Text = "Your move. The stranger watches your hand, not your face.";
            IReadOnlySet<PlayerAction> available = round.GetAvailableActions();
            if (available.Contains(PlayerAction.Hit))
                actionRow.Children.Add(ActionButton("HIT", () => Act(PlayerAction.Hit)));
            if (available.Contains(PlayerAction.Stand))
                actionRow.Children.Add(ActionButton("STAND", () => Act(PlayerAction.Stand), accent: true));
            return;
        }

        decimal net = round.Result?.TotalNet ?? 0m;
        outcome = net > 0m
            ? AdventureEncounterOutcome.Win
            : net < 0m
                ? AdventureEncounterOutcome.Loss
                : AdventureEncounterOutcome.Draw;
        statusText.Text = outcome switch
        {
            AdventureEncounterOutcome.Win => "You win the hand. The stranger's expression barely changes, but his fingers leave the black token.",
            AdventureEncounterOutcome.Loss => "The stranger wins. He gathers the cards first, then reaches for the five coins you promised.",
            _ => "Push. Neither side takes the wager. The stranger leaves the deck between you, inviting another hand.",
        };

        Button returnButton = ActionButton("RETURN TO STORY", ReturnToStoryAsync, accent: true);
        returnButton.MinWidth = 210;
        actionRow.Children.Add(returnButton);
    }

    private void Act(PlayerAction action)
    {
        if (round.Phase != RoundPhase.PlayerTurn) return;
        round.Act(action);
        Refresh();
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

    private Button ActionButton(string text, Action action, bool accent = false)
    {
        Button button = AdventureUi.Button(text, accent: accent, fontSize: 13, scaleOnHover: false);
        button.MinWidth = 120;
        button.Margin = new Thickness(6, 0, 6, 0);
        button.Click += (_, _) => action();
        return button;
    }

    private Button ActionButton(string text, Func<Task> action, bool accent = false)
    {
        Button button = AdventureUi.Button(text, accent: accent, fontSize: 13, scaleOnHover: false);
        button.MinWidth = 120;
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

    private static GameRules BuildRules(AdventureBlackjackEncounterRules encounter) => new()
    {
        DeckCount = Math.Clamp(encounter.DeckCount, 1, 8),
        DealerHitsSoft17 = encounter.DealerHitsSoft17,
        DealerPeeksForBlackjack = encounter.DealerPeeksForBlackjack,
        DealerTakesHoleCard = true,
        AllowInsurance = false,
        AllowLateSurrender = false,
        DoubleRule = DoubleRule.AnyTwo,
        DoubleAfterSplit = false,
        ResplitAces = false,
        HitSplitAces = false,
        MaxSplitHands = 1,
        BlackjackPayout = encounter.BlackjackPayout,
        Penetration = .75,
        MinimumBet = encounter.VirtualBet,
        MaximumBet = encounter.VirtualBet,
    };

    private static TextBlock HandLabel(string text) => new()
    {
        Text = text,
        FontFamily = new FontFamily("Georgia"),
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Foreground = AdventureUi.Ink("#78968B"),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 3, 0, 4),
    };

    private static TextBlock ValueText() => new()
    {
        FontFamily = new FontFamily("Georgia"),
        FontSize = 12,
        FontWeight = FontWeights.SemiBold,
        Foreground = AdventureUi.Ink("#C7B98E"),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 3, 0, 0),
    };
}
