using BlackjackCasino.Desktop;
using BlackjackCasino.Core;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Can run on a Windows CI runner without opening a GUI or creating browser
/// processes. Proves the new executable has no reference to PresentationFramework
/// and can use the exact current domain/protocol/persistence contracts.
/// </summary>
internal static class NativeShellContractSmoke
{
    internal static int Run()
    {
        string directory = Path.Combine(Path.GetTempPath(),
            "blackjack-native-shell-smoke-" + Guid.NewGuid().ToString("N"));
        try
        {
            var assembly = typeof(NativeShellForm).Assembly;
            if (assembly.GetReferencedAssemblies().Any(name =>
                    name.Name is "PresentationFramework" or "PresentationCore" or "WindowsBase"))
                throw new InvalidOperationException("Native shell still references WPF assemblies.");

            if (!BlackjackModernProtocol.Trusted("https://midnight-manor.local/blackjack.html")
                || BlackjackModernProtocol.Trusted("https://midnight-manor.local/reels.html")
                || !ModernReelsProtocol.Trusted("https://midnight-manor.local/reels.html")
                || ModernReelsProtocol.Trusted("https://example.com/reels.html")
                || !ModernNeonVaultProtocol.Trusted("https://midnight-manor.local/neon-vault.html")
                || ModernNeonVaultProtocol.Trusted("https://midnight-manor.local/nine-lives.html")
                || !ModernNineLivesProtocol.Trusted("https://midnight-manor.local/nine-lives.html")
                || ModernNineLivesProtocol.Trusted("https://example.com/nine-lives.html"))
                throw new InvalidOperationException("Browser page trust is not isolated.");

            using (var session = new NativePreviewRuntime(directory))
            {
                decimal initial = session.Bankroll;
                string id = Guid.NewGuid().ToString("D");
                var epoch = new BlackjackModernSession();
                var command = new BlackjackModernCommand(id, epoch.Id, 0, "bet.set", "25");
                BlackjackModernReply result = epoch.Execute(command, session.ApplyBlackjack);
                if (!result.Success || result.Revision != 1 || session.Bankroll != initial)
                    throw new InvalidOperationException("Blackjack native command failed.");
                if (epoch.Execute(command, _ => throw new Exception("Replayed command!")) != result)
                    throw new InvalidOperationException("Blackjack receipt was replayed.");

                session.OpenReels();
                var spinEpoch = new ModernReelsSession();
                var bet = new ModernReelsCommand(Guid.NewGuid().ToString("D"),
                    spinEpoch.Id, 0, "bet.set", "25");
                ModernReelsReply betReply = spinEpoch.Execute(bet, session.ApplyReels);
                if (!betReply.Success || betReply.Revision != 1)
                    throw new InvalidOperationException("Reels bet protocol failed.");
                var spin = new ModernReelsCommand(Guid.NewGuid().ToString("D"),
                    spinEpoch.Id, 1, "spin", null);
                ModernReelsReply receipt = spinEpoch.Execute(spin, session.ApplyReels);
                if (!receipt.Success || receipt.Revision != 2)
                    throw new InvalidOperationException("Authoritative Reels spin failed.");
                if (spinEpoch.Execute(spin, _ => throw new Exception("Replayed spin!")) != receipt)
                    throw new InvalidOperationException("Reels spin executed twice.");
                session.CloseReels();
                session.OpenNeonVault();
                var neonEpoch = new ModernNeonVaultSession();
                var neonCommand = new ModernNeonVaultCommand(Guid.NewGuid().ToString("D"),
                    neonEpoch.Id, 0, "spin", null);
                ModernNeonVaultReply neonReceipt = neonEpoch.Execute(neonCommand, session.ApplyNeonVault);
                if (!neonReceipt.Success || neonEpoch.Execute(neonCommand,
                        _ => throw new Exception("Duplicate Neon Vault spin!")) != neonReceipt)
                    throw new InvalidOperationException("Neon Vault receipt was not idempotent.");
                session.CloseNeonVault();

                session.OpenNineLives();
                var nineEpoch = new ModernNineLivesSession();
                var nineCommand = new ModernNineLivesCommand(Guid.NewGuid().ToString("D"),
                    nineEpoch.Id, 0, "spin", null);
                ModernNineLivesReply nineReceipt = nineEpoch.Execute(nineCommand, session.ApplyNineLives);
                if (!nineReceipt.Success || nineEpoch.Execute(nineCommand,
                        _ => throw new Exception("Duplicate Nine Lives spin!")) != nineReceipt)
                    throw new InvalidOperationException("Nine Lives receipt was not idempotent.");
                session.CloseNineLives();
                session.Persist();
            }
            AppPreferences saved = PlayerStateStore.Load(directory);
            if (saved.Bankroll < 0 || !File.Exists(Path.Combine(directory, "profile.json")))
                throw new InvalidOperationException("Isolated native-shell checkpoint was not stored.");
            Console.WriteLine("PASS: independent WinForms host, exact React protocol, Core Blackjack/Reels, "
                + "Neon Vault/Nine Lives receipts and preview-only save location; no WPF reference.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL native shell smoke: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
