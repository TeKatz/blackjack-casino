using System.IO;
using System.Reflection;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core.Jigsaw;
using Microsoft.Web.WebView2.Core;

/// <summary>
/// Exercises the packaged React/PixiJS Jigsaw board against the real native game,
/// while the original WPF JigsawSmoke remains rollback comparison coverage.
/// </summary>
internal static class ModernJigsawSmoke
{
    internal static async Task Run(MainWindow window, CoreWebView2 web, string output)
    {
        await Click(web, ".chill-jigsaw",
            "document.querySelectorAll('.jigsaw-gallery button').length===12",
            "React Jigsaw did not open the twelve-scene gallery.");
        Require(await Js(web,"document.querySelectorAll('.jigsaw-sizes button').length===8"),
            "One of the eight original puzzle sizes is missing.");
        await Wait(web, @"[...document.querySelectorAll('.jigsaw-gallery img')].length===12 &&
          [...document.querySelectorAll('.jigsaw-gallery img')].every(img=>
            img.complete && img.naturalWidth>=1600 && img.naturalHeight>=900 &&
            img.currentSrc.includes('/jigsaw/scenes/'))",
            "The new dedicated 12 high-resolution Jigsaw scene images did not load.");
        await Capture(web,Path.Combine(output,"modern-jigsaw-gallery.png"));

        await Click(web,".jigsaw-sizes button:first-child",
            "document.querySelector('.jigsaw-sizes button:first-child')?.getAttribute('aria-pressed')==='true'",
            "24-piece Jigsaw selection did not reach the native model.");
        await Click(web,".jigsaw-actions .jigsaw-primary:last-child",
            "document.querySelectorAll('.jigsaw-tray-grid button').length===24",
            "C# did not open a fresh 24-piece Jigsaw board.");
        await Wait(web,"Boolean(document.querySelector('.jigsaw-board[data-renderer-ready=true] canvas'))",
            "PixiJS did not mount the real Jigsaw canvas.");
        Require(await Js(web,"document.querySelector('.jigsaw-actions button[aria-pressed]')?.getAttribute('aria-pressed')==='false'"),
            "The finished puzzle reference must start hidden.");
        Require(await Js(web,"[...document.querySelectorAll('.jigsaw-tray-grid button')].some((b,i)=>Number(b.dataset.pieceId)!==i)"),
            "Jigsaw tray must not expose solved row-by-row piece order.");
        await Capture(web,Path.Combine(output,"modern-jigsaw-board.png"));

        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        ModernJigsawService service=(ModernJigsawService)
            typeof(MainWindow).GetField("modernJigsaw",flags)!.GetValue(window)!;
        JigsawGameState game=service.CurrentGame
            ?? throw new InvalidOperationException("Native Jigsaw engine did not start.");
        Guid id=game.GameId;
        Require(game.PieceCount==24 && game.Pieces.Count==24,"The native puzzle has wrong dimensions.");

        await web.ExecuteScriptAsync("document.querySelector('.jigsaw-tray-grid [data-piece-id=\"0\"]')?.click()");
        // Exercise a real pointer press/release pair. The Pixi board deliberately rejects
        // orphan pointerup events so dropping a piece from the tray cannot double-place it.
        // Compute the logical piece center through the letterboxed responsive canvas.
        // React sends puzzle coordinates; only the authoritative C# engine can lock it.
        await web.ExecuteScriptAsync(@"(()=>{
          const b=document.querySelector('.jigsaw-board');
          const r=b.getBoundingClientRect();
          const fit=Math.min(r.width/960,r.height/600);
          const x=r.left+(r.width-960*fit)/2+80*fit;
          const y=r.top+(r.height-600*fit)/2+75*fit;
          const common={bubbles:true,pointerId:11,pointerType:'mouse',
           isPrimary:true,button:0,clientX:x,clientY:y};
          b.dispatchEvent(new PointerEvent('pointerdown',{...common,buttons:1}));
          b.dispatchEvent(new PointerEvent('pointerup',{...common,buttons:0}));
        })()");
        await UntilAsync(()=>service.CurrentGame?.Pieces[0].Locked==true,
            "Pixi board did not commit the native first-piece snap.");
        Require(service.CurrentGame!.LockedCount==1,"One placement must not complete other pieces.");

        await Click(web,".chill-exit",
            "document.querySelectorAll('.jigsaw-gallery button').length===12",
            "Back navigation lost the active saved Jigsaw puzzle.");
        Require(await Js(web,"Boolean(document.querySelector('.jigsaw-actions .jigsaw-primary'))"),
            "Saved Jigsaw resume is missing.");
        await Click(web,".jigsaw-actions .jigsaw-primary",
            "document.querySelectorAll('.jigsaw-tray-grid button').length===23",
            "React Jigsaw did not restore the saved placed piece.");
        Require(service.CurrentGame?.GameId==id && service.CurrentGame.Pieces[0].Locked,
            "Saved Jigsaw session identity or locked piece was lost.");
        await Capture(web,Path.Combine(output,"modern-jigsaw-resumed.png"));

        await Click(web,".chill-exit",
            "document.querySelectorAll('.jigsaw-gallery button').length===12",
            "Jigsaw could not return to its React gallery.");
        await Click(web,".chill-exit",
            "Boolean(document.querySelector('.chill-jigsaw'))",
            "Jigsaw could not return to the React Lounge.");

        File.WriteAllText(Path.Combine(output,"modern-jigsaw-result.txt"),
            "PASS: twelve motifs, eight sizes, PixiJS render, native 24-piece state, first-piece snap, durable save, React resume and Lounge navigation.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Click(CoreWebView2 web,string selector,string ready,string failure)
    {
        string script="Boolean(document.querySelector("+System.Text.Json.JsonSerializer.Serialize(selector)+"))";
        await Wait(web,script,"Missing Jigsaw control: "+selector);
        await web.ExecuteScriptAsync("document.querySelector("+
            System.Text.Json.JsonSerializer.Serialize(selector)+")?.click()");
        await Wait(web,ready,failure);
    }
    private static async Task<bool> Js(CoreWebView2 web,string predicate) =>
        await web.ExecuteScriptAsync(predicate)=="true";
    private static async Task Wait(CoreWebView2 web,string predicate,string failure)
    {
        for(int i=0;i<150;i++)
        {
            if(await Js(web,predicate))return;
            await Task.Delay(100);
        }
        throw new InvalidOperationException(failure);
    }
    private static async Task UntilAsync(Func<bool> predicate,string failure)
    {
        for(int i=0;i<150;i++)
        {
            if(predicate())return;
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
