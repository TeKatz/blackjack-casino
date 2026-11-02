#!/usr/bin/env python3
"""Check room coverage, PNG integrity, unique pixels and prompt provenance."""
import hashlib
import json
import re
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'src/BlackjackCasino.Desktop/Assets/Empire'


def main():
    catalog = (ROOT / 'src/BlackjackCasino.Core/Empire/EmpireCatalog.cs').read_text(encoding='utf-8')
    rooms = set(re.findall(r'new\("([^"]+)",[^\n]+, [23], EmpireTag\.', catalog))
    source = (ROOT / 'src/BlackjackCasino.Desktop/EmpireRoomArtwork.cs').read_text(encoding='utf-8')
    mapping = dict(re.findall(r'\["([^"]+)"\] = "([^"]+)"', source))
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
    print('PASS: 10 individually assigned rooms, valid square PNGs, unique image data and recorded generation prompts.')


if __name__ == '__main__':
    main()
