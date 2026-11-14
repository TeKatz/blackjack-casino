using System.Drawing;
using System.Text.Json;
using BlackjackCasino.HostShared;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// WPF-free detached Empire controller, independently mounted WebView2 epoch.
/// Both embedded and detached windows share exactly one authoritative
/// EmpireService and durable native profile; never fork or simulate estate currencies.
/// </summary>
internal sealed class NativeEmpireDetachedForm : Form
{
    private readonly NativeCasinoRuntime runtime;
    private readonly Task<CoreWebView2Environment> environment;
    private readonly string folder;
    private readonly Action parentChanged;
    private readonly WebView2 web = new()
    {
        Dock = DockStyle.Fill,
        DefaultBackgroundColor = Color.FromArgb(7, 23, 20),
    };
    private readonly System.Windows.Forms.Timer tick = new() { Interval = 2000 };
    private readonly System.Windows.Forms.Timer timeout = new() { Interval = 18000 };
    private readonly EmpireModernSession commands = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private bool listening, ready, closing;
    private DateTimeOffset lastSave = DateTimeOffset.UtcNow;

    internal NativeEmpireDetachedForm(NativeCasinoRuntime runtime,
        Task<CoreWebView2Environment> environment, string folder,
        Action parentChanged)
    {
        this.runtime = runtime;
        this.environment = environment;
        this.folder = folder;
        this.parentChanged = parentChanged;
        Text = "Casino Empire · Detached";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1280, 820);
        MinimumSize = new Size(800, 600);
        Controls.Add(web);
        tick.Tick += (_, _) =>
        {
            if (!ready || closing || runtime.LoungeRequiresRestart) return;
            runtime.TickEmpire();
            RefreshEmpire();
            if (DateTimeOffset.UtcNow - lastSave >= TimeSpan.FromSeconds(30))
            {
                runtime.Persist();
                lastSave = DateTimeOffset.UtcNow;
            }
        };
        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            if (!ready && !closing) Close();
        };
        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (!File.Exists(Path.Combine(folder, "empire.html"))) { Close(); return; }
        timeout.Start();
        try
        {
            CoreWebView2Environment env = await environment;
            if (closing || IsDisposed) return;
            await web.EnsureCoreWebView2Async(env);
            if (closing || IsDisposed) return;
            CoreWebView2 core = web.CoreWebView2
                ?? throw new InvalidOperationException("Detached Empire WebView2 unavailable.");
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.SetVirtualHostNameToFolderMapping(
                ManorPreviewProtocol.VirtualHost, folder,
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, e) =>
            {
                if (!EmpireModernProtocol.Trusted(e.Uri)) e.Cancel = true;
            };
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.NavigationCompleted += (_, e) => { if (!e.IsSuccess && !closing) Close(); };
            core.ProcessFailed += (_, _) =>
            {
                if (!closing && IsHandleCreated) BeginInvoke(new Action(Close));
            };
            core.WebMessageReceived += OnMessage;
            web.Source = new Uri(
                "https://" + ManorPreviewProtocol.VirtualHost + "/empire.html");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            if (!closing && !IsDisposed) Close();
        }
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (closing || !EmpireModernProtocol.Trusted(e.Source)
            || e.WebMessageAsJson.Length > 8192) return;
        try
        {
            using JsonDocument json = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement msg = json.RootElement;
            if (msg.ValueKind != JsonValueKind.Object
                || !msg.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String) return;
            switch (type.GetString())
            {
                case "empire.ready":
                    listening = true;
                    RefreshEmpire();
                    break;
                case "empire.ui-ready" when listening:
                    if (!msg.TryGetProperty("visitId", out JsonElement visit)
                        || !visit.TryGetInt32(out int id) || id != 1) break;
                    ready = true;
                    timeout.Stop();
                    tick.Start();
                    Post("empire.activated", new { VisitId = 1 });
                    Post("empire.visibility", new { Visible = true });
                    break;
                case "empire.refresh" when listening:
                    RefreshEmpire();
                    break;
                case "empire.close" when listening:
                    Close();
                    break;
                case "empire.dismiss-offline" when listening:
                    RefreshEmpire();
                    break;
                case "empire.command" when listening && ready
                    && WindowState != FormWindowState.Minimized:
                {
                    EmpireModernCommand? intent = EmpireModernProtocol.Parse(e.WebMessageAsJson);
                    if (intent is null) break;
                    EmpireModernReply reply = commands.Execute(intent,
                        runtime.CommitEmpire, runtime.EmpirePreview);
                    Post("empire.result", reply);
                    if (!intent.Preview)
                    {
                        RefreshEmpire();
                        parentChanged();
                    }
                    break;
                }
            }
        }
        catch (JsonException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or InvalidDataException)
        {
            Close();
        }
    }

    private void Post(string type, object data)
    {
        if (listening && !closing && web.CoreWebView2 is { } core)
            core.PostWebMessageAsJson(
                JsonSerializer.Serialize(new { type, data }, Json));
    }

    internal void RefreshEmpire()
    {
        if (!listening || closing || !runtime.EmpireStarted) return;
        Post("empire.snapshot", new
        {
            Version = EmpireModernProtocol.Version,
            Session = commands.Id,
            commands.Revision,
            VisitId = 1,
            Notice = (string?)null,
            Offline = (object?)null,
            LastReply = (EmpireModernReply?)null,
            State = runtime.EmpireSnapshot(detached: true),
        });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        closing = true;
        tick.Stop();
        timeout.Stop();
        try
        {
            runtime.CloseEmpire();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidDataException) { }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tick.Dispose();
            timeout.Dispose();
            web.Dispose();
        }
        base.Dispose(disposing);
    }
}
