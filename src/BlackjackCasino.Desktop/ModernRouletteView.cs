using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

internal sealed class ModernRouletteView : ContentControl, IDisposable
{
    private const string Entry = "https://midnight-manor.local/roulette.html";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly WebView2 browser = new() { DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 5, 19, 16) };
    private readonly Func<object> snapshot;
    private readonly Func<Guid, JsonElement, object> spin;
    private readonly Action close, failure;
    private readonly DispatcherTimer watchdog = new() { Interval = TimeSpan.FromSeconds(18) };
    private bool initialized, disposed, failed, listening;
    internal bool Ready { get; private set; }
    internal WebView2 Browser => browser;
    internal static bool IsAvailable => File.Exists(Path.Combine(AppContext.BaseDirectory,"ModernFrontend","roulette.html"));
    internal ModernRouletteView(Func<object> snapshot, Func<Guid, JsonElement, object> spin, Action close, Action failure)
    {
        this.snapshot=snapshot; this.spin=spin; this.close=close; this.failure=failure;
        Name="ModernRoulettePresentation";
        Content=browser;
        Loaded+=OnLoaded;
        watchdog.Tick+=(_,_)=>{watchdog.Stop();if(!Ready)Fail();};
    }
    private static bool Trusted(string? uri)=>string.Equals(uri,Entry,StringComparison.Ordinal);
    private async void OnLoaded(object sender,RoutedEventArgs args)
    {
        if(initialized||disposed)return;
        initialized=true;watchdog.Start();
        try
        {
            var core=await ModernWebViewBootstrap.InitializeAsync(browser,()=>disposed,Path.Combine(AppContext.BaseDirectory,"ModernFrontend"),Trusted);
            if(core is null)return;
            core.NavigationCompleted+=(_,e)=>{if(!e.IsSuccess)Fail();};
            core.ProcessFailed+=(_,_)=>Fail();
            core.WebMessageReceived+=OnMessage;
            browser.Source=new Uri(Entry);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.COMException){Fail();}
    }
    private void OnMessage(object? sender,CoreWebView2WebMessageReceivedEventArgs args)
    {
        if(disposed||failed||!Trusted(args.Source)||args.WebMessageAsJson.Length>8192)return;
        try
        {
            using var doc=JsonDocument.Parse(args.WebMessageAsJson);
            var root=doc.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty("type",out var type)||type.ValueKind!=JsonValueKind.String)return;
            switch(type.GetString())
            {
                case "roulette.ready": listening=true;Post("roulette.snapshot",snapshot());break;
                case "roulette.ui-ready" when listening: Ready=true;watchdog.Stop();break;
                case "roulette.close" when Ready: close();break;
                case "roulette.spin" when Ready && IsVisible:
                    if(root.TryGetProperty("roundId",out var id)&&id.ValueKind==JsonValueKind.String&&Guid.TryParse(id.GetString(),out var round)&&round!=Guid.Empty&&root.TryGetProperty("bets",out var bets))
                    {Post("roulette.result",spin(round,bets));Post("roulette.snapshot",snapshot());}
                    break;
            }
        }
        catch(JsonException){}
    }
    private void Post(string type,object data)
    {
        if(!disposed&&browser.CoreWebView2 is { } core)core.PostWebMessageAsJson(JsonSerializer.Serialize(new {type,data},Json));
    }
    private void Fail()
    {
        if(disposed||failed)return;
        failed=true;Ready=false;watchdog.Stop();
        Dispatcher.BeginInvoke(new Action(()=>{if(!disposed)failure();}));
    }
    public void Dispose(){if(disposed)return;disposed=true;watchdog.Stop();Loaded-=OnLoaded;browser.Dispose();}
}
