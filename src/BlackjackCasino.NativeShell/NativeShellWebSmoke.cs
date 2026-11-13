using System.Windows.Forms;
using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// True Windows STA/WebView2 integration check. Unlike the protocol smoke,
/// this navigates each bundled page and waits for React's UI-ready handshake.
/// Uses a unique temporary save and browser profile, never installed player data.
/// </summary>
internal static class NativeShellWebSmoke
{
    internal static int Run()
    {
        string root = Path.Combine(Path.GetTempPath(),
            "blackjack-native-web-smoke-" + Guid.NewGuid().ToString("N"));
        int result = 1;
        // Exercise the complete read-only legacy-import route before opening a
        // real Windows WebView2 session. All files are synthetic and temporary.
        string legacy = Path.Combine(root, "legacy");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "player-state.json"),
            JsonSerializer.Serialize(new AppPreferences { Bankroll = 1975.25m }));
        File.WriteAllText(Path.Combine(legacy, "profile.json"),
            JsonSerializer.Serialize(new PlayerProfileState { DisplayName = "Cutover Web Smoke" }));
        var importedRound = new RoundHistoryEntry(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-20), DateTimeOffset.UtcNow,
            1975.25m, 1975.25m, 10m, 0m, 0m, 0m, new GameRules(), [], [], [], []);
        File.WriteAllText(Path.Combine(legacy, "hand-history.json"),
            JsonSerializer.Serialize(new[] { importedRound }));
        byte[] originalHistory = File.ReadAllBytes(Path.Combine(legacy, "hand-history.json"));
        string candidate = NativeHostMigration.Stage(legacy, "web-smoke",
            checkRunningHost: false);
        if (NativeHostMigration.OpenCandidate(legacy, "web-smoke") != candidate)
            throw new InvalidDataException("Staged WebView2 candidate was not verified.");
        // Run the real managed Development presentation: the React lobby must
        // occupy the entire native window, without the migration tab strip or
        // preview diagnostic footer.
        using var form = new NativeShellForm(candidate,
            migrationCandidate: true, managedDevelopment: true);
        if (form.Controls.Count != 1 || form.Controls[0] is not Panel host
            || host.Dock != DockStyle.Fill)
            throw new InvalidOperationException(
                "Managed NativeShell still exposes migration navigation or diagnostic footer.");

        form.Shown += async (_, _) =>
        {
            try
            {
                // Exercise the actual startup path and avoid racing a second
                // WebView2 initialization against the initial React lobby.
                bool startupLobbyReady = false;
                for (int attempt = 0; attempt < 180; attempt++)
                {
                    if (form.PreviewMode is null && form.PreviewReady
                        && form.PreviewBrowser is not null)
                    {
                        startupLobbyReady = true;
                        break;
                    }
                    await Task.Delay(100);
                }
                if (!startupLobbyReady)
                    throw new InvalidOperationException(
                        "Native React startup lobby did not acknowledge its UI within the watchdog.");
                string imported = await form.PreviewBrowser!.ExecuteScriptAsync(
                    "(()=>{const p=document.querySelector('.profile-widget');"
                    + "const b=document.querySelector('.lobby-finances');"
                    + "return Boolean(p?.textContent?.includes('Cutover Web Smoke')"
                    + "&&b?.textContent?.includes('1,975.25'))})()");
                if (imported != "true")
                    throw new InvalidOperationException(
                        "The imported profile/wallet was not restored in the real React lobby.");
                // The history screen must be populated from the isolated
                // imported hand-history file, not the live WPF user profile.
                await form.PreviewBrowser!.ExecuteScriptAsync(
                    "document.querySelector('.lobby-nav-item:nth-child(1)')?.getAttribute('aria-current')");
                Console.WriteLine("PASS: WPF-free native startup lobby restored imported state.");

                foreach (string mode in new[]
                    { "blackjack", "roulette", "reels", "neon-vault", "nine-lives", "casino-empire", "chill-lounge", "midnight-merge",
                        "midnight-dice", "midnight-manor" })
                {
                    await form.OpenForAcceptanceAsync(mode);
                    bool ready = false;
                    for (int attempt = 0; attempt < 180; attempt++)
                    {
                        if (form.PreviewMode == mode && form.PreviewReady
                            && form.PreviewBrowser is not null)
                        {
                            ready = true;
                            break;
                        }
                        await Task.Delay(100);
                    }
                    if (!ready)
                        throw new InvalidOperationException(
                            $"{mode} did not acknowledge its React UI within the startup watchdog.");

                    string domReady = await form.PreviewBrowser!.ExecuteScriptAsync(
                        "Boolean(document.readyState==='complete' && document.body"
                        + " && document.body.children.length>0)");
                    if (domReady != "true")
                        throw new InvalidOperationException(mode + " did not render its React document.");
                    Console.WriteLine("PASS: WPF-free WebView2 React UI-ready: " + mode);
                    form.ReturnForAcceptance();
                    if (form.PreviewMode is not null)
                        throw new InvalidOperationException(mode + " could not return to the native lobby.");
                }
                // Returning from a real game must show the real React lobby,
                // not a dead WinForms test menu or an untrusted document.
                bool lobbyReady = false;
                for (int attempt = 0; attempt < 180; attempt++)
                {
                    if (form.PreviewMode is null && form.PreviewReady
                        && form.PreviewBrowser is not null)
                    {
                        lobbyReady = true;
                        break;
                    }
                    await Task.Delay(100);
                }
                if (!lobbyReady)
                    throw new InvalidOperationException("Native React lobby was not ready.");
                string lobbyDom = await form.PreviewBrowser!.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.casino-lobby-shell'))");
                if (lobbyDom != "true")
                    throw new InvalidOperationException("Native lobby did not render React.");
                Console.WriteLine("PASS: WPF-free native React casino lobby.");
                result = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL native WinForms/WebView2 smoke: " + ex);
            }
            finally
            {
                form.Close();
            }
        };

        Application.Run(form);
        if (!originalHistory.SequenceEqual(
                File.ReadAllBytes(Path.Combine(legacy, "hand-history.json")))
            || JsonSerializer.Deserialize<AppPreferences>(
                File.ReadAllText(Path.Combine(legacy, "player-state.json")))?.Bankroll != 1975.25m)
            throw new InvalidOperationException("Candidate gameplay altered the legacy save.");
        try { Directory.Delete(root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return result;
    }
}
