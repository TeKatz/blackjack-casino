#!/usr/bin/env python3
"""Validate every shipped localization catalog for parity and unique keys."""

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_DIR = ROOT / "src" / "BlackjackCasino.Desktop" / "Resources"
LANGUAGES = ["de"]


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


def translated_path(base: Path, language: str) -> Path:
    return base.with_name(f"{base.stem}.{language}{base.suffix}")


def main() -> int:
    bases = sorted(
        path for path in RESOURCE_DIR.glob("*.resx")
        if not any(path.stem.endswith(f".{language}") for language in LANGUAGES)
    )
    if not bases:
        print("No neutral localization catalogs found.", file=sys.stderr)
        return 1

    failed = False
    all_base_keys: dict[str, Path] = {}
    total_keys = 0

    for base_path in bases:
        base = read(base_path)
        if not base:
            failed = True
            print(f"{base_path.name}: base catalog has no strings.", file=sys.stderr)
            continue

        total_keys += len(base)
        empty_base = sorted(key for key, value in base.items() if not value.strip())
        if empty_base:
            failed = True
            print(f"{base_path.name}: empty base values: {', '.join(empty_base)}", file=sys.stderr)

        for key in base:
            previous = all_base_keys.get(key)
            if previous is not None:
                failed = True
                print(f"Duplicate localization key across catalogs: {key} ({previous.name}, {base_path.name})", file=sys.stderr)
            else:
                all_base_keys[key] = base_path

        for language in LANGUAGES:
            path = translated_path(base_path, language)
            if not path.exists():
                failed = True
                print(f"Missing translation catalog: {path.name}", file=sys.stderr)
                continue

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

    print(f"Localization catalogs: {len(bases)} neutral files, {total_keys} unique keys")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
