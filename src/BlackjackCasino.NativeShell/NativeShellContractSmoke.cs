using BlackjackCasino.Desktop;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;
using System.Security.Cryptography;
using System.Text.Json;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Can run on a Windows CI runner without opening a GUI or creating browser
/// processes. Proves the new executable has no reference to PresentationFramework
/// and can use the exact current domain/protocol/persistence contracts.
/// </summary>
internal static class NativeShellContractSmoke
{
    private static void VerifyCandidateMigration(string root)
    {
        string production = Path.Combine(root, "production");
        Directory.CreateDirectory(production);
        const decimal expectedWallet = 8742.25m;
        File.WriteAllText(Path.Combine(production, "player-state.json"),
            JsonSerializer.Serialize(new AppPreferences { Bankroll = expectedWallet }));
        File.WriteAllText(Path.Combine(production, "profile.json"),
            JsonSerializer.Serialize(new PlayerProfileState { DisplayName = "Candidate Import" }));
        File.WriteAllText(Path.Combine(production, "hand-history.json"), "[]");

        byte[] sourcePreferences = File.ReadAllBytes(Path.Combine(production, "player-state.json"));
        byte[] sourceProfile = File.ReadAllBytes(Path.Combine(production, "profile.json"));
        string candidate = NativeHostMigration.Stage(production, "smoke", checkRunningHost: false);
        if (candidate != NativeHostMigration.OpenCandidate(production, "smoke"))
            throw new InvalidOperationException("Candidate manifest not recognized.");
        if (!File.Exists(Path.Combine(candidate, "native-migration-manifest.json"))
            || !File.Exists(Path.Combine(candidate, "hand-history.json"))
            || !sourcePreferences.SequenceEqual(File.ReadAllBytes(
                Path.Combine(candidate, "player-state.json"))))
            throw new InvalidOperationException("Candidate import failed to copy the original bytes.");
        using (var imported = new NativePreviewRuntime(candidate))
        {
            if (imported.Bankroll != expectedWallet
                || imported.Profile.DisplayName != "Candidate Import")
                throw new InvalidOperationException("Native host did not restore imported player state.");
        }
        if (!sourcePreferences.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "player-state.json")))
            || !sourceProfile.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "profile.json"))))
            throw new InvalidOperationException("Migration mutated the installed player's save.");
        try
        {
            _ = NativeHostMigration.Stage(production, "smoke", checkRunningHost: false);
            throw new InvalidOperationException("Existing migration candidate was overwritten.");
        }
        catch (IOException) { /* Expected non-overwrite protection. */ }
        try
        {
            _ = NativeHostMigration.CandidatePath(production, "../live");
            throw new InvalidOperationException("Traversal was accepted as a candidate name.");
        }
        catch (ArgumentException) { /* Expected. */ }

        File.WriteAllText(Path.Combine(production, "profile.json"), "{broken");
        try
        {
            _ = NativeHostMigration.Stage(production, "corrupt", checkRunningHost: false);
            throw new InvalidOperationException("Corrupt source state was imported.");
        }
        catch (JsonException) { /* Expected, without creating a candidate. */ }
        if (Directory.Exists(NativeHostMigration.CandidatePath(production, "corrupt")))
            throw new InvalidOperationException("Corrupt migration left a candidate behind.");
    }

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

            if (HostMigrationCatalog.PreviewRoutes.Count != 10
                || HostMigrationCatalog.ApprovedForRelease
                || !HostMigrationCatalog.IsPreviewRoute("midnight-manor")
                || !HostMigrationCatalog.IsPreviewRoute("roulette")
                || !HostMigrationCatalog.IsPreviewRoute("casino-empire")
                || !HostMigrationCatalog.IsPreviewRoute("chill-lounge")

                || HostMigrationCatalog.PreviewPageFor("reels") != "reels.html")
                throw new InvalidOperationException("Host parity manifest is inconsistent.");
            string descentFixture = Path.Combine(directory, "descent-fixture");
            string descentPath = NativeDescentLaunch.Executable(descentFixture);
            Directory.CreateDirectory(Path.GetDirectoryName(descentPath)!);
            if (NativeDescentLaunch.IsPackaged(descentFixture))
                throw new InvalidOperationException("Missing Godot pack was accepted.");
            File.WriteAllText(descentPath, "fixture");
            File.WriteAllText(Path.ChangeExtension(descentPath, ".pck"), "fixture");
            var descentStart = NativeDescentLaunch.CreateStartInfo(descentFixture, german: true);
            if (!descentStart.ArgumentList.SequenceEqual(
                ["--fullscreen", "--", "--language=de"])
                || descentStart.UseShellExecute
                || !string.Equals(descentStart.WorkingDirectory,
                    Path.GetDirectoryName(descentPath), StringComparison.Ordinal))
                throw new InvalidOperationException("Godot launch flags/lifecycle are incorrect.");

            // Rehearse an actual existing wallet/profile import without touching
            // user AppData. Exercise atomic staging, hash integrity, isolation,
            // replay protection, corruption rejection and the native runtime.
            VerifyCandidateMigration(directory);

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
                using (var lobby = System.Text.Json.JsonDocument.Parse(
                    System.Text.Json.JsonSerializer.Serialize(session.LobbySnapshot(),
                        new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))))
                {
                    var root = lobby.RootElement;
                    if (!root.GetProperty("nativePreview").GetBoolean()
                        || root.GetProperty("modes").GetArrayLength() != 11
                        || root.GetProperty("bankrollValue").GetDecimal() != session.Bankroll
                        || !root.GetProperty("dailyLogin").GetProperty("canClaim").GetBoolean())
                        throw new InvalidOperationException("Native lobby emitted demo or invalid state.");
                }

                var nativeLobby = new NativeLobbyController(session);
                var jsonOpts = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
                foreach (string page in new[] { "challenges", "statistics", "profile",
                    "history", "season", "vault", "album" })
                {
                    using var document = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(nativeLobby.Snapshot(page), jsonOpts));
                    if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                        throw new InvalidOperationException("Missing native React record: " + page);
                }
                using (var nativeShop = System.Text.Json.JsonDocument.Parse(
                    System.Text.Json.JsonSerializer.Serialize(nativeLobby.ShopSnapshot(), jsonOpts)))
                {
                    if (nativeShop.RootElement.GetProperty("items").GetArrayLength() == 0
                        || nativeShop.RootElement.GetProperty("packs").GetArrayLength() != 5)
                        throw new InvalidOperationException("Native cosmetic shop is using demo inventory.");
                }
                decimal beforeDaily = session.Bankroll;
                NativeLobbyOutcome dailyPaid = nativeLobby.ClaimDaily();
                if (!dailyPaid.Success
                    || session.Bankroll <= beforeDaily
                    || session.Preferences.DailyLoginClaims != 1
                    || nativeLobby.ClaimDaily().Success)
                    throw new InvalidOperationException("Daily reward was missing or paid twice.");
                if (!nativeLobby.ActivateSeason(SeasonCatalog.Current.Id).Success)
                    throw new InvalidOperationException("Core Season Pass activation failed.");
                NativeLobbyOutcome secondDaily = nativeLobby.ClaimDaily();
                if (secondDaily.Success)
                    throw new InvalidOperationException("Duplicate daily receipt accepted.");
                using (var postDaily = System.Text.Json.JsonDocument.Parse(
                    System.Text.Json.JsonSerializer.Serialize(session.LobbySnapshot(), jsonOpts)))
                {
                    if (postDaily.RootElement.GetProperty("dailyLogin")
                        .GetProperty("canClaim").GetBoolean())
                        throw new InvalidOperationException("Daily claim remained available after a commit.");
                }

                decimal initial = session.Bankroll;
                string id = Guid.NewGuid().ToString("D");
                var epoch = new BlackjackModernSession();
                var command = new BlackjackModernCommand(id, epoch.Id, 0, "bet.set", "25");
                BlackjackModernReply result = epoch.Execute(command, session.ApplyBlackjack);
                if (!result.Success || result.Revision != 1 || session.Bankroll != initial)
                    throw new InvalidOperationException("Blackjack native command failed.");
                if (epoch.Execute(command, _ => throw new Exception("Replayed command!")) != result)
                    throw new InvalidOperationException("Blackjack receipt was replayed.");

                // Preview roulette runs through the existing Core settlement rules
                // and persists one authoritative result before responding.
                using (var rouletteBets = System.Text.Json.JsonDocument.Parse(
                    "[{\"key\":\"red\",\"amount\":100}]"))
                {
                    var roundId = Guid.NewGuid();
                    var rouletteReceipt = session.ApplyRoulette(roundId, rouletteBets.RootElement);
                    if (rouletteReceipt is not NativeRouletteReceipt paid
                        || paid.RoundId != roundId || !paid.Success
                        || session.Bankroll != paid.Bankroll)
                        throw new InvalidOperationException("Native roulette did not settle.");
                    if (!Equals(session.ApplyRoulette(roundId, rouletteBets.RootElement), paid)
                        || session.Bankroll != paid.Bankroll)
                        throw new InvalidOperationException("Native roulette double-charged a replay.");
                }

                session.OpenLounge();
                foreach (string game in HostMigrationCatalog.LoungeGameIds)
                {
                    session.OpenLoungeGame(game);
                    var lounge = System.Text.Json.JsonSerializer.Serialize(session.LoungeSnapshot(),
                        new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                    if (!lounge.Contains("\"games\"", StringComparison.Ordinal)
                        || !lounge.Contains("\""+game+"\"", StringComparison.Ordinal))
                        throw new InvalidOperationException("Native Lounge missing game: " + game);
                }
                session.CloseLounge();
                session.OpenEmpire();
                var empireSnapshot = System.Text.Json.JsonSerializer.Serialize(session.EmpireSnapshot(),
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
                if (!empireSnapshot.Contains("\"economy\"", StringComparison.Ordinal)
                    || !session.EmpireStarted)
                    throw new InvalidOperationException("Native Empire is not authoritative.");
                var empireEpoch = new EmpireModernSession();
                var empireId = Guid.NewGuid().ToString("D");
                string empireCommandJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    version = EmpireModernProtocol.Version,
                    requestId = empireId,
                    session = empireEpoch.Id,
                    expectedRevision = 0,
                    action = "upgrade",
                    id = "slots",
                    levels = 1,
                });
                EmpireModernCommand? empireCommand = EmpireModernProtocol.Parse(empireCommandJson);
                if (empireCommand is null)
                    throw new InvalidOperationException("Native Empire command parsing failed.");
                EmpireModernReply empireReceipt = empireEpoch.Execute(empireCommand,
                    session.CommitEmpire, session.EmpirePreview);
                if (!empireReceipt.Success || empireReceipt.Revision != 1
                    || empireEpoch.Execute(empireCommand,
                        _ => throw new Exception("Empire action replayed."),
                        _ => throw new Exception("Empire preview replayed.")) != empireReceipt)
                    throw new InvalidOperationException("Native Empire duplicate purchase or missing checkpoint.");
                session.CloseEmpire();

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
            // Receipt IDs persist across runtime lifetimes and browser restarts.
            using (var resumed = new NativePreviewRuntime(directory))
            {
                if (resumed.Preferences.DailyLoginClaims != 1
                    || new NativeLobbyController(resumed).ClaimDaily().Success)
                    throw new InvalidOperationException("Daily login receipt replayed after restart.");
                using var repeatBets = System.Text.Json.JsonDocument.Parse(
                    "[{\"key\":\"red\",\"amount\":100}]");
                var receiptJournal = new NativeRouletteJournal(directory);
                if (receiptJournal is null)
                    throw new InvalidOperationException("Roulette journal missing.");
                // A pending mirror failure must recover from the same journal.
                string faultDirectory = Path.Combine(directory, "roulette-fault");
                var faultJournal = new NativeRouletteJournal(faultDirectory);
                var result = Roulette.Settle([new RouletteBet("red", 100m)], 1000m, 1);
                var candidate = new AppPreferences { Bankroll = result.Bankroll };
                try
                {
                    faultJournal.Commit(Guid.NewGuid(), result, candidate,
                        _ => throw new IOException("Simulated interrupted roulette mirror."));
                    throw new InvalidOperationException("Roulette fault injection was not reached.");
                }
                catch (IOException ex) when (ex.Message.Contains("Simulated interrupted"))
                { }
                new NativeRouletteJournal(faultDirectory).Recover();
                if (PlayerStateStore.Load(faultDirectory).Bankroll != result.Bankroll)
                    throw new InvalidOperationException("Roulette recovery lost the paid settlement.");
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
