#!/usr/bin/env python3
"""Audit German UI localization coverage; gate the migrated Phase-1 shell.

Parity alone cannot detect English strings that were never added to the resx
catalogs. This checker covers literal WPF surfaces and inventories legacy debt.
Do not treat names, save IDs, glyphs or resource expressions as translated copy.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "src" / "BlackjackCasino.Desktop" / "Resources"
DESKTOP = ROOT / "src" / "BlackjackCasino.Desktop"
CORE = ROOT / "src" / "BlackjackCasino.Core"

PHASE1_XAML = {
    "MainWindow.xaml",
    "Presentation/CasinoWindow.xaml",
}
PHASE2_XAML = {
    "KniffelView.xaml",
    "SlotMachineWindow.xaml",
    "ManorWindow.xaml",
}
PHASE2_CODE_AUDIT = {
    "KniffelSetupView.cs", "KniffelView.xaml.cs", "KniffelMatchResultView.cs",
    "SlotMachineWindow.xaml.cs", "ManorWindow.xaml.cs",
    "MemoryLoungeModule.cs", "MemoryLoungeVisualModule.cs",
    "SolitaireLoungeModule.cs", "WordSearchLoungeModule.cs",
    "SudokuPremiumMenu.cs", "EmpireView.cs", "EmpireManagementView.cs",
    "EmpireManagementView.Research.cs", "EmpireView.Intelligence.cs",
}
PHASE1_CODE = {
    "StartupWelcomeWindow.cs",
    "PlayerProfileWindow.cs",
    "MainWindow.TableRules.cs",
    "CasinoUxEnhancements.cs",
}
# Product identity is deliberately shared by both language versions.
BRANDING = {"Blackjack Casino", "THE MIDNIGHT CLUB", "BLACKJACK CASINO"}
DISPLAY_ATTRIBUTES = {"Text", "Content", "ToolTip", "Header", "Title"}
CODE_DISPLAY_STRING = re.compile(
    r'\b(?:Text|Content|ToolTip|Header|Title)\s*=\s*"((?:\\.|[^"\\])*)"'
)
CHALLENGE = re.compile(
    r'\b[CE]\s*\(\s*"([^"]+)"\s*,\s*ChallengeCategory\.\w+'
    r'\s*,\s*"[^"]+"\s*,\s*"[^"]+"'
)
STORY_SCENE = {
    "black-stag": re.compile(r'\b(?:Scene|Ending)\s*\(\s*"([^"]+)"\s*,\s*"[^"]+"\s*,\s*"'),
    "drowned-bell": re.compile(r'\bS\s*\(\s*"([^"]+)"\s*,\s*"[^"]+"\s*,\s*"'),
}


def resource_values() -> tuple[set[str], set[str]]:
    values: set[str] = set()
    keys: set[str] = set()
    for path in sorted(RESOURCES.glob("*.resx")):
        if path.stem.endswith(".de"):
            continue
        for node in ET.parse(path).getroot().findall("data"):
            key = node.attrib.get("name", "")
            value = node.findtext("value", default="").strip()
            keys.add(key)
            if value:
                values.add(value)
    return values, keys


def is_display_copy(value: str) -> bool:
    value = value.strip()
    if not value or value.startswith("{") or value in BRANDING:
        return False
    if value.startswith("http:") or value.startswith("https:"):
        return False
    # Numbers, keyboard glyphs and identifiers are not English prose.
    return bool(re.search(r"[A-Za-z]{3,}", value))


def xaml_missing(path: Path, values: set[str]) -> list[dict[str, str]]:
    result = []
    for element in ET.parse(path).getroot().iter():
        for attr, value in element.attrib.items():
            local = attr.rsplit("}", 1)[-1]
            if local in DISPLAY_ATTRIBUTES and is_display_copy(value) and value.strip() not in values:
                result.append({"attribute": local, "text": value.strip()[:180]})
    return result


def source_missing(path: Path, values: set[str]) -> list[dict[str, str | int]]:
    result = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if line.lstrip().startswith(("//", "*", "/*")):
            continue
        for match in CODE_DISPLAY_STRING.finditer(line):
            value = match.group(1).replace(r'\"', '"').strip()
            if is_display_copy(value) and value not in values:
                result.append({"line": number, "text": value[:180]})
    return result


def story_debt(keys: set[str]) -> dict[str, dict[str, int]]:
    result = {}
    for campaign, pattern in STORY_SCENE.items():
        file = CORE / "Adventure" / (
            "BlackStagAdventure.cs" if campaign == "black-stag" else "DrownedBellAdventure.cs"
        )
        identifiers = set(pattern.findall(file.read_text(encoding="utf-8")))
        known = sum(f"Adventure.{campaign}.Scene.{name}.Body" in keys for name in identifiers)
        result[campaign] = {"scenes": len(identifiers), "bodyTranslations": known,
                            "missingBodies": len(identifiers) - known}
    return result


def audit(strict: bool) -> int:
    values, keys = resource_values()
    shell_gaps = []
    phase2_xaml_gaps = []
    legacy_xaml = {}
    for path in sorted(DESKTOP.rglob("*.xaml")):
        rel = path.relative_to(DESKTOP).as_posix()
        gaps = xaml_missing(path, values)
        if rel in PHASE1_XAML:
            shell_gaps.extend({"file": rel, **gap} for gap in gaps)
        elif rel in PHASE2_XAML:
            phase2_xaml_gaps.extend({"file": rel, **gap} for gap in gaps)
        elif gaps:
            legacy_xaml[rel] = len(gaps)

    for rel in sorted(PHASE1_CODE):
        for gap in source_missing(DESKTOP / rel, values):
            shell_gaps.append({"file": rel, **gap})

    phase2_code_gaps = {
        rel: len(gaps)
        for rel in sorted(PHASE2_CODE_AUDIT)
        if (gaps := source_missing(DESKTOP / rel, values))
    }

    challenges = CHALLENGE.findall((CORE / "ChallengeCatalog.cs").read_text(encoding="utf-8"))
    challenges_missing = sum(
        f"Challenge.{id}.Name" not in keys or f"Challenge.{id}.Description" not in keys
        for id in challenges
    )
    archive = (DESKTOP / "CollectionLore.cs").read_text(encoding="utf-8")
    lore_ids = re.findall(r'\["([\w-]+-\d+)"\]\s*=', archive)
    lore_missing = sum(f"Album.Lore.{id}" not in keys for id in lore_ids)

    report = {
        "phase1ShellMissing": shell_gaps,
        "phase2GameXamlMissing": phase2_xaml_gaps,
        "phase2RuntimeSourceCandidates": phase2_code_gaps,
        "legacyXamlUnmappedByFile": legacy_xaml,
        "challengeEntries": len(challenges),
        "challengesMissingNameOrDescription": challenges_missing,
        "loreEntries": len(lore_ids),
        "loreMissing": lore_missing,
        "storySceneInventory": story_debt(keys),
        "note": "Unmapped is an audit candidate, not always an untranslated visible string.",
    }
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if strict and (shell_gaps or phase2_xaml_gaps):
        print(f"FAIL: {len(shell_gaps)} unlocalized shell and "
              f"{len(phase2_xaml_gaps)} unlocalized Phase-2 game XAML literals.",
              file=sys.stderr)
        return 1
    print("PASS: migrated shell and Phase-2 game XAML coverage. "
          "Runtime and remaining content debt is reported above.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict-shell", action="store_true",
                        help="Fail CI for unmapped literals in migrated shell and Phase-2 game XAML")
    args = parser.parse_args()
    return audit(args.strict_shell)


if __name__ == "__main__":
    raise SystemExit(main())
