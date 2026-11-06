using System.IO;
using System.Reflection;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core.Adventure;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Runs packaged React story/character UI against the original AdventureEngine,
/// save writer and WebView2 host. WPF BackroomLoungeSmoke remains rollback coverage.
/// </summary>
internal static class ModernAdventureSmoke
{
    internal static async Task Run(MainWindow window, CoreWebView2 web, string output)
    {
        await Click(web,".chill-tales-backroom",
            "Boolean(document.querySelector('[data-testid=\"modern-adventure-library\"]'))",
            "React Tales library failed to open.");
        Require(await Js(web,"document.querySelectorAll('.backroom-tale-grid article').length===4"),
            "The four original Tales must be in the modern library.");
        Require(await Js(web,"document.querySelectorAll('.backroom-creator select option').length>=4"),
            "Native character archetypes missing in React.");
        await Capture(web,Path.Combine(output,"modern-adventure-library.png"));

        await Click(web,".backroom-creator .backroom-primary",
            "Boolean(document.querySelector('[data-testid=\"modern-adventure-story\"]'))",
            "The native AdventureEngine did not begin a run.");
        ModernAdventureService service=(ModernAdventureService)typeof(MainWindow)
            .GetField("modernAdventure",BindingFlags.Instance|BindingFlags.NonPublic)!
            .GetValue(window)!;
        AdventureRunState run=service.ActiveRun ??
            throw new InvalidOperationException("Native Adventure run not active.");
        string runId=run.RunId;
        string adventureId=run.AdventureId;
        Require(AdventureCatalog.All.Any(a=>a.Id==adventureId),
            "React began an unknown adventure definition.");
        Require(await Js(web,"Boolean(document.querySelector('.backroom-story-prose p'))"),
            "Localized story prose missing.");
        Require(await Js(web,"document.querySelectorAll('.backroom-abilities>div').length===6"),
            "Character sheet lost an attribute.");
        await Capture(web,Path.Combine(output,"modern-adventure-scene.png"));

        if (await Js(web,"Boolean(document.querySelector('.backroom-choice'))"))
        {
            await Click(web,".backroom-choice:first-of-type",
                "Boolean(document.querySelector('.backroom-story-prose'))",
                "Native choice did not return a valid story snapshot.");
            Require(service.ActiveRun!.Statistics.ChoicesMade>=1,
                "The browser choice never reached authoritative C# statistics.");
            if (service.ActiveRun.PendingChoiceId is not null)
            {
                await Click(web,".backroom-d20.active button",
                    "Boolean(document.querySelector('.backroom-story-prose'))",
                    "Native D20 roll was not accepted.");
                Require(service.ActiveRun!.RollsMade>=1,
                    "The browser did not use the native C# die resolver.");
            }
        }

        if (service.Screen=="story")
        {
            await Click(web,".chill-exit",
                "Boolean(document.querySelector('[data-testid=\"modern-adventure-library\"]'))",
                "Return to the adventure library did not preserve the run.");
            Require(await Js(web,"Boolean(document.querySelector('.backroom-tale-actions .backroom-primary'))"),
                "Saved adventure resume is missing.");
            // Multiple completed stories may exist; use the native selected adventure identifier.
            string selector=".backroom-tale-grid article.selected .backroom-tale-actions .backroom-primary";
            await Click(web,selector,
                "Boolean(document.querySelector('[data-testid=\"modern-adventure-story\"]'))",
                "Resuming an unchanged C# story failed.");
            Require(service.ActiveRun?.RunId==runId,
                "Resume replaced the existing character or story identity.");
            await Capture(web,Path.Combine(output,"modern-adventure-resumed.png"));
        }

        if (service.Screen=="story")
        {
            await Click(web,".chill-exit",
                "Boolean(document.querySelector('[data-testid=\"modern-adventure-library\"]'))",
                "Adventure did not return to its library.");
        }
        if (service.Screen=="library")
        {
            await Click(web,".chill-exit",
                "Boolean(document.querySelector('.chill-tales-backroom'))",
                "Adventure library did not return to the React Lounge.");
        }

        File.WriteAllText(Path.Combine(output,"modern-adventure-result.txt"),
            "PASS: four stories, character classes, native run creation, story prose, character sheet, choice checks, saved resume and React Lounge navigation.");
    }

    private static void Require(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
    }

    private static async Task Click(CoreWebView2 web,string selector,string predicate,string failure)
    {
        await Wait(web,"Boolean(document.querySelector("+
            System.Text.Json.JsonSerializer.Serialize(selector)+"))",
            "Missing Tales control: "+selector);
        await web.ExecuteScriptAsync("document.querySelector("+
            System.Text.Json.JsonSerializer.Serialize(selector)+")?.click()");
        await Wait(web,predicate,failure);
    }

    private static async Task<bool> Js(CoreWebView2 web,string expression)
        => await web.ExecuteScriptAsync(expression)=="true";

    private static async Task Wait(CoreWebView2 web,string predicate,string failure)
    {
        for(int i=0;i<120;i++)
        {
            if(await Js(web,predicate))return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(failure);
    }

    private static async Task Capture(CoreWebView2 web,string path)
    {
        using FileStream stream=File.Create(path);
        await web.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,stream);
    }
}
