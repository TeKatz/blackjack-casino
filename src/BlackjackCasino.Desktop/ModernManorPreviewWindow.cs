using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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

    private readonly WebView2 browser=new()
    {
        Name="ModernManorBrowser", Visibility=Visibility.Hidden,
        DefaultBackgroundColor=System.Drawing.Color.FromArgb(255,14,12,21),
    };
    private readonly TextBlock notice;
    private readonly Border splash;
    private readonly BackgroundMusic soundtrack=new(Soundtrack.Manor);
    private readonly bool musicEnabled;
    private readonly double musicVolume;
    private bool surfaceReady;
    private readonly ManorModernGameService game;
    private bool initialized;
    private bool closed;
    private bool processing;

    internal ModernManorPreviewWindow(ManorController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        game=new ManorModernGameService(controller);
        musicEnabled=controller.Preferences.MusicEnabled && controller.Preferences.Presentation("midnight-manor").MusicEnabled;
        musicVolume=controller.Preferences.MusicVolume;
        Title="Midnight Manor";
        Width=1360;
        Height=870;
        MinWidth=940;
        MinHeight=660;
        Background=new SolidColorBrush(Color.FromRgb(14,12,21));

        var root=new Grid { Background=Background };
        notice=new TextBlock
        {
            Text="The house is stirring…",
            FontSize=16,
            Foreground=new SolidColorBrush(Color.FromRgb(209,190,169)),
            TextAlignment=TextAlignment.Center,
            Margin=new Thickness(0,20,0,0),
        };
        var splashCopy=new StackPanel
        {
            HorizontalAlignment=HorizontalAlignment.Center,
            VerticalAlignment=VerticalAlignment.Center,
            MaxWidth=660,
            Margin=new Thickness(30),
        };
        splashCopy.Children.Add(new TextBlock
        {
            Text="☾",
            FontFamily=new FontFamily("Georgia"),
            FontSize=72,
            Foreground=new SolidColorBrush(Color.FromRgb(214,173,114)),
            HorizontalAlignment=HorizontalAlignment.Center,
        });
        splashCopy.Children.Add(new TextBlock
        {
            Text="THE MIDNIGHT CLUB",
            FontSize=12,
            FontWeight=FontWeights.Bold,
            TextAlignment=TextAlignment.Center,
            Foreground=new SolidColorBrush(Color.FromRgb(189,157,119)),
            Margin=new Thickness(0,10,0,8),
        });
        splashCopy.Children.Add(new TextBlock
        {
            Text="MIDNIGHT MANOR",
            FontFamily=new FontFamily("Georgia"),
            FontSize=44,
            TextAlignment=TextAlignment.Center,
            Foreground=new SolidColorBrush(Color.FromRgb(246,220,172)),
        });
        splashCopy.Children.Add(notice);
        splash=new Border
        {
            Background=new LinearGradientBrush(
                Color.FromRgb(39,29,44),Color.FromRgb(10,15,25),95),
            Child=splashCopy,
            Visibility=Visibility.Visible,
            BorderBrush=new SolidColorBrush(Color.FromRgb(125,94,78)),
            BorderThickness=new Thickness(2),
        };
        root.Children.Add(browser);
        root.Children.Add(splash);
        Content=root;
        browser.Loaded+=Browser_Loaded;
        Closed+=(_,_)=>
        {
            closed=true;
            soundtrack.Dispose();
            browser.Dispose();
        };
    }

    private async void Browser_Loaded(object sender,RoutedEventArgs e)
    {
        if(initialized || closed) return;
        initialized=true;
        soundtrack.Start(musicEnabled,musicVolume);
        try
        {
            if(!IsAvailable)
                throw new FileNotFoundException("The modern Manor frontend is missing from this build.",EntryPath);

            CoreWebView2? core = await ModernWebViewBootstrap.InitializeAsync(
                browser, () => closed, Path.GetDirectoryName(EntryPath)!,
                ManorPreviewProtocol.IsTrustedSource);
            if (core is null) return;
            core.NavigationCompleted+=(_,args)=>
            {
                if(closed || args.IsSuccess) return;
                ShowStartupError("Midnight Manor failed to load: "+args.WebErrorStatus+". Reinstall the Development update if this persists.");
            };
            core.WebMessageReceived+=OnWebMessageReceived;
            browser.Source=new Uri(ManorPreviewProtocol.EntryPoint);
        }
        catch(Exception ex) when(ex is FileNotFoundException or IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            ShowStartupError("Midnight Manor could not start. Please check the Development installation.\n\n"+ex.Message);
        }
    }

    private void ShowStartupError(string message)
    {
        splash.BeginAnimation(OpacityProperty,null);
        splash.Opacity=1;
        splash.Visibility=Visibility.Visible;
        notice.Text=message;
        notice.TextWrapping=TextWrapping.Wrap;
    }

    private void RevealGame()
    {
        if(surfaceReady || closed) return;
        surfaceReady=true;
        browser.Visibility=Visibility.Visible;
        var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(360))
        { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } };
        fade.Completed+=(_,_)=>{ if(!closed) splash.Visibility=Visibility.Collapsed; };
        splash.BeginAnimation(OpacityProperty,fade);
    }

    private void OnWebMessageReceived(object? sender,CoreWebView2WebMessageReceivedEventArgs args)
    {
        if(closed || processing || !ManorPreviewProtocol.TryParseCommand(
            args.Source,args.WebMessageAsJson,out ManorPreviewProtocol.Command? command) || command is null)
            return;

        if(command.Type=="ui.ready")
        {
            RevealGame();
            return;
        }
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
