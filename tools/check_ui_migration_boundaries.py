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

    modern_guard = lounge.find("if (!forceLegacy && nativeGameId is null && ModernLoungeView.IsAvailable)")
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
    if snapshot_start >= 0 and snapshot_end >= 0 and "LoungeHostedGameRegistry" in modern[snapshot_start:snapshot_end]:
        issues.append("React snapshot still depends on the native WPF game registry.")
    if "LoungeHostedGameRegistry.SetSessionModules(" in modern:
        issues.append("React game routes may not register WPF game factories.")
    routing_start = modern.find("private void OnModernLoungeGameSelected(")
    routing_end = modern.find("private ModernSudokuReply ApplyModernSudokuCommand(", routing_start)
    if routing_start < 0 or routing_end < 0:
        issues.append("Modern game-selection routing boundary was not found.")
    elif "OpenLoungeCore(" in modern[routing_start:routing_end]:
        issues.append("React gameplay selection may not silently reopen a WPF game.")
    if "OpenLoungeCore(null, forceLegacy: true)" not in modern:
        issues.append("WPF emergency recovery must remain available on WebView2 failure.")

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
    if backend != browser or backend != tiles or len(set(backend)) != 6:
        issues.append(f"Mismatch in six-game manifest: C#={backend}, bridge={browser}, tiles={tiles}")
    if "LoungeHostedGameRegistry.Snapshot.Count == 0" not in smoke:
        issues.append("Windows smoke must verify no legacy factories for React Lounge.")

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
        has_source = (ROOT / "src/BlackjackCasino.Desktop" / (name + ".cs")).exists() or (
            ROOT / "src/BlackjackCasino.Desktop" / (name + ".xaml")).exists()
        if not has_source:
            print(f"  {name:<34} REMOVED / NOT A TOP-LEVEL SOURCE FILE")
            continue
        pattern = re.compile(r"\b" + re.escape(name) + r"\b")
        references: list[str] = []
        for path, body in contents:
            # The class's own .cs/.xaml or partial files aren't external usage.
            if path.name == name + ".cs" or path.name == name + ".xaml" or path.name.startswith(name + "."):
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
