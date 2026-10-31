#!/usr/bin/env python3
"""Validate that every shipped localization has the same non-empty resource keys."""

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_DIR = ROOT / "src" / "BlackjackCasino.Desktop" / "Resources"
BASE = RESOURCE_DIR / "Strings.resx"
TRANSLATIONS = [RESOURCE_DIR / "Strings.de.resx"]


def read(path: Path) -> dict[str, str]:
    root = ET.parse(path).getroot()
    values: dict[str, str] = {}
    duplicates: list[str] = []
    for node in root.findall("data"):
        key = node.attrib.get("name", "").strip()
        value_node = node.find("value")
        value = "" if value_node is None or value_node.text is None else value_node.text.strip()
        if not key:
            raise SystemExit(f"{path}: resource without a name")
        if key in values:
            duplicates.append(key)
        values[key] = value
    if duplicates:
        raise SystemExit(f"{path}: duplicate keys: {', '.join(sorted(set(duplicates)))}")
    return values


def main() -> int:
    base = read(BASE)
    if not base:
        print("Base localization has no strings.", file=sys.stderr)
        return 1

    failed = False
    for path in TRANSLATIONS:
        translated = read(path)
        missing = sorted(set(base) - set(translated))
        extra = sorted(set(translated) - set(base))
        empty = sorted(key for key in base if not translated.get(key, "").strip())
        if missing or extra or empty:
            failed = True
            print(f"Localization mismatch: {path.name}", file=sys.stderr)
            if missing:
                print("  missing: " + ", ".join(missing), file=sys.stderr)
            if extra:
                print("  extra: " + ", ".join(extra), file=sys.stderr)
            if empty:
                print("  empty: " + ", ".join(empty), file=sys.stderr)
        else:
            print(f"{path.name}: {len(translated)} / {len(base)} keys present and non-empty")

    empty_base = sorted(key for key, value in base.items() if not value.strip())
    if empty_base:
        failed = True
        print("Base localization contains empty values: " + ", ".join(empty_base), file=sys.stderr)

    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
