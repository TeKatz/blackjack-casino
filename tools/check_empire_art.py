#!/usr/bin/env python3
"""Guard the Casino Empire clicker's visible milestones and finite art tiers."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
catalog = (ROOT / "src/BlackjackCasino.Core/Empire/EmpireCatalog.cs").read_text(encoding="utf-8")
css = (ROOT / "frontend/src/empire/empire.css").read_text(encoding="utf-8")
main = (ROOT / "frontend/src/empire/main.tsx").read_text(encoding="utf-8")
protocol = (ROOT / "src/BlackjackCasino.HostShared/EmpireModernProtocol.cs").read_text(encoding="utf-8")

assert "MaximumLevel = 8" in catalog, "Buildings must be capped at eight levels."
ids = re.findall(r'^\s*new\("([a-z-]+)",', catalog, re.MULTILINE)
assert len(ids) == len(set(ids)) == 12, "Casino Empire needs twelve distinct buildings."
asset = (ROOT / "frontend/src/empire/BuildingArtwork.tsx").read_text(encoding="utf-8")
stack = (ROOT / "frontend/src/empire/CasinoStackArt.tsx").read_text(encoding="utf-8")
scene = ROOT / "frontend/src/empire/art/casino-atmosphere.svg"
images = sorted((ROOT / "src/BlackjackCasino.Assets/Empire").glob("*.png"))
assert len(images) == 11 and scene.is_file()
assert all(item in css for item in (".ce-stack-art", ".ce-accordion", ".ce-goal", ".ce-scene-tier-4"))
assert "rooms-atlas.png" in asset and "luxury-hotel" in asset
assert all(item in stack for item in ("ce-chip-column", "ce-felt", "ce-wood"))

assert all(item in main for item in ("ce-stack-button", "ce-building", "ce-goal", "building.upgrade"))
assert all(item in protocol for item in ('"tap"', '"building.upgrade"', '"prestige"'))
# Layout regression: components must form independent, bounded regions.
# The old badge was absolute-positioned over the milestone title, and
# prestige used to live below an unbounded building column.
assert "ce-stage-panel" in main and "ce-header-inner" in main
assert 'className="ce-goal-content"' in main and 'className="ce-goal-crest"' in main
assert 'className="ce-building-scroll"' in main
assert 'className="ce-prestige-panel"' in main
assert "setBuildingsOpen" in main and "setPrestigeOpen" in main
assert "grid-template-rows:92px minmax(0,1fr)" in css
assert ".ce-prestige-panel{" in css and "overflow-y:auto" in css
assert "/lobby/art/ambient.png" in css
assert not (ROOT / "src/BlackjackCasino.Core/Empire/EmpireAchievementBoostCatalog.cs").exists()
assert not (ROOT / "frontend/src/empire/EmpireCommandCenter.tsx").exists()
print("PASS: Casino Empire clicker has 12 capped buildings, four visual tiers, goals and no old command center.")
