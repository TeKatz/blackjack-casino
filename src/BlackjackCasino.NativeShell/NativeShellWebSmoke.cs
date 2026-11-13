using System.Windows.Forms;

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
        using var form = new NativeShellForm(root);

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
                Console.WriteLine("PASS: WPF-free native startup lobby ready.");

                foreach (string mode in new[]
                    { "blackjack", "roulette", "reels", "neon-vault", "nine-lives", "casino-empire", "midnight-merge",
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
        try { Directory.Delete(root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return result;
    }
}
