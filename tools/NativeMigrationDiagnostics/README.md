# Offline historical migration diagnostics

This development-only project preserves the original, unchanged four cutover,
rollback and WPF return implementations outside the published NativeShell.
It is built by the Windows solution and **never packaged into the casino**.

Run the synthetic, temporary-data regression with:

```powershell
dotnet run --project tools/NativeMigrationDiagnostics -c Release -- --rehearsal-smoke
```

The other explicit commands are preserved from the original executable:
`--stage-migration`, `--candidate`, `--prepare-cutover`,
`--verify-cutover`, `--rehearse-rollback`, `--stage-live-rehearsal`,
`--live-rehearsal`, `--stage-legacy-return`, `--verify-legacy-return`,
`--rehearse-wpf-return`, and `--recover-wpf-rehearsal`.

**Safety:** These commands can read existing local player data. They are for
explicit manual maintenance only. Never run them on an active installation or
against a writable shared profile; do not publish this diagnostic executable.
Routine production startup, first-time verified import, journaling, updates,
and normal save paths remain solely in `BlackjackCasino.NativeShell`.
