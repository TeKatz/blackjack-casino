using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Desktop;

/// <summary>
/// Required Windows browser acceptance: no WPF-only fallback can make this pass.
/// Rewards are claimed via actual React buttons and verified in the C# profile.
/// </summary>
internal static class ModernRecordsSmoke
{
    internal static async Task Run(MainWindow window, PlayerProfileState profile, string output)
    {
        ModernLobbyView modern = Find<ModernLobbyView>(window).Single();
        Require(modern.Ready && modern.IsVisible && modern.Browser.CoreWebView2 is not null,
            "The real React host is unavailable for Challenges/Statistics acceptance.");
        CoreWebView2 web = modern.Browser.CoreWebView2!;
        var ready = ChallengeRegistry.All.Where(c =>
            c.Category != ChallengeCategory.Jigsaw
            && ChallengeRegistry.Complete(profile, c)
            && !ChallengeRegistry.IsClaimed(profile,c)).ToArray();
        Require(ready.Length>0,"The test profile has no ready challenges.");
        string openChallenge = await web.ExecuteScriptAsync(
            "(()=>{const b=[...document.querySelectorAll('.lobby-nav-item')]"
            + ".find(x=>/Challenges|Aufgaben/.test(x.textContent||''));b?.click();return Boolean(b);})()");
        Require(openChallenge=="true","Challenges navigation is missing from the React casino shell.");
        await WaitFor(web,"Boolean(document.querySelector('.record-page')"
            + "&&document.querySelector('.record-challenge.record-ready')"
            + "&&document.querySelector('.record-claim-all'))",
            "React challenge board did not display native completion state.");
        Require(modern.RecordsPage=="challenges" && modern.IsVisible,
            "The native bridge failed to activate the React challenge view.");
        Require(Find<ProgressionWindow>(window).All(x=>!x.IsVisible),
            "The retired WPF challenge page is still visible above React.");

        // Claim one challenge through the visible card button, not through a
        // direct service call. Verify the native profile is debited/credited once.
        string rawId=await web.ExecuteScriptAsync(
            "document.querySelector('.record-challenge.record-ready')?.getAttribute('data-challenge-id')");
        string id=JsonSerializer.Deserialize<string>(rawId)??throw new InvalidOperationException("No claimable card ID.");
        ChallengeDefinition target = ChallengeRegistry.All.Single(c=>c.Id==id);
        long before=profile.Coins;
        await web.ExecuteScriptAsync(
            "document.querySelector('[data-challenge-id=\"" + id + "\"] .record-claim')?.click()");
        await WaitFor(web,"Boolean(!document.querySelector('[data-challenge-id=\"" + id + "\"].record-ready'))",
            "Challenge did not leave the ready state after claiming.");
        Require(profile.Coins==before+target.CoinReward && ChallengeRegistry.IsClaimed(profile,target),
            "React single-claim did not persist the correct native reward.");
        long charged=profile.Coins;
        await web.ExecuteScriptAsync(
            "document.querySelector('[data-challenge-id=\"" + id + "\"] .record-claim')?.click()");
        await Task.Delay(80);
        Require(profile.Coins==charged,"Claimed challenge could be redeemed twice.");

        var outstanding=ChallengeRegistry.All.Where(c=>
            c.Category!=ChallengeCategory.Jigsaw && ChallengeRegistry.Complete(profile,c)
            && !ChallengeRegistry.IsClaimed(profile,c)).ToArray();
        Require(outstanding.Length>0, "Test seed needs at least two challenges ready to verify Claim All.");

        // Hide rewards behind an unrelated category: Claim All must still
        // include every category, not just the filtered cards.
        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.record-category')].find(x=>x.textContent?.includes('Prestige'))?.click()");
        await WaitFor(web,"Boolean(document.querySelector('.record-filters'))",
            "Challenge category filters are missing.");
        long beforeAll=profile.Coins;
        await web.ExecuteScriptAsync("document.querySelector('.record-claim-all')?.click()");
        await Until(() => outstanding.All(c => ChallengeRegistry.IsClaimed(profile,c)),
            "Claim All did not finish the native reward transaction.");
        await WaitFor(web,
            "Boolean(document.querySelector('.record-claim-all')?.disabled)",
            "Claim All did not refresh its enabled state.");
        Require(profile.Coins==beforeAll+outstanding.Sum(c=>c.CoinReward),
            "Claim All missed a reward hidden by another category or filter.");
        Require(outstanding.All(c=>ChallengeRegistry.IsClaimed(profile,c)),
            "Claim All failed to persist every native challenge receipt.");
        await Capture(web,Path.Combine(output,"modern-challenges-native.png"));
        await web.ExecuteScriptAsync("document.querySelector('.record-page .shop-back')?.click()");
        await WaitFor(web,"Boolean(document.querySelector('.lobby-game-grid')"
            + "&&!document.querySelector('.record-page'))","Challenges failed to return to lobby.");
        Require(modern.RecordsPage is null,"Native Challenges route remained open after Back.");

        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Statistics|Statistik/.test(x.textContent||''))?.click()");
        await WaitFor(web,"Boolean(document.querySelector('.statistics-page')"
            + "&&document.querySelector('[data-stat-game=\"blackjack\"]')"
            + "&&document.querySelector('[data-stat-game=\"midnight-dice\"]')"
            + "&&document.querySelector('[data-stat-game=\"midnight-manor\"]')"
            + "&&document.querySelector('.session-metrics'))",
            "React lifetime statistics are missing real game and session metrics.");
        Require(modern.RecordsPage=="statistics","Native Statistics route was not activated.");
        await Capture(web,Path.Combine(output,"modern-statistics-native.png"));
        await web.ExecuteScriptAsync("document.querySelector('.statistics-page .shop-back')?.click()");
        await WaitFor(web,"Boolean(document.querySelector('.lobby-game-grid'))",
            "Statistics failed to return to the lobby.");
        Require(modern.RecordsPage is null,"Statistics remained open after Back.");

        // Season Pass is a React page with native-authoritative season selection.
        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.lobby-nav-item')].find(x=>/Season Pass/.test(x.textContent||''))?.click()");
        await WaitFor(web,"Boolean(document.querySelector('.season-page .season-tiers article'))",
            "React Season Pass is missing its reward track.");
        Require(modern.RecordsPage=="season","Season Pass did not open in React.");
        string originalSeason=profile.ActiveSeasonId;
        string alternate=SeasonCatalog.All.First(s=>s.Id!=originalSeason).Id;
        await web.ExecuteScriptAsync(
            "document.querySelectorAll('.season-library button')["
            + SeasonCatalog.All.ToList().FindIndex(s=>s.Id==alternate)
            + "]?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.season-heading .record-claim-all')?.click()");
        await Until(()=>profile.ActiveSeasonId==alternate,
            "Season selection did not commit to the native profile.");
        await web.ExecuteScriptAsync(
            "document.querySelectorAll('.season-library button')["
            + SeasonCatalog.All.ToList().FindIndex(s=>s.Id==originalSeason)
            + "]?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.season-heading .record-claim-all')?.click()");
        await Until(()=>profile.ActiveSeasonId==originalSeason,
            "Season selection did not restore the original active pass.");
        await Capture(web,Path.Combine(output,"modern-season-native.png"));
        await web.ExecuteScriptAsync("document.querySelector('.season-page .shop-back')?.click()");
        await Until(()=>modern.RecordsPage is null,"Season Pass did not return to lobby.");

        // The profile header opens a React account hub; its navigation returns to React statistics.
        await web.ExecuteScriptAsync("document.querySelector('.profile-widget')?.click()");
        await WaitFor(web,"Boolean(document.querySelector('.account-page .account-routes'))",
            "Player profile did not open the React account hub.");
        Require(modern.RecordsPage=="profile","Profile route was not activated.");
        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.account-routes button')].find(x=>/Statistics|Statistik/.test(x.textContent||''))?.click()");
        await Until(()=>modern.RecordsPage=="statistics"&&modern.IsVisible,
            "Profile Statistics did not open React.");
        await WaitFor(web,"Boolean(document.querySelector('.statistics-page'))",
            "Profile statistics did not render.");
        await web.ExecuteScriptAsync("document.querySelector('.statistics-page .shop-back')?.click()");
        await Until(()=>modern.RecordsPage is null,"Profile Statistics did not close properly.");
        File.WriteAllText(Path.Combine(output,"modern-records-result.txt"),
            "PASS: React Challenges and Statistics, single claim, duplicate-claim guard, Claim All across filters, stats/game/session data, React Season and Profile navigation, native season activation and clean return to lobby.");
    }

    private static async Task Capture(CoreWebView2 web,string path)
    {
        await using FileStream stream=File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);
        Require(stream.Length>16000,"WebView2 records screenshot was blank: "+path);
    }

    private static async Task WaitFor(CoreWebView2 web,string script,string message)
    {
        Stopwatch watch=Stopwatch.StartNew();
        while(watch.Elapsed<TimeSpan.FromSeconds(12))
        {
            if(await web.ExecuteScriptAsync(script)=="true")return;
            await Task.Delay(90);
        }
        throw new InvalidOperationException(message);
    }

    private static async Task Until(Func<bool> condition,string message)
    {
        Stopwatch watch=Stopwatch.StartNew();
        while(!condition())
        {
            if(watch.Elapsed>TimeSpan.FromSeconds(12))throw new InvalidOperationException(message);
            await Task.Delay(90);
        }
    }
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            DependencyObject child=VisualTreeHelper.GetChild(root,i);
            if(child is T item)yield return item;
            foreach(T nested in Find<T>(child))yield return nested;
        }
    }
    private static void Require(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
    }
}
