#!/usr/bin/env python3
"""Small dependency-free checks for repository docs and Development release guard.

Run from any cwd: python tools/check_repo_contract.py
"""
from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = (
    "README.md",
    "AGENTS.md",
    "CONTRIBUTING.md",
    "docs/architecture.md",
    "docs/development-update-delivery.md",
    "docs/modernization-roadmap.md",
    ".github/pull_request_template.md",
    ".github/workflows/development-release.yml",
)
DOCS = (
    "README.md",
    "AGENTS.md",
    "CONTRIBUTING.md",
    "docs/modernization-roadmap.md",
)
LINK = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
BRANCH_FILTER = re.compile(r"(?m)^\s*branches:\s*\[\"([^\"]+)\"\]")
JOB_GUARD = re.compile(r"(?m)^\s*if:\s*github\.ref\s*==\s*'refs/heads/([^']+)'")
# Documentation should describe current project decisions, never the conversation
# that produced them. Restrict this lint to Markdown, not source-code identifiers
# such as user-visible, which describe functionality rather than decision history.
CONVERSATION_NARRATION = re.compile(
    r"(?i)\b(?:the\s+)?(?:user|owner|game[ -]?director)\s+"
    r"(?:(?:has|had)\s+)?(?:explicitly\s+)?"
    r"(?:requested|instructed|asked|authorized|approved|clarified|directed|confirmed)\b"
    r"|\b(?:at|per)\s+(?:the\s+)?user(?:'s)?\s+request\b"
    r"|\bas\s+requested\s+by\s+(?:the\s+)?(?:user|owner)\b"
)

def main() -> int:
    problems: list[str] = []
    for name in REQUIRED:
        if not (ROOT / name).is_file():
            problems.append(f"Missing required file: {name}")

    for name in DOCS:
        file = ROOT / name
        if not file.exists():
            continue
        source = file.read_text(encoding="utf-8")
        for match in LINK.finditer(source):
            destination = match.group(1).strip()
            if not destination or destination.startswith(("#", "mailto:", "http:", "https:", "data:")):
                continue
            # Markdown links with titles are not part of our docs convention.
            target = unquote(urlsplit(destination).path)
            if not target:
                continue
            resolved = (file.parent / target).resolve()
            if not resolved.is_relative_to(ROOT) or not resolved.exists():
                line = source.count("\n", 0, match.start()) + 1
                problems.append(f"{name}:{line}: broken relative link: {destination}")

    # Scan project Markdown, excluding generated dependency/build directories.
    ignored = {".git", "node_modules", "bin", "obj", "dist", "artifacts", ".venv"}
    for file in ROOT.rglob("*.md"):
        if any(part in ignored for part in file.relative_to(ROOT).parts):
            continue
        for line_number, line in enumerate(file.read_text(encoding="utf-8").splitlines(), 1):
            if CONVERSATION_NARRATION.search(line):
                problems.append(f"{file.relative_to(ROOT)}:{line_number}: use project-state language instead of conversation narration")

    release = ROOT / ".github/workflows/development-release.yml"
    if release.exists():
        workflow = release.read_text(encoding="utf-8")
        filter_match = BRANCH_FILTER.search(workflow)
        guard_match = JOB_GUARD.search(workflow)
        if not filter_match or not guard_match:
            problems.append("Development release must have a single branch push filter and a matching job ref guard")
        elif filter_match.group(1) != guard_match.group(1):
            problems.append(
                "Development branch mismatch: push filter "
                f"{filter_match.group(1)!r}, job guard {guard_match.group(1)!r}"
            )


    # The default-branch music import action must commit and explicitly dispatch
    # on the same development branch that owns the mutable release tag.
    importer = ROOT / ".github/workflows/casino-music-import.yml"
    if release.exists() and importer.exists() and filter_match and guard_match:
        source_branch = filter_match.group(1)
        importer_source = importer.read_text(encoding="utf-8")
        required_importer_targets = (
            f"ref: {source_branch}",
            f"git push origin HEAD:refs/heads/{source_branch}",
            f"gh workflow run development-release.yml --ref {source_branch}",
        )
        for target in required_importer_targets:
            if target not in importer_source:
                problems.append(f"Music importer must target Development publisher: {target}")

    # Every active development code path needs CI on ordinary branch pushes.
    if filter_match:
        active_branch = filter_match.group(1)
        for path in (
            ".github/workflows/ci.yml",
            ".github/workflows/repository-hygiene.yml",
            ".github/workflows/modern-frontend.yml",
            ".github/workflows/presentation-regression.yml",
            ".github/workflows/midnight-descent.yml",
        ):
            candidate = ROOT / path
            if candidate.is_file():
                config = candidate.read_text(encoding="utf-8")
                if not re.search(r"(?m)^\\s*branches:\\s*\\[[^\\n]*\\b" + re.escape(active_branch) + r"\\b", config):
                    problems.append(f"{path}: missing Development branch {active_branch!r} push trigger")

    # Custom lobby illustrations are static image resources, not DOM-generated
    # placeholders. Validate every pair during ordinary repository-contract CI.
    art_dir = ROOT / "frontend/public/lobby/custom"
    if art_dir.is_dir():
        import xml.etree.ElementTree as ET
        subjects = (
            "blackjack", "kniffel", "chill-lounge", "casino-empire",
            "midnight-reels", "neon-vault", "midnight-manor", "midnight-merge",
        )
        if not (ROOT / "frontend/public/lobby/art/nine-lives.jpg").is_file():
            problems.append("Nine Lives original cat art is missing")
        for subject in subjects:
            for layer in ("back", "front"):
                asset = art_dir / f"{subject}-{layer}.svg"
                if not asset.is_file():
                    problems.append(f"Missing custom lobby key art: {asset.name}")
                    continue
                try:
                    artwork = ET.parse(asset).getroot()
                except ET.ParseError as exc:
                    problems.append(f"Invalid SVG {asset.name}: {exc}")
                    continue
                if artwork.get("viewBox") != "0 0 960 440":
                    problems.append(f"{asset.name}: unexpected design-space bounds")
                if any(el.tag.rsplit("}", 1)[-1] in ("image", "foreignObject")
                       for el in artwork.iter()):
                    problems.append(f"{asset.name}: embedded external images/HTML are forbidden")

    if problems:
        print("Repository contract FAILED:")
        for problem in problems:
            print(f"  - {problem}")
        return 1
    print("Repository contract OK: required docs, relative links, documentation style and Development branch guards")
    return 0

if __name__ == "__main__":
    sys.exit(main())
