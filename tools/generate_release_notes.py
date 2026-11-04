#!/usr/bin/env python3
"""Build offline, version-bound change notes for Stable and mutable Development releases."""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
from pathlib import Path
import re
import subprocess


def git(*args: str) -> str:
    result = subprocess.run(["git", *args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return result.stdout.strip() if result.returncode == 0 else ""


def valid_ref(reference: str) -> bool:
    return bool(reference) and bool(git("rev-parse", "--verify", "--quiet", reference + "^{commit}"))


def previous_stable_tag() -> str:
    current = os.environ.get("GITHUB_REF_NAME", "")
    for tag in git("tag", "--list", "v*.*.*", "--sort=-version:refname").splitlines():
        if tag == current or not valid_ref(tag):
            continue
        if subprocess.run(["git", "merge-base", "--is-ancestor", tag, "HEAD"], capture_output=True).returncode == 0:
            return tag
    return ""


def messages(previous_ref: str, channel: str) -> list[tuple[str, str]]:
    base = previous_ref if valid_ref(previous_ref) else previous_stable_tag() if channel == "stable" else ""
    if base and subprocess.run(["git", "merge-base", "--is-ancestor", base, "HEAD"],
                               capture_output=True).returncode != 0:
        # Mutable Development releases may jump between feature branches.
        # Compare from a real common ancestor instead of including unrelated histories.
        base = git("merge-base", base, "HEAD")
    # No previous release? Keep the initial changelog limited and don't invent missing history.
    span = f"{base}..HEAD" if base else "HEAD"
    data = git("log", "--no-merges", "--format=%s%x1f%b%x1e", "--max-count=120", span)
    result: list[tuple[str, str]] = []
    for item in data.split("\x1e"):
        if "\x1f" not in item:
            continue
        title, body = item.split("\x1f", 1)
        title = title.strip().splitlines()[0] if title.strip() else ""
        description = next((line.strip() for line in body.splitlines()
                            if line.strip() and not line.strip().startswith(("-", "#", "Co-authored-by:"))), "")
        if title and not title.lower().startswith(("merge ", "release ", "bump version")):
            result.append((title[:135], description[:360]))
    return result


def classify(title: str) -> tuple[str, str]:
    match = re.match(r"^(feat|fix|perf|refactor|style|docs|test|chore|build|ci)(?:\([^)]*\))?!?:\s*(.*)", title, re.I)
    if not match:
        return "Improvements", title
    kind, text = match.group(1).lower(), match.group(2).strip()
    category = {"feat": "Features", "fix": "Fixes", "perf": "Performance",
                "refactor": "Improvements", "style": "Improvements"}.get(kind, "Maintenance")
    return category, text or title



def german_editorial(category: str, title: str) -> tuple[str, str]:
    """Curated German summary. Unknown developer commits never leak untranslated."""
    lower = title.casefold()
    topics = [
        (("localiz", "german", "translation", "language"), "Deutsche Übersetzung", "Weitere deutsche Texte und Sprachwechsel wurden überarbeitet."),
        (("word search", "word-search"), "Wortsuche", "Inhalte oder Bedienung der Wortsuche wurden verbessert."),
        (("release note", "changelog"), "Versionshinweise", "Die Anzeige der Versionshinweise wurde überarbeitet."),
        (("adventure", "tales", "case board", "dead man's hand", "backroom"), "Tales from the Backroom", "Abenteuerinhalte oder ihre Darstellung wurden überarbeitet."),
        (("empire",), "Casino Empire", "Die Casinoverwaltung wurde verbessert."),
        (("midnight merge",), "Midnight Merge", "Inhalte oder Darstellung von Midnight Merge wurden verbessert."),
        (("midnight dice", "kniffel"), "Midnight Dice", "Inhalte oder Darstellung von Midnight Dice wurden verbessert."),
        (("midnight reels", "reels", "slot"), "Midnight Reels", "Spielinhalte oder Darstellung der Walzen wurden verbessert."),
        (("midnight manor", "manor"), "Midnight Manor", "Spielinhalte oder Darstellung von Midnight Manor wurden verbessert."),
        (("neon vault", "neon"), "Neon Vault", "Spielinhalte oder Darstellung von Neon Vault wurden verbessert."),
        (("nine lives",), "Nine Lives", "Spielinhalte oder Darstellung von Nine Lives wurden verbessert."),
        (("casino lobby", "lobby"), "Casino-Lobby", "Die Übersicht und Bedienung der Casino-Lobby wurden verbessert."),
        (("challenge",), "Herausforderungen", "Herausforderungen oder Fortschrittsanzeigen wurden überarbeitet."),
        (("album", "collection", "card set", "portrait", "cosmetic"), "Sammlung und Gestaltung", "Sammelinhalte oder kosmetische Darstellungen wurden überarbeitet."),
        (("startup",), "Programmstart", "Das Startverhalten wurde überarbeitet."),
        (("performance", "render", "frame"), "Leistung", "Abläufe wurden auf bessere Leistung hin überarbeitet."),
        (("sound", "music", "audio"), "Musik und Klänge", "Audioinhalte oder ihre Wiedergabe wurden überarbeitet."),
    ]
    for terms, heading, body in topics:
        if any(token in lower for token in terms):
            return heading, body
    return {
        "Features": ("Neue Funktionen", "Das Spiel wurde um weitere Inhalte oder Funktionen erweitert."),
        "Fixes": ("Fehlerbehebung", "Ein Fehler im Spiel wurde behoben."),
        "Performance": ("Leistungsverbesserung", "Die Leistung des Spiels wurde verbessert."),
        "Improvements": ("Allgemeine Verbesserung", "Bestehende Funktionen oder Ansichten wurden überarbeitet."),
    }.get(category, ("Wartung", "Technische Wartungsarbeiten wurden durchgeführt."))

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--version", required=True)
    parser.add_argument("--channel", choices=["stable", "development"], required=True)
    parser.add_argument("--previous-ref", default="")
    parser.add_argument("--output-json", type=Path, required=True)
    parser.add_argument("--output-markdown", type=Path, required=True)
    args = parser.parse_args()
    changes = []
    seen: set[str] = set()
    for subject, description in messages(args.previous_ref, args.channel):
        category, title = classify(subject)
        if category == "Maintenance" or title.casefold() in seen:
            continue
        title_de, description_de = german_editorial(category, title)
        changes.append({"title": title, "description": description,
                        "titleDe": title_de, "descriptionDe": description_de, "category": category})
        seen.add(title.casefold())
        if len(changes) >= 24:
            break
    if not changes:
        changes = [{"title": "No user-facing changes noted",
                    "description": "This build contains no individually documented feature or fix entries.",
                    "titleDe": "Keine dokumentierten Neuerungen",
                    "descriptionDe": "Für diese Version liegen keine einzelnen Änderungen vor.",
                    "category": "Maintenance"}]

    document = {"schemaVersion": 1, "version": args.version, "channel": args.channel,
                "publishedAt": dt.datetime.now(dt.timezone.utc).isoformat(),
                "sourceCommit": git("rev-parse", "HEAD"), "changes": changes}
    args.output_json.parent.mkdir(parents=True, exist_ok=True)
    args.output_markdown.parent.mkdir(parents=True, exist_ok=True)
    args.output_json.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    lines = [f"# Blackjack Casino {args.version}", "",
             f"Channel: **{args.channel.capitalize()}**", "",
             "Changes since the previous available release:" if args.previous_ref else "Changes included in this release:", ""]
    for category in ("Features", "Fixes", "Performance", "Improvements", "Maintenance"):
        group = [entry for entry in changes if entry["category"] == category]
        if not group:
            continue
        lines.extend([f"## {category}", ""])
        for entry in group:
            lines.append(f"- **{entry['title']}**" + (f" — {entry['description']}" if entry["description"] else ""))
        lines.append("")
    args.output_markdown.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Generated {len(changes)} version-bound notes for {args.version} ({args.channel}).")


if __name__ == "__main__":
    main()
