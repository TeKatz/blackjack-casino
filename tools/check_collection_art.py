#!/usr/bin/env python3
"""Verify exhaustive, unique and checksum-pinned card illustration assignments without extra dependencies."""
import hashlib
import json
import re
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'src/BlackjackCasino.Desktop/Assets/CollectionArt'


def main():
    manifest = json.loads((ART / 'manifest.json').read_text())
    assert manifest['Version'] == 1, 'Unsupported artwork manifest'
    catalog = (ROOT / 'src/BlackjackCasino.Core/Collections/CollectionCatalog.cs').read_text()
    expected = {}
    for match in re.finditer(r'\("([^"\n]+)", "([^"\n]+)", "[^"\n]+", "[^"\n]+", \[([^\]\n]+)\]\)', catalog):
        theme, _, names = match.groups()
        for number, name in enumerate(re.findall(r'"([^"]+)"', names), 1):
            expected[f'{theme}-{number:02}'] = name
    assert len(expected) == 136, 'Catalog parsing/size changed; review artwork coverage'
    atlases = {a['Id']: a for a in manifest['Atlases']}
    assert len(atlases) == 16, 'Every theme needs its own illustration atlas'
    assert {a['File'] for a in atlases.values()} == {p.name for p in ART.glob('*.png')}, 'Missing or unused atlas'
    for atlas in atlases.values():
        raw = (ART / atlas['File']).read_bytes()
        assert raw[:8] == b'\x89PNG\r\n\x1a\n', f"Not a PNG: {atlas['File']}"
        assert struct.unpack('>II', raw[16:24]) == (atlas['Width'], atlas['Height']), f"Wrong dimensions: {atlas['File']}"
        assert hashlib.sha256(raw).hexdigest() == atlas['Sha256'], f"Unreviewed image change: {atlas['File']}"
    cards = manifest['Cards']
    assert len(cards) == len(expected) == len({c['Id'] for c in cards}), 'Duplicate or missing card ID'
    assert {c['Id'] for c in cards} == set(expected), 'Artwork IDs do not match the collection catalog'
    assert len({(c['Atlas'], c['X'], c['Y'], c['Width'], c['Height']) for c in cards}) == 136, 'Illustration region reused'
    assert len({c['ContentHash'] for c in cards}) == 136, 'Duplicate illustration pixels'
    assert len({c['Subject'] for c in cards}) == 136, 'Duplicate subject assignment'
    for i, card in enumerate(cards):
        assert card['Title'] == expected[card['Id']], f"Title mismatch: {card['Id']}"
        atlas = atlases[card['Atlas']]
        x, y, w, h = (card[k] for k in ('X', 'Y', 'Width', 'Height'))
        assert x >= 0 and y >= 0 and w >= 200 and h >= 300, f"Invalid/undersized region: {card['Id']}"
        assert x + w <= atlas['Width'] and y + h <= atlas['Height'], f"Region outside image: {card['Id']}"
        if card['UltraRare']:
            assert w >= 400 and h >= 600, f"Ultra rare art needs higher resolution: {card['Id']}"
        for other in cards[:i]:
            if card['Atlas'] != other['Atlas']:
                continue
            overlap = (max(x, other['X']) < min(x+w, other['X']+other['Width'])
                       and max(y, other['Y']) < min(y+h, other['Y']+other['Height']))
            assert not overlap, f"Overlapping artwork regions: {card['Id']} / {other['Id']}"
    print('PASS: 136 unique card illustrations, 16 checked atlases, full catalog coverage, valid non-overlapping regions and high-resolution ultra rare art.')


if __name__ == '__main__':
    main()
