#!/usr/bin/env python3
"""Native casino localization audit: preserve C# catalog and authored lore coverage.

The old WPF XAML-only coverage rules have been replaced with the shipped
NativeShell/React story, challenge and album localization checks.
"""
from __future__ import annotations
import argparse
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "src/BlackjackCasino.HostShared/Resources"
CORE = ROOT / "src/BlackjackCasino.Core"
SHARED = ROOT / "src/BlackjackCasino.HostShared"

def audit(strict: bool) -> int:
    keys = set()
    for resource in RESOURCES.glob("*.resx"):
        if resource.stem.endswith(".de"):
            continue
        keys.update(node.attrib["name"] for node in
                    ET.parse(resource).getroot().findall("data"))
    challenge_code = (CORE / "ChallengeCatalog.cs").read_text(encoding="utf-8")
    challenges = re.findall(r'\b[CE]\s*\(\s*"([^"]+)"\s*,\s*ChallengeCategory\.\w+'
                            r'\s*,\s*"[^"]+"\s*,\s*"[^"]+"', challenge_code)
    missing_challenges = [id for id in challenges if
        f"Challenge.{id}.Name" not in keys or f"Challenge.{id}.Description" not in keys]
    lore = (SHARED / "CollectionLore.cs").read_text(encoding="utf-8")
    lore_ids = re.findall(r'\["([\w-]+-\d+)"\]\s*=', lore)
    missing_lore = [id for id in lore_ids if f"Album.Lore.{id}" not in keys]
    report = {
        "authoritativeSource": "NativeShell/React",
        "loreEntries": len(lore_ids),
        "missingLore": missing_lore,
        "challengeEntries": len(challenges),
        "missingChallengeCopy": missing_challenges,
        "resourceCatalogs": len(list(RESOURCES.glob("*.resx"))),
    }
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if strict and (not lore_ids or not challenges or missing_lore or missing_challenges):
        return 1
    return 0

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict-shell", action="store_true")
    raise SystemExit(audit(parser.parse_args().strict_shell))
