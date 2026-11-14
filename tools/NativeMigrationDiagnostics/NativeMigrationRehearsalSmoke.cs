using BlackjackCasino.HostShared;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Hosting;
using System.Security.Cryptography;
using System.Text.Json;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Rehearses historical migration and WPF return contracts only on synthetic temporary data.
/// </summary>
internal static class NativeMigrationRehearsalSmoke
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
        using (var imported = new NativeCasinoRuntime(candidate))
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
                using var duplicate = new NativeCasinoRuntime(candidate);
                throw new InvalidOperationException("Two simultaneous native writers were admitted.");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already in use", StringComparison.Ordinal))
            {
                // Expected; the first runtime's lease remains held.
            }
        }
        using (var reopened = new NativeCasinoRuntime(candidate))
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

        // Hold only the writer lease here. Disposing a real NativeCasinoRuntime
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
        using (var active = new NativeCasinoRuntime(promoted))
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
        using (var runningNative = new NativeCasinoRuntime(promoted))
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
        string root = Path.Combine(Path.GetTempPath(),
            "blackjack-native-migration-diagnostics-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            VerifyCandidateMigration(root);
            Console.WriteLine("PASS: isolated migration, verified checkpoint, legacy export, torn-rollback and receipt replay.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
            catch (IOException) { /* Retain the isolated fixture for inspection when cleanup is blocked. */ }
        }
    }
}
