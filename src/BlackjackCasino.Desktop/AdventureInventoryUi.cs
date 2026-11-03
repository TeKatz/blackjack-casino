using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Adventure;
using WpfPath = System.Windows.Shapes.Path;

namespace BlackjackCasino.Desktop;

internal static class AdventureInventoryUi
{
    internal static Border Build(AdventureEngine engine, Func<string, Task>? useChoice = null)
    {
        Border shell = AdventureUi.Panel("#101A17", "#08110F", "#385148", new Thickness(18, 14, 18, 14), radius: 15);
        shell.Margin = new Thickness(0, 16, 20, 0);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(BuildInventory(engine, useChoice));

        FrameworkElement conditions = BuildConditions(engine);
        Grid.SetColumn(conditions, 2);
        grid.Children.Add(conditions);

        shell.Child = grid;
        return shell;
    }

    internal static Border BuildEventFeed(IReadOnlyList<AdventureFeedbackEvent> feedback, bool animationsEnabled)
    {
        Border panel = AdventureUi.Panel("#111C19", "#091310", "#30443D", new Thickness(12, 10, 12, 10), radius: 10, shadow: false);
        panel.Width = 214;
        panel.MinHeight = 76;
        panel.HorizontalAlignment = HorizontalAlignment.Right;
        panel.VerticalAlignment = VerticalAlignment.Center;

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.Children.Add(new TextBlock
        {
            Text = "ADVENTURE EVENT",
            Foreground = AdventureUi.Ink("#607A70"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
        });

        var host = new Border { Margin = new Thickness(0, 5, 0, 0), MinHeight = 42 };
        Grid.SetRow(host, 1);
        layout.Children.Add(host);
        panel.Child = layout;

        if (feedback.Count == 0)
        {
            host.Child = new TextBlock
            {
                Text = "No recent event",
                Foreground = AdventureUi.Ink("#536A62"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
            };
            return panel;
        }

        if (!animationsEnabled)
        {
            AdventureFeedbackEvent latest = feedback[^1];
            panel.BorderBrush = AdventureUi.Ink(AccentFor(latest.Kind));
            host.Child = BuildFeedbackContent(latest);
            return panel;
        }

        host.Opacity = 0;
        panel.Loaded += async (_, _) => await PlayFeedbackQueueAsync(panel, host, feedback);
        return panel;
    }

    private static FrameworkElement BuildInventory(AdventureEngine engine, Func<string, Task>? useChoice)
    {
        var stack = new StackPanel();
        stack.Children.Add(AdventureUi.Eyebrow("PACK & COINS", 10.5));

        var wrap = new WrapPanel { Margin = new Thickness(0, 7, 0, 0) };
        wrap.Children.Add(ItemChip("coin", "Coins", engine.State.Coins.ToString(System.Globalization.CultureInfo.InvariantCulture), "Spendable during this adventure.", "#D6B860"));

        foreach (AdventureItemState state in engine.State.ItemStacks.Values.Where(item => item.Quantity > 0).OrderBy(item => item.ItemId))
        {
            AdventureItemDefinition? definition = engine.ItemDefinition(state.ItemId);
            string name = definition?.Name ?? Humanize(state.ItemId);
            string icon = definition?.IconKey ?? "item";
            string quantity = state.Quantity > 1 ? $"×{state.Quantity}" : "";
            string description = definition?.Description ?? "Adventure item.";
            string accent = definition?.Kind switch
            {
                AdventureItemKind.Quest => "#D8BD76",
                AdventureItemKind.Consumable => "#B76F69",
                AdventureItemKind.Relic => "#9A7CC8",
                _ => "#82B8A2",
            };
            Border chip = ItemChip(icon, name, quantity, description, accent);
            AdventureChoiceDefinition[] uses = engine.PendingCheck is null && !engine.State.Completed
                ? engine.AvailableChoices().Where(choice => (choice.Conditions ?? []).Any(condition =>
                    condition.Kind == AdventureConditionKind.ItemOwned && condition.Expected && condition.Key == state.ItemId)).ToArray()
                : [];
            if (useChoice is not null && uses.Length > 0)
            {
                chip.Cursor = CasinoCursors.Hand;
                chip.Focusable = true;
                chip.BorderBrush = AdventureUi.Ink("#E0C883");
                chip.ToolTip = description + "\n\nClick to use here.";
                var menu = new ContextMenu { Background = AdventureUi.Ink("#13201C"), Foreground = AdventureUi.Ink("#F0E0C6") };
                foreach (AdventureChoiceDefinition choice in uses)
                {
                    var option = new MenuItem { Header = choice.Text, Background = menu.Background, Foreground = menu.Foreground };
                    option.Click += async (_, _) => await useChoice(choice.Id);
                    menu.Items.Add(option);
                }
                chip.ContextMenu = menu;
                chip.MouseLeftButtonUp += (_, e) => { menu.PlacementTarget = chip; menu.IsOpen = true; e.Handled = true; };
                chip.KeyDown += (_, e) =>
                {
                    if (e.Key is not System.Windows.Input.Key.Enter and not System.Windows.Input.Key.Space) return;
                    menu.PlacementTarget = chip; menu.IsOpen = true; e.Handled = true;
                };
                System.Windows.Automation.AutomationProperties.SetName(chip, "Use " + name);
            }
            wrap.Children.Add(chip);
        }

        if (engine.State.ItemStacks.Values.All(item => item.Quantity <= 0))
        {
            wrap.Children.Add(new TextBlock
            {
                Text = "No carried items yet.",
                Foreground = AdventureUi.Ink("#6F857D"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 11,
                Margin = new Thickness(4, 10, 0, 0),
            });
        }

        stack.Children.Add(new ScrollViewer
        {
            Content = wrap, MaxHeight = 112, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        });
        return stack;
    }

    private static FrameworkElement BuildConditions(AdventureEngine engine)
    {
        var stack = new StackPanel();
        stack.Children.Add(AdventureUi.Eyebrow("CONDITIONS", 10.5));
        var wrap = new WrapPanel { Margin = new Thickness(0, 7, 0, 0) };

        foreach (AdventureStatusState state in engine.State.ActiveConditions.Values.OrderBy(condition => condition.ConditionId))
        {
            AdventureStatusDefinition? definition = engine.ConditionDefinition(state.ConditionId);
            string name = definition?.Name ?? Humanize(state.ConditionId);
            string duration = state.Unit == AdventureDurationUnit.Permanent
                ? "UNTIL CLEARED"
                : $"{state.Remaining} {state.Unit.ToString().ToUpperInvariant()}{(state.Remaining == 1 ? "" : "S")}";
            string accent = definition?.Beneficial == true ? "#76BA97" : state.ConditionId == "bleeding" ? "#C66D67" : "#C2A664";
            Border chip = ItemChip(definition?.IconKey ?? "status", name, duration, definition?.Description ?? "Temporary condition.", accent);
            chip.Effect = new DropShadowEffect
            {
                BlurRadius = 9,
                ShadowDepth = 0,
                Opacity = .16,
                Color = (Color)ColorConverter.ConvertFromString(accent),
            };
            wrap.Children.Add(chip);
        }

        if (engine.State.ActiveConditions.Count == 0)
        {
            wrap.Children.Add(new TextBlock
            {
                Text = "No active conditions.",
                Foreground = AdventureUi.Ink("#6F857D"),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 11,
                Margin = new Thickness(4, 10, 0, 0),
            });
        }

        stack.Children.Add(wrap);
        return stack;
    }

    private static async Task PlayFeedbackQueueAsync(Border panel, Border host, IReadOnlyList<AdventureFeedbackEvent> feedback)
    {
        foreach (AdventureFeedbackEvent item in feedback)
        {
            if (!panel.IsLoaded) return;

            panel.BorderBrush = AdventureUi.Ink(AccentFor(item.Kind));
            host.Child = BuildFeedbackContent(item);
            host.Opacity = 0;
            var translate = new TranslateTransform(8, 0);
            host.RenderTransform = translate;

            host.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });
            translate.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(380))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });

            await Task.Delay(3600);
            if (!panel.IsLoaded) return;

            host.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
                });
            await Task.Delay(540);
        }

        if (!panel.IsLoaded) return;
        host.BeginAnimation(UIElement.OpacityProperty, null);
        host.Opacity = 1;
        host.RenderTransform = Transform.Identity;
        host.Child = new TextBlock
        {
            Text = "No recent event",
            Foreground = AdventureUi.Ink("#536A62"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.BorderBrush = AdventureUi.Ink("#30443D");
    }

    private static FrameworkElement BuildFeedbackContent(AdventureFeedbackEvent feedback)
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(31) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        FrameworkElement icon = Icon(feedback.Key, AccentFor(feedback.Kind), 25);
        icon.VerticalAlignment = VerticalAlignment.Center;
        content.Children.Add(icon);

        var copy = new StackPanel { Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = feedback.Title.ToUpperInvariant(),
            Foreground = AdventureUi.Ink("#E8D4A6"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9.2,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        copy.Children.Add(new TextBlock
        {
            Text = feedback.Detail,
            Foreground = AdventureUi.Ink("#AFBFB8"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.2,
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 36,
            Margin = new Thickness(0, 3, 0, 0),
        });
        Grid.SetColumn(copy, 1);
        content.Children.Add(copy);
        return content;
    }

    private static Border ItemChip(string iconKey, string name, string suffix, string description, string accent)
    {
        var chip = new Border
        {
            Background = AdventureUi.Ink("#13211D"),
            BorderBrush = AdventureUi.Ink(accent),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(8, 6, 9, 6),
            Margin = new Thickness(0, 0, 8, 7),
            ToolTip = description,
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Icon(iconKey, accent, 24));
        var text = new StackPanel { Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock
        {
            Text = name,
            Foreground = AdventureUi.Ink("#E5D9C4"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
        });
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            text.Children.Add(new TextBlock
            {
                Text = suffix,
                Foreground = AdventureUi.Ink(accent),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
            });
        }
        row.Children.Add(text);
        chip.Child = row;
        return chip;
    }

    private static FrameworkElement Icon(string key, string accent, double size)
    {
        var grid = new Grid { Width = size, Height = size };
        Brush brush = AdventureUi.Ink(accent);
        string normalized = key.ToLowerInvariant();

        Geometry? geometry = normalized switch
        {
            "key" => Geometry.Parse("M3,12 A4,4 0 1 1 11,12 A4,4 0 1 1 3,12 M11,12 L22,12 M17,12 L17,16 M20,12 L20,15"),
            "potion-red" or "potion-green" => Geometry.Parse("M9,3 L15,3 M10,3 L10,8 L6,15 C4,20 7,22 12,22 C17,22 20,20 18,15 L14,8 L14,3 Z M7,15 L17,15"),
            "rope" => Geometry.Parse("M5,7 C5,2 13,2 13,7 C13,12 5,12 5,17 C5,22 13,22 13,17 C13,12 19,12 19,7 C19,2 13,2 13,7"),
            "coin" => Geometry.Parse("M12,2 A10,10 0 1 1 11.9,2 M12,6 L12,18 M8,9 C8,6 16,6 16,10 C16,14 8,10 8,15 C8,19 16,18 16,15"),
            "letter" => Geometry.Parse("M3,5 L21,5 L21,19 L3,19 Z M3,6 L12,14 L21,6"),
            "ledger" or "list" => Geometry.Parse("M5,3 L19,3 L19,21 L5,21 Z M9,3 L9,21 M11,8 L17,8 M11,12 L17,12 M11,16 L17,16"),
            "token" or "seal" => Geometry.Parse("M12,2 L18,5 L22,11 L19,18 L12,22 L5,18 L2,11 L6,5 Z M8,13 C9,8 15,8 16,13"),
            "leaf-charm" => Geometry.Parse("M4,18 C5,7 12,3 20,4 C20,13 15,20 6,20 M6,20 C10,15 13,11 18,7"),
            "rubbing" => Geometry.Parse("M4,3 L20,3 L20,21 L4,21 Z M8,17 C10,8 14,8 16,17 M10,11 L7,7 M14,11 L17,7"),
            "blood" => Geometry.Parse("M12,2 C10,7 5,11 5,16 A7,7 0 0 0 19,16 C19,11 14,7 12,2 Z"),
            "poison" => Geometry.Parse("M8,8 A4,4 0 1 1 16,8 A4,4 0 1 1 8,8 M6,18 L18,18 M8,14 L16,22 M16,14 L8,22"),
            "eye-slash" or "eye-hidden" => Geometry.Parse("M2,12 C7,5 17,5 22,12 C17,19 7,19 2,12 Z M9,12 A3,3 0 1 1 15,12 M4,3 L20,21"),
            "chains" or "bindings" => Geometry.Parse("M4,8 C4,4 10,4 10,8 C10,12 4,12 4,16 C4,20 10,20 10,16 M14,8 C14,4 20,4 20,8 C20,12 14,12 14,16 C14,20 20,20 20,16 M9,12 L15,12"),
            "prone" => Geometry.Parse("M4,17 L20,17 M7,13 L12,10 L17,13 M12,10 L12,6 M12,6 A2,2 0 1 1 11.9,6"),
            "stun" or "lightning" => Geometry.Parse("M14,2 L6,13 L11,13 L9,22 L18,10 L13,10 Z"),
            "fear" => Geometry.Parse("M12,3 L21,20 L3,20 Z M12,8 L12,14 M12,17 L12,18"),
            "sun" => Geometry.Parse("M12,6 A6,6 0 1 1 11.9,6 M12,1 L12,4 M12,20 L12,23 M1,12 L4,12 M20,12 L23,12 M4,4 L6,6 M18,18 L20,20 M20,4 L18,6 M6,18 L4,20"),
            "rune" => Geometry.Parse("M12,2 L18,8 L15,12 L20,18 L12,22 L4,18 L9,12 L6,8 Z"),
            "hood" => Geometry.Parse("M5,20 C5,10 7,4 12,3 C17,4 19,10 19,20 M8,13 C10,16 14,16 16,13"),
            _ => null,
        };

        if (geometry is not null)
        {
            grid.Children.Add(new WpfPath
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = 1.6,
                Fill = Brushes.Transparent,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2),
            });
        }
        else
        {
            grid.Children.Add(new TextBlock
            {
                Text = "◆",
                Foreground = brush,
                FontSize = size * .62,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        return grid;
    }

    private static string AccentFor(AdventureFeedbackKind kind) => kind switch
    {
        AdventureFeedbackKind.ConditionApplied => "#C8766D",
        AdventureFeedbackKind.ConditionRemoved => "#76B99A",
        AdventureFeedbackKind.ItemAcquired => "#D2B56E",
        AdventureFeedbackKind.ItemRemoved => "#A68A60",
        AdventureFeedbackKind.CoinsChanged => "#D2B56E",
        AdventureFeedbackKind.HitPointsChanged => "#C8766D",
        _ => "#729B8A",
    };

    private static string Humanize(string value) =>
        string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
