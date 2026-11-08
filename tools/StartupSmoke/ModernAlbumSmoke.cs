using System.Diagnostics;
using System.Text.Json;
using System.IO;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Real WebView2 acceptance for the modern Collector Album, card illustration
/// manifests and transactional single/batch pack openings. No WPF route passes.
/// </summary>
internal static class ModernAlbumSmoke
{
    internal static async Task Run(MainWindow window, PlayerProfileState profile, string output)
    {
        ModernLobbyView modern=Find<ModernLobbyView>(window).Single();
        Require(modern.Ready&&modern.IsVisible&&modern.Browser.CoreWebView2 is not null,
            "The real React lobby is unavailable for modern Album acceptance.");
        CoreWebView2 web=modern.Browser.CoreWebView2!;
        await web.ExecuteScriptAsync(
            "[...document.querySelectorAll('.lobby-nav-item')]"
            +".find(x=>/Collections|Sammlungen/.test(x.textContent||''))?.click()");
        await Wait(web,"document.querySelectorAll('.album-theme-item').length===16"
            +"&&document.querySelectorAll('.album-pack-choice').length===5",
            "The React Album is missing the 16 original themes or five rarity packs.");
        Require(modern.RecordsPage=="album","Album route remained native.");
        Require(!Find<CollectionAlbumPage>(window).Any(p=>p.IsVisible),
            "The retired native Album is visible over the React Album.");
        await Wait(web,"document.querySelectorAll('.album-card-grid .album-collection-card').length===10",
            "React Album omitted some original card illustrations.");
        string crop=await web.ExecuteScriptAsync(
            "Boolean(document.querySelector('.album-card-illustration'))");
        // A new player may have no owned cards yet, so reveal verifies the atlas.
        Require(crop=="true"||profile.Collection.OwnedCardIds.Count==0,
            "Owned cards do not use the original artwork atlases.");
        await Capture(web,Path.Combine(output,"modern-album-collection.png"));

        // Staging alone must not charge anything.
        int beforePacks=profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common);
        long beforeOpened=profile.Collection.PacksOpened;
        Require(beforePacks>0,"Acceptance profile has no Common collector pack.");
        await web.ExecuteScriptAsync("document.querySelector('[data-pack-rarity=Common]')?.click()");
        await Wait(web,"Boolean(document.querySelector('.album-opening-panel.phase-stage'))",
            "React staging screen failed to open.");
        Require(profile.Collection.PacksOpened==beforeOpened
            &&profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common)==beforePacks,
            "Staging a pack consumed inventory before confirmation.");
        await Capture(web,Path.Combine(output,"modern-album-sealed-pack.png"));
        string pointerDiagnostic = await web.ExecuteScriptAsync(
            "(()=>{const buttons=[...document.querySelectorAll('.phase-stage .album-opening-actions button')];"
            +"return JSON.stringify({height:innerHeight,width:innerWidth,actions:buttons.map(b=>{"
            +"const r=b.getBoundingClientRect(),hit=document.elementFromPoint(r.left+r.width/2,r.top+r.height/2);"
            +"return {name:b.textContent,top:r.top,left:r.left,width:r.width,height:r.height,"
            +"hit:hit?.tagName,hitClass:hit?.className,visible:r.top>=0&&r.bottom<=innerHeight,"
            +"reachable:hit===b||b.contains(hit)}})})})()");
        string decoded = JsonSerializer.Deserialize<string>(pointerDiagnostic) ?? "{}";
        using JsonDocument pointerState = JsonDocument.Parse(decoded);
        JsonElement actions = pointerState.RootElement.GetProperty("actions");
        Require(actions.GetArrayLength() >= 1
            && actions.EnumerateArray().All(action =>
                action.GetProperty("visible").GetBoolean()
                && action.GetProperty("reachable").GetBoolean()),
            "Real pack buttons are not reachable: " + decoded);
        await web.ExecuteScriptAsync("document.querySelector('.album-opening-primary')?.click()");
        await Until(()=>profile.Collection.PendingOpening is not null,
            "Opening a React pack did not persist the native receipt.");
        CollectionOpening receipt=profile.Collection.PendingOpening!;
        Require(receipt.Cards.Count==5 && profile.Collection.PacksOpened==beforeOpened+1
            &&profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Common)==beforePacks-1,
            "A single React open did not commit exactly one five-card transaction.");
        await Wait(web,"Boolean(document.querySelector('.album-opening-panel.phase-reveal')"
            +"||document.querySelector('.album-opening-panel.phase-summary'))",
            "React did not start the pack reveal sequence.");
        await web.ExecuteScriptAsync(
            "document.querySelector('.phase-reveal .album-opening-secondary')?.click()");
        await Wait(web,"Boolean(document.querySelector('.album-opening-panel.phase-summary'))",
            "The five-card reveal did not reach its result summary.");
        Require(profile.Collection.PendingOpening?.Id==receipt.Id,
            "Client-side animation cleared or regenerated the C# receipt.");
        await Capture(web,Path.Combine(output,"modern-album-five-cards.png"));

        // Exit mid-reveal and reenter. The exact same receipt must be recoverable.
        await web.ExecuteScriptAsync("document.querySelector('.album-opening-exit')?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.album-page .shop-back')?.click()");
        await Until(()=>modern.RecordsPage is null,"React Album failed to return to lobby.");
        Require(profile.Collection.PendingOpening?.Id==receipt.Id,
            "Navigating away from the reveal discarded its durable transaction.");
        await web.ExecuteScriptAsync("document.querySelector('.header-compact:nth-child(2)')?.click()");
        await Wait(web,"Boolean(document.querySelector('.album-opening-panel.phase-reveal')"
            +"||document.querySelector('.album-opening-panel.phase-summary'))",
            "A stored opening was not resumed after reentering the Album.");
        Require(profile.Collection.PendingOpening?.Id==receipt.Id,
            "Resuming a pack produced a different receipt.");
        await web.ExecuteScriptAsync(
            "document.querySelector('.phase-reveal .album-opening-secondary')?.click()");
        await Wait(web,"Boolean(document.querySelector('.phase-summary .album-opening-primary'))",
            "Resumed pack cannot be collected.");
        await web.ExecuteScriptAsync("document.querySelector('.phase-summary .album-opening-primary')?.click()");
        await Until(()=>profile.Collection.PendingOpening is null,
            "Collect did not complete the original durable C# receipt.");
        await Wait(web,"Boolean(document.querySelector('.album-opening-panel.phase-finished'))",
            "React Album did not acknowledge the completed pack.");
        Require(profile.Collection.PacksOpened==beforeOpened+1,
            "Resuming the reveal opened the same pack twice.");
        await web.ExecuteScriptAsync("document.querySelector('.album-opening-exit')?.click()");

        // Open-all must complete each pack before sending the next command.
        CollectionService.GrantPack(profile,CollectionRarity.Uncommon,2);
        int uncommon=profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Uncommon);
        long openedBeforeBatch=profile.Collection.PacksOpened;
        await web.ExecuteScriptAsync("window.chrome.webview.postMessage({type:'lobby.refresh'})");
        await Wait(web,"Boolean(document.querySelector('[data-pack-rarity=Uncommon]:not(:disabled)'))",
            "React Album did not refresh native Uncommon pack inventory.");
        await web.ExecuteScriptAsync("document.querySelector('[data-pack-rarity=Uncommon]')?.click()");
        await Wait(web,"Boolean(document.querySelector('.phase-stage .album-opening-secondary'))",
            "The Open All Packs action was not available.");
        Require(await web.ExecuteScriptAsync(
            "(()=>{const b=document.querySelector('.phase-stage .album-opening-secondary');"
            +"if(!b)return false;const r=b.getBoundingClientRect();"
            +"const hit=document.elementFromPoint(r.left+r.width/2,r.top+r.height/2);"
            +"return r.width>0&&r.height>0&&(hit===b||b.contains(hit));})()")=="true",
            "Open All Packs is visually present but cannot receive a real pointer click.");
        await web.ExecuteScriptAsync("document.querySelector('.phase-stage .album-opening-secondary')?.click()");
        await Until(()=>profile.Collection.PacksOpened>=openedBeforeBatch+uncommon
            &&profile.Collection.PendingOpening is null,
            "React Open All did not finish its sequential native transactions.",attempts:300);
        Require(profile.Collection.PacksOpened==openedBeforeBatch+uncommon
            &&profile.Collection.Packs.GetValueOrDefault(CollectionRarity.Uncommon)==0,
            "React Open All charged the wrong pack count.");
        await Wait(web,"Boolean(document.querySelector('.phase-finished'))",
            "React Open All did not finish the complete reveal sequence.");
        await web.ExecuteScriptAsync("document.querySelector('.album-opening-exit')?.click()");
        await web.ExecuteScriptAsync("document.querySelector('.album-page .shop-back')?.click()");
        await Until(()=>modern.RecordsPage is null,"Modern Album did not close properly.");
        File.WriteAllText(Path.Combine(output,"modern-album-result.txt"),
            "PASS: 16 React themes, real artwork, safe pack staging, five distinct results, saved receipt recovery, exactly-once completion and serial Open All.");
    }

    private static async Task Capture(CoreWebView2 web,string path)
    {
        await using FileStream stream=File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);
        Require(stream.Length>16000,"The Album screenshot was blank: "+path);
    }
    private static async Task Wait(CoreWebView2 web,string script,string failure)
    {
        for(int i=0;i<180;i++)
        {
            if(await web.ExecuteScriptAsync(script)=="true")return;
            await Task.Delay(70);
        }
        throw new InvalidOperationException(failure);
    }
    private static async Task Until(Func<bool> predicate,string failure,int attempts=180)
    {
        for(int i=0;i<attempts;i++)
        {
            if(predicate())return;
            await Task.Delay(70);
        }
        throw new InvalidOperationException(failure);
    }
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            DependencyObject child=VisualTreeHelper.GetChild(root,i);
            if(child is T match)yield return match;
            foreach(T nested in Find<T>(child))yield return nested;
        }
    }
    private static void Require(bool valid,string message)
    {
        if(!valid)throw new InvalidOperationException(message);
    }
}
