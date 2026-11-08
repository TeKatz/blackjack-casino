#!/usr/bin/env python3
"""Import one CC0 OpenGameArt audio file into the casino's existing audio slots."""
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import unquote, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[1]
MUSIC = ROOT / "src/BlackjackCasino.Desktop/Assets/Music/Licensed"
CREDITS = MUSIC / "credits.json"
AREAS = {
    "midnight-manor": "midnight-manor.mp3",
    "chill-lounge": "chill-lounge.mp3",
    "lobby": "lobby.mp3",
    "midnight-dice": "midnight-dice.mp3",
}
HOSTS = {"opengameart.org", "www.opengameart.org",
         "static.opengameart.org", "files.opengameart.org"}
EXTENSIONS = {".mp3", ".ogg", ".oga", ".opus", ".wav", ".flac"}
MAX_BYTES = 60 * 1024 * 1024


class ImportFailure(Exception):
    pass


def validate_url(value, direct):
    try:
        url = urlsplit(value)
        valid = (url.scheme == "https" and url.hostname in HOSTS
                 and url.port in (None, 443) and not url.username
                 and not url.password)
    except ValueError:
        valid = False
    if not valid:
        raise ImportFailure("Only HTTPS OpenGameArt links are accepted.")
    path = unquote(url.path)
    if direct:
        if not path.startswith("/sites/default/files/"):
            raise ImportFailure("Use the direct audio file URL, not the OpenGameArt content page.")
        if Path(path).suffix.lower() not in EXTENSIONS:
            raise ImportFailure("Expected a direct MP3, OGG, OGA, OPUS, WAV or FLAC audio link.")
    elif not path.startswith("/content/"):
        raise ImportFailure("Source page must be an OpenGameArt /content/ URL.")
    return value


class SafeRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        validate_url(newurl, True)
        return super().redirect_request(req, fp, code, msg, headers, newurl)


def download(url, path):
    opener = build_opener(SafeRedirect())
    with opener.open(Request(url, headers={"User-Agent": "CasinoAudioImporter/1.0"}), timeout=40) as response:
        validate_url(response.geturl(), True)
        total = 0
        with path.open("wb") as out:
            while chunk := response.read(131072):
                total += len(chunk)
                if total > MAX_BYTES:
                    raise ImportFailure("Source exceeds the 60 MiB download limit.")
                out.write(chunk)
    if total < 1024:
        raise ImportFailure("Downloaded audio is empty or unexpectedly small.")


def probe(path):
    raw = subprocess.run(["ffprobe", "-v", "error", "-show_streams",
                          "-show_format", "-of", "json", str(path)],
                         check=True, capture_output=True, text=True)
    info = json.loads(raw.stdout)
    sound = next((s for s in info.get("streams", [])
                  if s.get("codec_type") == "audio"), None)
    if not sound:
        raise ImportFailure("File contains no audio stream.")
    duration = float(info.get("format", {}).get("duration") or 0)
    if not 1 <= duration <= 1200:
        raise ImportFailure("Track must be 1 second to 20 minutes long.")
    return sound.get("codec_name"), duration


def convert(src, dst, codec):
    if codec == "mp3" and src.suffix == ".mp3":
        shutil.copyfile(src, dst)
    else:
        subprocess.run(["ffmpeg", "-nostdin", "-hide_banner", "-loglevel", "error",
                        "-y", "-i", str(src), "-map", "0:a:0", "-vn", "-sn", "-dn",
                        "-ar", "44100", "-ac", "2", "-codec:a", "libmp3lame",
                        "-b:a", "192k", str(dst)], check=True, timeout=120)
    output_codec, _ = probe(dst)
    if output_codec != "mp3" or dst.stat().st_size > 24 * 1024 * 1024:
        raise ImportFailure("Converted output is not a valid MP3 smaller than 24 MiB.")


def main():
    area = os.environ.get("IMPORT_AREA", "")
    if area not in AREAS:
        raise ImportFailure("Unknown destination.")
    if os.environ.get("IMPORT_CC0_CONFIRMED", "").lower() != "true":
        raise ImportFailure("Confirm the CC0 license on the OpenGameArt source page.")
    url = validate_url(os.environ.get("IMPORT_DIRECT_URL", "").strip(), True)
    page = validate_url(os.environ.get("IMPORT_SOURCE_PAGE", "").strip(), False)
    artist = os.environ.get("IMPORT_CREATOR", "").strip()
    if not artist or len(artist) > 200 or "\n" in artist or "\r" in artist:
        raise ImportFailure("Provide the artist name (single line, at most 200 characters).")
    target = MUSIC / AREAS[area]
    if target.exists() and os.environ.get("IMPORT_REPLACE", "").lower() != "true":
        raise ImportFailure("Existing music found. Check Replace existing track to continue.")
    if CREDITS.exists():
        credits = json.loads(CREDITS.read_text(encoding="utf-8"))
        if credits.get("schemaVersion") != 1 or not isinstance(credits.get("tracks"), dict):
            raise ImportFailure("Unexpected credits.json format.")
    else:
        credits = {"schemaVersion": 1, "tracks": {}}
    MUSIC.mkdir(parents=True, exist_ok=True)
    suffix = Path(unquote(urlsplit(url).path)).suffix.lower()
    with tempfile.TemporaryDirectory(prefix="casino-audio-") as td:
        source = Path(td) / ("source" + suffix)
        converted = Path(td) / "output.mp3"
        print("Downloading", url, flush=True)
        download(url, source)
        codec, duration = probe(source)
        convert(source, converted, codec)
        digest = hashlib.sha256(converted.read_bytes()).hexdigest()
        shutil.copyfile(converted, target)
    credits["tracks"][area] = {
        "file": AREAS[area],
        "artist": artist,
        "license": "CC0-1.0",
        "sourcePage": page,
        "directUrl": url,
        "sha256": digest,
        "durationSeconds": round(duration, 2),
        "importedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "licenseAutomaticallyVerified": False,
    }
    CREDITS.write_text(json.dumps(credits, indent=2, ensure_ascii=False) + "\n",
                       encoding="utf-8")
    print("Imported", target.name, target.stat().st_size, "bytes", digest)


def self_test():
    assert AREAS["lobby"] == "lobby.mp3"
    assert validate_url("https://opengameart.org/sites/default/files/example.ogg", True)
    assert validate_url("https://opengameart.org/content/example", False)
    for bad, is_direct in [
        ("http://opengameart.org/sites/default/files/example.ogg", True),
        ("https://evil.example/sites/default/files/example.ogg", True),
        ("https://opengameart.org/content/example", True),
        ("https://opengameart.org/sites/default/files/example.zip", True),
        ("https://opengameart.org/sites/default/files/example.mp3", False),
    ]:
        try:
            validate_url(bad, is_direct)
        except ImportFailure:
            continue
        raise AssertionError("Unsafe URL accepted: " + bad)
    print("Importer self-test passed.")


if __name__ == "__main__":
    try:
        if sys.argv[1:] == ["--self-test"]:
            self_test()
        else:
            main()
    except (ImportFailure, OSError, ValueError, subprocess.CalledProcessError,
            subprocess.TimeoutExpired) as e:
        print("Music import failed:", e, file=sys.stderr)
        sys.exit(1)
