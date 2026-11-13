#!/usr/bin/env python3
"""Phase A host-migration invariants; no dependency on a .NET/Windows runner.

This checks correspondence to the current code, not complete parity. Release
approval still requires manual and runtime testing under Windows.
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def source(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8-sig")


def main() -> int:
    issues: list[str] = []
    catalog = source("src/BlackjackCasino.Core/Hosting/HostMigrationContracts.cs")
    lobby = source("src/BlackjackCasino.Desktop/MainWindow.Lobby.cs")
    lounge = source("src/BlackjackCasino.Desktop/ModernLoungeCatalog.cs")
    native = source("src/BlackjackCasino.NativeShell/NativeShellForm.cs")
    runtime = source("src/BlackjackCasino.NativeShell/NativePreviewRuntime.cs")
    desktop_project = source("src/BlackjackCasino.Desktop/BlackjackCasino.Desktop.csproj")
    native_project = source("src/BlackjackCasino.NativeShell/BlackjackCasino.NativeShell.csproj")

    # Canonical list is in the exact registration order of MainWindow.
    registrations = re.findall(r'modeRegistry\.Register\(new\("([^"]+)"', lobby)
    routes = re.findall(
        r'new\("([^"]+)", "[^"]+", (?:"([^"]+)"|null), (?:"([^"]+\.html)"|null)',
        catalog,
    )
    if not routes or [item[0] for item in routes] != registrations:
        issues.append("Host catalog must exactly match the real desktop lobby registration order.")
    preview = [(alias, page) for _, alias, page in routes if alias]
    expected = {
        "blackjack", "roulette", "reels", "neon-vault", "nine-lives",
        "casino-empire", "chill-lounge",
        "midnight-merge", "midnight-dice", "midnight-manor",
    }
    if len(preview) != 10 or {alias for alias, _ in preview} != expected:
        issues.append("Unexpected NativeShell preview route manifest.")
    for alias, document in preview:
        if not (ROOT / "frontend" / document).is_file():
            issues.append(f"Missing packaged local frontend document: {alias} -> {document}")
        if f'OpenModeAsync("{alias}")' not in native:
            issues.append(f"NativeShell does not expose documented preview route: {alias}")

    lounge_block = lounge.split("GameIds { get; } =", 1)[-1].split("];", 1)[0]
    actual_lounge = re.findall(r'"([a-z][a-z-]*)"', lounge_block)
    expected_lounge = catalog.split("LoungeGameIds { get; } =", 1)[-1].split("});", 1)[0]
    catalog_lounge = re.findall(r'"([a-z][a-z-]*)"', expected_lounge)
    if actual_lounge != catalog_lounge or len(catalog_lounge) != 8:
        issues.append("Host Lounge inventory differs from authoritative ModernLoungeCatalog.")

    if "HostMigrationCatalog.IsPreviewRoute(mode)" not in native or (
        "HostMigrationCatalog.PreviewPageFor(mode)" not in native
    ):
        issues.append("NativeShell must use the shared route allowlist and document mapping.")
    if "HostPreviewStorage.RequireIsolatedDirectory" not in runtime:
        issues.append("NativeShell must validate its preview storage path before writing.")
    if "<UseWindowsForms>true</UseWindowsForms>" not in native_project or (
        "<UseWPF>false</UseWPF>" not in native_project
    ):
        issues.append("NativeShell must remain WPF-free.")
    if "<UseWPF>true</UseWPF>" not in desktop_project:
        issues.append("Phase A must not replace the existing WPF production host.")

    for workflow in (".github/workflows/development-release.yml", ".github/workflows/release.yml"):
        text = source(workflow)
        if "dotnet publish src/BlackjackCasino.Desktop/BlackjackCasino.Desktop.csproj" not in text:
            issues.append(f"{workflow}: production target changed before parity acceptance.")
        # Native host migration rehearsals may be published alongside the
        # WPF release, but must be kept out of its updater package directory.
        if "dotnet publish src/BlackjackCasino.NativeShell/" in text:
            if workflow.endswith("release.yml") and not workflow.endswith("development-release.yml"):
                issues.append(f"{workflow}: stable release cannot publish the unapproved native host.")
            elif "--output artifacts/native-host-candidate" not in text:
                issues.append(f"{workflow}: native host candidate needs an isolated artifact output.")
            elif "artifacts/app/BlackjackCasino.Desktop.exe" not in text:
                issues.append(f"{workflow}: development updater must still validate WPF executable.")

    for workflow in (".github/workflows/ci.yml", ".github/workflows/repository-hygiene.yml"):
        if "tools/check_host_phase_a.py" not in source(workflow):
            issues.append(f"{workflow}: host parity guard is missing.")

    if issues:
        print("Phase A migration guard FAILED:")
        for issue in issues:
            print("  - " + issue)
        return 1
    print(f"Phase A guard OK: {len(routes)} desktop routes, {len(preview)} preview bridges, "
          f"{len(catalog_lounge)} Lounge games; production and preview isolated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
