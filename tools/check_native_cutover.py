#!/usr/bin/env python3
"""Fail closed when WPF code, release targets, or unsafe host restart contracts reappear."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")

def main():
    problems = []
    def require(condition, message):
        if not condition: problems.append(message)

    require(not (ROOT / "src/BlackjackCasino.Desktop").exists(),
            "Old WPF desktop tree is still present")
    require(not (ROOT / "tools/StartupSmoke").exists()
            and not (ROOT / "tools/PresentationSmoke").exists()
            and not (ROOT / "tools/ManorSmoke").exists(),
            "WPF-specific runtime smoke projects remain")
    # C# API cleanup: old UI namespaces must not return.
    # Keep the compatibility executable name and on-disk save directory stable.
    for directory in ("src/BlackjackCasino.HostShared",
                      "src/BlackjackCasino.NativeShell",
                      "tests/BlackjackCasino.Tests", "tools/AudioRender"):
        for file in (ROOT / directory).rglob("*.cs"):
            source = file.read_text(encoding="utf-8-sig")
            require(not re.search(
                r"\b(?:namespace|using(?:\s+static)?)\s+BlackjackCasino\.Desktop\b",
                source),
                f"{file.relative_to(ROOT)}: retired Desktop C# namespace")
    require("HostReleaseRequirement" not in read(
        "src/BlackjackCasino.Core/Hosting/HostMigrationContracts.cs"),
        "Obsolete all-false preview release approvals must not be compiled")
    for project in ROOT.rglob("*.csproj"):
        content = project.read_text(encoding="utf-8")
        require("<UseWPF>true</UseWPF>" not in content,
                f"{project.relative_to(ROOT)}: WPF must not be enabled")
    for folder in ("src/BlackjackCasino.Core", "src/BlackjackCasino.HostShared",
                   "src/BlackjackCasino.NativeShell", "src/BlackjackCasino.CompatLauncher"):
        for file in (ROOT / folder).rglob("*.cs"):
            source = file.read_text(encoding="utf-8-sig")
            require(not re.search(r"^\s*(?:global\s+)?using\s+System\.Windows\s*;", source,
                                  flags=re.MULTILINE),
                    f"{file.relative_to(ROOT)}: WPF namespace import")
            require("Microsoft.Web.WebView2.Wpf" not in source,
                    f"{file.relative_to(ROOT)}: WPF WebView2 control")
    for name in ("NativeCutoverCheckpoint.cs", "NativeCutoverPromotion.cs",
                 "NativeLegacyReturnExport.cs", "NativeLegacyRollbackRehearsal.cs"):
        require(not (ROOT / "src/BlackjackCasino.NativeShell" / name).exists(),
                f"Historical tool still compiled into production: {name}")
        require((ROOT / "tools/NativeMigrationDiagnostics/Rehearsal" / name).exists(),
                f"Historical diagnostic fixture missing: {name}")
    source = read("src/BlackjackCasino.NativeShell/Program.cs")
    require('if (args is ["--stage-migration"' not in source,
            "Historical command handlers still exist in production startup")
    require('if (args is ["--web-smoke"])' in source and
            'if (args is ["--smoke"])' in source,
            "Normal NativeShell acceptance commands must remain")
    require((ROOT / "src/BlackjackCasino.NativeShell/NativeCasinoRuntime.cs").exists()
            and not (ROOT / "src/BlackjackCasino.NativeShell/NativePreviewRuntime.cs").exists(),
            "Production runtime must use the native class name, not the pilot class")
    require((ROOT / "src/BlackjackCasino.HostShared/CasinoUpdateService.cs").exists()
            and not (ROOT / "src/BlackjackCasino.HostShared/DesktopUpdateService.cs").exists(),
            "Update transport must be host-neutral")
    require("InternalsVisibleTo" in read(
        "src/BlackjackCasino.NativeShell/AssemblyInfo.cs"),
        "Diagnostic tool must use explicit friend assembly access")
    native = read("src/BlackjackCasino.NativeShell/BlackjackCasino.NativeShell.csproj")
    require("<UseWPF>false</UseWPF>" in native and "<UseWindowsForms>true</UseWindowsForms>" in native,
            "Release NativeShell must use WinForms/WebView2 without WPF")
    require("BlackjackCasino.HostShared/" in native and
            "BlackjackCasino.Desktop/" not in native, "NativeShell depends on old desktop sources")
    for source in ("BlackjackCasino.sln", ".github/workflows/ci.yml",
                   ".github/workflows/development-release.yml", ".github/workflows/release.yml"):
        content = read(source)
        require("src/BlackjackCasino.Desktop/BlackjackCasino.Desktop.csproj" not in content,
                f"{source}: old WPF host publication/reference")
    launcher = read("src/BlackjackCasino.HostShared/UpdaterLauncher.cs")
    require('startInfo.ArgumentList.Add("BlackjackCasino.NativeShell.exe")' in launcher,
            "Updater must restart the actual NativeShell executable")
    for workflow in (".github/workflows/development-release.yml", ".github/workflows/release.yml"):
        content = read(workflow)
        require("dotnet publish src/BlackjackCasino.NativeShell/" in content,
                f"{workflow}: NativeShell missing from release")
        require("dotnet publish src/BlackjackCasino.CompatLauncher/" in content,
                f"{workflow}: old updater compatibility launcher missing")
        require("--web-smoke" in content and "--smoke" in content,
                f"{workflow}: native receipt/browser acceptance missing")
    require((ROOT / "src/BlackjackCasino.Assets/midnight-club.ico").exists(),
            "Release application icon is missing")
    require(len(list((ROOT / "src/BlackjackCasino.Assets/CollectionArt").glob("*.png"))) == 16,
            "Expected 16 original album atlases")
    if problems:
        for problem in problems: print("FAIL:", problem)
        raise SystemExit(1)
    print("PASS: WPF-free build, NativeShell release and legacy updater compatibility contracts")

if __name__ == "__main__":
    main()
