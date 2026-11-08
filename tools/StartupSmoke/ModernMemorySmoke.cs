using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using BlackjackCasino.Core.Memory;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Exercises real React/WebView2 and the authoritative C# Memory session, including
/// face-down privacy, native flip/match/miss rules, mismatch delay, resume, completion and replay.
/// Existing WPF MemorySmoke remains emergency-fallback coverage.
/// </summary>
internal static class ModernMemorySmoke
{
    internal static async Task Run(MainWindow window, CoreWebView2 web, string output)
    {
        await Click(web,".chill-memory",
            "document.querySelectorAll('.memo-tier').length===5",
            "Memory did not open the five native difficulty choices in React.");
        Require(await Js(web,"document.querySelectorAll('.memo-deck').length===4"),
            "The modern Memory cabinet lost one of the four original decks.");
        Require(await Js(web,"document.querySelectorAll('.memo-preview-card').length===16"),
            "React Memory is missing the curated card-art previews.");
        Require(await Js(web,"[...document.querySelectorAll('.memo-preview-card')].every(c=>{const a=c.getBoundingClientRect(),b=c.querySelector('.memo-face').getBoundingClientRect();return a.width>0&&b.width<=a.width+1&&b.height<=a.height+1;})"),
            "A Memory deck preview escaped its card and covered the cabinet.");
        await Capture(web,Path.Combine(output,"modern-memory-cabinet.png"));

        await Click(web,"[data-testid=\"memory-deck-animals\"]",
            "document.querySelector('[data-testid=\"memory-deck-animals\"]')?.getAttribute('aria-pressed')==='true'",
            "The original Animals deck was not selected and saved.");
        await Click(web,"[data-testid=\"memory-start-Easy\"]",
            "Boolean(document.querySelector('[data-testid=\"modern-memory-game\"]'))",
            "A native 4x4 Memory game did not open in React.");
        Require(await Js(web,"document.querySelectorAll('.memo-card').length===16"),
            "Easy Memory must have sixteen cards.");
        Require(await Js(web,"[...document.querySelectorAll('.memo-card')].every(c=>c.dataset.faceUp==='false')"),
            "Cards must start face-down, without revealing any symbol.");
        await Capture(web,Path.Combine(output,"modern-memory-board.png"));

        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        ModernMemoryService service=(ModernMemoryService)
            typeof(MainWindow).GetField("modernMemory",flags)!.GetValue(window)!;
        MemoryGameState game=service.CurrentGame ??
            throw new InvalidOperationException("Native Memory engine never started.");
        Guid firstGameId=game.GameId;
        Require(game.DeckId=="animals","Deck selection did not reach the C# native game.");
        int mate=Enumerable.Range(1,game.Cards.Count-1).Single(i=>game.Cards[i]==game.Cards[0]);

        await Flip(web,0,"document.querySelector('[data-testid=\"memory-card-0\"]')?.dataset.faceUp==='true'");
        Require(await Js(web,
            "[...document.querySelectorAll('.memo-card')].filter(c=>c.dataset.faceUp==='true').length===1"),
            "Native first-card flip leaked other face-down cards.");
        await Capture(web,Path.Combine(output,"modern-memory-flipped.png"));
        await Click(web,"[data-testid=\"memory-back\"]",
            "Boolean(document.querySelector('[data-testid=\"memory-library\"]'))",
            "An unfinished Memory table did not return to the React cabinet.");
        Require(await Js(web,"Boolean(document.querySelector('[data-testid=\"memory-resume\"]'))"),
            "Leaving the Memory game did not preserve a resumable game.");

        await Click(web,"[data-testid=\"memory-resume\"]",
            "Boolean(document.querySelector('[data-testid=\"modern-memory-game\"]'))",
            "The old native Memory game did not resume.");
        Require(service.CurrentGame!.GameId==firstGameId,
            "Resume replaced the original saved Memory game ID.");
        Require(await Js(web,"document.querySelector('[data-testid=\"memory-card-0\"]')?.dataset.faceUp==='true'"),
            "Memory resume lost the already-turned first card.");

        await Flip(web,mate,"document.querySelector('[data-testid=\"memory-pairs\"]')?.textContent==='1/8'");
        Require(await Js(web,
            "document.querySelector('[data-testid=\"memory-card-0\"]')?.dataset.matched==='true'"),
            "The C# engine did not keep a completed pair face-up.");

        game = service.CurrentGame!;
        int mismatchA=Enumerable.Range(0,game.Cards.Count).First(i=>!game.MatchedIndices.Contains(i));
        int mismatchB=Enumerable.Range(0,game.Cards.Count).First(i=>
            !game.MatchedIndices.Contains(i)&&i!=mismatchA&&game.Cards[i]!=game.Cards[mismatchA]);
        await Flip(web,mismatchA,
            "document.querySelector('[data-testid=\"memory-card-"+mismatchA+"\"]')?.dataset.faceUp==='true'");
        // Observe the mismatch counter and both visible faces in one browser
        // snapshot. A separate query after the 760 ms native reveal period
        // can see the cards already turned down on a busy Windows runner.
        // This still requires the two wrong cards to be visibly face-up.
        await Flip(web,mismatchB,
            "document.querySelector('[data-testid=\"memory-misses\"]')?.textContent==='1'&&"
           +"document.querySelector('[data-testid=\"memory-card-"+mismatchA+"\"]')?.dataset.faceUp==='true'&&"
           +"document.querySelector('[data-testid=\"memory-card-"+mismatchB+"\"]')?.dataset.faceUp==='true'");
        await Wait(web,
            "document.querySelector('[data-testid=\"memory-card-"+mismatchA+"\"]')?.dataset.faceUp==='false'&&"
           +"document.querySelector('[data-testid=\"memory-card-"+mismatchB+"\"]')?.dataset.faceUp==='false'",
            "Native mismatch resolution failed to turn the two cards back over.");
        Require(service.CurrentGame!.WrongMatches==1,
            "Mismatch counting or state persistence disagrees with the original C# rules.");

        // The smoke reads the authoritative internal card identities; only this trusted
        // test knows the solutions. The packaged browser snapshot deliberately does not.
        int found=service.CurrentGame.PairsFound;
        int[][] remaining=game.Cards.Select((card,index)=>(card,index))
            .Where(item=>!service.CurrentGame!.MatchedIndices.Contains(item.index))
            .GroupBy(item=>item.card,StringComparer.Ordinal)
            .Select(g=>g.Select(x=>x.index).ToArray()).ToArray();
        foreach(int[] pair in remaining)
        {
            Require(pair.Length==2,"Memory generator created a non-paired symbol.");
            await Flip(web,pair[0],
                "document.querySelector('[data-testid=\"memory-card-"+pair[0]+"\"]')?.dataset.faceUp==='true'");
            found++;
            await Flip(web,pair[1],
                "document.querySelector('[data-testid=\"memory-pairs\"]')?.textContent==='"
                +found+"/8'");
        }
        await Wait(web,
            "Boolean(document.querySelector('[data-testid=\"memory-completion\"]'))",
            "Clearing the C# Memory table did not show the modern result screen.");
        Require(await Js(web,
            "document.querySelector('[data-testid=\"memory-misses\"]')?.textContent==='1'"),
            "Completion statistics dropped the recorded miss.");
        Require(service.CurrentGame is null,
            "A completed Memory game should no longer occupy the active save slot.");
        await Capture(web,Path.Combine(output,"modern-memory-complete.png"));
        await Click(web,"[data-testid=\"memory-result-again\"]",
            "Boolean(document.querySelector('[data-testid=\"modern-memory-game\"]'))",
            "Play Again did not create a new native Memory game.");
        Require(service.CurrentGame!.GameId!=firstGameId &&
                service.CurrentGame.DeckId=="animals" && service.CurrentGame.Difficulty==MemoryDifficulty.Easy,
            "Play Again did not preserve the original difficulty and deck.");
        await Click(web,"[data-testid=\"memory-back\"]",
            "Boolean(document.querySelector('[data-testid=\"memory-library\"]'))",
            "Replayed Memory could not return to the modern cabinet.");
        await Click(web,".chill-exit",
            "Boolean(document.querySelector('.chill-memory'))",
            "The Memory cabinet did not return to the React Lounge.");
        Require(await Js(web,
            "(document.querySelector('.chill-memory .chill-card-bottom span')?.textContent||'').includes('Resume')"),
            "The Lounge Memory tile did not show the persisted active game.");

        File.WriteAllText(Path.Combine(output,"modern-memory-result.txt"),
            "PASS: all five difficulty choices, four decks and previews, private 4x4 C# board, flip and match, mismatch delay, native resume, full victory, statistics, replay and Lounge navigation.");
    }

    private static async Task Flip(CoreWebView2 web,int index,string expected)
    {
        await Click(web,"[data-testid=\"memory-card-"+index+"\"]",expected,
            "Native Memory rejected or failed to render card "+index+".");
    }
    private static async Task Click(CoreWebView2 web,string selector,string expected,string failure)
    {
        string action=await web.ExecuteScriptAsync(
            "(()=>{const el=document.querySelector("+JsonSerializer.Serialize(selector)+");"
           +"if(!el||el.disabled)return false;el.click();return true})()");
        Require(action=="true","Missing or disabled React Memory action: "+selector);
        await Wait(web,expected,failure);
    }
    private static async Task<bool> Js(CoreWebView2 web,string script)=>
        await web.ExecuteScriptAsync(script)=="true";
    private static async Task Wait(CoreWebView2 web,string predicate,string failure)
    {
        Stopwatch elapsed=Stopwatch.StartNew();
        while(elapsed.Elapsed<TimeSpan.FromSeconds(30))
        {
            if(await Js(web,predicate))return;
            await Task.Delay(90);
        }
        throw new InvalidOperationException(failure);
    }
    private static async Task Capture(CoreWebView2 web,string path)
    {
        using FileStream stream=File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);
    }
    private static void Require(bool pass,string error)
    {
        if(!pass)throw new InvalidOperationException(error);
    }
}
