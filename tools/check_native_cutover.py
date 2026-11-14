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
