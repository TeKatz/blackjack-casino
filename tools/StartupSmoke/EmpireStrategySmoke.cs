using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

internal static class EmpireStrategySmoke
{
    internal static async Task Run(MainWindow window, string output)
    {
        // Supply a late-career fixture without altering the user's profile or adding a window.
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var counter = new EmpireBalanceCounter(); counter.SetAmount(EmpireRules.MaximumCredits,false);
        Require(counter.Text.Contains("Oc",StringComparison.Ordinal),"Empire balance counter truncated the Resort technical credit range.");
        var old = (EmpireService)typeof(MainWindow).GetField("empire", flags)!.GetValue(window)!;
        EmpireState baseState = EmpireState.Normalize(new EmpireState
        {
            Started = true, LastAccruedAt = DateTimeOffset.UtcNow, Prestige = 25, Credits = EmpireRules.MaximumCredits,
            Levels = EmpireCatalog.Departments.ToDictionary(room => room.Id, _ => 100),
            Research = new() { ["construction"] = 2, ["compound"] = 1, ["automation"] = 1 },
        });
        var estate = baseState.Properties[baseState.ActivePropertyId] with { Projects = new(["grand-entrance","executive-offices","resort-wing","skyline-property"]), Levels = EmpireCatalog.Departments.ToDictionary(room=>room.Id,_=>100) };
        var service = new EmpireService(baseState with { Properties = new(baseState.Properties) { [baseState.ActivePropertyId] = estate } });
        typeof(MainWindow).GetField("empire",flags)!.SetValue(window,service);
        try
        {
            NavigationSmoke.OpenCovered(window, "casino-empire"); await Task.Delay(100); window.UpdateLayout();
            int windows = Application.Current.Windows.Cast<Window>().Count(w=>w.IsVisible);
            Click(window,"EmpireFloor3Button"); window.UpdateLayout();
            Require(Find<EmpireRoomCard>(window).Count(card=>card.IsVisible)==5,"Resort Floor did not render its five departments.");
            Require(Find<Grid>(window).Single(g => g.Name == "EmpireRoomIntelligence").IsVisible, "Estate overview panel is missing.");
            Require(!Find<Button>(window).Any(b => b.Name == "EmpireQuickResearch"), "The permanent feature cards returned.");
            Capture(window,Path.Combine(output,"empire-resort.png"));
            Click(window, "EmpireRoom_hotel");
            foreach (var size in new[] { (1080d,720d), (1366d,768d), (1920d,1080d) })
            {
                window.Width = size.Item1; window.Height = size.Item2; window.UpdateLayout(); await Task.Delay(80); window.UpdateLayout();
                foreach (string name in new[] { "EmpireOverviewButton", "EmpireDevelopmentButton", "EmpireOperationsButton", "EmpireOpportunitiesButton", "EmpireUpgradeButton", "EmpireClaimButton" })
                {
                    Button visible = Find<Button>(window).Single(b => b.Name == name);
                    Rect bounds = visible.TransformToAncestor(window).TransformBounds(new Rect(visible.RenderSize));
                    Require(visible.IsVisible && bounds.Width > 20 && bounds.Height > 10 && bounds.Left >= 0 && bounds.Right <= window.ActualWidth && bounds.Bottom <= window.ActualHeight,
                        $"Empire overview clips {name} at {size.Item1}x{size.Item2}.");
                }
                Grid intelligence = Find<Grid>(window).Single(g => g.Name == "EmpireRoomIntelligence");
                Rect infoBounds = intelligence.TransformToAncestor(window).TransformBounds(new Rect(intelligence.RenderSize));
                Require(infoBounds.Height > 100 && infoBounds.Bottom <= window.ActualHeight, "Room intelligence is clipped.");
                Capture(window, Path.Combine(output, $"empire-command-center-{(int)size.Item1}.png"));
            }
            await VerifyRoomPreviews(window, service, output);
            Click(window, "EmpireTab_Development"); Click(window, "EmpireProject_casino_vault");
            Require(service.Projects.Contains("casino-vault"), "Dashboard construction did not commit its suggested project.");
            Click(window, "EmpireTab_Research");
            Require(Find<System.Windows.Shapes.Polyline>(window).Count(l => l.Name.StartsWith("EmpireResearchLink_", StringComparison.Ordinal)) == 4,
                "Operations research lost a prerequisite connection.");
            Click(window, "EmpireResearchBranch_Gaming");
            Require(Find<Button>(window).Any(b => b.Name == "EmpireResearchNode_high_roller"), "Gaming research is unreachable.");
            Click(window, "EmpireResearchNode_high_roller");
            Require(!Find<Button>(window).Single(b => b.Name == "EmpireResearch_high_roller").IsEnabled, "Research ignored its missing Table Authority prerequisite.");
            Click(window, "EmpireResearchBranch_Hospitality");
            Require(Find<Button>(window).Any(b => b.Name == "EmpireResearchNode_guests"), "Hospitality research is unreachable.");
            Click(window, "EmpireManagementClose");

            Click(window,"EmpireTab_Research");
            Require(Find<EmpireManagementView>(window).Single().IsVisible,"Management did not mount in place.");
            Click(window,"EmpireResearch_construction");
            Require(service.ResearchRank("construction")==3,"Research UI did not spend a Legacy Point.");
            Click(window,"EmpireTab_Development");
            Click(window,"EmpireProject_luxury_atrium");
            Require(service.Projects.Contains("luxury-atrium"),"Capital project UI did not persist its purchase.");
            Click(window,"EmpireTab_Operations");
            PreviewAndLeave(window, "EmpireManagerAssign_elena");
            Require(!service.ManagerAssignments.ContainsKey("elena"), "Manager hover committed an assignment.");
            Click(window,"EmpireManagerAssign_elena");
            Require(service.ManagerAssignments.ContainsKey("elena"),"Manager assignment UI did not reach Core.");
            Click(window,"EmpireTab_Operations");
            Find<CheckBox>(window).Single(c=>c.Name=="EmpireAutomationEnabled").IsChecked=true;
            Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationReserve").Text="1000000";
            Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationPercent").Text="25";
            Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationTarget").Text="110";
            Find<EmpireView>(window).Single().Refresh();
            Require(Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationTarget").Text=="110", "A production refresh replaced unsaved automation inputs.");
            PreviewAndLeave(window, "EmpireAutomationSave");
            Require(!service.Automation.Enabled && service.Automation.TargetLevel == 100, "Automation hover saved edited inputs.");
            Click(window,"EmpireAutomationSave");
            Require(service.Automation.Enabled && service.Automation.TargetLevel==110,"Automation input was not saved.");
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                Click(window,"EmpireTab_Operations");
                Require(Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationPercent").Text=="25", "Automation mixed invariant display with localized parsing.");
                Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationReserve").Text="1000000,50";
                Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationPercent").Text="12,5";
                Click(window,"EmpireAutomationSave");
                Require(service.Automation.Reserve==1000000.5m && service.Automation.SpendPercent==.125m,"German automation decimals were not saved exactly.");
            }
            finally { CultureInfo.CurrentCulture = originalCulture; }
            Click(window,"EmpireTab_Opportunities");
            var reachableEvents = new HashSet<string>(StringComparer.Ordinal);
            for (int eventPage = 0; eventPage < EmpireEventCatalog.All.Count; eventPage++)
            {
                foreach (Button choice in Find<Button>(window).Where(b => b.Name.StartsWith("EmpireEvent_", StringComparison.Ordinal))) reachableEvents.Add(choice.Name);
                Button? next = Find<Button>(window).SingleOrDefault(b => b.Name == "EmpirePageNext");
                if (next is not { IsEnabled: true }) break;
                Click(window,"EmpirePageNext");
            }
            Require(EmpireEventCatalog.All.All(e => reachableEvents.Contains("EmpireEvent_" + e.Id)), "The shared Opportunities overview lost a catalog event.");
            Click(window,"EmpireTab_Opportunities"); Click(window,"EmpireEvent_dice");
            Require(service.ActiveEvent?.Id=="dice","Event start UI failed.");
            Click(window,"EmpireTab_Opportunities"); Click(window,"EmpireContract_expansion_drive");
            Require(service.ActiveContract?.Id=="expansion-drive","Contract acceptance UI failed.");
            Click(window,"EmpireManagementClose");
            Click(window,"EmpireFloor1Button"); Click(window,"EmpireRoom_slots"); Click(window,"EmpireBulk50Button"); Click(window,"EmpireUpgradeButton");
            Require(service.CanClaimContract, "Bulk room upgrades did not complete the dashboard contract.");
            Click(window,"EmpireTab_Opportunities"); Click(window,"EmpireContractClaim");
            Require(service.ActiveContract is null && service.ContractBuff?.ContractId == "expansion-drive", "Dashboard contract claim did not save its reward.");
            Click(window,"EmpireManagementClose"); Click(window,"EmpireBulk1Button");
            foreach(string tab in new[]{"Research","Synergies","Development","Operations","Opportunities","Development"})
            {
                Click(window,"EmpireTab_"+tab); window.UpdateLayout();
                foreach(var size in new[]{(1080d,720d),(1440d,900d)})
                {
                    window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();await Task.Delay(80);window.UpdateLayout();
                    Button back=Find<Button>(window).Single(b=>b.Name=="EmpireManagementClose");
                    Rect bounds=back.TransformToAncestor(window).TransformBounds(new Rect(back.RenderSize));
                    Require(bounds.Left>=0 && bounds.Top>=0 && bounds.Right<=window.ActualWidth && bounds.Bottom<=window.ActualHeight,"Management back button is clipped.");
                    Capture(window,Path.Combine(output,$"empire-{tab.ToLowerInvariant()}-{(int)size.Item1}.png"));
                    if (tab is "Development" or "Operations" or "Opportunities")
                    {
                        EmpireManagementView view = Find<EmpireManagementView>(window).Single();
                        Require(!Find<Button>(view).Any(b => b.Name is "EmpirePageNext" or "EmpirePagePrevious"), "A combined overview still paginates.");
                        Require(!Find<ScrollViewer>(view).Any(scroll => scroll.IsVisible && scroll.ScrollableHeight > 1 && scroll.VerticalScrollBarVisibility == ScrollBarVisibility.Auto), "A combined overview needs scrolling.");
                        string[] actions = tab switch
                        {
                            "Development" => EmpireProjectCatalog.All.Select(p => "EmpireProject_" + p.Id.Replace('-', '_')).Concat(EmpirePropertyCatalog.All.Select(p => "EmpireProperty_" + p.Id.Replace('-', '_'))).ToArray(),
                            "Operations" => EmpireManagerCatalog.All.Select(m => "EmpireManagerAssign_" + m.Id).Append("EmpireAutomationSave").ToArray(),
                            _ => EmpireEventCatalog.All.Select(e => "EmpireEvent_" + e.Id).Concat(EmpireContractCatalog.All.Select(c => "EmpireContract_" + c.Id.Replace('-', '_'))).ToArray(),
                        };
                        foreach (string action in actions)
                        {
                            Button control = Find<Button>(view).Single(b => b.Name == action);
                            Rect area = control.TransformToAncestor(window).TransformBounds(new Rect(control.RenderSize));
                            Require(control.IsVisible && area.Width > 8 && area.Height > 5 && area.Left >= 0 && area.Right <= window.ActualWidth + 1 && area.Bottom <= window.ActualHeight + 1,
                                $"{action} is clipped on the {tab} overview at {size}: {area}, visible={control.IsVisible}.");
                        }
                    }
                    Capture(window,Path.Combine(output,$"empire-{tab.ToLowerInvariant()}-{(int)size.Item1}.png"));
                }
            }
            Click(window,"EmpireProperty_neon_bay");
            Require(service.OwnedProperties.Any(p=>p.Id=="neon-bay"),"Property purchase UI failed.");
            Click(window,"EmpireProperty_neon_bay");
            Require(service.ActivePropertyId=="neon-bay" && service.Level("slots")==1,"Property navigation did not select its independent estate.");
            Click(window,"EmpireManagementClose");
            Click(window,"EmpireOperationsButton");
            Require(Find<Button>(window).Any(b=>b.Name=="EmpireManagerAssign_elena"), "Overview Team shortcut opened the wrong section.");
            Click(window,"EmpireManagementClose");
            Click(window,"EmpireTab_Opportunities");
            Require(Find<Button>(window).Any(b=>b.Name=="EmpireContractClaim"), "Overview contract shortcut opened the wrong section.");
            Click(window,"EmpireManagementClose");
            ComboBox picker=Find<ComboBox>(window).Single(c=>c.Name=="EmpirePropertySelector");
            picker.SelectedValue="midnight-club"; window.UpdateLayout();
            Require(service.ActivePropertyId=="midnight-club","Property quick selector did not return to the original estate.");
            Require(Application.Current.Windows.Cast<Window>().Count(w=>w.IsVisible)==windows,"Management created an unnecessary native window.");
            Click(window,"EmpireStatisticsButton"); window.UpdateLayout();
            Require(Find<TextBlock>(window).Any(t=>t.Text.Contains("RESORT FLOOR",StringComparison.Ordinal)),"Statistics omitted the catalog-defined Resort Floor.");
            Capture(window,Path.Combine(output,"empire-portfolio-statistics.png"));
            Click(window,"EmpireStatisticsCloseButton"); Click(window,"EmpireBackButton");
            window.Modes.OpenSecondary("casino-empire"); await Task.Delay(80);
            Window detached = Application.Current.Windows.Cast<Window>().Single(w=>w.Title=="Casino Empire · Detached");
            Click(detached,"EmpireTab_Research"); Click(detached,"EmpireTab_Development");
            Require(Find<EmpireManagementView>(detached).Any(),"Detached estate did not mount the shared management pages.");
            Capture(detached,Path.Combine(output,"empire-detached-management.png"));
            Click(detached,"EmpireManagementClose");
            var sessionField = typeof(MainWindow).GetField("session",flags)!;
            var oldSession = (CasinoSession)sessionField.GetValue(window)!;
            var liveHand = new CasinoSession(new GameRules { AllowInsurance=false },1000m,()=>new EightShoe());
            liveHand.StartRound(100m); sessionField.SetValue(window,liveHand);
            decimal wallet = liveHand.Bankroll;
            decimal reserve = service.ClaimableDividend;
            try
            {
                bool claimed = (bool)typeof(MainWindow).GetMethod("ClaimEmpireDividend",flags)!.Invoke(window,null)!;
                Require(!claimed && liveHand.Bankroll==wallet && service.ClaimableDividend==reserve,"Detached owner claim changed a live Blackjack wallet or consumed its reserve.");
            }
            finally { sessionField.SetValue(window,oldSession); detached.Close(); }
            File.WriteAllText(Path.Combine(output,"empire-strategy-result.txt"),"PASS: data-driven Resort Floor, research purchase, capital project, manager assignment, automation configuration, event, contract, multi-property purchase/navigation, all combined management overviews at 1080x720 and 1440x900, portfolio statistics and single-window hosting.");
        }
        finally
        {
            if(Find<Button>(window).Any(b=>b.Name=="EmpireManagementClose")) Click(window,"EmpireManagementClose");
            if(Find<Button>(window).Any(b=>b.Name=="EmpireStatisticsCloseButton")) Click(window,"EmpireStatisticsCloseButton");
            if(Find<Button>(window).Any(b=>b.Name=="EmpireBackButton" && b.IsVisible)) Click(window,"EmpireBackButton");
            typeof(MainWindow).GetField("empire",flags)!.SetValue(window,old);
        }
    }
    private static async Task VerifyRoomPreviews(MainWindow window, EmpireService service, string output)
    {
        EmpireView view = Find<EmpireView>(window).Single();
        // Routed hover events exercise the same preview callbacks as pointer and keyboard input.
        foreach (int count in new[] { 1, 5, 10, 25, 50, 100 })
        {
            Button choice = Find<Button>(window).Single(b => b.Name == "EmpireBulk" + count + "Button");
            int before = service.Level("hotel"); decimal budget = service.Credits;
            choice.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseEnterEvent });
            window.UpdateLayout();
            Require(service.Level("hotel") == before && service.Credits == budget, "Hover purchased room levels.");
            Require(Find<TextBlock>(view).Any(t => t.Text.StartsWith("PREVIEW /", StringComparison.Ordinal)), "Bulk hover did not show a preview.");
            if (count >= 10) Require(Find<TextBlock>(view).Any(t => t.Text.Contains("×2 milestone at LV 110", StringComparison.Ordinal)), "Hover omitted the crossed production milestone.");
            Require(Find<TextBlock>(view).Any(t => t.Text.Contains("Luxury Hotel: 100 / 100 →", StringComparison.Ordinal)), "Circuit member progress did not preview the upgraded room.");
            choice.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseLeaveEvent });
            Require(!Find<TextBlock>(view).Any(t => t.Text.StartsWith("PREVIEW /", StringComparison.Ordinal)), "Preview survived mouse leave.");
        }
        Button buy = Find<Button>(window).Single(b => b.Name == "EmpireUpgradeButton");
        buy.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseEnterEvent });
        Capture(window, Path.Combine(output, "empire-upgrade-hover.png"));
        Click(window, "EmpireRoom_spa");
        Require(!Find<TextBlock>(view).Any(t => t.Text.StartsWith("PREVIEW /", StringComparison.Ordinal)), "Preview leaked into another room.");
        Click(window, "EmpireClearRoomSelection");
        Require(Find<TextBlock>(view).Any(t => t.Text.StartsWith("ESTATE OVERVIEW /", StringComparison.Ordinal)), "Clear selection lost the estate overview.");
        Click(window, "EmpireRoom_hotel");
        await Task.Delay(180);
    }

    private static void PreviewAndLeave(DependencyObject root, string name)
    {
        Button owner = Find<Button>(root).Single(b => b.Name == name);
        Require(owner.IsEnabled, name + " is not available for preview.");
        owner.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseEnterEvent });
        Require(Find<TextBlock>(root).Any(t => t.IsVisible && t.Text.StartsWith("PREVIEW /", StringComparison.Ordinal)), name + " did not preview its effect.");
        owner.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = UIElement.MouseLeaveEvent });
        Require(!Find<TextBlock>(root).Any(t => t.IsVisible && t.Text.StartsWith("PREVIEW /", StringComparison.Ordinal)), name + " left a stale preview.");
    }

    private sealed class EightShoe : ICardShoe
    {
        public int Remaining { get; private set; } = 312;
        public Card Draw() { Remaining--; return new(Rank.Eight,Suit.Clubs); }
    }
    private static void Click(DependencyObject root,string name)
    {
        if (root is UIElement surface) surface.UpdateLayout();
        Button? button=Find<Button>(root).SingleOrDefault(b=>b.Name==name);
        if (button is null && name.StartsWith("EmpireTab_", StringComparison.Ordinal))
        {
            string group = name[10..] switch
            {
                "Research" or "Development" => "EmpireDevelopmentButton",
                "Operations" => "EmpireOperationsButton",
                "Opportunities" => "EmpireOpportunitiesButton", _ => "EmpireOverviewButton",
            };
            Click(root, group); button = Find<Button>(root).SingleOrDefault(b => b.Name == name);
        }
        // Catalogs use two or four cards according to available height. Follow the
        // real page controls when a target is on a later page; never click hidden entries.
        for (int page=0; button is null && page<10; page++)
        {
            Button? next=Find<Button>(root).SingleOrDefault(b=>b.Name=="EmpirePageNext");
            if(next is not { IsEnabled:true }) break;
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if(root is UIElement paged) paged.UpdateLayout();
            button=Find<Button>(root).SingleOrDefault(b=>b.Name==name);
        }
        if(button is null) throw new InvalidOperationException($"{name} is unreachable through the current section's page controls.");
        Require(button.IsEnabled,$"{name} is disabled unexpectedly.");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (root is UIElement updated) updated.UpdateLayout();
    }
    private static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
    private static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
    {
        if(root is T match)yield return match;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) foreach(T child in Find<T>(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    private static void Capture(FrameworkElement root,string path)
    {
        root.UpdateLayout(); var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
}
