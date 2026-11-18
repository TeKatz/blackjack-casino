#!/usr/bin/env python3
"""Import exactly the two user-selected CC0 Poly Haven diffuse textures.

Reproducible developer-time import. Images are bundled with the Godot game;
never downloaded by players. The metadata API resolves authoritative URLs.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
from urllib.error import URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen
import time

ASSETS = {
    "grass_ground": "grass_ground_diff_1k.jpg",
    "rocks_ground_05": "rocks_ground_05_diff_1k.jpg",
}
DEST = Path(__file__).resolve().parents[1] / "assets"
AGENT = "MidnightIndustries-CC0-AssetImporter/1.0 (Poly Haven official API)"


def download(url: str) -> bytes:
    if not url.startswith("https://"):
        raise ValueError("Only HTTPS downloads are permitted")
    if urlparse(url).hostname not in {"api.polyhaven.com", "dl.polyhaven.org"}:
        raise ValueError("Unexpected download origin")
    last = None
    for trial in range(3):
        try:
            request = Request(url, headers={"User-Agent": AGENT})
            with urlopen(request, timeout=60) as response:
                data = response.read(7_000_000)
            if not data:
                raise RuntimeError("Empty Poly Haven response")
            return data
        except (OSError, URLError) as exc:
            last = exc
            time.sleep(2 ** trial)
    raise RuntimeError(f"Could not download {url}: {last}")


def locate_diffuse(asset_id: str) -> str:
    info = json.loads(download(f"https://api.polyhaven.com/files/{asset_id}"))
    maps = next((v for k, v in info.items() if k.lower() in {
        "diffuse", "diff", "basecolor", "base_color"}), None)
    if not isinstance(maps, dict):
        raise ValueError(f"No diffuse texture listed for {asset_id}: {list(info)}")
    resolution = maps.get("1k")
    if not isinstance(resolution, dict):
        raise ValueError(f"No 1K texture for {asset_id}: {list(maps)}")
    jpg = resolution.get("jpg") or resolution.get("jpeg")
    if not isinstance(jpg, dict) or not isinstance(jpg.get("url"), str):
        raise ValueError(f"No 1K JPG diffuse for {asset_id}: {list(resolution)}")
    return jpg["url"]


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    for name, filename in ASSETS.items():
        url = locate_diffuse(name)
        data = download(url)
        if not data.startswith(b"\xff\xd8\xff") or len(data) < 20_000:
            raise ValueError(f"Poly Haven returned an invalid JPEG: {name}")
        dest = DEST / filename
        dest.write_bytes(data)
        sha = hashlib.sha256(data).hexdigest()
        print(f"CC0 {name}: {dest} ({len(data)} bytes; SHA256 {sha}); source {url}")


if __name__ == "__main__":
    main()
