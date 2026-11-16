using System.Windows.Forms;
using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// True Windows STA/WebView2 integration check. Unlike the protocol smoke,
/// this navigates each bundled page and waits for React's UI-ready handshake.
/// Uses a unique temporary save and browser profile, never installed player data.
/// </summary>
internal static class NativeShellWebSmoke
{
    // WebView2's cold environment/controller initialization can be substantially
    // slower on a clean GitHub Actions Windows runner than on a developer PC.
    // The UI acknowledgement has a separate bounded deadline in NativeShellForm.
    private const int StartupPollAttempts =
        (NativeShellForm.BrowserInitializationTimeoutMs +
         NativeShellForm.BrowserUiReadyTimeoutMs + 5000) / 100;

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
            migrationCandidate: true, managedRelease: true, unattendedSmoke: true);
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
                for (int attempt = 0; attempt < StartupPollAttempts; attempt++)
                {
                    if (form.BrowserMode is null && form.BrowserReady
                        && form.BrowserCore is not null)
                    {
                        startupLobbyReady = true;
                        break;
                    }
                    if (form.BrowserStartupDiagnostic.Contains("phase=failed:", StringComparison.Ordinal))
                        break;
                    await Task.Delay(100);
                }
                if (!startupLobbyReady)
                    throw new InvalidOperationException(
                        "Native React startup lobby did not acknowledge its UI. " +
                        form.BrowserStartupDiagnostic);
                string imported = await form.BrowserCore!.ExecuteScriptAsync(
                    "(()=>{const p=document.querySelector('.profile-widget');"
                    + "const b=document.querySelector('.lobby-finances');"
                    + "return Boolean(p?.textContent?.includes('Cutover Web Smoke')"
                    + "&&b?.textContent?.includes('1,975.25'))})()");
                if (imported != "true")
                    throw new InvalidOperationException(
                        "The imported profile/wallet was not restored in the real React lobby.");
                // News is a one-time startup dialog, not a preview-only mock.
                // It must contain editorial copy from the installed offline feed.
                string newsOpened = await form.BrowserCore.ExecuteScriptAsync(
                    "(()=>{const dialog=document.querySelector('dialog.casino-news-editorial');"
                    + "return !!dialog?.open&&dialog.querySelectorAll('.news-editorial-item').length>0"
                    + "&&!!dialog.querySelector('.news-editorial-enter');})()");
                if (newsOpened != "true")
                    throw new InvalidOperationException(
                        "NativeShell first launch did not open its editorial News dialog.");
                await form.BrowserCore.ExecuteScriptAsync(
                    "document.querySelector('dialog.casino-news-editorial .news-editorial-enter')?.click()");
                await Task.Delay(100);
                string newsClosed = await form.BrowserCore.ExecuteScriptAsync(
                    "!document.querySelector('dialog.casino-news-editorial')");
                if (newsClosed != "true")
                    throw new InvalidOperationException("News could not be dismissed.");

                // The history screen must be populated from the isolated
                // imported hand-history file, not the live WPF user profile.
                await form.BrowserCore!.ExecuteScriptAsync(
                    "document.querySelector('.lobby-nav-item:nth-child(1)')?.getAttribute('aria-current')");
                // Regression: navigating back to Lobby must not animate
                // its entire art grid or show the former pixelated logo.
                string lobbyLayout = await form.BrowserCore!.ExecuteScriptAsync(
                    "(()=>{const grid=document.querySelector('.lobby-game-grid');"
                    + "const logo=document.querySelector('.brand-spade');"
                    + "return !!grid&&!!logo&&getComputedStyle(grid).animationName==='none'"
                    + "&&getComputedStyle(logo).backgroundImage.includes('club-spade.svg')})()");
                if (lobbyLayout != "true")
                    throw new InvalidOperationException(
                        "Lobby artwork re-entry animation or obsolete logo returned.");

                // Regression: the formerly animated Lobby tile cards must remain
                // the exact same DOM elements across a Slots -> Lobby filter trip.
                // Recreating them invalidates DOM-keyed Pixi scenes and triggers
                // the delayed fallback-art morph seen in production.
                string captured = await form.BrowserCore!.ExecuteScriptAsync(
                    "(()=>{const tiles=[...document.querySelectorAll('.lobby-game')];"
                    + "const buttons=[...document.querySelectorAll('.lobby-nav-item')];"
                    + "const slots=buttons.find(x=>x.textContent?.trim()==='Slots');"
                    + "const lobby=buttons.find(x=>x.textContent?.trim()==='Lobby');"
                    + "if(tiles.length<8||!slots||!lobby)return false;"
                    + "window.__nativeTileIdentitySmoke={tiles,ready:tiles.filter(x=>"
                    + "x.querySelector('.game-art-window')?.getAttribute('data-pixi-art-ready')==='true').length};"
                    + "slots.click();return true;})()");
                if (captured != "true")
                    throw new InvalidOperationException(
                        "Could not capture the original React lobby tiles for Pixi identity regression.");
                await Task.Delay(250);
                string filtered = await form.BrowserCore.ExecuteScriptAsync(
                    "(()=>{const state=window.__nativeTileIdentitySmoke;"
                    + "if(!state||state.tiles.length!==document.querySelectorAll('.lobby-game').length)return false;"
                    + "return state.tiles.every(x=>x.isConnected)&&"
                    + "state.tiles.filter(x=>getComputedStyle(x).display!=='none').length===4;})()");
                if (filtered != "true")
                    throw new InvalidOperationException(
                        "Lobby Slots tab remounted tiles or did not hide non-slot modes.");
                await form.BrowserCore.ExecuteScriptAsync(
                    "([...document.querySelectorAll('.lobby-nav-item')]"
                    + ".find(x=>x.textContent?.trim()==='Lobby'))?.click()");
                await Task.Delay(250);
                string restoredTiles = await form.BrowserCore.ExecuteScriptAsync(
                    "(()=>{const state=window.__nativeTileIdentitySmoke;"
                    + "if(!state)return false;const now=[...document.querySelectorAll('.lobby-game')];"
                    + "const same=now.length===state.tiles.length&&"
                    + "now.every((tile,i)=>tile===state.tiles[i]&&getComputedStyle(tile).display!=='none');"
                    + "const ready=now.filter(x=>x.querySelector('.game-art-window')"
                    + "?.getAttribute('data-pixi-art-ready')==='true').length;"
                    + "delete window.__nativeTileIdentitySmoke;return same&&ready>=state.ready;})()");
                if (restoredTiles != "true")
                    throw new InvalidOperationException(
                        "Lobby filter re-entry recreated DOM-keyed Pixi tiles or lost ready artwork.");
                Console.WriteLine("PASS: NativeShell Pixi lobby tile DOM identity survives Slots/Lobby filtering.");

                // The real WebView2 document owns focus. Simulate its F11
                // keyboard dispatch and require the native window to toggle
                // in BOTH directions without a WinForms toolbar.
                await form.BrowserCore.ExecuteScriptAsync(
                    "window.dispatchEvent(new KeyboardEvent('keydown',"
                    + "{key:'F11',bubbles:true,cancelable:true}))");
                bool full = false;
                for (int i = 0; i < 30; i++)
                {
                    if (form.FormBorderStyle == FormBorderStyle.None
                        && form.WindowState == FormWindowState.Maximized)
                    {
                        full = true;
                        break;
                    }
                    await Task.Delay(50);
                }
                if (!full) throw new InvalidOperationException(
                    "React/WebView2 F11 did not toggle the native host to fullscreen.");
                await form.BrowserCore.ExecuteScriptAsync(
                    "window.dispatchEvent(new KeyboardEvent('keydown',"
                    + "{key:'F11',bubbles:true,cancelable:true}))");
                bool restored = false;
                for (int i = 0; i < 30; i++)
                {
                    if (form.FormBorderStyle == FormBorderStyle.Sizable
                        && form.WindowState == FormWindowState.Normal)
                    {
                        restored = true;
                        break;
                    }
                    await Task.Delay(50);
                }
                if (!restored) throw new InvalidOperationException(
                    "React/WebView2 F11 failed to leave fullscreen.");
                if (form.Icon is null)
                    throw new InvalidOperationException(
                        "NativeShell executable has no casino window icon.");
                Console.WriteLine("PASS: WPF-free native startup lobby restored imported state.");

                foreach (string mode in new[]
                    { "blackjack", "roulette", "reels", "indian-ruby", "neon-vault", "nine-lives", "casino-empire", "chill-lounge", "midnight-merge",
                        "midnight-dice", "midnight-manor" })
                {
                    // Move the real Windows pointer inside the transitioning
                    // viewport. Without this probe earlier tests stayed green
                    // even when the default OS cursor flashed during loading.
                    Point cursorBefore = System.Windows.Forms.Cursor.Position;
                    Point cursorProbe = form.PointToScreen(
                        new Point(form.ClientSize.Width / 2, form.ClientSize.Height / 2));
                    bool canProbe = SystemInformation.UserInteractive;
                    try
                    {
                        if (canProbe) System.Windows.Forms.Cursor.Position = cursorProbe;
                        await form.OpenForAcceptanceAsync(mode);
                        if (canProbe && System.Windows.Forms.Cursor.Position == cursorProbe
                            && !form.TransitionOwnsPointerAt(cursorProbe))
                            throw new InvalidOperationException(mode +
                                " transition window lost native pointer hit-testing.");
                    }
                    finally
                    {
                        if (canProbe) System.Windows.Forms.Cursor.Position = cursorBefore;
                    }
                    if (!form.TransitionVisible)
                        throw new InvalidOperationException(mode
                            + " detached its WebView without the native transition veil.");
                    bool ready = false;
                    for (int attempt = 0; attempt < StartupPollAttempts; attempt++)
                    {
                        if (form.BrowserMode == mode && form.BrowserReady
                            && form.BrowserCore is not null)
                        {
                            ready = true;
                            break;
                        }
                        if (form.BrowserStartupDiagnostic.Contains("phase=failed:", StringComparison.Ordinal))
                            break;
                        await Task.Delay(100);
                    }
                    if (!ready)
                        throw new InvalidOperationException(
                            $"{mode} did not acknowledge its React UI. " +
                            form.BrowserStartupDiagnostic);

                    string domReady = await form.BrowserCore!.ExecuteScriptAsync(
                        "Boolean(document.readyState==='complete' && document.body"
                        + " && document.body.children.length>0)");
                    if (domReady != "true")
                        throw new InvalidOperationException(mode + " did not render its React document.");
                    if (mode is "blackjack" or "roulette")
                    {
                        // The native veil must remain until WebView2 is ready
                        // and then disappear; otherwise it can hide gameplay.
                        await Task.Delay(750);
                        if (form.TransitionVisible)
                            throw new InvalidOperationException(mode
                                + " left the native transition covering the ready game.");
                        string htmlMask = await form.BrowserCore!.ExecuteScriptAsync(
                            "document.getElementById('mode-boot')===null");
                        if (htmlMask != "true")
                            throw new InvalidOperationException(mode
                                + " did not dismiss the unified in-document loading mask.");
                    }
                    if (mode == "indian-ruby")
                    {
                        // The first-ever spin used to skip motion because the initial empty
                        // native snapshot did not trigger the first-result effect.
                        string headline = await form.BrowserCore!.ExecuteScriptAsync(
                            "Boolean(document.querySelector('.sunset-title')"
                            + "?.textContent?.includes('SUNSET RUBY')"
                            + " && !document.querySelector('.ruby-machine-top')"
                            + "?.textContent?.includes('SUNSET RUBY'))");
                        if (headline != "true")
                            throw new InvalidOperationException(
                                "Sunset Ruby lost its single premium title.");
                        string balancedLayout = await form.BrowserCore.ExecuteScriptAsync(
                            "(() => { const strap=document.querySelector('.sunset-title-strap');"
                            + " const title=document.querySelector('.sunset-title');"
                            + " const subtitle=document.querySelector('.sunset-title-subtitle');"
                            + " const wallet=document.querySelector('.sunset-balance');"
                            + " const wager=document.querySelector('.sunset-bet-console');"
                            + " if(!strap||!title||!subtitle||!wallet||!wager)return false;"
                            + " const a=strap.getBoundingClientRect();"
                            + " const b=title.getBoundingClientRect();"
                            + " const c=subtitle.getBoundingClientRect();"
                            + " const d=wallet.getBoundingClientRect();"
                            + " return a.bottom<=b.top+2 && b.bottom<=c.top+2"
                            + " && d.width>=150 && d.width<=260"
                            + " && wager.querySelectorAll('.sunset-bet-ticks span').length===9"
                            + " && Boolean(wager.querySelector('[data-testid=ruby-bet-value]'));"
                            + " })()");
                        if (balancedLayout != "true")
                            throw new InvalidOperationException(
                                "Sunset Ruby header overlaps, wallet is too wide or bet console is incomplete.");
                        string canPlay = await form.BrowserCore.ExecuteScriptAsync(
                            "Boolean(document.querySelector('.ruby-spin')"
                            + " && !document.querySelector('.ruby-spin').disabled)");
                        if (canPlay == "true")
                        {
                            await form.BrowserCore.ExecuteScriptAsync(
                                "document.querySelector('.ruby-spin').click(); true");
                            bool firstSpinAnimated = false;
                            for (int frame = 0; frame < 28; frame++)
                            {
                                await Task.Delay(90);
                                string isRolling = await form.BrowserCore.ExecuteScriptAsync(
                                    "Boolean(document.querySelector('.ruby-reel-column.rolling'"
                                    + " + '[data-tape-ready=\"true\"]'))");
                                if (isRolling == "true")
                                {
                                    firstSpinAnimated = true;
                                    break;
                                }
                            }
                            // GitHub Windows runners may enable system reduced-motion.
                            // In that case the C# spin must settle without a tape by design.
                            string reducedMode = await form.BrowserCore.ExecuteScriptAsync(
                                "window.matchMedia('(prefers-reduced-motion: reduce)').matches");
                            if (!firstSpinAnimated && reducedMode == "true")
                            {
                                string settled = await form.BrowserCore.ExecuteScriptAsync(
                                    "document.querySelector('.ruby-premium-shell')"
                                    + "?.getAttribute('data-phase')==='result'");
                                if (settled == "true")
                                {
                                    firstSpinAnimated = true;
                                    Console.WriteLine("PASS: Sunset Ruby first spin settles under reduced motion.");
                                }
                            }
                            if (!firstSpinAnimated)
                            {
                                string diagnostic = await form.BrowserCore.ExecuteScriptAsync(
                                    "JSON.stringify({phase:document.querySelector('.ruby-premium-shell')"
                                    + "?.getAttribute('data-phase'),reduced:window.matchMedia("
                                    + "'(prefers-reduced-motion: reduce)').matches,"
                                    + "rolling:document.querySelectorAll('.ruby-reel-column.rolling').length,"
                                    + "tapes:document.querySelectorAll('[data-tape-ready]').length,"
                                    + "notice:document.querySelector('.ruby-bridge-notice')?.textContent,"
                                    + "spinDisabled:document.querySelector('.ruby-spin')?.disabled})");
                                throw new InvalidOperationException(
                                    "Sunset Ruby first affordable spin skipped its reel animation. "
                                    + diagnostic);
                            }
                        }
                    }
                    Console.WriteLine("PASS: WPF-free WebView2 React UI-ready: " + mode);
                    form.ReturnForAcceptance();
                    if (form.BrowserMode is not null)
                        throw new InvalidOperationException(mode + " could not return to the native lobby.");
                    // Route switching is serialized. Wait for the React lobby
                    // before attempting the next game rather than racing its
                    // initializing WebView2 controller.
                    bool returnedToLobby = false;
                    for (int attempt = 0; attempt < StartupPollAttempts; attempt++)
                    {
                        if (form.BrowserMode is null && form.BrowserReady
                            && form.BrowserCore is not null)
                        {
                            returnedToLobby = true;
                            break;
                        }
                        if (form.BrowserStartupDiagnostic.Contains("phase=failed:", StringComparison.Ordinal))
                            break;
                        await Task.Delay(100);
                    }
                    if (!returnedToLobby)
                        throw new InvalidOperationException(mode
                            + " could not restore the ready React lobby. " +
                            form.BrowserStartupDiagnostic);
                }
                // Returning from a real game must show the real React lobby,
                // not a dead WinForms test menu or an untrusted document.
                bool lobbyReady = false;
                for (int attempt = 0; attempt < StartupPollAttempts; attempt++)
                {
                    if (form.BrowserMode is null && form.BrowserReady
                        && form.BrowserCore is not null)
                    {
                        lobbyReady = true;
                        break;
                    }
                    await Task.Delay(100);
                }
                if (!lobbyReady)
                    throw new InvalidOperationException("Native React lobby was not ready.");
                string lobbyDom = await form.BrowserCore!.ExecuteScriptAsync(
                    "Boolean(document.querySelector('.casino-lobby-shell'))");
                if (lobbyDom != "true")
                    throw new InvalidOperationException("Native lobby did not render React.");
                // The same process may recreate the lobby many times while
                // switching games. None of those returns may reopen News.
                string repeatedNews = await form.BrowserCore.ExecuteScriptAsync(
                    "Boolean(document.querySelector('dialog.casino-news-editorial'))");
                if (repeatedNews != "false")
                    throw new InvalidOperationException(
                        "News reopened after returning from a casino game.");
                // The sidebar still opens News on demand even after auto-open was consumed.
                await form.BrowserCore.ExecuteScriptAsync(
                    "([...document.querySelectorAll('.lobby-nav-item')]"
                    + ".find(b=>b.textContent?.trim()==='News'))?.click()");
                await Task.Delay(100);
                string manualNews = await form.BrowserCore.ExecuteScriptAsync(
                    "Boolean(document.querySelector('dialog.casino-news-editorial')?.open)");
                if (manualNews != "true")
                    throw new InvalidOperationException("Manual sidebar News entry did not open.");
                await form.BrowserCore.ExecuteScriptAsync(
                    "document.querySelector('dialog.casino-news-editorial .news-editorial-enter')?.click()");
                Console.WriteLine("PASS: one-time native startup News and manual reopen.");
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
