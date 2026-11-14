#!/usr/bin/env python3
"""Check room coverage, PNG integrity, unique pixels and prompt provenance."""
import hashlib
import json
import re
import struct
import zlib
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'src/BlackjackCasino.Assets/Empire'


def main():
    catalog = (ROOT / 'src/BlackjackCasino.Core/Empire/EmpireCatalog.cs').read_text(encoding='utf-8')
    rooms = set(re.findall(r'new\("([^"]+)",[^\n]+, [23], EmpireTag\.', catalog))
    source = (ROOT / 'frontend/src/empire/EmpireArtwork.tsx').read_text(encoding='utf-8')
    art_line = next((line for line in source.splitlines() if line.startswith('const files:')), '')
    assert art_line, 'Missing authoritative React room artwork mapping'
    mapping = {quoted or plain: filename for quoted, plain, filename in
               re.findall(r"(?:'([^']+)'|([A-Za-z][A-Za-z0-9-]*)):'([^']+)'", art_line)}
    assert rooms == set(mapping) and len(rooms) == 10, 'Upper/Resort room coverage changed'
    prompts = json.loads((ART / 'room-prompts.json').read_text(encoding='utf-8'))
    assert {entry['file'] for entry in prompts['images']} == {name + '.png' for name in mapping.values()}, 'Missing prompt/asset provenance'
    pixels = set()
    for room, name in mapping.items():
        raw = (ART / (name + '.png')).read_bytes()
        assert raw[:8] == b'\x89PNG\r\n\x1a\n', f'{room}: PNG signature missing'
        width, height, depth, color = struct.unpack('>IIBB', raw[16:26])
        assert width == height and width >= 768 and depth == 8 and color in (2, 6), f'{room}: invalid room format/resolution'
        offset, image, ended = 8, bytearray(), False
        while offset < len(raw):
            length = struct.unpack('>I', raw[offset:offset + 4])[0]
            chunk = raw[offset + 4:offset + 8]
            data = raw[offset + 8:offset + 8 + length]
            checksum = struct.unpack('>I', raw[offset + 8 + length:offset + 12 + length])[0]
            assert zlib.crc32(chunk + data) == checksum, f'{room}: corrupt {chunk!r} chunk'
            if chunk == b'IDAT':
                image.extend(data)
            if chunk == b'IEND':
                ended = True
                break
            offset += length + 12
        decoded = zlib.decompress(image)
        assert ended and len(decoded) == height * (1 + width * (3 if color == 2 else 4)), f'{room}: truncated image'
        digest = hashlib.sha256(decoded).digest()
        assert digest not in pixels, f'{room}: duplicated room artwork'
        pixels.add(digest)
    print('PASS: native React room mapping verified; 10 individually assigned rooms, valid square PNGs, unique image data and recorded generation prompts.')


if __name__ == '__main__':
    main()
