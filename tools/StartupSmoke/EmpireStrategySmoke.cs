using System.IO;
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
            Started = true, LastAccruedAt = DateTimeOffset.UtcNow, Prestige = 25, Credits = 100_000_000_000_000m,
            Levels = EmpireCatalog.Departments.ToDictionary(room => room.Id, _ => 100),
            Research = new() { ["construction"] = 2, ["compound"] = 1, ["automation"] = 1 },
        });
        var estate = baseState.Properties[baseState.ActivePropertyId] with { Projects = new(["grand-entrance","executive-offices","resort-wing","skyline-property"]), Levels = EmpireCatalog.Departments.ToDictionary(room=>room.Id,_=>100) };
        var service = new EmpireService(baseState with { Properties = new(baseState.Properties) { [baseState.ActivePropertyId] = estate } });
        typeof(MainWindow).GetField("empire",flags)!.SetValue(window,service);
        try
        {
            window.Modes.Open("casino-empire"); await Task.Delay(100); window.UpdateLayout();
            int windows = Application.Current.Windows.Cast<Window>().Count(w=>w.IsVisible);
            Click(window,"EmpireFloor3Button"); window.UpdateLayout();
            Require(Find<EmpireRoomCard>(window).Count(card=>card.IsVisible)==5,"Resort Floor did not render its five departments.");
            Capture(window,Path.Combine(output,"empire-resort.png"));
            Click(window,"EmpireManagementButton");
            Require(Find<EmpireManagementView>(window).Single().IsVisible,"Management did not mount in place.");
            Click(window,"EmpireResearch_construction");
            Require(service.ResearchRank("construction")==3,"Research UI did not spend a Legacy Point.");
            Click(window,"EmpireTab_Projects");
            Click(window,"EmpireProject_casino_vault");
            Require(service.Projects.Contains("casino-vault"),"Capital project UI did not persist its purchase.");
            Click(window,"EmpireTab_Managers");
            Click(window,"EmpireManagerAssign_elena");
            Require(service.ManagerAssignments.ContainsKey("elena"),"Manager assignment UI did not reach Core.");
            Click(window,"EmpireTab_Automation");
            Find<CheckBox>(window).Single(c=>c.Name=="EmpireAutomationEnabled").IsChecked=true;
            Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationReserve").Text="1000000";
            Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationPercent").Text="25";
            Find<TextBox>(window).Single(c=>c.Name=="EmpireAutomationTarget").Text="110";
            Click(window,"EmpireAutomationSave");
            Require(service.Automation.Enabled && service.Automation.TargetLevel==110,"Automation input was not saved.");
            Click(window,"EmpireTab_Events"); Click(window,"EmpireEvent_dice");
            Require(service.ActiveEvent?.Id=="dice","Event start UI failed.");
            Click(window,"EmpireTab_Contracts"); Click(window,"EmpireContract_expansion_drive");
            Require(service.ActiveContract?.Id=="expansion-drive","Contract acceptance UI failed.");
            foreach(string tab in new[]{"Research","Synergies","Projects","Managers","Automation","Events","Contracts","Properties"})
            {
                Click(window,"EmpireTab_"+tab); window.UpdateLayout();
                foreach(var size in new[]{(1080d,720d),(1440d,900d)})
                {
                    window.Width=size.Item1;window.Height=size.Item2;window.UpdateLayout();
                    Button back=Find<Button>(window).Single(b=>b.Name=="EmpireManagementClose");
                    Rect bounds=back.TransformToAncestor(window).TransformBounds(new Rect(back.RenderSize));
                    Require(bounds.Left>=0 && bounds.Top>=0 && bounds.Right<=window.ActualWidth && bounds.Bottom<=window.ActualHeight,"Management back button is clipped.");
                    Capture(window,Path.Combine(output,$"empire-{tab.ToLowerInvariant()}-{(int)size.Item1}.png"));
                }
            }
            Click(window,"EmpireProperty_neon_bay");
            Require(service.OwnedProperties.Any(p=>p.Id=="neon-bay"),"Property purchase UI failed.");
            Click(window,"EmpireProperty_neon_bay");
            Require(service.ActivePropertyId=="neon-bay" && service.Level("slots")==1,"Property navigation did not select its independent estate.");
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
            Click(detached,"EmpireManagementButton"); Click(detached,"EmpireTab_Properties");
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
            File.WriteAllText(Path.Combine(output,"empire-strategy-result.txt"),"PASS: data-driven Resort Floor, research purchase, capital project, manager assignment, automation configuration, event, contract, multi-property purchase/navigation, all eight management pages at 1080x720 and 1440x900, portfolio statistics and single-window hosting.");
        }
        finally
        {
            if(Find<Button>(window).Any(b=>b.Name=="EmpireManagementClose")) Click(window,"EmpireManagementClose");
            if(Find<Button>(window).Any(b=>b.Name=="EmpireStatisticsCloseButton")) Click(window,"EmpireStatisticsCloseButton");
            if(Find<Button>(window).Any(b=>b.Name=="EmpireBackButton" && b.IsVisible)) Click(window,"EmpireBackButton");
            typeof(MainWindow).GetField("empire",flags)!.SetValue(window,old);
        }
    }
    private sealed class EightShoe : ICardShoe
    {
        public int Remaining { get; private set; } = 312;
        public Card Draw() { Remaining--; return new(Rank.Eight,Suit.Clubs); }
    }
    private static void Click(DependencyObject root,string name)
    {
        Button button=Find<Button>(root).Single(b=>b.Name==name);
        Require(button.IsEnabled,$"{name} is disabled unexpectedly.");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
