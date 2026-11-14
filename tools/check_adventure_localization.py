#!/usr/bin/env python3
"""Check authored English/German Tales text without requiring the Windows desktop assembly."""
from __future__ import annotations
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent / "src/BlackjackCasino.HostShared/Resources"


def load(name: str) -> dict[str, str]:
    root = ET.parse(ROOT / name).getroot()
    return {
        entry.attrib["name"]: entry.findtext("value", default="").strip()
        for entry in root.findall("data")
    }


def main() -> None:
    original = load("AdventureStrings.resx")
    german = load("AdventureStrings.de.resx")
    if set(original) != set(german):
        missing_de = sorted(set(original) - set(german))
        missing_en = sorted(set(german) - set(original))
        raise SystemExit(f"Adventure key mismatch: missing DE={missing_de}, missing EN={missing_en}")
    if any(not text for text in german.values()):
        raise SystemExit("Blank German adventure translation.")

    # These are real story IDs, never translated IDs or localized save keys.
    required = [
        ("black-stag", "crooked-crown"),
        ("drowned-bell", "greyhaven-road"),
        ("dead-mans-hand", "rain-office"),
        ("strangers-wager", "rainy-tavern"),
    ]
    for campaign, opening in required:
        for field in ("Title", "Body"):
            key = f"Adventure.{campaign}.Scene.{opening}.{field}"
            if key not in original:
                raise SystemExit(f"Missing translated opening: {key}")
            if german[key] == original[key]:
                raise SystemExit(f"Opening translation still English: {key}")

    for key in ("Adventure.Ui.CharacterSheet", "Adventure.Ui.AdventureLibrary",
                "Adventure.Ui.InvestigationBoard", "Adventure.Ui.CaseNotes"):
        if key not in german or german[key] == original[key]:
            raise SystemExit(f"Untranslated essential adventure UI: {key}")

    narrative = [k for k in original if ".Scene." in k or ".Choice." in k]
    print(f"PASS: {len(original)} matching adventure keys, {len(narrative)} starter story keys; "
          "full act-by-act translation still outstanding.")


if __name__ == "__main__":
    main()
