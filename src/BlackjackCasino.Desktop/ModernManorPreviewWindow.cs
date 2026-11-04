using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace BlackjackCasino.Desktop;

/// <summary>
/// The modern Manor host renders from a C# journal-backed session. The same
/// Core engine and durable wallet/profile checkpoint power both WPF and PixiJS.
/// </summary>
internal sealed class ModernManorPreviewWindow : Window
{
    internal static string EntryPath => Path.Combine(AppContext.BaseDirectory,"ModernFrontend","index.html");
    internal static bool IsAvailable => File.Exists(EntryPath);

    private readonly WebView2 browser=new();
    private readonly TextBlock notice;
    private readonly ManorModernGameService game;
    private bool initialized;
    private bool closed;
    private bool processing;

    internal ModernManorPreviewWindow(ManorController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        game=new ManorModernGameService(controller);
        Title="Midnight Manor";
        Width=1360;
        Height=870;
        MinWidth=940;
        MinHeight=660;
        Background=new SolidColorBrush(Color.FromRgb(14,12,21));

        var root=new Grid { Background=Background };
        notice=new TextBlock
        {
            Text="Loading Midnight Manor…",
            FontSize=17,
            Foreground=Brushes.Bisque,
            TextWrapping=TextWrapping.Wrap,
            HorizontalAlignment=HorizontalAlignment.Center,
            VerticalAlignment=VerticalAlignment.Center,
            Margin=new Thickness(40),
            Visibility=Visibility.Collapsed,
        };
        root.Children.Add(browser);
        root.Children.Add(notice);
        Content=root;
        browser.Loaded+=Browser_Loaded;
        Closed+=(_,_)=>
        {
            closed=true;
            browser.Dispose();
        };
    }

    private async void Browser_Loaded(object sender,RoutedEventArgs e)
    {
        if(initialized || closed) return;
        initialized=true;
        try
        {
            if(!IsAvailable)
                throw new FileNotFoundException("The modern Manor frontend is missing from this build.",EntryPath);

            await browser.EnsureCoreWebView2Async();
            if(closed) return;
            CoreWebView2 core=browser.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled=false;
            core.Settings.AreDevToolsEnabled=false;
            core.Settings.AreHostObjectsAllowed=false;
            core.Settings.IsStatusBarEnabled=false;
            core.Settings.IsWebMessageEnabled=true;
            core.Settings.IsZoomControlEnabled=false;
            core.SetVirtualHostNameToFolderMapping(
                ManorPreviewProtocol.VirtualHost,
                Path.GetDirectoryName(EntryPath)!,
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting+=(_,args)=>
            {
                if(!ManorPreviewProtocol.IsTrustedSource(args.Uri)) args.Cancel=true;
            };
            core.NewWindowRequested+=(_,args)=>args.Handled=true;
            core.PermissionRequested+=(_,args)=>args.State=CoreWebView2PermissionState.Deny;
            core.NavigationCompleted+=(_,args)=>
            {
                if(closed || args.IsSuccess) return;
                notice.Text="Midnight Manor failed to load: "+args.WebErrorStatus+". Reinstall the Development update if this persists.";
                notice.Visibility=Visibility.Visible;
            };
            core.WebMessageReceived+=OnWebMessageReceived;
            browser.Source=new Uri(ManorPreviewProtocol.EntryPoint);
        }
        catch(Exception ex) when(ex is FileNotFoundException or IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            notice.Text="Midnight Manor could not start. Please check the Development installation.\n\n"+ex.Message;
            notice.Visibility=Visibility.Visible;
        }
    }

    private void OnWebMessageReceived(object? sender,CoreWebView2WebMessageReceivedEventArgs args)
    {
        if(closed || processing || !ManorPreviewProtocol.TryParseCommand(
            args.Source,args.WebMessageAsJson,out ManorPreviewProtocol.Command? command) || command is null)
            return;

        if(command.Type=="ui.close")
        {
            Close();
            return;
        }
        if(command.Type is "client.ready" or "snapshot.request")
        {
            browser.CoreWebView2.PostWebMessageAsJson(
                ManorPreviewProtocol.SerializeSnapshot(game.Snapshot(),command.RequestId));
            return;
        }

        processing=true;
        try
        {
            ManorPreviewProtocol.Snapshot snapshot=game.Execute(command);
            browser.CoreWebView2.PostWebMessageAsJson(
                ManorPreviewProtocol.SerializeSnapshot(snapshot,command.RequestId));
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException
            or InvalidDataException or InvalidOperationException or ArgumentException or OverflowException)
        {
            // On uncertain journal failures, the service blocks all further mutations
            // until the player closes and reopens to run standard checkpoint recovery.
            browser.CoreWebView2.PostWebMessageAsJson(
                ManorPreviewProtocol.SerializeError(command.RequestId,ex.Message,game.Snapshot(),
                    game.RequiresRestart));
        }
        finally
        {
            processing=false;
        }
    }
}
