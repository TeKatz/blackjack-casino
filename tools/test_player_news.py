#!/usr/bin/env python3
"""Unit tests for player-facing, bilingual release highlights."""
from __future__ import annotations

import re
import unittest
from generate_release_notes import player_news


class PlayerNewsTests(unittest.TestCase):
    def test_known_user_features_get_editorial_localized_copy(self) -> None:
        entries = player_news([
            "feat(news): restore startup release notes",
            "feat(scratchcards): Platinum Vault and Diamond Royale added",
            "fix(ci): avoid xunit CLI issue",
        ])
        self.assertGreaterEqual(len(entries), 2)
        self.assertEqual("Neuigkeiten im Midnight Club", entries[0]["titleDe"])
        self.assertIn("Rubbellose", entries[1]["titleDe"])
        for entry in entries:
            self.assertTrue(re.fullmatch(r"#[0-9a-fA-F]{6}", entry["accent"]))
            self.assertNotIn("feat(", entry["title"])
            self.assertNotIn("ci)", entry["descriptionDe"])
            self.assertTrue(entry["description"])
            self.assertTrue(entry["descriptionDe"])
            self.assertIn(entry["category"], ("feature", "improvement", "fix"))

    def test_raw_technical_titles_never_appear_in_lobby(self) -> None:
        private = "refactor(core): replace internal server-state persistence src/foo.tsx"
        entries = player_news([private])
        self.assertEqual(1, len(entries))
        self.assertNotIn("refactor", entries[0]["title"])
        self.assertNotIn("src/foo.tsx", entries[0]["description"])
        self.assertNotIn("server-state", entries[0]["descriptionDe"])

    def test_bounded_and_offline(self) -> None:
        subjects = [
            "feat(news): updates", "feat(scratchcards): new tickets",
            "feat(sunset ruby): bonus", "feat(casino empire): new upgrade",
            "feat(midnight manor): bonus", "feat(midnight descent): level",
            "fix(lobby): filter", "fix(audio): music",
        ]
        entries = player_news(subjects)
        self.assertEqual(6, len(entries))
        self.assertTrue(all(len(item["descriptionDe"]) <= 280 for item in entries))


if __name__ == "__main__":
    unittest.main()
