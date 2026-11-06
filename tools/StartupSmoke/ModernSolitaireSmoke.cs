using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Full native-host React card-table acceptance. The legacy WPF Solitaire smoke
/// continues as rollback coverage in the same startup profile.
/// </summary>
internal static class ModernSolitaireSmoke
{
    internal static async Task Run(CoreWebView2 web, string output)
    {
        await Click(web, ".chill-solitaire",
            "document.querySelectorAll('.sol-option').length===8",
            "Modern Solitaire cabinet did not show six games and three Spider difficulties.");
        Require(await Js(web, "Boolean(document.querySelector('[data-testid=\"solitaire-deal-number\"]'))"),
            "Numbered FreeCell input is missing.");
        Require(await Js(web, "Boolean(document.querySelector('[data-testid=\"solitaire-achievements-toggle\"]'))"),
            "Solitaire achievements navigation is missing.");
        await Capture(web, Path.Combine(output, "modern-solitaire-cabinet.png"));

        bool resumeKlondike = await Js(web,
            "Boolean(document.querySelector('[data-testid=\"solitaire-resume-Klondike-1\"]:not(:disabled)'))");
        await Click(web, resumeKlondike ? "[data-testid=\"solitaire-resume-Klondike-1\"]" :
                "[data-testid=\"solitaire-new-Klondike-1\"]",
            "document.querySelector('[data-testid=\"modern-solitaire-game\"]')?.dataset.variant==='Klondike'",
            "Klondike did not open in the React Lounge.");
        Require(await Js(web,"document.querySelectorAll('[data-testid^=\"solitaire-pile-T\"]').length===7"),
            "Klondike must render seven tableau columns.");
        Require(await Js(web,"document.querySelector('[data-testid=\"solitaire-pile-Stock\"] .sol-stock-count')?.textContent==='24'"),
            "Klondike stock did not contain the original 24 cards.");
        await Capture(web, Path.Combine(output, "modern-solitaire-klondike.png"));
        await Click(web,"[data-testid=\"solitaire-pile-Stock\"] .sol-playing-card",
            "document.querySelector('[data-testid=\"solitaire-moves\"]')?.textContent==='1'",
            "Drawing from the native Klondike stock did not commit a move.");
        Require(await Js(web,"document.querySelector('[data-testid=\"solitaire-pile-Stock\"] .sol-stock-count')?.textContent==='23'"),
            "Klondike draw changed the move counter but not the actual native deck.");
        await Click(web,"[data-testid=\"solitaire-back\"]",
            "Boolean(document.querySelector('[data-testid=\"solitaire-library\"]'))",
            "Klondike could not return to the React cabinet.");
        await Click(web,"[data-testid=\"solitaire-resume-Klondike-1\"]",
            "document.querySelector('[data-testid=\"modern-solitaire-game\"]')?.dataset.variant==='Klondike'",
            "Klondike resume did not recover the persisted deal.");
        Require(await Js(web,"document.querySelector('[data-testid=\"solitaire-moves\"]')?.textContent==='1'"),
            "Resuming Klondike lost the previous native move.");
        await Back(web);

        // Replay a specific numbered FreeCell deal in the actual React UI.
        string numbered=await web.ExecuteScriptAsync(
            "(()=>{const input=document.querySelector('[data-testid=\"solitaire-deal-number\"]');"
            + "if(!input)return false;const setter=Object.getOwnPropertyDescriptor("
            + "HTMLInputElement.prototype,'value').set;setter.call(input,'617');"
            + "input.dispatchEvent(new Event('input',{bubbles:true}));return true;})()");
        Require(numbered=="true","React FreeCell deal-number input is unavailable.");
        await Click(web,"[data-testid=\"solitaire-numbered\"]",
            "document.querySelector('[data-testid=\"modern-solitaire-game\"]')?.dataset.variant==='FreeCell'",
            "Numbered FreeCell deal did not open.");
        Require(await Js(web,"document.querySelectorAll('[data-testid^=\"solitaire-pile-C\"]').length===12"),
            "FreeCell must render eight columns and four free cells.");
        Require(await Js(web,"document.querySelector('.sol-game-stats b')?.textContent==='#617'"),
            "FreeCell ignored the requested deterministic deal number.");
        await Capture(web,Path.Combine(output,"modern-solitaire-freecell.png"));
        await Back(web);

        foreach (int suits in new[] {1,2,4})
        {
            await Click(web,$"[data-testid=\"solitaire-new-Spider-{suits}\"]",
                "document.querySelector('[data-testid=\"modern-solitaire-game\"]')?.dataset.variant==='Spider'",
                $"Spider ({suits} suits) did not open in the React table.");
            Require(await Js(web,"document.querySelectorAll('[data-testid^=\"solitaire-pile-T\"]').length===10"),
                "Spider must display ten tableau columns.");
            Require(await Js(web,"document.querySelector('[data-testid=\"solitaire-pile-Stock\"] .sol-stock-count')?.textContent==='50'"),
                "Spider lost its original stock of 50 cards.");
            if(suits==2)await Capture(web,Path.Combine(output,"modern-solitaire-spider.png"));
            await Back(web);
        }

        foreach ((string name,int expected) in new[] {("Pyramid",28),("Golf",7),("TriPeaks",28)})
        {
            await Click(web,$"[data-testid=\"solitaire-new-{name}-1\"]",
                "document.querySelector('[data-testid=\"modern-solitaire-game\"]')?.dataset.variant==='"
                    +name+"'",
                $"React {name} failed to load.");
            string prefix=name=="Golf"?"G":"P";
            Require(await Js(web,"document.querySelectorAll('[data-testid^=\"solitaire-pile-"+prefix+"\"]').length==="+expected),
                $"React {name} omitted some of its native columns or pyramid positions.");
            await Capture(web,Path.Combine(output,$"modern-solitaire-{name.ToLowerInvariant()}.png"));
            await Back(web);
        }

        await Click(web, ".chill-exit",
            "Boolean(document.querySelector('.chill-solitaire'))",
            "Solitaire cabinet did not return to the React Lounge.");
        File.WriteAllText(Path.Combine(output,"modern-solitaire-result.txt"),
            "PASS: native C# React Solitaire across Klondike, numbered FreeCell, all Spider suit modes, Pyramid, Golf and TriPeaks; stock draw, durable resume, achievements, cabinet and Lounge back navigation.");
    }

    private static async Task Back(CoreWebView2 web) =>
        await Click(web,"[data-testid=\"solitaire-back\"]",
            "Boolean(document.querySelector('[data-testid=\"solitaire-library\"]'))",
            "Solitaire could not return to the game cabinet.");

    private static async Task Click(CoreWebView2 web,string selector,string predicate,string message)
    {
        string result=await web.ExecuteScriptAsync(
            "(()=>{const el=document.querySelector("+JsonSerializer.Serialize(selector)
            +");if(!el||el.disabled)return false;el.click();return true})()");
        Require(result=="true","Unavailable Solitaire button: "+selector);
        await Wait(web,predicate,message);
    }
    private static async Task<bool> Js(CoreWebView2 web,string script) =>
        await web.ExecuteScriptAsync(script)=="true";
    private static async Task Wait(CoreWebView2 web,string condition,string message)
    {
        Stopwatch watch=Stopwatch.StartNew();
        while(watch.Elapsed<TimeSpan.FromSeconds(25))
        {
            if(await Js(web,condition))return;
            await Task.Delay(80);
        }
        throw new InvalidOperationException(message);
    }
    private static async Task Capture(CoreWebView2 web,string path)
    {
        using FileStream stream=File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);
    }
    private static void Require(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
    }
}
