using BlackjackCasino.HostShared;
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
        var round = new RoundHistoryEntry(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-30), DateTimeOffset.UtcNow,
            expectedWallet, expectedWallet, 10m, 0m, 0m, 0m,
            new GameRules(), [], [], [], []);
        File.WriteAllText(Path.Combine(production, "hand-history.json"),
            JsonSerializer.Serialize(new[] { round }));
        string browserData = Path.Combine(production, "WebView2", "Default",
            "Local Storage", "leveldb");
        Directory.CreateDirectory(browserData);
        // Chromium LevelDB is opaque to the migration; its bytes and filenames
        // must be transported unchanged while the legacy browser is stopped.
        File.WriteAllText(Path.Combine(browserData, "CURRENT"), "MANIFEST-000001\\n");
        File.WriteAllBytes(Path.Combine(browserData, "000001.ldb"), [0, 255, 0, 17, 61]);

        byte[] sourcePreferences = File.ReadAllBytes(Path.Combine(production, "player-state.json"));
        byte[] sourceProfile = File.ReadAllBytes(Path.Combine(production, "profile.json"));
        byte[] sourceHistory = File.ReadAllBytes(Path.Combine(production, "hand-history.json"));
        byte[] sourceBrowser = File.ReadAllBytes(Path.Combine(browserData, "000001.ldb"));
        // The installed WPF host and the migration process must be unable
        // to modify the same player directory concurrently. This is an OS
        // file-sharing lease, not a check-then-copy process-name heuristic.
        using (HostWriterLease.Acquire(production))
        {
            try
            {
                _ = NativeHostMigration.Stage(production, "blocked",
                    checkRunningHost: false);
                throw new InvalidOperationException("Migration ignored the active WPF writer.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already in use", StringComparison.Ordinal))
            {
                // Expected: the old host still owns the production directory.
            }
        }
        if (Directory.Exists(NativeHostMigration.CandidatePath(production, "blocked")))
            throw new InvalidOperationException("Blocked migration published an incomplete candidate.");
        string candidate = NativeHostMigration.Stage(production, "smoke", checkRunningHost: false);
        if (candidate != NativeHostMigration.OpenCandidate(production, "smoke"))
            throw new InvalidOperationException("Candidate manifest not recognized.");
        if (!File.Exists(Path.Combine(candidate, "native-migration-manifest.json"))
            || !File.Exists(Path.Combine(candidate, "hand-history.json"))
            || !sourceBrowser.SequenceEqual(File.ReadAllBytes(Path.Combine(candidate,
                "WebView2", "Default", "Local Storage", "leveldb", "000001.ldb")))
            || !sourcePreferences.SequenceEqual(File.ReadAllBytes(
                Path.Combine(candidate, "player-state.json"))))
            throw new InvalidOperationException("Candidate import failed to copy the original bytes.");
        using (var imported = new NativePreviewRuntime(candidate))
        {
            if (imported.Bankroll != expectedWallet
                || imported.Profile.DisplayName != "Candidate Import"
                || imported.Progression.History.Count != 1
                || imported.Progression.History[0].RoundId != round.RoundId)
                throw new InvalidOperationException("Native host did not restore imported player state.");

            // A second controller must not race the first one for the same
            // wallet, Manor/roulette checkpoints or browser profile.
            try
            {
                using var duplicate = new NativePreviewRuntime(candidate);
                throw new InvalidOperationException("Two simultaneous native writers were admitted.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already in use", StringComparison.Ordinal))
            {
                // Expected; the first runtime's lease remains held.
            }
        }
        using (var reopened = new NativePreviewRuntime(candidate))
        {
            if (reopened.Bankroll != expectedWallet)
                throw new InvalidOperationException("Native writer lease did not release on close.");
        }
        if (!sourcePreferences.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "player-state.json")))
            || !sourceProfile.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "profile.json")))
            || !sourceHistory.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "hand-history.json")))
            || !sourceBrowser.SequenceEqual(File.ReadAllBytes(
                Path.Combine(browserData, "000001.ldb"))))
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

        // An immutable, separate two-sided rollback point may be created only
        // while the original WPF state still matches the imported baseline.
        using (HostWriterLease.Acquire(production))
        {
            try
            {
                _ = NativeCutoverCheckpoint.Prepare(production, "smoke",
                    "blocked", checkProcesses: false);
                throw new InvalidOperationException("Cutover ignored the active WPF writer.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already in use", StringComparison.Ordinal))
            {
                // Expected: no checkpoint may be built while WPF is writing.
            }
        }
        if (Directory.Exists(NativeCutoverCheckpoint.CheckpointPath(production, "blocked")))
            throw new InvalidOperationException("Blocked cutover produced a checkpoint.");
        string backup = NativeCutoverCheckpoint.Prepare(production,
            "smoke", "cutover1", checkProcesses: false);
        CutoverCheckpoint report = NativeCutoverCheckpoint.Verify(production, "cutover1");
        if (report.Version != 1 || report.ProductionActivated
            || report.Native.Length == 0 || report.Legacy.Length == 0)
            throw new InvalidOperationException("Cutover evidence was incomplete or activated.");
        string restored = NativeCutoverCheckpoint.RestoreRehearsal(
            production, "cutover1", "legacy", Path.Combine(root, "rollback-rehearsal"));
        if (!sourcePreferences.SequenceEqual(File.ReadAllBytes(
                Path.Combine(restored, "player-state.json")))
            || !sourceProfile.SequenceEqual(File.ReadAllBytes(
                Path.Combine(restored, "profile.json")))
            || !sourceBrowser.SequenceEqual(File.ReadAllBytes(
                Path.Combine(restored, "WebView2", "Default", "Local Storage", "leveldb",
                    "000001.ldb"))))
            throw new InvalidOperationException("Rollback rehearsal did not preserve the original bytes.");
        try
        {
            _ = NativeCutoverCheckpoint.Prepare(production,
                "smoke", "cutover1", checkProcesses: false);
            throw new InvalidOperationException("Existing cutover backup was overwritten.");
        }
        catch (IOException) { /* Existing snapshots must never be overwritten. */ }
        try
        {
            _ = NativeCutoverCheckpoint.RestoreRehearsal(
                production, "cutover1", "legacy", restored);
            throw new InvalidOperationException("Existing rollback destination was overwritten.");
        }
        catch (IOException) { /* Existing restoration must never be overwritten. */ }

        // An altered installed WPF profile is a fork, not a safe single-writer
        // cutover: never combine it with transactions made in NativeShell.
        // The next handover step creates a separate writable native data root
        // from the immutable native backup. It must not replace WPF production,
        // mutate the original candidate, or ignore the current writer lease.
        using (HostWriterLease.Acquire(production))
        {
            try
            {
                _ = NativeCutoverPromotion.Stage(production, "cutover1",
                    "blocked-root", checkProcesses: false);
                throw new InvalidOperationException("Promotion ignored WPF writer ownership.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already in use", StringComparison.Ordinal)) { }
        }
        if (Directory.Exists(NativeCutoverPromotion.WorkspacePath(production, "blocked-root")))
            throw new InvalidOperationException("Blocked native handover published a workspace.");

        // Hold only the writer lease here. Disposing a real NativePreviewRuntime
        // persists a normalized save, intentionally changing the source hashes
        // after the checkpoint and invalidating this otherwise clean fixture.
        using (var candidateWriter = new FileStream(
            Path.Combine(candidate, ".native-host-writer.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            try
            {
                _ = NativeCutoverPromotion.Stage(production, "cutover1",
                    "blocked-candidate", checkProcesses: false);
                throw new InvalidOperationException("Promotion ignored the candidate writer lease.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("Close the NativeShell candidate", StringComparison.Ordinal)) { }
        }

        // The isolated native profile can be normalized when its original
        // controller closes. The rehearsal must copy the NATIVE checkpoint
        // bytes, while the older sourceProfile remains the immutable WPF baseline.
        byte[] nativeProfileAtCheckpoint = File.ReadAllBytes(
            Path.Combine(candidate, "profile.json"));
        string promoted = NativeCutoverPromotion.Stage(production, "cutover1",
            "isolated-live", checkProcesses: false);
        if (NativeCutoverPromotion.Open(production, "isolated-live") != promoted
            || !nativeProfileAtCheckpoint.SequenceEqual(File.ReadAllBytes(
                Path.Combine(promoted, "profile.json")))
            || !sourceBrowser.SequenceEqual(File.ReadAllBytes(Path.Combine(promoted,
                "WebView2", "Default", "Local Storage", "leveldb", "000001.ldb"))))
            throw new InvalidOperationException("Native rehearsal did not reconstruct the candidate state.");
        using (var active = new NativePreviewRuntime(promoted))
        {
            if (active.Bankroll != expectedWallet ||
                active.Progression.History.Count != 1)
                throw new InvalidOperationException("Rehearsal runtime failed to restore existing player data.");
        }
        // Simulate new rounds after cutover: they must be exported from the
        // native workspace, not replaced by the original WPF checkpoint.
        const decimal earned = 83.25m;
        decimal updatedWallet = checked(expectedWallet + earned);
        PlayerStateStore.SaveOrThrow(
            PlayerStateStore.Load(promoted) with { Bankroll = updatedWallet }, promoted);
        // Reusing the exact source profile is allowed; bankroll is the
        // authoritative changed player progress in this rollback fixture.
        using (var runningNative = new NativePreviewRuntime(promoted))
        {
            try
            {
                _ = NativeLegacyReturnExport.Stage(production,
                    "isolated-live", "blocked-native", checkProcesses: false);
                throw new InvalidOperationException("Legacy export ignored active native writer.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("Close the NativeShell workspace", StringComparison.Ordinal)) { }
        }
        if (Directory.Exists(NativeLegacyReturnExport.ExportPath(production, "blocked-native")))
            throw new InvalidOperationException("Legacy export ignored native writer lock.");

        string legacyExport = NativeLegacyReturnExport.Stage(production,
            "isolated-live", "return1", checkProcesses: false);
        LegacyReturnExportManifest legacyReport =
            NativeLegacyReturnExport.Verify(production, "return1");
        if (legacyReport.AppliedToProduction
            || legacyReport.LegacyBankroll != expectedWallet
            || legacyReport.NativeBankroll != updatedWallet
            || PlayerStateStore.Load(legacyExport).Bankroll != updatedWallet
            || !sourceBrowser.SequenceEqual(File.ReadAllBytes(Path.Combine(legacyExport,
                "WebView2", "Default", "Local Storage", "leveldb", "000001.ldb")))
            || File.Exists(Path.Combine(legacyExport, "native-promotion-rehearsal.json"))
            || File.Exists(Path.Combine(legacyExport, "native-player-aggregate.json"))
            || File.Exists(Path.Combine(legacyExport, "native-roulette-checkpoint.json")))
            throw new InvalidOperationException("WPF return did not preserve new native progress.");

        try
        {
            _ = NativeLegacyReturnExport.Stage(production,
                "isolated-live", "return1", checkProcesses: false);
            throw new InvalidOperationException("Existing legacy return export was overwritten.");
        }
        catch (IOException) { /* Never replace an earlier export. */ }
        try
        {
            _ = NativeLegacyReturnExport.Stage(production,
                "missing-workspace", "invalid-return", checkProcesses: false);
            throw new InvalidOperationException("Unverified native workspace was exported.");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException) { }
        // Crash-recovery contract: both atomic rename interruption points
        // restore the old WPF data without destroying newer native files.
        foreach (var failure in new[]
        {
            (Name: "failure-after-old", Fault: RollbackFault.AfterOldMove),
            (Name: "failure-after-new", Fault: RollbackFault.AfterNewMove),
        })
        {
            try
            {
                _ = NativeLegacyRollbackRehearsal.Run(production, "return1",
                    failure.Name, checkProcesses: false, fault: failure.Fault);
                throw new InvalidOperationException("Injected rollback failure was ignored.");
            }
            catch (IOException error) when (
                error.Message.Contains("Simulated crash", StringComparison.Ordinal)) { }

            if (failure.Fault == RollbackFault.AfterNewMove)
            {
                // A torn NEW save must not prevent the verified OLD WPF backup
                // from being recovered; preserve the damaged new bytes.
                string damaged = Path.Combine(NativeLegacyRollbackRehearsal.PathFor(
                    production, failure.Name), "current", "player-state.json");
                File.AppendAllText(damaged, "corrupted during outage");
            }
            _ = NativeLegacyRollbackRehearsal.Recover(production, failure.Name);
            RollbackRehearsalJournal journal =
                NativeLegacyRollbackRehearsal.Inspect(production, failure.Name);
            string recovered = Path.Combine(
                NativeLegacyRollbackRehearsal.PathFor(production, failure.Name),
                "current", "player-state.json");
            if (journal.State != "rolled-back"
                || !sourcePreferences.SequenceEqual(File.ReadAllBytes(recovered)))
                throw new InvalidOperationException("Interrupted return lost the original WPF wallet.");
        }
        string rehearsal = NativeLegacyRollbackRehearsal.Run(production,
            "return1", "completed-wpf-swap", checkProcesses: false);
        RollbackRehearsalJournal finished =
            NativeLegacyRollbackRehearsal.Inspect(production, "completed-wpf-swap");
        if (finished.State != "completed"
            || PlayerStateStore.Load(Path.Combine(rehearsal, "current")).Bankroll
                != updatedWallet
            || PlayerStateStore.Load(Path.Combine(rehearsal, "previous")).Bankroll
                != expectedWallet)
            throw new InvalidOperationException("WPF rehearsal did not preserve both wallet states.");

        // A previous WPF export without the durable roulette ledger must
        // still be blocked after the NativeShell processes a roulette round.
        string receiptFile = Path.Combine(promoted, "native-roulette-checkpoint.json");
        Guid nativeRoundId = Guid.NewGuid();
        var settledNativeReceipt = new NativeRouletteReceipt(
            nativeRoundId, true, 7, 100m, 0m, -100m, updatedWallet);
        File.WriteAllText(receiptFile, JsonSerializer.Serialize(
            new NativeRouletteCheckpoint(1, Pending: false,
                PlayerStateStore.Load(promoted), [settledNativeReceipt])));
        try
        {
            _ = NativeLegacyRollbackRehearsal.Run(production,
                "return1", "unbridged-receipts", checkProcesses: false);
            throw new InvalidOperationException("WPF export without roulette receipt ledger was accepted.");
        }
        catch (InvalidOperationException error) when (
            error.Message.Contains("receipt-ID continuity", StringComparison.Ordinal)) { }
        if (Directory.Exists(NativeLegacyRollbackRehearsal.PathFor(
                production, "unbridged-receipts")))
            throw new InvalidOperationException("Unsafe legacy export was published.");

        // A NEW export includes the original v1 roulette WAL byte-for-byte.
        // The WPF reader must recognize the same settlement ID after the
        // cutover and never treat it as an unpaid round.
        string bridgedExport = NativeLegacyReturnExport.Stage(
            production, "isolated-live", "return-with-roulette", checkProcesses: false);
        LegacyReturnExportManifest bridgedReport = NativeLegacyReturnExport.Verify(
            production, "return-with-roulette");
        if (!File.ReadAllBytes(receiptFile).SequenceEqual(File.ReadAllBytes(
                Path.Combine(bridgedExport, "native-roulette-checkpoint.json")))
            || bridgedReport.ExcludedNativeJournals.Contains(
                "native-roulette-checkpoint.json", StringComparer.OrdinalIgnoreCase)
            || NativeRouletteJournal.ValidateSettled(bridgedExport) != 1)
            throw new InvalidOperationException("Native roulette receipt IDs did not survive the WPF return.");
        string bridgedRehearsal = NativeLegacyRollbackRehearsal.Run(
            production, "return-with-roulette", "bridged-roulette", checkProcesses: false);
        _ = NativeLegacyRollbackRehearsal.Inspect(production, "bridged-roulette");
        string wpfSide = Path.Combine(bridgedRehearsal, "current");
        var wpfJournal = new NativeRouletteJournal(wpfSide);
        if (!wpfJournal.TryReceipt(nativeRoundId, out NativeRouletteReceipt? paid)
            || paid != settledNativeReceipt)
            throw new InvalidOperationException("WPF lost the imported native roulette receipt.");
        var repeat = Roulette.Settle([new RouletteBet("black", 100m)], updatedWallet, 0);
        if (wpfJournal.Commit(nativeRoundId, repeat,
                PlayerStateStore.Load(wpfSide)) != settledNativeReceipt
            || PlayerStateStore.Load(wpfSide).Bankroll != updatedWallet)
            throw new InvalidOperationException("WPF double-settled a native roulette round.");
        // This journal belongs ONLY to the disposable native rehearsal.
        File.Delete(receiptFile);

        File.AppendAllText(Path.Combine(legacyExport, "player-state.json"), " ");
        try
        {
            _ = NativeLegacyReturnExport.Verify(production, "return1");
            throw new InvalidOperationException("Tampered WPF return was accepted.");
        }
        catch (InvalidDataException) { /* Expected SHA-256 rejection. */ }

        if (!sourcePreferences.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "player-state.json")))
            || !sourceProfile.SequenceEqual(File.ReadAllBytes(
                Path.Combine(production, "profile.json"))))
            throw new InvalidOperationException("Rehearsal changed the legacy save.");
        try
        {
            _ = NativeCutoverPromotion.Stage(production, "cutover1",
                "isolated-live", checkProcesses: false);
            throw new InvalidOperationException("Existing handover workspace was overwritten.");
        }
        catch (IOException) { /* Rehearsal workspaces must be unique and immutable at creation. */ }

        File.WriteAllText(Path.Combine(production, "profile.json"), "{broken");
        try
        {
            _ = NativeCutoverPromotion.Stage(production, "cutover1",
                "divergent-live", checkProcesses: false);
            throw new InvalidOperationException("Divergent source was promoted.");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("changed since", StringComparison.Ordinal)) { }
        if (Directory.Exists(NativeCutoverPromotion.WorkspacePath(production, "divergent-live")))
            throw new InvalidOperationException("Divergent handover published an unsafe workspace.");
        try
        {
            _ = NativeCutoverCheckpoint.Prepare(production,
                "smoke", "divergent", checkProcesses: false);
            throw new InvalidOperationException("Divergent WPF progress was silently merged.");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("changed since", StringComparison.Ordinal))
        {
            // Expected: preserve both copies, require a new import.
        }
        if (Directory.Exists(NativeCutoverCheckpoint.CheckpointPath(production, "divergent")))
            throw new InvalidOperationException("Divergent checkpoint was published.");
        // No checksum mismatch can be silently accepted by the verified rollback.
        File.AppendAllText(Path.Combine(backup, "legacy", "player-state.json"), " ");
        try
        {
            _ = NativeCutoverCheckpoint.Verify(production, "cutover1");
            throw new InvalidOperationException("Tampered cutover backup passed verification.");
        }
        catch (InvalidDataException) { /* Expected SHA-256 rejection. */ }

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

            if (NativeHostAudio.TrackFor(null) != Soundtrack.Lobby
                || NativeHostAudio.TrackFor("midnight-dice") != Soundtrack.DiceLounge
                || NativeHostAudio.TrackFor("chill-lounge") != Soundtrack.ChillLounge
                || NativeHostAudio.TrackFor("midnight-manor") != Soundtrack.Manor)
                throw new InvalidOperationException("WPF-free native music routes are incomplete.");

            if (HostMigrationCatalog.PreviewRoutes.Count != 10
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
                // Every Lounge subgame must return to the shared selection
                // screen after an acknowledged exit, not remain in the
                // NativeShell snapshot and softlock subsequent navigation.
                foreach (var game in new[]
                {
                    (Id: "sudoku", Type: "lounge.sudoku.command", Prefix: "sudoku",
                        Session: "session", Revision: "revision", Action: "sudoku.exit"),
                    (Id: "solitaire", Type: "lounge.solitaire.command", Prefix: "solitaire",
                        Session: "solitaireSession", Revision: "solitaireRevision", Action: "solitaire.exit"),
                    (Id: "word-search", Type: "lounge.word-search.command", Prefix: "wordSearch",
                        Session: "wordSearchSession", Revision: "wordSearchRevision", Action: "word-search.exit"),
                    (Id: "memory", Type: "lounge.memory.command", Prefix: "memory",
                        Session: "memorySession", Revision: "memoryRevision", Action: "memory.exit"),
                    (Id: "jigsaw", Type: "lounge.jigsaw.command", Prefix: "jigsaw",
                        Session: "jigsawSession", Revision: "jigsawRevision", Action: "jigsaw.exit"),
                    (Id: "tales-backroom", Type: "lounge.adventure.command", Prefix: "adventure",
                        Session: "adventureSession", Revision: "adventureRevision", Action: "adventure.exit"),
                    (Id: "tower-defense", Type: "lounge.tower-defense.command", Prefix: "towerDefense",
                        Session: "towerDefenseSession", Revision: "towerDefenseRevision", Action: "tower.exit"),
                    (Id: "minesweeper", Type: "lounge.minesweeper.command", Prefix: "minesweeper",
                        Session: "minesweeperSession", Revision: "minesweeperRevision", Action: "mine.exit"),
                })
                {
                    session.OpenLoungeGame(game.Id);
                    using var snapshot = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(session.LoungeSnapshot(), jsonOpts));
                    var current = snapshot.RootElement;
                    if (current.GetProperty(game.Prefix).ValueKind != System.Text.Json.JsonValueKind.Object)
                        throw new InvalidOperationException("Native Lounge missing active game: " + game.Id);
                    string commandJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        type = game.Type,
                        version = 1,
                        requestId = Guid.NewGuid().ToString("D"),
                        session = current.GetProperty(game.Session).GetString(),
                        expectedRevision = current.GetProperty(game.Revision).GetInt64(),
                        action = game.Action,
                    });
                    NativeLoungeReply? reply = session.ApplyLounge(game.Type, commandJson);
                    if (reply is null)
                        throw new InvalidOperationException("Native Lounge exit was not handled: " + game.Id);
                    using var acknowledged = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(reply.Data, jsonOpts));
                    if (!acknowledged.RootElement.GetProperty("success").GetBoolean())
                        throw new InvalidOperationException("Native Lounge exit failed: " + game.Id);
                    using var restored = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(session.LoungeSnapshot(), jsonOpts));
                    if (restored.RootElement.GetProperty(game.Prefix).ValueKind
                        != System.Text.Json.JsonValueKind.Null)
                        throw new InvalidOperationException("Native Lounge never returned to menu: " + game.Id);
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
