#!/usr/bin/env python3
"""Audit the WPF-to-React Lounge boundary and report legacy UI removal candidates.

This check is deliberately conservative: code still required by the emergency WPF
fallback, legacy smoke tests, crash recovery or XAML resources is not called dead.
It does not delete or rewrite files. Run: python tools/check_ui_migration_boundaries.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
L = ROOT / "src/BlackjackCasino.Desktop/MainWindow.Lounge.cs"
M = ROOT / "src/BlackjackCasino.Desktop/MainWindow.ModernLounge.cs"
C = ROOT / "src/BlackjackCasino.Desktop/ModernLoungeCatalog.cs"
TS = ROOT / "frontend/src/lounge/bridge.ts"
UI = ROOT / "frontend/src/lounge/main.tsx"
SMOKE = ROOT / "tools/StartupSmoke/ModernLoungeSmoke.cs"

# These candidates are reports, not permission to delete the class:
# no references does not establish absence of reflection, resource bindings or
# separately published WPF rollback binaries.
CANDIDATES = (
    "StartupWelcomeWindow", "CrispViewboxText",
    "CasinoUxEnhancements", "PremiumConfirmationWindow",
    "PlayerProfileWindow", "ProgressionWindow",
    "CollectionThemeRewardWindow", "CollectionThemeRewardPresenter",
    "KniffelSetupView", "KniffelMatchResultView",
    "SlotMachineWindow", "ManorWindow", "EmpireView",
    "SudokuView", "SolitaireView", "JigsawPlayView",
    "AdventureCaseBoardView", "MemoryLoungeModuleVisual",
    "WordSearchLoungeModule", "LoungeWindow",
)


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def main() -> int:
    issues: list[str] = []
    missing = [p for p in (L, M, C, TS, UI, SMOKE) if not p.is_file()]
    if missing:
        for path in missing:
            issues.append(f"Missing migration boundary file: {path.relative_to(ROOT)}")
        print("\n".join(issues))
        return 1

    lounge, modern, catalog = text(L), text(M), text(C)
    bridge, frontend, smoke = text(TS), text(UI), text(SMOKE)

    modern_guard = lounge.find("if (!forceLegacy && nativeGameId is null)")
    fallback_def = lounge.find("private void OpenLegacyLounge(")
    native_register = lounge.find("LoungeHostedGameRegistry.SetSessionModules(")
    native_styles = lounge.find("SudokuLoungePolish.EnsureInstalled(")
    if -1 in (modern_guard, fallback_def, native_register, native_styles):
        issues.append("Missing explicit React/legacy route or WPF module bootstrap.")
    elif not modern_guard < fallback_def < min(native_register, native_styles):
        issues.append("WPF factories/styles are initialized before the modern Lounge route.")

    snapshot_start = modern.find("private object CreateModernLoungeSnapshot()")
    snapshot_end = modern.find("private string CurrentModernSolitaireStatus()", snapshot_start)
    if snapshot_start < 0 or snapshot_end < 0:
        issues.append("React Lounge snapshot boundary was not found.")
    elif "ModernLoungeCatalog.Create(" not in modern[snapshot_start:snapshot_end]:
        issues.append("React snapshot must be created from its independent modern catalog.")
    if "LoungeHostedGameRegistry" in modern:
        issues.append("React host must not reference native WPF game registration, even on close.")
    routing_start = modern.find("private void OnModernLoungeGameSelected(")
    routing_end = modern.find("private ModernSudokuReply ApplyModernSudokuCommand(", routing_start)
    if routing_start < 0 or routing_end < 0:
        issues.append("Modern game-selection routing boundary was not found.")
    elif "OpenLoungeCore(" in modern[routing_start:routing_end]:
        issues.append("React gameplay selection may not silently reopen a WPF game.")
    recovery_host = ROOT / "src/BlackjackCasino.Desktop/MainWindow.BrowserRecovery.cs"
    recovery_view = ROOT / "src/BlackjackCasino.Desktop/BrowserRecoveryView.cs"
    recovery_logic = ROOT / "src/BlackjackCasino.Desktop/BrowserRecoverySession.cs"
    recovery_smoke = ROOT / "tools/StartupSmoke/BrowserRecoverySmoke.cs"
    if not all(p.is_file() for p in (recovery_host, recovery_view, recovery_logic, recovery_smoke)):
        issues.append("Independent Lounge recovery UI, policy or Windows smoke is missing.")
    else:
        host = text(recovery_host)
        view = text(recovery_view)
        if "OpenLoungeCore(null, forceLegacy: true)" not in host:
            issues.append("Explicit native Lounge recovery must remain available.")
        if "MountModernSurface(view)" not in host or "UnmountModernSurface(view)" not in host:
            issues.append("Recovery UI bypasses the exclusive modern desktop surface.")
        if "LoungeWindow" in view or "LoungeHostedGameRegistry" in view:
            issues.append("Recovery UI must not instantiate native WPF game factories.")
        if "BrowserRecoverySmoke.Run();" not in text(ROOT / "tools/StartupSmoke/Program.cs"):
            issues.append("Windows recovery screen lifecycle smoke is missing.")
    failure_start = modern.find("private void OnModernLoungeFailed()")
    failure_end = modern.find("private void OpenModernCaseBoardWindow()", failure_start)
    if failure_start < 0 or failure_end < 0 or (
            "OpenLoungeCore(" in modern[failure_start:failure_end]):
        issues.append("React browser failure must not silently launch WPF games.")
    if "ShowLoungeBrowserRecovery(assetsAvailable: false)" not in lounge:
        issues.append("Missing browser assets must route to explicit recovery choices.")

    # Generic recovery must service all six primary React casino routes; no
    # non-Lounge game may silently fall back to WPF, discard a pending wager,
    # or replace the browser with a modal error-only dead end.
    casino_recovery = (
        ("MainWindow.ModernBlackjack.cs", "OnModernBlackjackFailed"),
        ("MainWindow.ModernDice.cs", "OnModernDiceFailed"),
        ("MainWindow.ModernReels.cs", "OnModernReelsFailed"),
        ("MainWindow.ModernNeonVault.cs", "OnModernNeonVaultFailed"),
        ("MainWindow.ModernNineLives.cs", "OnModernNineLivesFailed"),
        ("MainWindow.MidnightMerge.cs", "OnModernMergeFailed"),
    )
    for file_name, failure_method in casino_recovery:
        source = text(ROOT / "src/BlackjackCasino.Desktop" / file_name)
        if source.count("ShowBrowserRecovery(") < 2:
            issues.append(f"{file_name}: missing frontend or browser crash bypasses recovery.")
        start = source.find(f"private void {failure_method}()")
        stop = source.find("private void ", start + 1)
        failure_body = source[start:stop if stop >= 0 else None]
        if start < 0 or "ShowBrowserRecovery(" not in failure_body:
            issues.append(f"{file_name}: browser crash does not offer recovery choices.")
        if file_name == "MainWindow.ModernReels.cs":
            close_start = source.find("private void CloseModernReels()")
            close_body = source[close_start:]
            # The explicit recovery callback legitimately references the old slot.
            # Only launching it from CloseModernReels itself is a silent fallback.
            if "EmbeddedSlots_Click(" in close_body:
                issues.append("Midnight Reels silently launches native WPF slots on crash.")
            if "ShowBrowserRecovery(" not in failure_body or (
                    "EmbeddedSlots_Click(" not in failure_body):
                issues.append("Midnight Reels lost its explicit native recovery option.")
    if recovery_host.is_file():
        host = text(recovery_host)
        policy = text(recovery_logic)
        if "openLegacyGames is not null" not in host:
            issues.append("Generic browser recovery must gate legacy actions by availability.")
        if "legacyGamesAvailable" not in policy:
            issues.append("Browser recovery policy must reject unavailable native games.")

    def ids(source: str, marker: str, end: str) -> list[str]:
        start = source.find(marker)
        stop = source.find(end, start + len(marker))
        if start < 0 or stop < 0:
            return []
        return re.findall(r'["\x27]([a-z]+(?:-[a-z]+)*)["\x27]', source[start + len(marker):stop])

    backend = ids(catalog, "GameIds { get; } =", "];")
    browser = ids(bridge, "export const loungeIds = [", "] as const;")
    tiles = re.findall(r"\bid:\s*'([a-z]+(?:-[a-z]+)*)'", frontend[
        frontend.find("const catalog: GameInfo[] = ["):frontend.find("const words = {")
    ])
    if backend != browser or backend != tiles or len(set(backend)) != 8:
        issues.append(f"Mismatch in eight-game manifest: C#={backend}, bridge={browser}, tiles={tiles}")
    if "LoungeHostedGameRegistry.Snapshot.Count == 0" not in smoke:
        issues.append("Windows smoke must verify no legacy factories for React Lounge.")

    # The separate WinForms candidate is compiled and smoke-tested by Windows CI.
    # It must not smuggle XAML/PresentationFramework into its project or become
    # the production release target before the parity matrix has been accepted.
    native_shell_project = ROOT / "src/BlackjackCasino.NativeShell/BlackjackCasino.NativeShell.csproj"
    native_shell_form = ROOT / "src/BlackjackCasino.NativeShell/NativeShellForm.cs"
    native_shell_doc = ROOT / "docs/native-shell-migration.md"
    if not all(path.is_file() for path in (native_shell_project, native_shell_form, native_shell_doc)):
        issues.append("WPF-free native shell candidate and parity documentation are missing.")
    else:
        candidate = text(native_shell_project)
        form = text(native_shell_form)
        if "<UseWPF>false</UseWPF>" not in candidate or "<UseWindowsForms>true</UseWindowsForms>" not in candidate:
            issues.append("Independent shell must be WinForms/WebView2 without WPF.")
        if any(token in form for token in ("System.Windows.Controls", "System.Windows.Threading", "PresentationFramework")):
            issues.append("New native shell reintroduced WPF types.")
        if any(page not in form for page in ("blackjack.html", "reels.html", "neon-vault.html", "nine-lives.html", "midnight-merge.html", "midnight-dice.html", "index.html")):
            issues.append("Native shell lost a validated seven-game browser route.")
        if "BlackjackCasino.NativeShell/BlackjackCasino.NativeShell.csproj" not in text(
                ROOT / ".github/workflows/ci.yml"):
            issues.append("Windows CI no longer exercises the WPF-free host.")
        if "--web-smoke" not in text(ROOT / ".github/workflows/ci.yml"):
            issues.append("Real independent WPF-free WebView2 startup smoke is missing.")
        release = text(ROOT / ".github/workflows/development-release.yml")
        if "dotnet publish src/BlackjackCasino.NativeShell" in release:
            issues.append("Experimental native shell has been mistakenly switched into Development updates.")

    # Casino navigation descriptors must be reusable by a non-WPF native shell.
    registry_path = ROOT / "src/BlackjackCasino.Desktop/CasinoModeRegistry.cs"
    fallback_art = ROOT / "src/BlackjackCasino.Desktop/NativeLobbyArtwork.cs"
    native_lobby = ROOT / "src/BlackjackCasino.Desktop/CasinoLobbyView.cs"
    if not all(p.is_file() for p in (registry_path, fallback_art, native_lobby)):
        issues.append("WPF-free game routing registry or native artwork adapter is missing.")
    else:
        registry_source = text(registry_path)
        if "System.Windows" in registry_source or "FrameworkElement" in registry_source:
            issues.append("CasinoModeRegistry must not depend on WPF view types.")
        if "NativeLobbyArtwork.Create(mode.Id)" not in text(native_lobby):
            issues.append("Emergency WPF lobby no longer owns its artwork factories.")
        if "new LobbyGameArtwork" in text(ROOT / "src/BlackjackCasino.Desktop/MainWindow.Lobby.cs"):
            issues.append("Casino mode registration reintroduced WPF artwork factories.")

    # Phase 5: the interim WPF shell must have one presentation owner instead
    # of seven independent browser overlays / input-disabling implementations.
    surface_path = ROOT / "src/BlackjackCasino.Desktop/ModernGameSurface.cs"
    embedded_path = ROOT / "src/BlackjackCasino.Desktop/MainWindow.EmbeddedGames.cs"
    surface_smoke = ROOT / "tools/StartupSmoke/ModernGameSurfaceSmoke.cs"
    if not all(p.is_file() for p in (surface_path, embedded_path, surface_smoke)):
        issues.append("Shared modern desktop surface or Windows lifecycle smoke is missing.")
    else:
        surface_source = text(surface_path)
        embedded_source = text(embedded_path)
        if not all(token in surface_source for token in (
            "internal void Mount(UIElement view)",
            "internal void Unmount(UIElement view)",
            "content.Content is not null",
            "ReferenceEquals(active, view)",
            "element.SetCurrentValue(UIElement.IsEnabledProperty, enabled)",
        )):
            issues.append("Modern surface lost its single-page ownership or input restoration guard.")
        if "modernGameSurface!.Mount(view)" not in embedded_source or (
                "modernGameSurface.Unmount(view)" not in embedded_source):
            issues.append("The desktop host does not delegate React mounting to the shared surface.")
        if "ModernGameSurfaceSmoke.Run()" not in text(ROOT / "tools/StartupSmoke/Program.cs"):
            issues.append("Windows startup must exercise the shared modern surface lifecycle.")

    routes = (
        "MainWindow.ModernBlackjack.cs", "MainWindow.ModernDice.cs",
        "MainWindow.ModernLounge.cs", "MainWindow.MidnightMerge.cs",
        "MainWindow.ModernNeonVault.cs", "MainWindow.ModernNineLives.cs",
        "MainWindow.ModernReels.cs",
    )
    for filename in routes:
        source = text(ROOT / "src/BlackjackCasino.Desktop" / filename)
        if "MountModernSurface(" not in source or "UnmountModernSurface(" not in source:
            issues.append(f"{filename}: React host lifecycle bypasses the shared surface.")
        if "embeddedGameHost!.Content =" in source or (
                "embeddedGameOverlay!.Visibility = Visibility.Visible" in source):
            issues.append(f"{filename}: React route still manipulates the old overlay directly.")
        if "CoveredElements.Add(" in source or "CoveredElements.Clear()" in source:
            issues.append(f"{filename}: duplicate native-input coverage has returned.")

    # Phase 5: all current React WebView2 hosts must apply exactly one
    # shared offline-browser setup, while keeping each page's trusted URI
    # predicate and independent failure/message callbacks.
    webview_views = (
        "ModernLobbyView.cs", "ModernLoungeView.cs",
        "ModernBlackjackView.cs", "ModernDiceView.cs",
        "ModernReelsView.cs", "ModernNineLivesView.cs",
        "ModernNeonVaultView.cs", "ModernMergeView.cs",
        "ModernEmpireView.cs", "ModernManorPreviewWindow.cs",
    )
    webview_policy = ROOT / "src/BlackjackCasino.Desktop/ModernWebViewBootstrap.cs"
    if not webview_policy.is_file():
        issues.append("Shared offline WebView2 bootstrap was not found.")
    else:
        policy = text(webview_policy)
        for token in (
            "EnsureCoreWebView2Async(environment)", "CoreWebView2HostResourceAccessKind.DenyCors",
            "CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder)",
            "Environment.SpecialFolder.LocalApplicationData",
            "AreDevToolsEnabled = false", "AreHostObjectsAllowed = false",
            "AreDefaultContextMenusEnabled = false",
            "IsWebMessageEnabled = true", "NewWindowRequested",
            "PermissionRequested", "NavigationStarting", "trustedPage(args.Uri)",
            "if (isClosed()) return null;",
        ):
            if token not in policy:
                issues.append("Shared WebView2 security policy lost: " + token)
    for name in webview_views:
        src = text(ROOT / "src/BlackjackCasino.Desktop" / name)
        if src.count("ModernWebViewBootstrap.InitializeAsync(") != 1:
            issues.append(f"{name}: the shared WebView2 startup must be invoked exactly once.")
        if "SetVirtualHostNameToFolderMapping(" in src or "EnsureCoreWebView2Async(" in src:
            issues.append(f"{name}: duplicate WebView2 initialization or host mapping remains.")
        if "core.WebMessageReceived" not in src or "core.NavigationCompleted" not in src:
            issues.append(f"{name}: native message bridge or navigation failure handling is missing.")
    if "ModernWebViewBootstrapSmoke.RequireHardened(" not in text(
            ROOT / "tools/StartupSmoke/Program.cs"):
        issues.append("Real Windows lobby security smoke is missing.")
    if "ModernWebViewBootstrapSmoke.RequireHardened(" not in smoke:
        issues.append("Real Windows Lounge security smoke is missing.")

    print("React/WPF Lounge migration boundary:")
    print("  C# catalog:", ", ".join(backend))
    print("  Normal React snapshot independent of WPF factories:", "yes" if not issues else "review")
    print("  Crash-safe native fallback retained:", "yes")
    print("\nLegacy WPF UI reference inventory (conservative, source-level):")

    roots = (ROOT / "src", ROOT / "tests", ROOT / "tools")
    source_paths: list[Path] = []
    for root in roots:
        source_paths.extend(p for p in root.rglob("*")
            if p.is_file() and p.suffix.lower() in (".cs", ".xaml")
            and not any(part in {"obj", "bin", "artifacts", "TestResults"} for part in p.parts))
    contents = [(p, text(p)) for p in source_paths]

    for name in CANDIDATES:
        declaration = re.compile(r"\b(?:class|struct|record)\s+" + re.escape(name) + r"\b")
        owning = {p for p, body in contents if declaration.search(body)}
        if not owning:
            print(f"  {name:<34} NO COMPILED TYPE DECLARATION")
            continue
        pattern = re.compile(r"\b" + re.escape(name) + r"\b")
        references: list[str] = []
        for path, body in contents:
            # A class's own declaration and partial implementation are not usage.
            if path in owning or path.name == name + ".xaml" or path.name.startswith(name + "."):
                continue
            if pattern.search(body):
                references.append(str(path.relative_to(ROOT)).replace("\\", "/"))
        native = [p for p in references if p.startswith("src/")]
        tests = [p for p in references if not p.startswith("src/")]
        label = "REVIEW/REFERENCED" if references else "POTENTIALLY ORPHANED"
        print(f"  {name:<34} native={len(native):>2} tests={len(tests):>2} {label}")
        if not references:
            print("    no direct references; inspect WPF/XAML and runtime reflection before removal")
        elif len(references) <= 4:
            print("    " + ", ".join(references))

    if issues:
        for issue in issues:
            print("ERROR:", issue, file=sys.stderr)
        return 1
    print("\nPASS: browser and emergency WPF routes are structurally isolated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
