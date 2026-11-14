using System.Drawing;
using System.Text.Json;
using BlackjackCasino.HostShared;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Independent native case-board window sharing the original Tale service and
/// receipt epoch. Only board-position/view intents can mutate this surface.
/// </summary>
internal sealed class NativeLoungeCaseBoardForm : Form
{
    private const string Host = "midnight-manor.local";
    private readonly NativePreviewRuntime runtime;
    private readonly Task<CoreWebView2Environment> environment;
    private readonly string folder;
    private readonly Action parentChanged;
    private readonly WebView2 web = new()
    {
        Dock = DockStyle.Fill,
        DefaultBackgroundColor = Color.FromArgb(7, 23, 20),
    };
    private bool listening;
    private bool closing;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal NativeLoungeCaseBoardForm(NativePreviewRuntime runtime,
        Task<CoreWebView2Environment> environment, string folder,
        Action parentChanged)
    {
        this.runtime = runtime;
        this.environment = environment;
        this.folder = folder;
        this.parentChanged = parentChanged;
        Text = "Tales from the Backroom · Case Board";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1200, 800);
        MinimumSize = new Size(800, 600);
        Controls.Add(web);
        Shown += async (_, _) => await InitializeAsync();
        FormClosed += (_, _) => closing = true;
    }

    private static bool Trusted(string? source) =>
        string.Equals(source, "https://" + Host + "/lounge.html", StringComparison.Ordinal);

    private async Task InitializeAsync()
    {
        if (!File.Exists(Path.Combine(folder, "lounge.html"))) { Close(); return; }
        try
        {
            CoreWebView2Environment env = await environment;
            if (closing || IsDisposed) return;
            await web.EnsureCoreWebView2Async(env);
            if (closing || IsDisposed) return;
            CoreWebView2 core = web.CoreWebView2
                ?? throw new InvalidOperationException("Tales case board WebView2 unavailable.");
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.SetVirtualHostNameToFolderMapping(
                Host, folder, CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, e) => { if (!Trusted(e.Uri)) e.Cancel = true; };
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, e) => { if (!e.IsSuccess) Close(); };
            core.ProcessFailed += (_, _) => { if (!closing) BeginInvoke(new Action(Close)); };
            core.WebMessageReceived += OnMessage;
            web.Source = new Uri("https://" + Host + "/lounge.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            if (!closing && !IsDisposed) Close();
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (closing || !Trusted(args.Source) || args.WebMessageAsJson.Length > 2048) return;
        try
        {
            using JsonDocument json = JsonDocument.Parse(args.WebMessageAsJson);
            JsonElement root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("version", out JsonElement version)
                || !version.TryGetInt32(out int protocol) || protocol != 1) return;
            switch (type.GetString())
            {
                case "lounge.ready":
                    listening = true;
                    RefreshBoard();
                    break;
                case "lounge.ui-ready" when listening:
                    RefreshBoard();
                    break;
                case "lounge.close" or "lounge.adventure.board-window" when listening:
                    Close();
                    break;
                case "lounge.adventure.command" when listening:
                {
                    if (ModernAdventureProtocol.Parse(args.WebMessageAsJson) is not { } command)
                        break;
                    if (command.Action == "adventure.board.close") { Close(); break; }
                    if (command.Action is not ("adventure.board.move" or "adventure.board.view"))
                        break;
                    NativeLoungeReply? result = runtime.ApplyLounge(
                        "lounge.adventure.command", args.WebMessageAsJson, boardOnly: true);
                    if (result is not null) Post(result.Type, result.Data);
                    RefreshBoard();
                    parentChanged();
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Never replay a failed board command without a fresh saved state.
            Close();
        }
    }

    private void Post(string type, object data)
    {
        if (!closing && web.CoreWebView2 is { } core)
            core.PostWebMessageAsJson(JsonSerializer.Serialize(new { type, data }, Json));
    }

    internal void RefreshBoard()
    {
        if (listening && !closing && !runtime.LoungeRequiresRestart)
            Post("lounge.snapshot", runtime.LoungeSnapshot(boardOnly: true));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        closing = true;
        base.OnFormClosing(e);
    }
}
