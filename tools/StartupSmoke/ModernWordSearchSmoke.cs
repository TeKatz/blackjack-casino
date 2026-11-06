using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using BlackjackCasino.Core.WordSearch;
using BlackjackCasino.Desktop;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Real WebView2 Word Search acceptance in the modern Lounge.
/// Legacy WPF smoke still exercises its emergency fallback implementation.
/// </summary>
internal static class ModernWordSearchSmoke
{
    internal static async Task Run(MainWindow window, CoreWebView2 web, string output)
    {
        await Click(web, ".chill-word-search",
            "document.querySelectorAll('.ws-level').length===6",
            "React Word Search did not open all difficulty tiers.");
        Require(await Js(web,
            "document.querySelectorAll('.ws-level').length===6 && document.querySelector('.ws-stats')?.textContent?.includes('288')===true"),
            "Expanded Word Search catalog must show 288 puzzles.");
        Require(await Js(web,
            "[...document.querySelectorAll('.ws-progress-text')].every(el=>el.textContent.includes('/48'))"),
            "Word Search must show 48 puzzles in every difficulty.");
        await Capture(web, Path.Combine(output, "modern-word-search-library.png"));

        // The native fallback smoke ran first; the next Relaxed puzzle must load
        // without resetting its completed puzzle IDs or serialized German/English grids.
        if (await Js(web,"Boolean(document.querySelector('[data-testid=\"word-search-resume\"]'))"))
        {
            await Click(web,"[data-testid=\"word-search-resume\"]",
                "Boolean(document.querySelector('[data-testid=\"modern-word-search-game\"]'))",
                "The previously saved Word Search did not resume.");
        }
        else
        {
            await Click(web,"[data-testid=\"word-search-start-Relaxed\"]",
                "Boolean(document.querySelector('[data-testid=\"modern-word-search-game\"]'))",
                "A Relaxed Word Search did not open in React.");
        }
        Require(await Js(web,"document.querySelectorAll('.ws-cell').length===64"),
            "Relaxed Word Search did not render its 8x8 native board.");
        await Capture(web, Path.Combine(output, "modern-word-search-game.png"));

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        ModernWordSearchService service =
            (ModernWordSearchService)typeof(MainWindow).GetField("modernWordSearch", flags)!.GetValue(window)!;
        WordSearchGameState game = service.CurrentGame
            ?? throw new InvalidOperationException("The modern Word Search service has no active game.");
        WordSearchPlacement[] placements = game.Puzzle.Placements.ToArray();
        int current = game.FoundWords.Count;
        WordSearchPlacement? firstUnfound = placements.FirstOrDefault(p => !game.FoundWords.Contains(p.Word));
        Require(firstUnfound is not null, "Expected an unfinished native Word Search puzzle.");

        await FindWord(web, firstUnfound!, ++current);
        await Click(web,"[data-testid=\"word-search-hint\"]",
            "document.querySelector('[data-testid=\"word-search-hints\"]')?.textContent==='1'",
            "C# Word Search hint count did not update.");
        Require(await Js(web,"Boolean(document.querySelector('.ws-cell.hint'))"),
            "The Word Search hint did not highlight the saved native start letter.");

        await Click(web,"[data-testid=\"word-search-back\"]",
            "Boolean(document.querySelector('[data-testid=\"word-search-library\"]'))",
            "Word Search could not return to its modern difficulty cabinet.");
        Require(await Js(web,"Boolean(document.querySelector('[data-testid=\"word-search-resume\"]'))"),
            "Leaving an unfinished Word Search must preserve a resumable puzzle.");
        await Click(web,"[data-testid=\"word-search-resume\"]",
            "Boolean(document.querySelector('[data-testid=\"modern-word-search-game\"]'))",
            "Resuming Word Search failed.");
        Require(await Js(web,
            "document.querySelector('[data-testid=\"word-search-found\"]')?.textContent==='"
              +current+"/"+placements.Length+"'"),
            "Resumed Word Search lost the previously found word.");
        Require(await Js(web,
            "document.querySelector('[data-testid=\"word-search-hints\"]')?.textContent==='1'"),
            "Resuming Word Search lost hint usage.");

        foreach (WordSearchPlacement p in placements.Where(p => p.Word != firstUnfound!.Word && !game.FoundWords.Contains(p.Word)))
            await FindWord(web,p,++current);

        await Wait(web, "Boolean(document.querySelector('[data-testid=\"word-search-completion\"]'))",
            "Completing all native C# words did not show the React reward summary.");
        Require(current==placements.Length,"The completion path skipped a target word.");
        await Capture(web, Path.Combine(output, "modern-word-search-complete.png"));
        await Click(web,"[data-testid=\"word-search-back\"]",
            "Boolean(document.querySelector('[data-testid=\"word-search-library\"]'))",
            "The completed puzzle did not return to the modern library.");
        Require(await Js(web,"!document.querySelector('[data-testid=\"word-search-resume\"]')"),
            "Completed Word Search was incorrectly retained as an unfinished save.");
        await Click(web,".chill-exit",
            "Boolean(document.querySelector('.chill-word-search'))",
            "Word Search did not return to the React Lounge.");
        Require(await Js(web,
            "/\\d+ puzzles complete/.test(document.querySelector('.chill-word-search .chill-card-bottom span')?.textContent||'')"),
            "Modern Word Search activity tile did not refresh the completion count.");

        File.WriteAllText(Path.Combine(output, "modern-word-search-result.txt"),
            "PASS: 288 Word Search puzzles, six tiers of 48, React 8x8 game, native found-word selection, reverse support via C#, hint highlight, durable resume, full native completion, reward summary and refreshed Lounge status.");
    }

    private static async Task FindWord(CoreWebView2 web, WordSearchPlacement p, int expected)
    {
        string source="[data-testid=\"word-search-cell-"+p.StartRow+"-"+p.StartColumn+"\"]";
        string dest="[data-testid=\"word-search-cell-"+p.EndRow+"-"+p.EndColumn+"\"]";
        string clicked=await web.ExecuteScriptAsync(
            "(()=>{const start=document.querySelector("+JsonSerializer.Serialize(source)+");"
            +"const end=document.querySelector("+JsonSerializer.Serialize(dest)+");"
            +"if(!start||!end)return false;start.click();end.click();return true})()");
        Require(clicked=="true","The React word selection endpoints are not interactive.");
        await Wait(web,
            "document.querySelector('[data-testid=\"word-search-found\"]')?.textContent?.startsWith('"
            +expected+"/')===true",
            "Native Word Search did not accept the authored word "+p.Word);
    }

    private static async Task Click(CoreWebView2 web,string selector,string predicate,string message)
    {
        string done=await web.ExecuteScriptAsync(
            "(()=>{const el=document.querySelector("+JsonSerializer.Serialize(selector)
            +");if(!el||el.disabled)return false;el.click();return true})()");
        Require(done=="true","Unavailable Word Search button: "+selector);
        await Wait(web,predicate,message);
    }
    private static async Task<bool> Js(CoreWebView2 web,string script)=>
        await web.ExecuteScriptAsync(script)=="true";
    private static async Task Wait(CoreWebView2 web,string predicate,string message)
    {
        Stopwatch watch=Stopwatch.StartNew();
        while(watch.Elapsed<TimeSpan.FromSeconds(30))
        {
            if(await Js(web,predicate))return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(message);
    }
    private static async Task Capture(CoreWebView2 web,string path)
    {
        using FileStream stream=File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);
    }
    private static void Require(bool success,string message)
    {
        if(!success)throw new InvalidOperationException(message);
    }
}
