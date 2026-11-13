using BlackjackCasino.Desktop;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;

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

            if (HostMigrationCatalog.PreviewRoutes.Count != 7
                || HostMigrationCatalog.ApprovedForRelease
                || !HostMigrationCatalog.IsPreviewRoute("midnight-manor")
                || HostMigrationCatalog.IsPreviewRoute("casino-empire")
                || HostMigrationCatalog.PreviewPageFor("reels") != "reels.html")
                throw new InvalidOperationException("Host parity manifest is inconsistent.");
            string live = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlackjackCasino");
            try
            {
                HostPreviewStorage.RequireIsolatedDirectory(live,
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                throw new InvalidOperationException("Native preview accepted live player data.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "NativeShell preview cannot use production player storage.")
            {
                // Correct: production state must remain untouched.
            }

            if (!BlackjackModernProtocol.Trusted("https://midnight-manor.local/blackjack.html")
                || BlackjackModernProtocol.Trusted("https://midnight-manor.local/reels.html")
                || !ModernReelsProtocol.Trusted("https://midnight-manor.local/reels.html")
                || ModernReelsProtocol.Trusted("https://example.com/reels.html")
                || !ModernNeonVaultProtocol.Trusted("https://midnight-manor.local/neon-vault.html")
                || ModernNeonVaultProtocol.Trusted("https://midnight-manor.local/nine-lives.html")
                || !ModernNineLivesProtocol.Trusted("https://midnight-manor.local/nine-lives.html")
                || ModernNineLivesProtocol.Trusted("https://example.com/nine-lives.html")
                || !ModernMergeProtocol.Trusted("https://midnight-manor.local/midnight-merge.html")
                || ModernMergeProtocol.Trusted("https://example.com/midnight-merge.html")
                || !ModernDiceProtocol.Trusted("https://midnight-manor.local/midnight-dice.html")
                || ModernDiceProtocol.Trusted("https://example.com/midnight-dice.html")
                || !ManorPreviewProtocol.IsTrustedSource("https://midnight-manor.local/index.html")
                || ManorPreviewProtocol.IsTrustedSource("https://example.com/index.html"))
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

                session.OpenMerge();
                var mergeEpoch = new ModernMergeSession();
                var spawn = new ModernMergeCommand(Guid.NewGuid().ToString("D"),
                    mergeEpoch.Id, session.MergeRevision, "spawn", "chip-case",
                    null, null, null, null);
                ModernMergeReply mergeReceipt = mergeEpoch.Execute(spawn,
                    session.MergeRevision, session.ApplyMerge);
                if (!mergeReceipt.Success || mergeReceipt.Revision != session.MergeRevision ||
                    mergeEpoch.Execute(spawn, session.MergeRevision,
                        _ => throw new Exception("Duplicate Merge spawn!")) != mergeReceipt)
                    throw new InvalidOperationException("Merge receipt was not idempotent.");
                session.CloseMerge();

                session.OpenDice();
                var diceEpoch = new ModernDiceSession();
                var casual = new ModernDiceCommand(Guid.NewGuid().ToString("D"),
                    diceEpoch.Id, 0, "casual.start", null, null, null);
                ModernDiceReply casualReceipt = diceEpoch.Execute(casual, session.ApplyDice);
                if (!casualReceipt.Success || casualReceipt.Revision != 1)
                    throw new InvalidOperationException("Casual Midnight Dice start failed.");
                var roll = new ModernDiceCommand(Guid.NewGuid().ToString("D"),
                    diceEpoch.Id, 1, "roll", null, null, null);
                ModernDiceReply rollReceipt = diceEpoch.Execute(roll, session.ApplyDice);
                if (!rollReceipt.Success || rollReceipt.Revision != 2 ||
                    diceEpoch.Execute(roll, _ => throw new Exception("Replayed Dice roll!")) != rollReceipt)
                    throw new InvalidOperationException("Dice roll was not a single authoritative C# command.");
                session.CloseDice();

                session.OpenManor();
                string manorId = Guid.NewGuid().ToString("D");
                var manorSpin = new ManorPreviewProtocol.Command("spin.paid", manorId,
                    long.Parse(session.ManorSnapshot().Revision, System.Globalization.CultureInfo.InvariantCulture),
                    Bet: session.Preferences.ManorBet);
                var manorBefore = session.ManorSnapshot();
                ManorPreviewProtocol.Snapshot manorAfter = session.ApplyManor(manorSpin);
                if (long.Parse(manorAfter.Revision, System.Globalization.CultureInfo.InvariantCulture)
                    != long.Parse(manorBefore.Revision, System.Globalization.CultureInfo.InvariantCulture) + 1)
                    throw new InvalidOperationException("Manor journal did not advance.");
                if (session.ApplyManor(manorSpin).Revision != manorAfter.Revision)
                    throw new InvalidOperationException("Manor replay was not idempotent.");
                session.CloseManor();
                session.Persist();
            }
            AppPreferences saved = PlayerStateStore.Load(directory);
            if (saved.Bankroll < 0 || !File.Exists(Path.Combine(directory, "profile.json")))
                throw new InvalidOperationException("Isolated native-shell checkpoint was not stored.");
            string english = NativeMergeText.Translate("Merge.Title", UiLanguagePreference.English);
            string german = NativeMergeText.Translate("Merge.Eyebrow", UiLanguagePreference.German);
            if (english != "Midnight Merge" || !german.Contains("RESTAURIERUNG", StringComparison.Ordinal))
                throw new InvalidOperationException("Original English/German Merge resources were not loaded.");

            Console.WriteLine("PASS: WPF-free WinForms host; Blackjack, Reels, Neon Vault, "
                + "Nine Lives, Merge and Dice; original C# commands, replay protection and isolated saves.");
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
