# Optional OpenGameArt music

These four imported audio tracks are normalized MP3s in the source asset folder:

| File | Plays in | Source file |
| --- | --- | --- |
| `midnight-manor.mp3` | Midnight Manor gameplay | `forest.ogg` |
| `chill-lounge.mp3` | Chill Lounge hub/menu only; activities retain original score | `07 HoliznaCC0 - Glad To Be Stuck Inside.mp3.ogg` |
| `lobby.mp3` | Main casino lobby, not the Blackjack table | `03 HoliznaCC0 - Pretty Little Lies.mp3` |
| `midnight-dice.mp3` | Midnight Dice gameplay | `02 HoliznaCC0 - Everything You Ever Dreamed.mp3` |

Two OGG/Vorbis sources were transcoded to MP3 for built-in Windows playback.
The two source MP3s were left unchanged.

The NativeShell project copies present `*.mp3` files to `Music/Licensed/` under
the build and published application directory. If a track is absent or fails
to open, the original synthesized soundtrack remains available. The existing
music toggle, per-area volume and area exit cleanup apply to these tracks.
The music is not downloaded at runtime.

**Attribution:** The exact OpenGameArt source pages and licenses have not
yet been supplied, and file names do not prove a license. Verify each track's
license before public distribution and record the source page and attribution
requirements. Three filenames identify HoliznaCC0; `forest.ogg` has no artist
metadata in the uploaded file.
